using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning.C16
{
    public sealed class C16ToC18SequenceController : MonoBehaviour
    {
        [Header("장면")]
        [SerializeField] private CanvasGroup c16;
        [SerializeField] private CanvasGroup c17;
        [SerializeField] private CanvasGroup c18LampOff;
        [SerializeField] private CanvasGroup c18LampOn;

        [Header("진행")]
        [SerializeField, Min(0f)] private float initialBlackHold = 0.8f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.9f;
        [SerializeField, Min(0f)] private float sceneHold = 1.7f;
        [SerializeField, Min(0f)] private float blackHold = 0.75f;

        [Header("C18 가로등")]
        [SerializeField, Min(0.1f)] private float lampTurnOnDuration = 0.55f;
        [SerializeField] private Vector2 flickerInterval = new(1.2f, 2.6f);

        [Header("다음 장면")]
        [SerializeField] private string nextSceneName = "Burning_C19_Playable";

        public string CurrentChapter { get; private set; } = "C16";
        public string CurrentState { get; private set; } = "검은 화면";

        private int startChapter = 16;

        private void Start()
        {
            string requestedChapter = BurningChapterDebugRequest.Consume();
            if (requestedChapter.Length == 3
                && int.TryParse(requestedChapter.Substring(1), out int requestedNumber)
                && requestedNumber is >= 16 and <= 18)
            {
                startChapter = requestedNumber;
            }

            SetAlpha(c16, 0f);
            SetAlpha(c17, 0f);
            SetAlpha(c18LampOff, 0f);
            SetAlpha(c18LampOn, 0f);
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            yield return new WaitForSecondsRealtime(initialBlackHold);
            if (startChapter <= 16)
            {
                yield return ShowThenBlack(c16, "C15", "C16", "C17");
            }

            if (startChapter <= 17)
            {
                yield return ShowThenBlack(c17, "C16", "C17", "C18");
            }

            BurningChapterSettings.ResolvedTiming c17ToC18 = BurningChapterSettings.Resolve(
                "C17", "C18", fadeDuration, blackHold, fadeDuration);
            CurrentChapter = "C18";
            CurrentState = "가로등 꺼진 골목";
            yield return Fade(c18LampOff, 0f, 1f, c17ToC18.FadeInDuration);
            yield return new WaitForSecondsRealtime(0.65f);
            CurrentState = "가로등 켜짐";
            yield return Fade(c18LampOn, 0f, 1f, lampTurnOnDuration);
            CurrentState = "가로등 깜빡임";
            StartCoroutine(FlickerLamp());
            yield return new WaitForSecondsRealtime(sceneHold);
            CurrentState = "C19 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();
            yield return LoadC19();
        }

        private IEnumerator LoadC19()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName)
                || !Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"C19 씬을 불러올 수 없음: {nextSceneName}", this);
                yield break;
            }

            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C18", "C19", 0f, 0f, 0f);
            yield return FadePair(c18LampOff, c18LampOn, timing.FadeOutDuration);
            if (timing.BlackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(timing.BlackHoldDuration);
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator ShowThenBlack(
            CanvasGroup scene,
            string previousChapter,
            string chapter,
            string nextChapter)
        {
            BurningChapterSettings.ResolvedTiming incoming = BurningChapterSettings.Resolve(
                previousChapter, chapter, fadeDuration, blackHold, fadeDuration);
            BurningChapterSettings.ResolvedTiming outgoing = BurningChapterSettings.Resolve(
                chapter, nextChapter, fadeDuration, blackHold, fadeDuration);
            CurrentChapter = chapter;
            CurrentState = $"{chapter} 장면";
            yield return Fade(scene, 0f, 1f, incoming.FadeInDuration);
            yield return new WaitForSecondsRealtime(sceneHold);
            CurrentState = $"{nextChapter} 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();
            CurrentState = "검은 화면";
            yield return Fade(scene, 1f, 0f, outgoing.FadeOutDuration);
            SetAlpha(scene, 0f);
            if (outgoing.BlackHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(outgoing.BlackHoldDuration);
            }
        }

        private static IEnumerator FadePair(CanvasGroup first, CanvasGroup second, float duration)
        {
            float firstStart = first != null ? first.alpha : 0f;
            float secondStart = second != null ? second.alpha : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetAlpha(first, Mathf.LerpUnclamped(firstStart, 0f, t));
                SetAlpha(second, Mathf.LerpUnclamped(secondStart, 0f, t));
                yield return null;
            }

            SetAlpha(first, 0f);
            SetAlpha(second, 0f);
        }

        private IEnumerator FlickerLamp()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(flickerInterval.x, flickerInterval.y));
                int pulseCount = Random.Range(1, 4);

                for (int pulse = 0; pulse < pulseCount; pulse++)
                {
                    float dimAlpha = Random.Range(0.05f, 0.32f);
                    yield return Fade(c18LampOn, c18LampOn.alpha, dimAlpha, Random.Range(0.04f, 0.09f));
                    yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.13f));
                    yield return Fade(c18LampOn, dimAlpha, 1f, Random.Range(0.07f, 0.14f));

                    if (pulse < pulseCount - 1)
                    {
                        yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.1f));
                    }
                }
            }
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                group.alpha = Mathf.LerpUnclamped(from, to, eased);
                yield return null;
            }

            group.alpha = to;
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
