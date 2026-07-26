using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning.C08
{
    public sealed class C08ToC12SequenceController : MonoBehaviour
    {
        [Header("C08")]
        [SerializeField] private CanvasGroup c08Root;
        [SerializeField] private CanvasGroup c08BrightBackground;
        [SerializeField, Min(0f)] private float c08OpeningHold = 1.5f;
        [SerializeField, Min(0.01f)] private float c08DissolveDuration = 3.8f;
        [SerializeField, Min(0f)] private float c08BrightHold = 1.5f;
        [SerializeField, Min(0.01f)] private float c08ToBlackDuration = 2.4f;

        [Header("C09")]
        [SerializeField] private CanvasGroup c09Root;
        [SerializeField] private CanvasGroup haemiDark;
        [SerializeField] private CanvasGroup haemiLit;
        [SerializeField] private RectTransform jongsuRect;
        [SerializeField] private float jongsuStartOffset = 119.12f;
        [SerializeField, Min(0.01f)] private float lightFlickerDuration = 1.65f;
        [SerializeField, Min(0f)] private float approachDelay = 0.75f;
        [SerializeField, Min(0.01f)] private float c09PromptDuration = 0.78f;
        [SerializeField, Min(0f)] private float c09PromptRestDuration = 0.7f;
        [SerializeField, Min(0f)] private float c09PromptDistance = 7f;
        [SerializeField, Range(0f, 1f)] private float c09JongsuHitStart = 0.48f;
        [SerializeField, Min(0.01f)] private float c09DragEaseTime = 0.055f;
        [SerializeField, Min(0f)] private float c09CompletionDistance = 5f;
        [SerializeField, Min(0.01f)] private float c09SnapDuration = 0.24f;
        [SerializeField, Min(0f)] private float lightConnectionDelay = 0.12f;
        [SerializeField, Min(0.01f)] private float haemiDissolveDuration = 1.05f;
        [SerializeField, Min(0f)] private float c09EndingHold = 1.3f;

        [Header("C10")]
        [SerializeField] private CanvasGroup c10Root;
        [SerializeField] private CanvasGroup c10Neutral;
        [SerializeField] private CanvasGroup c10Blink;
        [SerializeField] private CanvasGroup c10Glance;
        [SerializeField] private CanvasGroup c10HaemiOnly;
        [SerializeField, Min(0.01f)] private float c10FocusDissolveDuration = 2.5f;
        [SerializeField, Min(0f)] private float c10FocusHold = 1.5f;

        [Header("C11")]
        [SerializeField] private CanvasGroup c11Root;
        [SerializeField] private CanvasGroup c11Watch;
        [SerializeField] private RectTransform c11WatchRect;
        [SerializeField] private float c11WatchStartOffset = -150f;
        [SerializeField, Min(0.01f)] private float c11WatchPresentDuration = 0.85f;
        [SerializeField, Min(0f)] private float c11EndingHold = 1f;

        [Header("C12")]
        [SerializeField] private CanvasGroup c12Root;
        [SerializeField] private CanvasGroup c12Smile;
        [SerializeField] private CanvasGroup c12Laugh;
        [SerializeField, Min(0f)] private float c12SmileHold = 1.2f;
        [SerializeField, Min(0.01f)] private float c12LaughDissolveDuration = 0.9f;
        [SerializeField, Min(0f)] private float c12EndingHold = 1.8f;

        [Header("장면 전환")]
        [SerializeField] private CanvasGroup blackOverlay;
        [SerializeField, Min(0.01f)] private float chapterFadeOutDuration = 1.5f;
        [SerializeField, Min(0.01f)] private float chapterFadeInDuration = 1.25f;
        [SerializeField] private string nextSceneName = "Burning_C13_Playable";
        [SerializeField, Min(0.01f)] private float nextSceneFadeDuration = 1.5f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private string legacyReplacementSceneName = "Burning_C08_C12_Playable";

        private Coroutine sequenceRoutine;
        private bool c11TouchRequested;
        private bool c09Dragging;
        private bool c09MoveCompleted;
        private float c09JongsuPositionX;
        private float c09JongsuTargetX;
        private float c09DragVelocity;
        private float c09PromptElapsed;
        private float c09DragPointerStartX;
        private float c09DragJongsuStartX;
        private int startChapter = 8;

        public event Action Finished;
        public bool IsWaitingForC11Touch { get; private set; }
        public bool IsWaitingForNextScene { get; private set; }
        public bool IsC09DragPlayable { get; private set; }
        public string CurrentChapter { get; private set; } = "재생 대기";
        public string CurrentState { get; private set; } = "대기";

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == "Burning_C08_C09_Playable")
            {
                if (Application.CanStreamedLevelBeLoaded(legacyReplacementSceneName))
                {
                    SceneManager.LoadScene(legacyReplacementSceneName);
                }
                else
                {
                    Debug.LogError($"현재 C08-C12 씬을 불러올 수 없음: {legacyReplacementSceneName}", this);
                }
                return;
            }

            string requestedChapter = BurningChapterDebugRequest.Consume();
            if (requestedChapter.Length == 3
                && int.TryParse(requestedChapter.Substring(1), out int requestedNumber)
                && requestedNumber is >= 8 and <= 12)
            {
                startChapter = requestedNumber;
            }

            Prepare();

            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            ReadPointer(out Vector2 pointerPosition, out bool pressed, out bool held, out bool released);

            if (IsC09DragPlayable)
            {
                HandleC09Drag(pointerPosition, pressed, held, released);
                return;
            }

            if (!pressed)
            {
                return;
            }

            if (IsWaitingForC11Touch)
            {
                c11TouchRequested = true;
            }
        }

        public void Play()
        {
            StopSequence();
            Prepare();
            sequenceRoutine = StartCoroutine(PlayRoutine());
        }

        public void StopSequence()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            IsWaitingForC11Touch = false;
            IsWaitingForNextScene = false;
            IsC09DragPlayable = false;
            c11TouchRequested = false;
            c09Dragging = false;
        }

        private void Prepare()
        {
            ShowOnly(c08Root, c09Root, c10Root, c11Root, c12Root);
            SetAlpha(c08BrightBackground, 0f);
            SetAlpha(haemiDark, 1f);
            SetAlpha(haemiLit, 0f);
            ShowC10Frame(c10Neutral);
            SetAlpha(c11Watch, 0f);
            SetAlpha(c12Smile, 1f);
            SetAlpha(c12Laugh, 0f);
            SetAlpha(blackOverlay, 0f);

            if (jongsuRect != null)
            {
                jongsuRect.anchoredPosition = new Vector2(jongsuStartOffset, 0f);
            }

            c09JongsuPositionX = jongsuStartOffset;
            c09JongsuTargetX = jongsuStartOffset;
            c09DragVelocity = 0f;
            c09PromptElapsed = 0f;
            c09Dragging = false;
            c09MoveCompleted = false;

            if (c11WatchRect != null)
            {
                c11WatchRect.anchoredPosition = new Vector2(0f, c11WatchStartOffset);
            }

            IsWaitingForC11Touch = false;
            IsWaitingForNextScene = false;
            IsC09DragPlayable = false;
            c11TouchRequested = false;
            CurrentChapter = "재생 대기";
            CurrentState = "대기";
        }

        private IEnumerator PlayRoutine()
        {
            if (startChapter <= 8)
            {
                yield return PlayC08();
            }

            if (startChapter <= 9)
            {
                if (startChapter == 9)
                {
                    SetAlpha(blackOverlay, 1f);
                }
                yield return PlayC09();
            }

            if (startChapter <= 10)
            {
                if (startChapter < 10)
                {
                    yield return TransitionChapter(c09Root, c10Root, "C10", "무심히 담배 피우는 해미");
                }
                else
                {
                    PrepareDirectChapter(c10Root, "C10", "무심히 담배 피우는 해미");
                }
                yield return PlayC10();
            }

            if (startChapter <= 11)
            {
                if (startChapter < 11)
                {
                    yield return TransitionChapter(c10Root, c11Root, "C11", "화면 터치 대기");
                }
                else
                {
                    PrepareDirectChapter(c11Root, "C11", "화면 터치 대기");
                }
                yield return PlayC11();
            }

            if (startChapter <= 12)
            {
                if (startChapter < 12)
                {
                    yield return TransitionChapter(c11Root, c12Root, "C12", "시계를 받은 해미");
                }
                else
                {
                    PrepareDirectChapter(c12Root, "C12", "시계를 받은 해미");
                }
                yield return PlayC12();
            }

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"다음 씬을 불러올 수 없습니다: {nextSceneName}", this);
                sequenceRoutine = null;
                yield break;
            }

            CurrentState = "C13으로 전환";
            BurningChapterSettings.ResolvedTiming c12ToC13 = BurningChapterSettings.Resolve(
                "C12", "C13", nextSceneFadeDuration, 0f, 0f);
            yield return Fade(blackOverlay, blackOverlay.alpha, 1f, c12ToC13.FadeOutDuration);
            yield return Wait(c12ToC13.BlackHoldDuration);
            SceneManager.LoadScene(nextSceneName);

            sequenceRoutine = null;
            Finished?.Invoke();
        }

        private void PrepareDirectChapter(CanvasGroup visible, string chapter, string state)
        {
            SetAlpha(c08Root, 0f);
            SetAlpha(c09Root, 0f);
            SetAlpha(c10Root, 0f);
            SetAlpha(c11Root, 0f);
            SetAlpha(c12Root, 0f);
            SetAlpha(visible, 1f);
            SetAlpha(blackOverlay, 0f);
            CurrentChapter = chapter;
            CurrentState = state;
        }

        private IEnumerator PlayC08()
        {
            CurrentChapter = "C08";
            CurrentState = "서로를 인지";
            yield return Wait(c08OpeningHold);

            CurrentState = "밝은 배경으로 디졸브";
            yield return Fade(c08BrightBackground, 0f, 1f, c08DissolveDuration);
            yield return Wait(c08BrightHold);
            CurrentState = "C09 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();

            CurrentState = "블랙으로 디졸브";
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C08", "C09", c08ToBlackDuration, 0f, lightFlickerDuration);
            yield return Fade(blackOverlay, 0f, 1f, timing.FadeOutDuration);
            yield return Wait(timing.BlackHoldDuration);
        }

        private IEnumerator PlayC09()
        {
            SetAlpha(c08Root, 0f);
            SetAlpha(c09Root, 1f);
            CurrentChapter = "C09";
            CurrentState = "불빛 점멸";
            yield return Wait(0.35f);
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C08", "C09", c08ToBlackDuration, 0f, lightFlickerDuration);
            yield return FlickerReveal(timing.FadeInDuration);

            CurrentState = "종수 이동 안내";
            yield return Wait(approachDelay);

            if (jongsuRect != null)
            {
                IsC09DragPlayable = true;
                CurrentState = "종수를 왼쪽으로 이동";
                while (!c09MoveCompleted)
                {
                    yield return null;
                }
                IsC09DragPlayable = false;
                yield return SnapJongsuToEnd();
            }

            yield return Wait(lightConnectionDelay);

            CurrentState = "담배불 연결";
            yield return CrossFade(haemiDark, haemiLit, haemiDissolveDuration);
            CurrentState = "장면 유지";
            yield return Wait(c09EndingHold);
            CurrentState = "C10 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();
        }

        private IEnumerator PlayC10()
        {
            ShowC10Frame(c10Neutral);
            CurrentState = "종수의 첫 힐끔";
            yield return Wait(1f);
            ShowC10Frame(c10Blink);
            yield return Wait(0.25f);
            ShowC10Frame(c10Glance);
            yield return Wait(0.875f);

            ShowC10Frame(c10Neutral);
            CurrentState = "종수의 두 번째 힐끔";
            yield return Wait(1.125f);
            ShowC10Frame(c10Blink);
            yield return Wait(0.125f);
            ShowC10Frame(c10Glance);
            yield return Wait(1.75f);

            CurrentState = "해미에게 시선 집중";
            yield return CrossFade(c10Glance, c10HaemiOnly, c10FocusDissolveDuration);
            yield return Wait(c10FocusHold);
            CurrentState = "C11 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();
        }

        private IEnumerator PlayC11()
        {
            SetAlpha(c11Watch, 0f);

            if (c11WatchRect != null)
            {
                c11WatchRect.anchoredPosition = new Vector2(0f, c11WatchStartOffset);
            }

            IsWaitingForC11Touch = true;
            c11TouchRequested = false;

            while (!c11TouchRequested)
            {
                yield return null;
            }

            IsWaitingForC11Touch = false;
            CurrentState = "시계를 내밈";
            yield return PresentWatch();
            yield return Wait(c11EndingHold);
            CurrentState = "C12 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();
        }

        private IEnumerator PlayC12()
        {
            CurrentState = "기쁜 미소";
            yield return Wait(c12SmileHold);
            CurrentState = "환하게 웃음";
            yield return CrossFade(c12Smile, c12Laugh, c12LaughDissolveDuration);
            yield return Wait(c12EndingHold);
            CurrentState = "C13 이동 대기";
            IsWaitingForNextScene = true;
            yield return BurningContinuePrompt.WaitForContinue();
            IsWaitingForNextScene = false;
        }

        private IEnumerator TransitionChapter(CanvasGroup from, CanvasGroup to, string chapter, string state)
        {
            string fromChapter = CurrentChapter;
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                fromChapter, chapter, chapterFadeOutDuration, 0f, chapterFadeInDuration);
            yield return Fade(blackOverlay, blackOverlay.alpha, 1f, timing.FadeOutDuration);
            yield return Wait(timing.BlackHoldDuration);
            SetAlpha(from, 0f);
            SetAlpha(to, 1f);
            CurrentChapter = chapter;
            CurrentState = state;
            yield return Fade(blackOverlay, 1f, 0f, timing.FadeInDuration);
        }

        private IEnumerator FlickerReveal(float duration)
        {
            float[] alphas = { 1f, 0.22f, 0.72f, 0.08f, 0.42f, 0f };
            float[] portions = { 0.22f, 0.12f, 0.20f, 0.12f, 0.34f };

            for (int i = 0; i < portions.Length; i++)
            {
                yield return Fade(blackOverlay, alphas[i], alphas[i + 1], duration * portions[i]);
            }
        }

        private void HandleC09Drag(Vector2 screenPosition, bool pressed, bool held, bool released)
        {
            if (jongsuRect == null || c09MoveCompleted)
            {
                return;
            }

            if (pressed && IsPointerOverJongsu(screenPosition))
            {
                c09Dragging = true;
                c09DragPointerStartX = ScreenToC09Position(screenPosition).x;
                c09DragJongsuStartX = c09JongsuPositionX;
                c09JongsuTargetX = c09JongsuPositionX;
                c09DragVelocity = 0f;
            }

            if (c09Dragging && held)
            {
                float pointerX = ScreenToC09Position(screenPosition).x;
                c09JongsuTargetX = Mathf.Clamp(
                    c09DragJongsuStartX + pointerX - c09DragPointerStartX,
                    0f,
                    jongsuStartOffset);
                c09JongsuPositionX = Mathf.SmoothDamp(
                    c09JongsuPositionX,
                    c09JongsuTargetX,
                    ref c09DragVelocity,
                    c09DragEaseTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
                SetJongsuPosition(c09JongsuPositionX);

                if (c09JongsuTargetX <= c09CompletionDistance)
                {
                    c09MoveCompleted = true;
                    c09Dragging = false;
                    return;
                }
            }

            if (c09Dragging && (released || !held))
            {
                c09Dragging = false;
                c09JongsuTargetX = c09JongsuPositionX;
                c09DragVelocity = 0f;
            }

            if (!c09Dragging)
            {
                c09PromptElapsed += Time.unscaledDeltaTime;
                float moveDuration = Mathf.Max(0.01f, c09PromptDuration);
                float cycleDuration = moveDuration + c09PromptRestDuration;
                float cycleTime = Mathf.Repeat(c09PromptElapsed, cycleDuration);
                float prompt = 0f;

                if (cycleTime < moveDuration)
                {
                    float normalized = cycleTime / moveDuration;
                    prompt = Mathf.SmoothStep(0f, 1f, Mathf.Sin(normalized * Mathf.PI));
                }

                SetJongsuPosition(Mathf.Max(0f, c09JongsuPositionX - prompt * c09PromptDistance));
            }
        }

        private IEnumerator SnapJongsuToEnd()
        {
            float startX = jongsuRect.anchoredPosition.x;
            float elapsed = 0f;

            while (elapsed < c09SnapDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / c09SnapDuration);
                float movement = EaseOutBack(normalized, 0.35f);
                SetJongsuPosition(Mathf.LerpUnclamped(startX, 0f, movement));
                yield return null;
            }

            c09JongsuPositionX = 0f;
            c09JongsuTargetX = 0f;
            SetJongsuPosition(0f);
        }

        private bool IsPointerOverJongsu(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    jongsuRect, screenPosition, null, out Vector2 localPosition))
            {
                return false;
            }

            Rect rect = jongsuRect.rect;
            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPosition.x);
            return rect.Contains(localPosition) && normalizedX >= c09JongsuHitStart;
        }

        private Vector2 ScreenToC09Position(Vector2 screenPosition)
        {
            RectTransform parentRect = jongsuRect.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPosition, null, out Vector2 localPosition);
            return localPosition;
        }

        private void SetJongsuPosition(float x)
        {
            jongsuRect.anchoredPosition = new Vector2(x, 0f);
        }

        private IEnumerator PresentWatch()
        {
            float elapsed = 0f;
            Vector2 start = new(0f, c11WatchStartOffset);

            while (elapsed < c11WatchPresentDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / c11WatchPresentDuration);
                float movement = EaseOutBack(normalized, 0.7f);
                SetAlpha(c11Watch, EaseOutCubic(normalized));

                if (c11WatchRect != null)
                {
                    c11WatchRect.anchoredPosition = Vector2.LerpUnclamped(start, Vector2.zero, movement);
                }

                yield return null;
            }

            SetAlpha(c11Watch, 1f);

            if (c11WatchRect != null)
            {
                c11WatchRect.anchoredPosition = Vector2.zero;
            }
        }

        private IEnumerator CrossFade(CanvasGroup from, CanvasGroup to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                SetAlpha(from, 1f - normalized);
                SetAlpha(to, normalized);
                yield return null;
            }

            SetAlpha(from, 0f);
            SetAlpha(to, 1f);
        }

        private void ShowC10Frame(CanvasGroup visible)
        {
            SetAlpha(c10Neutral, visible == c10Neutral ? 1f : 0f);
            SetAlpha(c10Blink, visible == c10Blink ? 1f : 0f);
            SetAlpha(c10Glance, visible == c10Glance ? 1f : 0f);
            SetAlpha(c10HaemiOnly, visible == c10HaemiOnly ? 1f : 0f);
        }

        private static void ShowOnly(CanvasGroup visible, params CanvasGroup[] hidden)
        {
            SetAlpha(visible, 1f);

            foreach (CanvasGroup group in hidden)
            {
                SetAlpha(group, 0f);
            }
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                SetAlpha(group, Mathf.LerpUnclamped(from, to, normalized));
                yield return null;
            }

            SetAlpha(group, to);
        }

        private static IEnumerator Wait(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
        }

        private static void ReadPointer(
            out Vector2 position,
            out bool pressed,
            out bool held,
            out bool released)
        {
            if (Touchscreen.current != null
                && (Touchscreen.current.primaryTouch.press.isPressed
                    || Touchscreen.current.primaryTouch.press.wasPressedThisFrame
                    || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                held = Touchscreen.current.primaryTouch.press.isPressed;
                released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
                return;
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                return;
            }

            position = default;
            pressed = false;
            held = false;
            released = false;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float value, float overshoot)
        {
            float shifted = Mathf.Clamp01(value) - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted
                + overshoot * shifted * shifted;
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
