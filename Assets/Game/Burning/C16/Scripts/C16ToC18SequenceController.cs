using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

        private bool c18Ready;
        private bool transitionStarted;

        private void Start()
        {
            SetAlpha(c16, 0f);
            SetAlpha(c17, 0f);
            SetAlpha(c18LampOff, 0f);
            SetAlpha(c18LampOn, 0f);
            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            if (!c18Ready || transitionStarted)
            {
                return;
            }

            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touchPressed = Touchscreen.current != null
                && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (mousePressed || touchPressed)
            {
                LoadC19();
            }
        }

        private IEnumerator PlaySequence()
        {
            yield return new WaitForSecondsRealtime(initialBlackHold);
            yield return ShowThenBlack(c16, "C16");
            yield return ShowThenBlack(c17, "C17");

            CurrentChapter = "C18";
            CurrentState = "가로등 꺼진 골목";
            yield return Fade(c18LampOff, 0f, 1f, fadeDuration);
            yield return new WaitForSecondsRealtime(0.65f);
            CurrentState = "가로등 켜짐";
            yield return Fade(c18LampOn, 0f, 1f, lampTurnOnDuration);
            CurrentState = "가로등 깜빡임";
            StartCoroutine(FlickerLamp());
            c18Ready = true;
        }

        private void LoadC19()
        {
            transitionStarted = true;
            if (string.IsNullOrWhiteSpace(nextSceneName)
                || !Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"C19 씬을 불러올 수 없음: {nextSceneName}", this);
                transitionStarted = false;
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator ShowThenBlack(CanvasGroup scene, string chapter)
        {
            CurrentChapter = chapter;
            CurrentState = $"{chapter} 장면";
            yield return Fade(scene, 0f, 1f, fadeDuration);
            yield return new WaitForSecondsRealtime(sceneHold);
            CurrentState = "검은 화면";
            yield return Fade(scene, 1f, 0f, fadeDuration);
            SetAlpha(scene, 0f);
            yield return new WaitForSecondsRealtime(blackHold);
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
