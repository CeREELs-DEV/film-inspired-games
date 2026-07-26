using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning.C04
{
    public sealed class C04RewardSequenceController : MonoBehaviour
    {
        [Header("손잡이")]
        [SerializeField] private RectTransform handleRect;
        [SerializeField, Min(30f)] private float handleInteractionRadius = 110f;
        [SerializeField, Min(45f)] private float requiredHandleRotation = 540f;
        [SerializeField, Min(0f)] private float handleCompleteHold = 0.25f;
        [SerializeField, Range(0.1f, 2f)] private float handleDownwardSensitivity = 0.65f;
        [SerializeField, Min(90f)] private float handleMaxInputSpeed = 360f;
        [SerializeField, Min(90f)] private float handleVisualFollowSpeed = 300f;

        [Header("캡슐")]
        [SerializeField] private CanvasGroup capsuleUp;
        [SerializeField] private CanvasGroup capsuleDown;
        [SerializeField] private RectTransform capsuleUpRect;
        [SerializeField] private RectTransform capsuleDownRect;

        [Header("보상")]
        [SerializeField] private CanvasGroup watch;
        [SerializeField] private RectTransform watchRect;

        [Header("등장")]
        [SerializeField, Min(0f)] private float openingHold = 0.45f;
        [SerializeField, Min(0.01f)] private float capsuleAppearDuration = 0.68f;
        [SerializeField, Range(0.5f, 1f)] private float capsuleStartScale = 0.84f;
        [SerializeField, Range(0f, 0.2f)] private float capsuleScalePop = 0.075f;
        [SerializeField, Range(0f, 0.1f)] private float capsuleSettleDip = 0.018f;
        [SerializeField, Min(0f)] private float capsuleDropDistance = 68f;
        [SerializeField, Range(0f, 0.2f)] private float capsuleLandingSquash = 0.09f;
        [SerializeField, Min(0f)] private float capsuleHold = 0.35f;

        [Header("입력 대기")]
        [SerializeField, Min(0.1f)] private float idlePulseDuration = 1f;
        [SerializeField, Range(0f, 0.1f)] private float idleScaleAmount = 0.025f;
        [SerializeField, Min(0f)] private float idleLiftAmount = 8f;
        [SerializeField] private Vector2 capsuleHitAreaMin = new(0.14f, 0.27f);
        [SerializeField] private Vector2 capsuleHitAreaMax = new(0.86f, 0.72f);

        [Header("열림")]
        [SerializeField, Min(0.01f)] private float openAnticipationDuration = 0.13f;
        [SerializeField, Range(0f, 0.2f)] private float openAnticipationSquash = 0.1f;
        [SerializeField, Min(0.01f)] private float capsuleOpenDuration = 0.46f;
        [SerializeField] private Vector2 capsuleUpOffset = new(95f, 130f);
        [SerializeField] private Vector2 capsuleDownOffset = new(-70f, -150f);
        [SerializeField] private float capsuleUpRotation = -10f;
        [SerializeField] private float capsuleDownRotation = 8f;
        [SerializeField, Range(0f, 1f)] private float openedCapsuleAlpha = 0.16f;

        [Header("시계")]
        [SerializeField, Min(0f)] private float watchAppearDelay = 0.08f;
        [SerializeField, Min(0.01f)] private float watchFadeDuration = 0.42f;
        [SerializeField, Range(0.2f, 1f)] private float watchStartScale = 0.56f;
        [SerializeField, Min(0f)] private float watchRiseDistance = 42f;
        [SerializeField, Range(0f, 0.25f)] private float watchScalePop = 0.13f;
        [SerializeField, Range(-20f, 20f)] private float watchStartRotation = -7f;
        [SerializeField, Min(0.01f)] private float watchSettleDuration = 0.32f;
        [SerializeField, Min(0f)] private float rewardHold = 0.35f;
        [SerializeField] private bool playOnStart = true;

        [Header("단독 씬 진행")]
        [SerializeField] private string standaloneNextSceneName = "Burning_C06_C07_Playable";
        [SerializeField, Min(0f)] private float standaloneNextDelay = 0.35f;

        [Header("장면 신호")]
        [SerializeField] private UnityEvent onCapsuleAppeared;
        [SerializeField] private UnityEvent onCapsuleOpened;
        [SerializeField] private UnityEvent onRewardShown;

        private Coroutine sequenceRoutine;
        private bool openRequested;
        private bool draggingHandle;
        private bool handleTurnComplete;
        private bool hasTouchedHandle;
        private float previousPointerAngle;
        private Vector2 previousPointerPosition;
        private float handleRotation;
        private float displayedHandleRotation;
        private Vector2 handleStartPosition;
        private Camera canvasCamera;

        public event Action Finished;
        public bool IsWaitingForHandle { get; private set; }
        public bool IsWaitingForOpen { get; private set; }
        public float HandleProgress => Mathf.Clamp01(handleRotation / requiredHandleRotation);
        public string CurrentState { get; private set; } = "대기";

        private void Start()
        {
            if (handleRect != null)
            {
                handleStartPosition = handleRect.anchoredPosition;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Prepare();

            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            ReadPointer(out Vector2 pointerPosition, out bool pressed, out bool held, out bool released);

            if (IsWaitingForHandle)
            {
                UpdateHandle(pointerPosition, pressed, held, released);
                return;
            }

            if (!IsWaitingForOpen)
            {
                return;
            }

            if (pressed && IsInsideCapsule(pointerPosition))
            {
                RequestOpen();
            }
        }

        public void Play()
        {
            StopSequence();
            Prepare();
            sequenceRoutine = StartCoroutine(PlayRoutine());
        }

        public void RequestOpen()
        {
            if (IsWaitingForOpen)
            {
                openRequested = true;
            }
        }

        public void StopSequence()
        {
            if (sequenceRoutine == null)
            {
                return;
            }

            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
            IsWaitingForHandle = false;
            IsWaitingForOpen = false;
            openRequested = false;
        }

        private void Prepare()
        {
            SetAlpha(capsuleUp, 0f);
            SetAlpha(capsuleDown, 0f);
            SetAlpha(watch, 0f);

            ResetHandle();
            ResetRect(capsuleUpRect, capsuleStartScale);
            ResetRect(capsuleDownRect, capsuleStartScale);
            ResetRect(watchRect, watchStartScale);
            SetWatchPose(0f);
            IsWaitingForHandle = false;
            IsWaitingForOpen = false;
            openRequested = false;
            draggingHandle = false;
            handleTurnComplete = false;
            hasTouchedHandle = false;
            handleRotation = 0f;
            displayedHandleRotation = 0f;
            CurrentState = "손잡이 돌리기 대기";
        }

        private IEnumerator PlayRoutine()
        {
            yield return new WaitForSecondsRealtime(openingHold);
            yield return AnimateHandleUntilTurned();
            yield return new WaitForSecondsRealtime(handleCompleteHold);

            CurrentState = "캡슐 등장";
            yield return AnimateCapsuleAppearance();
            onCapsuleAppeared?.Invoke();

            yield return AnimateCapsuleIdleUntilClicked();
            yield return AnimateOpenAnticipation();
            CurrentState = "캡슐 열리는 중";
            yield return AnimateCapsuleOpening();
            onCapsuleOpened?.Invoke();

            if (watch != null && watch.alpha < 1f)
            {
                yield return Fade(watch, watch.alpha, 1f, watchFadeDuration);
            }

            yield return AnimateWatchSettle();

            onRewardShown?.Invoke();
            CurrentState = "시계 획득";
            yield return new WaitForSecondsRealtime(rewardHold);

            bool hasFlowController = Finished != null;
            Finished?.Invoke();

            if (hasFlowController)
            {
                sequenceRoutine = null;
                yield break;
            }

            sequenceRoutine = StartCoroutine(LoadStandaloneNextScene());
        }

        private IEnumerator LoadStandaloneNextScene()
        {
            yield return new WaitForSecondsRealtime(standaloneNextDelay);
            yield return BurningContinuePrompt.WaitForContinue();

            if (string.IsNullOrWhiteSpace(standaloneNextSceneName)
                || !Application.CanStreamedLevelBeLoaded(standaloneNextSceneName))
            {
                Debug.LogError($"C04 다음 씬을 불러올 수 없음: {standaloneNextSceneName}", this);
                sequenceRoutine = null;
                yield break;
            }

            SceneManager.LoadScene(standaloneNextSceneName);
        }

        private IEnumerator AnimateHandleUntilTurned()
        {
            IsWaitingForHandle = true;
            CurrentState = "손잡이를 시계 방향으로 돌리기";
            float hintTime = 0f;

            while (!handleTurnComplete)
            {
                if (!draggingHandle && !hasTouchedHandle && handleRect != null)
                {
                    hintTime += Time.unscaledDeltaTime;
                    float pulse = (1f - Mathf.Cos(hintTime * Mathf.PI * 1.6f)) * 0.5f;
                    handleRect.localRotation = Quaternion.Euler(0f, 0f, -pulse * 7f);
                }
                yield return null;
            }

            IsWaitingForHandle = false;
            CurrentState = "손잡이 회전 완료";
            float start = displayedHandleRotation;
            float elapsed = 0f;
            const float snapDuration = 0.28f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / snapDuration);
                float overshoot = Mathf.Sin(t * Mathf.PI) * 12f;
                SetHandleRotation(Mathf.Lerp(start, requiredHandleRotation, EaseOutCubic(t)) + overshoot);
                yield return null;
            }

            handleRotation = requiredHandleRotation;
            displayedHandleRotation = requiredHandleRotation;
            SetHandleRotation(handleRotation);
        }

        private void UpdateHandle(Vector2 pointerPosition, bool pressed, bool held, bool released)
        {
            if (pressed && IsInsideHandle(pointerPosition))
            {
                draggingHandle = true;
                hasTouchedHandle = true;
                previousPointerAngle = PointerAngle(pointerPosition);
                previousPointerPosition = pointerPosition;
                SetHandleRotation(handleRotation);
            }

            if (draggingHandle && held)
            {
                float angle = PointerAngle(pointerPosition);
                float delta = Mathf.DeltaAngle(previousPointerAngle, angle);
                Vector2 pointerDelta = pointerPosition - previousPointerPosition;
                previousPointerAngle = angle;
                previousPointerPosition = pointerPosition;

                float clockwiseRotation = 0f;
                if (Mathf.Abs(delta) < 70f)
                {
                    clockwiseRotation = Mathf.Max(0f, -delta);
                }

                // 중심에서 아래로 당겨도 손잡이가 자연스럽게 돌아가도록 보완
                float downwardRotation = Mathf.Max(0f, -pointerDelta.y) * handleDownwardSensitivity;
                float maxRotationDelta = handleMaxInputSpeed * Time.unscaledDeltaTime;
                float rotationDelta = Mathf.Min(
                    Mathf.Max(clockwiseRotation, downwardRotation), maxRotationDelta);
                if (rotationDelta > 0f)
                {
                    handleRotation = Mathf.Clamp(handleRotation + rotationDelta, 0f, requiredHandleRotation);
                }
            }

            if (released || !held)
            {
                draggingHandle = false;
            }

            displayedHandleRotation = Mathf.MoveTowards(
                displayedHandleRotation,
                handleRotation,
                handleVisualFollowSpeed * Time.unscaledDeltaTime);
            SetHandleRotation(displayedHandleRotation);

            if (handleRotation >= requiredHandleRotation
                && Mathf.Approximately(displayedHandleRotation, requiredHandleRotation))
            {
                draggingHandle = false;
                handleTurnComplete = true;
            }
        }

        private IEnumerator AnimateCapsuleAppearance()
        {
            float elapsed = 0f;

            while (elapsed < capsuleAppearDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / capsuleAppearDuration);
                float alpha = EvaluatePopAlpha(normalized);
                float scale = EvaluateCapsulePop(normalized);
                float landing = Mathf.Exp(-Mathf.Pow((normalized - 0.68f) / 0.13f, 2f));
                float stretch = Mathf.Sin(Mathf.Clamp01(normalized / 0.68f) * Mathf.PI) * 0.045f;
                float scaleX = scale + landing * capsuleLandingSquash;
                float scaleY = scale + stretch - landing * capsuleLandingSquash * 0.72f;
                float vertical = EvaluateCapsuleDrop(normalized);
                float rotation = Mathf.Sin(normalized * Mathf.PI * 2.4f) * (1f - normalized) * 4f;

                SetAlpha(capsuleUp, alpha);
                SetAlpha(capsuleDown, alpha);
                SetCapsulePose(capsuleUpRect, scaleX, scaleY, vertical, rotation);
                SetCapsulePose(capsuleDownRect, scaleX, scaleY, vertical, rotation);
                yield return null;
            }

            SetAlpha(capsuleUp, 1f);
            SetAlpha(capsuleDown, 1f);
            SetScale(capsuleUpRect, 1f);
            SetScale(capsuleDownRect, 1f);
        }

        private IEnumerator AnimateCapsuleIdleUntilClicked()
        {
            IsWaitingForOpen = true;
            CurrentState = "캡슐을 눌러 열기";
            float elapsed = 0f;

            while (!openRequested || elapsed < capsuleHold)
            {
                elapsed += Time.unscaledDeltaTime;
                float phase = elapsed / idlePulseDuration * Mathf.PI * 2f;
                float pulse = (1f - Mathf.Cos(phase)) * 0.5f;
                float scale = 1f + pulse * idleScaleAmount;
                float lift = pulse * idleLiftAmount;
                float sway = Mathf.Sin(phase) * 1.15f;

                SetIdlePose(capsuleUpRect, scale, lift, sway);
                SetIdlePose(capsuleDownRect, scale, lift, sway);
                yield return null;
            }

            IsWaitingForOpen = false;
            SetIdlePose(capsuleUpRect, 1f, 0f, 0f);
            SetIdlePose(capsuleDownRect, 1f, 0f, 0f);
        }

        private IEnumerator AnimateOpenAnticipation()
        {
            CurrentState = "캡슐 개봉 준비";
            float elapsed = 0f;
            while (elapsed < openAnticipationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / openAnticipationDuration);
                float squeeze = Mathf.Sin(normalized * Mathf.PI);
                float scaleX = 1f + squeeze * openAnticipationSquash;
                float scaleY = 1f - squeeze * openAnticipationSquash * 0.72f;
                float drop = -squeeze * 9f;

                SetCapsulePose(capsuleUpRect, scaleX, scaleY, drop, 0f);
                SetCapsulePose(capsuleDownRect, scaleX, scaleY, drop, 0f);
                yield return null;
            }

            SetCapsulePose(capsuleUpRect, 1f, 1f, 0f, 0f);
            SetCapsulePose(capsuleDownRect, 1f, 1f, 0f, 0f);
        }

        private IEnumerator AnimateCapsuleOpening()
        {
            float elapsed = 0f;

            while (elapsed < capsuleOpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / capsuleOpenDuration);
                float movement = EaseOutBack(normalized, 1.7f);
                float fade = EaseOutCubic(Mathf.InverseLerp(0.12f, 0.78f, normalized));

                AnimateCapsulePiece(capsuleUp, capsuleUpRect, capsuleUpOffset, capsuleUpRotation, movement, fade);
                AnimateCapsulePiece(capsuleDown, capsuleDownRect, capsuleDownOffset, capsuleDownRotation, movement, fade);

                float watchNormalized = Mathf.Clamp01((elapsed - watchAppearDelay) / watchFadeDuration);
                SetWatchPose(watchNormalized);
                yield return null;
            }

            AnimateCapsulePiece(capsuleUp, capsuleUpRect, capsuleUpOffset, capsuleUpRotation, 1f, 1f);
            AnimateCapsulePiece(capsuleDown, capsuleDownRect, capsuleDownOffset, capsuleDownRotation, 1f, 1f);
            SetWatchPose(1f);
        }

        private IEnumerator AnimateWatchSettle()
        {
            if (watchRect == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < watchSettleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / watchSettleDuration);
                float pulse = Mathf.Sin(normalized * Mathf.PI * 2f) * (1f - normalized);
                float scale = 1f + pulse * 0.055f;
                float rotation = pulse * 1.6f;
                watchRect.localScale = new Vector3(scale, scale, 1f);
                watchRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
                yield return null;
            }

            watchRect.localScale = Vector3.one;
            watchRect.localRotation = Quaternion.identity;
        }

        private float EvaluateCapsulePop(float normalized)
        {
            const float popEnd = 0.62f;
            const float reboundEnd = 0.84f;

            if (normalized < popEnd)
            {
                float t = EaseOutCubic(normalized / popEnd);
                return Mathf.LerpUnclamped(capsuleStartScale, 1f + capsuleScalePop, t);
            }

            if (normalized < reboundEnd)
            {
                float t = Mathf.SmoothStep(0f, 1f, (normalized - popEnd) / (reboundEnd - popEnd));
                return Mathf.LerpUnclamped(1f + capsuleScalePop, 1f - capsuleSettleDip, t);
            }

            float settle = Mathf.SmoothStep(0f, 1f, (normalized - reboundEnd) / (1f - reboundEnd));
            return Mathf.LerpUnclamped(1f - capsuleSettleDip, 1f, settle);
        }

        private static float EvaluatePopAlpha(float normalized)
        {
            if (normalized < 0.1f)
            {
                return 0f;
            }

            if (normalized < 0.38f)
            {
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.38f, normalized));
            }

            if (normalized < 0.56f)
            {
                return Mathf.Lerp(1f, 0.82f, Mathf.InverseLerp(0.38f, 0.56f, normalized));
            }

            if (normalized < 0.72f)
            {
                return Mathf.Lerp(0.82f, 1f, Mathf.InverseLerp(0.56f, 0.72f, normalized));
            }

            return 1f;
        }

        private float EvaluateCapsuleDrop(float normalized)
        {
            const float impactTime = 0.68f;
            if (normalized < impactTime)
            {
                float fall = EaseInCubic(normalized / impactTime);
                return Mathf.Lerp(capsuleDropDistance, -10f, fall);
            }

            float rebound = Mathf.Clamp01((normalized - impactTime) / (1f - impactTime));
            return Mathf.LerpUnclamped(-10f, 0f, EaseOutBack(rebound, 0.55f));
        }

        private void AnimateCapsulePiece(
            CanvasGroup group,
            RectTransform rect,
            Vector2 offset,
            float rotation,
            float movement,
            float fade)
        {
            SetAlpha(group, Mathf.LerpUnclamped(1f, openedCapsuleAlpha, fade));

            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = Vector2.LerpUnclamped(Vector2.zero, offset, movement);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(0f, rotation, movement));
            float scale = 1f + Mathf.Sin(Mathf.Clamp01(movement) * Mathf.PI) * 0.045f;
            rect.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetWatchPose(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            float movement = EaseOutBack(normalized, 1.4f);
            float alpha = EvaluateRewardAlpha(normalized);
            float scale = Mathf.LerpUnclamped(
                watchStartScale,
                1f + Mathf.Sin(normalized * Mathf.PI) * watchScalePop,
                movement);

            SetAlpha(watch, alpha);
            if (watchRect == null)
            {
                return;
            }

            watchRect.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-watchRiseDistance, 0f, movement));
            watchRect.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.LerpUnclamped(watchStartRotation, 0f, EaseOutCubic(normalized)));
            watchRect.localScale = new Vector3(scale, scale, 1f);
        }

        private static float EvaluateRewardAlpha(float normalized)
        {
            if (normalized < 0.12f)
            {
                return 0f;
            }

            if (normalized < 0.4f)
            {
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.4f, normalized));
            }

            if (normalized < 0.54f)
            {
                return Mathf.Lerp(1f, 0.88f, Mathf.InverseLerp(0.4f, 0.54f, normalized));
            }

            if (normalized < 0.7f)
            {
                return Mathf.Lerp(0.88f, 1f, Mathf.InverseLerp(0.54f, 0.7f, normalized));
            }

            return 1f;
        }

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        private static float EaseOutBack(float value, float overshoot)
        {
            float shifted = Mathf.Clamp01(value) - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted
                + overshoot * shifted * shifted;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
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

        private static void ResetRect(RectTransform rect, float scale)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            SetScale(rect, scale);
        }

        private void ResetHandle()
        {
            if (handleRect == null)
            {
                return;
            }

            handleRect.anchoredPosition = handleStartPosition;
            handleRect.localRotation = Quaternion.identity;
            SetScale(handleRect, 1f);
        }

        private static void SetIdlePose(RectTransform rect, float scale, float lift, float rotation)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = new Vector2(0f, lift);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            SetScale(rect, scale);
        }

        private static void SetCapsulePose(
            RectTransform rect,
            float scaleX,
            float scaleY,
            float vertical,
            float rotation)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = new Vector2(0f, vertical);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            rect.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private bool IsInsideCapsule(Vector2 screenPosition)
        {
            if (capsuleUpRect == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    capsuleUpRect,
                    screenPosition,
                    null,
                    out Vector2 localPosition))
            {
                return false;
            }

            Rect rect = capsuleUpRect.rect;
            Vector2 normalized = new(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPosition.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPosition.y));

            return normalized.x >= capsuleHitAreaMin.x
                && normalized.x <= capsuleHitAreaMax.x
                && normalized.y >= capsuleHitAreaMin.y
                && normalized.y <= capsuleHitAreaMax.y;
        }

        private bool IsInsideHandle(Vector2 screenPosition)
        {
            if (handleRect == null)
            {
                return false;
            }

            Vector2 center = RectTransformUtility.WorldToScreenPoint(canvasCamera, handleRect.position);
            float radius = handleInteractionRadius * Screen.width / 540f;
            return Vector2.Distance(center, screenPosition) <= radius;
        }

        private float PointerAngle(Vector2 screenPosition)
        {
            Vector2 center = RectTransformUtility.WorldToScreenPoint(canvasCamera, handleRect.position);
            Vector2 direction = screenPosition - center;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private void SetHandleRotation(float clockwiseRotation)
        {
            if (handleRect != null)
            {
                handleRect.localRotation = Quaternion.Euler(0f, 0f, -clockwiseRotation);
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

        private static void SetScale(RectTransform rect, float scale)
        {
            if (rect != null)
            {
                rect.localScale = new Vector3(scale, scale, 1f);
            }
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
