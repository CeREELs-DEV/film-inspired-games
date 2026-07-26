using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C14
{
    public sealed class C14DrinkingSequenceController : MonoBehaviour
    {
        [Header("화면 이동")]
        [SerializeField] private RectTransform scrollContent;
        [SerializeField, Min(0f)] private float scrollDistance = 465.66f;
        [SerializeField, Min(0.1f)] private float scrollDuration = 2.2f;

        [Header("Part 1")]
        [SerializeField] private CanvasGroup couple;
        [SerializeField] private CanvasGroup backgroundAfter;
        [SerializeField] private CanvasGroup bottle;
        [SerializeField] private CanvasGroup glass1;
        [SerializeField] private CanvasGroup glass2;
        [SerializeField] private RectTransform bottleRect;
        [SerializeField] private RectTransform glass1Rect;
        [SerializeField] private RectTransform glass2Rect;
        [SerializeField] private Image bottleLiquid;
        [SerializeField] private Image glass1Liquid;
        [SerializeField] private Image glass2Liquid;
        [SerializeField, Min(0.2f)] private float fillDuration = 2.4f;

        [Header("길게 누르기 안내")]
        [SerializeField] private Sprite holdHintSprite;
        [SerializeField] private Vector2 holdHintSize = new(34f, 90f);
        [SerializeField, Min(0f)] private float holdHintGap = 58f;
        [SerializeField, Min(0.1f)] private float holdHintCycleDuration = 2.35f;
        [SerializeField, Min(0f)] private float holdHintPressDistance = 10f;

        [Header("Part 2")]
        [SerializeField] private CanvasGroup part2SojuSet;
        [SerializeField] private CanvasGroup[] part2Images;

        [Header("다음 장면")]
        [SerializeField] private string nextSceneName = "Burning_C15_Playable";
        [SerializeField, Min(0.1f)] private float transitionToBlackDuration = 1.1f;

        private Stage stage = Stage.Intro;
        private float glassFill;
        private float bottleFill = 1f;
        private Coroutine sequence;
        private Camera canvasCamera;
        private CanvasGroup holdHint;
        private RectTransform holdHintRect;
        private float holdHintCycleStartedAt;
        private bool holdHintDismissed;

        public string CurrentChapter => "C14";
        public string CurrentPart => stage < Stage.Part2 ? "Part 1" : "Part 2";
        public string CurrentState { get; private set; } = "두 사람이 마주 보는 장면";
        public float GlassFill => glassFill;
        public float BottleFill => bottleFill;

        private enum Stage
        {
            Intro,
            Scrolling,
            FillGlass1,
            DrinkGlass1,
            FillGlass2,
            DrinkGlass2,
            Transition,
            Part2,
            Complete
        }

        private void Start()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            CreateHoldHint();

            SetAlpha(couple, 1f);
            SetAlpha(backgroundAfter, 0f);
            SetAlpha(bottle, 0f);
            SetAlpha(glass1, 0f);
            SetAlpha(glass2, 0f);
            SetAlpha(part2SojuSet, 0f);
            foreach (CanvasGroup image in part2Images)
            {
                SetAlpha(image, 0f);
            }

            ApplyLiquidLevels();
        }

        private void Update()
        {
            ReadPointer(out Vector2 position, out bool pressed, out bool held);

            if (stage == Stage.Intro && pressed)
            {
                StartSequence(ScrollAndReveal());
                return;
            }

            if (stage == Stage.FillGlass1 || stage == Stage.FillGlass2)
            {
                RectTransform activeGlass = stage == Stage.FillGlass1 ? glass1Rect : glass2Rect;
                bool holdingGlass = held && Contains(activeGlass, position);
                if (holdingGlass && !holdHintDismissed)
                {
                    holdHintDismissed = true;
                    SetAlpha(holdHint, 0f);
                }
                else if (!holdHintDismissed)
                {
                    AnimateHoldHint(activeGlass);
                }
                if (holdingGlass)
                {
                    FillActiveGlass(Time.unscaledDeltaTime / fillDuration);
                }

                return;
            }

            SetAlpha(holdHint, 0f);

            if ((stage == Stage.DrinkGlass1 || stage == Stage.DrinkGlass2) && pressed)
            {
                RectTransform activeGlass = stage == Stage.DrinkGlass1 ? glass1Rect : glass2Rect;
                if (Contains(activeGlass, position))
                {
                    StartSequence(DrinkActiveGlass());
                }
            }
        }

        private IEnumerator ScrollAndReveal()
        {
            stage = Stage.Scrolling;
            CurrentState = "포차 장면으로 이동";
            Vector2 start = scrollContent.anchoredPosition;
            Vector2 end = new(start.x, scrollDistance);
            yield return Animate(scrollDuration, t =>
                scrollContent.anchoredPosition = Vector2.LerpUnclamped(start, end, EaseInOut(t)));

            yield return Fade(couple, couple.alpha, 0f, 0.45f);
            CurrentState = "소주병 등장";
            yield return Fade(bottle, 0f, 1f, 0.65f);
            yield return new WaitForSecondsRealtime(0.18f);
            CurrentState = "첫 번째 잔 등장";
            yield return Fade(glass1, 0f, 1f, 0.55f);
            yield return new WaitForSecondsRealtime(0.16f);
            CurrentState = "두 번째 잔 등장";
            yield return Fade(glass2, 0f, 1f, 0.55f);
            yield return new WaitForSecondsRealtime(0.25f);

            stage = Stage.FillGlass1;
            holdHintCycleStartedAt = Time.unscaledTime;
            CurrentState = "첫 번째 잔을 누르고 있어 술 채우기";
        }

        private void FillActiveGlass(float amount)
        {
            glassFill = Mathf.Clamp01(glassFill + amount);
            bottleFill = Mathf.Clamp01(bottleFill - amount * 0.5f);
            ApplyLiquidLevels();

            float tilt = Mathf.Sin(Time.unscaledTime * 5.5f) * 1.5f - 3f;
            bottleRect.localRotation = Quaternion.Euler(0f, 0f, tilt);

            if (glassFill < 1f)
            {
                return;
            }

            SetAlpha(holdHint, 0f);
            bottleRect.localRotation = Quaternion.identity;
            if (stage == Stage.FillGlass1)
            {
                stage = Stage.DrinkGlass1;
                CurrentState = "가득 찬 첫 번째 잔을 눌러 마시기";
            }
            else
            {
                stage = Stage.DrinkGlass2;
                CurrentState = "가득 찬 두 번째 잔을 눌러 마시기";
            }
        }

        private void CreateHoldHint()
        {
            if (holdHintSprite == null || scrollContent == null)
            {
                return;
            }

            GameObject hintObject = new(
                "HoldGlassHint",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            holdHintRect = hintObject.GetComponent<RectTransform>();
            holdHintRect.SetParent(scrollContent, false);
            holdHintRect.anchorMin = new Vector2(0f, 1f);
            holdHintRect.anchorMax = new Vector2(0f, 1f);
            holdHintRect.pivot = new Vector2(0.5f, 0.5f);
            holdHintRect.sizeDelta = holdHintSize;
            holdHintRect.SetAsLastSibling();

            Image image = hintObject.GetComponent<Image>();
            image.sprite = holdHintSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            holdHint = hintObject.GetComponent<CanvasGroup>();
            holdHint.alpha = 0f;
            holdHint.interactable = false;
            holdHint.blocksRaycasts = false;
            holdHintCycleStartedAt = Time.unscaledTime;
        }

        private void AnimateHoldHint(RectTransform activeGlass)
        {
            if (holdHint == null || holdHintRect == null || activeGlass == null)
            {
                return;
            }

            Vector2 restPosition = activeGlass.anchoredPosition
                + Vector2.up * (activeGlass.rect.height * 0.5f + holdHintGap);
            float elapsed = Mathf.Repeat(
                Time.unscaledTime - holdHintCycleStartedAt,
                Mathf.Max(0.1f, holdHintCycleDuration));
            float descendEnd = holdHintCycleDuration * 0.18f;
            float pressEnd = holdHintCycleDuration * 0.62f;
            float fadeEnd = holdHintCycleDuration * 0.74f;

            if (elapsed < descendEnd)
            {
                float t = EaseOut(elapsed / descendEnd);
                holdHint.alpha = t;
                holdHintRect.anchoredPosition = restPosition + Vector2.down * (holdHintPressDistance * t);
                holdHintRect.localScale = Vector3.one;
                return;
            }

            if (elapsed < pressEnd)
            {
                float pulse = (Mathf.Sin((elapsed - descendEnd) * 8f) + 1f) * 0.5f;
                holdHint.alpha = Mathf.Lerp(0.82f, 1f, pulse);
                holdHintRect.anchoredPosition = restPosition + Vector2.down * holdHintPressDistance;
                holdHintRect.localScale = new Vector3(1.04f, Mathf.Lerp(0.88f, 0.93f, pulse), 1f);
                return;
            }

            if (elapsed < fadeEnd)
            {
                float t = EaseInOut(Mathf.InverseLerp(pressEnd, fadeEnd, elapsed));
                holdHint.alpha = 1f - t;
                holdHintRect.anchoredPosition = restPosition
                    + Vector2.down * Mathf.Lerp(holdHintPressDistance, 0f, t);
                holdHintRect.localScale = Vector3.one;
                return;
            }

            holdHint.alpha = 0f;
            holdHintRect.anchoredPosition = restPosition;
            holdHintRect.localScale = Vector3.one;
        }

        private IEnumerator DrinkActiveGlass()
        {
            bool first = stage == Stage.DrinkGlass1;
            CanvasGroup activeGroup = first ? glass1 : glass2;
            RectTransform activeRect = first ? glass1Rect : glass2Rect;
            Image activeLiquid = first ? glass1Liquid : glass2Liquid;
            CurrentState = first ? "첫 번째 잔 마시는 중" : "두 번째 잔 마시는 중";

            Vector3 originalScale = activeRect.localScale;
            yield return Animate(0.18f, t =>
            {
                activeGroup.alpha = 1f - t;
                activeRect.localScale = originalScale * Mathf.Lerp(1f, 0.88f, EaseIn(t));
            });

            activeLiquid.fillAmount = 0f;
            glassFill = 0f;
            yield return new WaitForSecondsRealtime(0.22f);
            yield return Animate(0.32f, t =>
            {
                activeGroup.alpha = t;
                activeRect.localScale = originalScale * Mathf.Lerp(0.92f, 1f, EaseOut(t));
            });
            activeRect.localScale = originalScale;

            if (first)
            {
                stage = Stage.FillGlass2;
                holdHintCycleStartedAt = Time.unscaledTime;
                CurrentState = "두 번째 잔을 누르고 있어 술 채우기";
                sequence = null;
                yield break;
            }

            stage = Stage.Transition;
            CurrentState = "밤이 깊어지는 중";
            yield return TransitionToPart2();
        }

        private IEnumerator TransitionToPart2()
        {
            yield return Animate(1.8f, t => backgroundAfter.alpha = EaseInOut(t));
            yield return Animate(0.8f, t =>
            {
                float eased = EaseInOut(t);
                bottle.alpha = 1f - eased;
                glass1.alpha = 1f - eased;
                glass2.alpha = 1f - eased;
                part2SojuSet.alpha = eased;
            });

            stage = Stage.Part2;
            CurrentState = "서로의 장면이 이어지는 중";
            yield return PlayPart2Cuts();
            stage = Stage.Complete;
            CurrentState = "C14 완료";
            yield return new WaitForSecondsRealtime(0.55f);
            yield return BurningContinuePrompt.WaitForContinue();
            yield return TransitionToC15();
            sequence = null;
        }

        private IEnumerator TransitionToC15()
        {
            CurrentState = "검은 화면으로 전환";
            CanvasGroup overlay = CreateBlackOverlay();
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C14", "C15", transitionToBlackDuration, 0.35f, 0f);
            yield return Fade(overlay, 0f, 1f, timing.FadeOutDuration);
            if (timing.BlackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(timing.BlackHoldDuration);
            }

            if (string.IsNullOrWhiteSpace(nextSceneName)
                || !Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"C15 씬을 불러올 수 없음: {nextSceneName}", this);
                yield break;
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private CanvasGroup CreateBlackOverlay()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject overlayObject = new(
                "TransitionToC15", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(canvas.transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsLastSibling();

            Image image = overlayObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            CanvasGroup group = overlayObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            return group;
        }

        private IEnumerator PlayPart2Cuts()
        {
            if (part2Images == null || part2Images.Length == 0)
            {
                yield break;
            }

            yield return Fade(part2Images[0], 0f, 1f, 0.8f);
            yield return new WaitForSecondsRealtime(0.55f);

            for (int i = 1; i < part2Images.Length; i++)
            {
                CanvasGroup previous = part2Images[i - 1];
                CanvasGroup next = part2Images[i];
                yield return Fade(next, 0f, 1f, 0.8f);
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Fade(previous, 1f, 0f, 0.55f);
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        private void ApplyLiquidLevels()
        {
            bottleLiquid.fillAmount = bottleFill;
            if (stage == Stage.FillGlass2 || stage == Stage.DrinkGlass2)
            {
                glass2Liquid.fillAmount = glassFill;
            }
            else
            {
                glass1Liquid.fillAmount = glassFill;
            }
        }

        private void StartSequence(IEnumerator routine)
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
            }
            sequence = StartCoroutine(routine);
        }

        private bool Contains(RectTransform rect, Vector2 screenPosition)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, canvasCamera);
        }

        private static void ReadPointer(out Vector2 position, out bool pressed, out bool held)
        {
            if (Touchscreen.current?.primaryTouch.press.isPressed == true)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                held = true;
                return;
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                return;
            }

            position = default;
            pressed = false;
            held = false;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            yield return Animate(duration, t => group.alpha = Mathf.LerpUnclamped(from, to, EaseInOut(t)));
            group.alpha = to;
        }

        private static IEnumerator Animate(float duration, System.Action<float> apply)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                apply(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            apply(1f);
        }

        private static float EaseIn(float t) => t * t;
        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInOut(float t) => t * t * (3f - 2f * t);

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
    }
}
