using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C13
{
    public sealed class C13WatchMemoryController : MonoBehaviour
    {
        [Header("시곗바늘")]
        [SerializeField] private RectTransform minuteHand;
        [SerializeField] private RectTransform hourHand;
        [SerializeField] private RectTransform watchCenter;
        [SerializeField] private Sprite rotateHintSprite;
        [SerializeField] private Vector2 rotateHintPosition = new(292f, -138f);
        [SerializeField] private Vector2 rotateHintSize = new(64f, 64f);
        [SerializeField] private float rotateHintAngle;
        [SerializeField, Range(0.5f, 1f)] private float rotateHintMaxFill = 0.93f;
        [SerializeField, Min(1f)] private float interactionRadius = 115f;
        [SerializeField, Min(1f)] private float finalRotation = 1350f;
        [SerializeField, Min(0.1f)] private float rotateHintDrawDuration = 1.15f;
        [SerializeField, Min(0f)] private float rotateHintHoldDuration = 0.25f;
        [SerializeField, Min(0.1f)] private float rotateHintFadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float rotateHintPauseDuration = 1f;

        [Header("기억 이미지")]
        [SerializeField] private CanvasGroup walkImage;
        [SerializeField] private CanvasGroup glance1Image;
        [SerializeField] private CanvasGroup glance2Image;
        [SerializeField] private CanvasGroup stopImage;
        [SerializeField] private CanvasGroup stopMark1;
        [SerializeField] private CanvasGroup stopMark2;
        [SerializeField] private CanvasGroup pubImage;
        [SerializeField, Min(1f)] private float revealAngle = 55f;

        [Header("다음 장면")]
        [SerializeField] private CanvasGroup transitionOverlay;
        [SerializeField] private string nextSceneName = "Burning_C14_Playable";
        [SerializeField, Min(0.1f)] private float transitionDuration = 1.1f;

        private bool dragging;
        private bool hasInteracted;
        private float previousPointerAngle;
        private float accumulatedRotation;
        private Camera canvasCamera;
        private bool transitioning;
        private CanvasGroup rotateHint;
        private RectTransform rotateHintRect;
        private Image rotateHintImage;
        private float rotateHintCycleStartedAt;
        private bool continuePromptShown;

        public string CurrentChapter => "C13";
        public string CurrentState { get; private set; } = "시곗바늘을 돌려 기억 탐색";
        public float Progress => Mathf.Clamp01(accumulatedRotation / finalRotation);

        private void Start()
        {
            CreateRotateHint();
            Canvas canvas = GetComponentInParent<Canvas>();
            canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            ApplyVisuals();
            SetRotateHintVisible(true);
        }

        private void CreateRotateHint()
        {
            if (rotateHintSprite == null || rotateHint != null)
            {
                return;
            }

            GameObject hintObject = new(
                "RotateHint",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            rotateHintRect = hintObject.GetComponent<RectTransform>();
            rotateHintRect.SetParent(transform, false);
            rotateHintRect.anchorMin = new Vector2(0f, 1f);
            rotateHintRect.anchorMax = new Vector2(0f, 1f);
            rotateHintRect.pivot = new Vector2(0.5f, 0.5f);
            rotateHintRect.anchoredPosition = rotateHintPosition;
            rotateHintRect.sizeDelta = rotateHintSize;

            Image image = hintObject.GetComponent<Image>();
            image.sprite = rotateHintSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
            rotateHintImage = image;

            rotateHint = hintObject.GetComponent<CanvasGroup>();
            rotateHint.interactable = false;
            rotateHint.blocksRaycasts = false;

            if (transitionOverlay != null)
            {
                rotateHintRect.SetSiblingIndex(transitionOverlay.transform.GetSiblingIndex());
            }
        }

        private void Update()
        {
            Vector2 pointerPosition = default;
            bool pressed = false;
            bool held = false;
            bool released = false;

            if (Touchscreen.current?.primaryTouch.press.isPressed == true)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                held = true;
                released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
            }

            bool rotationComplete = accumulatedRotation >= finalRotation;

            if (pressed && !rotationComplete && IsNearWatch(pointerPosition))
            {
                dragging = true;
                previousPointerAngle = PointerAngle(pointerPosition);
            }

            if (dragging && held && !rotationComplete)
            {
                float angle = PointerAngle(pointerPosition);
                float delta = Mathf.DeltaAngle(previousPointerAngle, angle);
                previousPointerAngle = angle;

                if (Mathf.Abs(delta) < 50f)
                {
                    float nextRotation = Mathf.Clamp(
                        accumulatedRotation - delta, 0f, finalRotation);
                    if (!hasInteracted && Mathf.Abs(nextRotation - accumulatedRotation) > 0.1f)
                    {
                        hasInteracted = true;
                        SetRotateHintVisible(false);
                    }

                    accumulatedRotation = nextRotation;
                    if (accumulatedRotation >= finalRotation)
                    {
                        accumulatedRotation = finalRotation;
                        dragging = false;
                    }
                    ApplyVisuals();
                }
            }

            if (released || !held)
            {
                dragging = false;
            }

            if (!hasInteracted && accumulatedRotation < finalRotation)
            {
                if (minuteHand != null)
                {
                    float hint = Mathf.Sin(Time.unscaledTime * 2.8f) * 5f;
                    minuteHand.localRotation = Quaternion.Euler(0f, 0f, hint);
                }

                AnimateRotateHint();
            }

            if (rotationComplete && !continuePromptShown && !transitioning)
            {
                continuePromptShown = true;
                BurningContinuePrompt.Show(() => StartCoroutine(LoadNextScene()));
            }
        }

        private void AnimateRotateHint()
        {
            if (rotateHint == null || rotateHintRect == null || rotateHintImage == null)
            {
                return;
            }

            float cycleDuration = rotateHintDrawDuration
                + rotateHintHoldDuration
                + rotateHintFadeDuration
                + rotateHintPauseDuration;
            float elapsed = Mathf.Repeat(Time.unscaledTime - rotateHintCycleStartedAt, cycleDuration);

            rotateHintRect.localScale = Vector3.one;
            rotateHintRect.localRotation = Quaternion.Euler(0f, 0f, rotateHintAngle);

            if (elapsed < rotateHintDrawDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / rotateHintDrawDuration);
                rotateHintImage.fillAmount = t * rotateHintMaxFill;
                rotateHint.alpha = 1f;
                return;
            }

            elapsed -= rotateHintDrawDuration;
            rotateHintImage.fillAmount = rotateHintMaxFill;
            if (elapsed < rotateHintHoldDuration)
            {
                rotateHint.alpha = 1f;
                return;
            }

            elapsed -= rotateHintHoldDuration;
            if (elapsed < rotateHintFadeDuration)
            {
                rotateHint.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / rotateHintFadeDuration);
                return;
            }

            rotateHint.alpha = 0f;
            rotateHintImage.fillAmount = 0f;
        }

        private void SetRotateHintVisible(bool visible)
        {
            SetAlpha(rotateHint, 0f);
            if (rotateHintImage != null)
            {
                rotateHintImage.fillAmount = 0f;
            }
            if (rotateHintRect != null)
            {
                rotateHintRect.localScale = Vector3.one;
                rotateHintRect.localRotation = Quaternion.Euler(0f, 0f, rotateHintAngle);
            }
            if (visible)
            {
                rotateHintCycleStartedAt = Time.unscaledTime;
            }
        }

        private bool IsNearWatch(Vector2 screenPosition)
        {
            if (watchCenter == null)
            {
                return false;
            }

            Vector2 center = RectTransformUtility.WorldToScreenPoint(canvasCamera, watchCenter.position);
            float scaledRadius = interactionRadius * Screen.width / 540f;
            return Vector2.Distance(center, screenPosition) <= scaledRadius;
        }

        private float PointerAngle(Vector2 screenPosition)
        {
            Vector2 center = RectTransformUtility.WorldToScreenPoint(canvasCamera, watchCenter.position);
            Vector2 direction = screenPosition - center;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private void ApplyVisuals()
        {
            if (minuteHand != null)
            {
                minuteHand.localRotation = Quaternion.Euler(0f, 0f, -accumulatedRotation);
            }

            if (hourHand != null)
            {
                hourHand.localRotation = Quaternion.Euler(0f, 0f, -accumulatedRotation / 12f);
            }

            float walk = WindowAlpha(accumulatedRotation, 270f, 630f);
            float glance = WindowAlpha(accumulatedRotation, 630f, 990f);
            float stop = WindowAlpha(accumulatedRotation, 990f, 1350f);
            float pub = Ramp(accumulatedRotation, 1350f);
            float glanceSwap = Ramp(accumulatedRotation, 810f);

            SetAlpha(walkImage, walk);
            SetAlpha(glance1Image, glance * (1f - glanceSwap));
            SetAlpha(glance2Image, glance * glanceSwap);
            SetAlpha(stopImage, stop);
            SetAlpha(stopMark1, stop * Ramp(accumulatedRotation, 1080f));
            SetAlpha(stopMark2, stop * Ramp(accumulatedRotation, 1170f));
            SetAlpha(pubImage, pub);

            CurrentState = accumulatedRotation switch
            {
                < 270f => "시곗바늘을 돌려 기억 탐색",
                < 630f => "함께 걷던 시간",
                < 990f => "힐끔거리는 종수",
                < 1080f => "멈춘 발걸음",
                < 1170f => "멈춤 표시 첫 번째",
                < 1350f => "멈춤 표시 두 번째",
                _ => "봉천동 포차 / C14 이동 대기"
            };
        }

        private IEnumerator LoadNextScene()
        {
            transitioning = true;
            CurrentState = "C14로 전환";
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C13", "C14", transitionDuration, 0f, 0f);
            float elapsed = 0f;
            while (elapsed < timing.FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (transitionOverlay != null)
                {
                    float t = Mathf.Clamp01(elapsed / timing.FadeOutDuration);
                    transitionOverlay.alpha = t * t * (3f - 2f * t);
                }
                yield return null;
            }

            if (timing.BlackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(timing.BlackHoldDuration);
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private float WindowAlpha(float value, float start, float end)
        {
            float fadeIn = Ramp(value, start);
            float fadeOut = 1f - Ramp(value, end);
            return Mathf.Min(fadeIn, fadeOut);
        }

        private float Ramp(float value, float start)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, start + revealAngle, value));
        }

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
    }
}
