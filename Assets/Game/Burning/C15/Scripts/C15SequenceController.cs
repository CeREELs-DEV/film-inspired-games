using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning.C15
{
    public sealed class C15SequenceController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] cuts;
        [SerializeField, Min(0f)] private float initialBlackHold = 0.8f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.85f;
        [SerializeField, Min(0f)] private float cutHold = 1.45f;
        [SerializeField, Min(0f)] private float blackHold = 0.7f;
        [SerializeField] private string nextSceneName = "Burning_C16_C18_Playable";

        public string CurrentChapter => "C15";
        public string CurrentState { get; private set; } = "검은 화면";

        private void Start()
        {
            foreach (CanvasGroup cut in cuts)
            {
                SetAlpha(cut, 0f);
            }

            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            yield return new WaitForSecondsRealtime(initialBlackHold);

            for (int index = 0; index < cuts.Length; index++)
            {
                CanvasGroup cut = cuts[index];
                if (cut == null)
                {
                    continue;
                }

                CurrentState = $"컷 {index + 1} 등장";
                yield return Fade(cut, 0f, 1f, fadeDuration);
                yield return new WaitForSecondsRealtime(cutHold);

                if (index == cuts.Length - 1)
                {
                    BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                        "C15", "C16", fadeDuration, blackHold, 0.9f);
                    CurrentState = "마지막 컷";
                    yield return new WaitForSecondsRealtime(0.4f);
                    CurrentState = "C16 이동 대기";
                    yield return BurningContinuePrompt.WaitForContinue();
                    CurrentState = "검은 화면";
                    yield return Fade(cut, 1f, 0f, timing.FadeOutDuration);
                    SetAlpha(cut, 0f);
                    if (timing.BlackHoldDuration > 0f)
                    {
                        yield return new WaitForSecondsRealtime(timing.BlackHoldDuration);
                    }

                    if (string.IsNullOrWhiteSpace(nextSceneName)
                        || !Application.CanStreamedLevelBeLoaded(nextSceneName))
                    {
                        Debug.LogError($"C16-C18 씬을 불러올 수 없음: {nextSceneName}", this);
                        yield break;
                    }

                    SceneManager.LoadScene(nextSceneName);
                    yield break;
                }

                CurrentState = "검은 화면";
                yield return Fade(cut, 1f, 0f, fadeDuration);
                SetAlpha(cut, 0f);
                yield return new WaitForSecondsRealtime(blackHold);
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
