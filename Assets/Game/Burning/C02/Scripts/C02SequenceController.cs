using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C02
{
    public sealed class C02SequenceController : MonoBehaviour
    {
        public event Action Finished;

        [Header("장면")]
        [SerializeField] private Image sceneImage;
        [SerializeField] private Sprite jongsuSprite;
        [SerializeField] private Sprite jongsuLookSprite;
        [SerializeField] private RectTransform sceneRect;
        [SerializeField] private Vector2 jongsuFramePosition;
        [SerializeField] private Vector2 warehouseFramePosition = new(-720f, 0f);

        [Header("상자 게임")]
        [SerializeField] private CanvasGroup stackGameGroup;
        [SerializeField] private C02BoxStackGame stackGame;
        [SerializeField] private CanvasGroup darkness;

        [Header("시간")]
        [SerializeField, Min(0f)] private float openingHold = 0.8f;
        [SerializeField, Min(0.01f)] private float cutDuration = 0.76f;
        [SerializeField, Range(0f, 1f)] private float warehouseDarkness = 0.38f;
        [SerializeField, Min(0.01f)] private float gameFadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float completionHold = 0.45f;
        [SerializeField, Min(0f)] private float lookDelay = 0.12f;
        [SerializeField] private bool playOnStart = true;

        [Header("단독 씬 진행")]
        [SerializeField] private string standaloneNextSceneName = "Burning_C04_Playable";
        [SerializeField, Min(0f)] private float standaloneNextDelay = 0.35f;

        [Header("장면 신호")]
        [SerializeField] private UnityEvent onWarehouseShown;
        [SerializeField] private UnityEvent onStackCompleted;
        [SerializeField] private UnityEvent onJongsuLooksAtCamera;

        private Coroutine sequenceRoutine;
        private bool waitingForBoxes;

        private void OnEnable()
        {
            if (stackGame != null)
            {
                stackGame.Completed += HandleStackCompleted;
            }
        }

        private void Start()
        {
            PrepareOpening();

            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (stackGame != null)
            {
                stackGame.Completed -= HandleStackCompleted;
            }
        }

        public void Play()
        {
            StopSequence();
            PrepareOpening();
            sequenceRoutine = StartCoroutine(PlayRoutine());
        }

        public void StopSequence()
        {
            waitingForBoxes = false;

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (stackGame != null)
            {
                stackGame.SetInteraction(false);
            }
        }

        private void PrepareOpening()
        {
            if (sceneImage != null && jongsuSprite != null)
            {
                sceneImage.sprite = jongsuSprite;
            }

            if (sceneRect != null)
            {
                sceneRect.anchoredPosition = jongsuFramePosition;
            }

            SetGroup(stackGameGroup, 0f, false);
            SetGroup(darkness, 0f, false);
        }

        private IEnumerator PlayRoutine()
        {
            yield return new WaitForSecondsRealtime(openingHold);

            yield return AnimateFrame(
                jongsuFramePosition,
                warehouseFramePosition,
                0f,
                0f,
                cutDuration);

            onWarehouseShown?.Invoke();
            yield return FadeGroupsTogether(
                darkness,
                warehouseDarkness,
                stackGameGroup,
                1f,
                gameFadeDuration);
            SetGroup(stackGameGroup, 1f, true);

            if (stackGame == null)
            {
                Debug.LogError("C02 장면 진행에 Box Stack Game 연결 필요", this);
                sequenceRoutine = null;
                yield break;
            }

            waitingForBoxes = true;
            stackGame.Begin();
            sequenceRoutine = null;
        }

        private void HandleStackCompleted()
        {
            if (!waitingForBoxes)
            {
                return;
            }

            waitingForBoxes = false;
            onStackCompleted?.Invoke();
            sequenceRoutine = StartCoroutine(ReturnToJongsuRoutine());
        }

        private IEnumerator ReturnToJongsuRoutine()
        {
            yield return new WaitForSecondsRealtime(completionHold);
            yield return FadeGroup(stackGameGroup, 0f, gameFadeDuration);
            SetGroup(stackGameGroup, 0f, false);

            yield return AnimateFrame(
                warehouseFramePosition,
                jongsuFramePosition,
                warehouseDarkness,
                0f,
                cutDuration);

            if (sceneImage != null && jongsuLookSprite != null)
            {
                sceneImage.sprite = jongsuLookSprite;
            }

            yield return new WaitForSecondsRealtime(lookDelay);

            onJongsuLooksAtCamera?.Invoke();
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
                Debug.LogError($"C02 다음 씬을 불러올 수 없음: {standaloneNextSceneName}", this);
                sequenceRoutine = null;
                yield break;
            }

            SceneManager.LoadScene(standaloneNextSceneName);
        }

        private IEnumerator AnimateFrame(
            Vector2 fromPosition,
            Vector2 toPosition,
            float fromDarkness,
            float toDarkness,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (sceneRect != null)
                {
                    sceneRect.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, t);
                }

                if (darkness != null)
                {
                    darkness.alpha = Mathf.LerpUnclamped(fromDarkness, toDarkness, t);
                }

                yield return null;
            }

            if (sceneRect != null)
            {
                sceneRect.anchoredPosition = toPosition;
            }

            if (darkness != null)
            {
                darkness.alpha = toDarkness;
            }
        }

        private static IEnumerator FadeGroup(CanvasGroup group, float target, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                group.alpha = Mathf.LerpUnclamped(start, target, t);
                yield return null;
            }

            group.alpha = target;
        }

        private static IEnumerator FadeGroupsTogether(
            CanvasGroup first,
            float firstTarget,
            CanvasGroup second,
            float secondTarget,
            float duration)
        {
            float firstStart = first != null ? first.alpha : 0f;
            float secondStart = second != null ? second.alpha : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (first != null)
                {
                    first.alpha = Mathf.LerpUnclamped(firstStart, firstTarget, t);
                }

                if (second != null)
                {
                    second.alpha = Mathf.LerpUnclamped(secondStart, secondTarget, t);
                }

                yield return null;
            }

            if (first != null)
            {
                first.alpha = firstTarget;
            }

            if (second != null)
            {
                second.alpha = secondTarget;
            }
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool interactable)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
    }
}
