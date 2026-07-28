using System;
using System.Collections;
using FilmInspiredGames.Burning.C02;
using FilmInspiredGames.Burning.C04;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning
{
    public sealed class BurningAct1FlowController : MonoBehaviour
    {
        private enum Step
        {
            Title,
            C01,
            C01ToC02,
            C02Playing,
            C02Complete,
            C02ToC03,
            C03First,
            C03ToSecond,
            C03Second,
            C03ToC04,
            C04Playing,
            C04Complete,
            C04ToC06,
            Complete
        }

        [Header("챕터 화면")]
        [SerializeField] private CanvasGroup c01Group;
        [SerializeField] private CanvasGroup c02Group;
        [SerializeField] private CanvasGroup c03Group;
        [SerializeField] private GameObject c03FirstFrame;
        [SerializeField] private GameObject c03SecondFrame;
        [SerializeField] private CanvasGroup c04Group;

        [Header("화면 전환")]
        [SerializeField] private CanvasGroup transitionFade;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.65f;
        [SerializeField, Min(0f)] private float fadeHoldDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.8f;

        [Header("C02")]
        [SerializeField] private C02SequenceController c02Sequence;

        [Header("C04")]
        [SerializeField] private C04RewardSequenceController c04Sequence;

        [Header("다음 장면")]
        [SerializeField] private string nextSceneName = "Burning_C06_C07_Playable";
        [SerializeField, Min(0.01f)] private float nextSceneFadeDuration = 1.5f;
        [SerializeField, Min(0f)] private float nextSceneFadeHold = 0.2f;

        [Header("장면 신호")]
        [SerializeField] private UnityEvent onC01Started;
        [SerializeField] private UnityEvent onC02Started;
        [SerializeField] private UnityEvent onC03Started;
        [SerializeField] private UnityEvent onC04Started;
        [SerializeField] private UnityEvent onActCompleted;

        private Step currentStep = Step.Title;
        private Coroutine transitionRoutine;

        public event Action ActCompleted;

        public string CurrentChapter => currentStep switch
        {
            Step.C01 or Step.C01ToC02 => "C01",
            Step.C02Playing or Step.C02Complete or Step.C02ToC03 => "C02",
            Step.C03First or Step.C03ToSecond or Step.C03Second or Step.C03ToC04 => "C03",
            Step.C04Playing or Step.C04Complete or Step.C04ToC06 => "C04",
            Step.Complete => "완료",
            _ => "-"
        };

        public string CurrentState => currentStep switch
        {
            Step.C01 => "C01 화면",
            Step.C01ToC02 => "C01 → C02 전환",
            Step.C02Playing => "C02 상자 배치 진행 중",
            Step.C02Complete => "C02 완료",
            Step.C02ToC03 => "C02 → C03 전환",
            Step.C03First => "C03 첫 번째 화면",
            Step.C03ToSecond => "C03 화면 전환",
            Step.C03Second => "C03 두 번째 화면",
            Step.C03ToC04 => "C03 → C04 전환",
            Step.C04Playing => c04Sequence != null ? c04Sequence.CurrentState : "C04 보상 연출 진행 중",
            Step.C04Complete => "C04 보상 연출 완료",
            Step.C04ToC06 => "C04 → C06 전환",
            Step.Complete => "1막 완료",
            _ => "-"
        };

        private void OnEnable()
        {
            if (c02Sequence != null)
            {
                c02Sequence.Finished += HandleC02Finished;
            }

            if (c04Sequence != null)
            {
                c04Sequence.Finished += HandleC04Finished;
            }
        }

        private void Start()
        {
            string requestedChapter = BurningChapterDebugRequest.Consume();
            if (string.IsNullOrEmpty(requestedChapter))
            {
                PrepareTitle();
                BurningTitleScreen.Show(this);
                return;
            }

            ShowC01();
            if (requestedChapter is "C02" or "C03" or "C04")
            {
                StartCoroutine(StartDebugChapter(requestedChapter));
            }
        }

        private IEnumerator StartDebugChapter(string chapter)
        {
            yield return null;

            switch (chapter)
            {
                case "C02":
                    ShowC02();
                    break;
                case "C03":
                    ShowC03();
                    break;
                case "C04":
                    ShowC04();
                    break;
            }
        }

        private void OnDisable()
        {
            if (c02Sequence != null)
            {
                c02Sequence.Finished -= HandleC02Finished;
            }

            if (c04Sequence != null)
            {
                c04Sequence.Finished -= HandleC04Finished;
            }
        }

        public void Advance()
        {
            BurningContinuePrompt.Hide();
            switch (currentStep)
            {
                case Step.C01:
                    transitionRoutine = StartCoroutine(TransitionToC02());
                    break;
                case Step.C02Complete:
                    transitionRoutine = StartCoroutine(TransitionToC03First());
                    break;
                case Step.C03First:
                    transitionRoutine = StartCoroutine(TransitionToC03Second());
                    break;
                case Step.C03Second:
                    transitionRoutine = StartCoroutine(TransitionToC04());
                    break;
                case Step.C04Complete:
                    transitionRoutine = StartCoroutine(TransitionToC06());
                    break;
            }
        }

        public void HandleScreenClick()
        {
            if (currentStep == Step.C03First)
            {
                Advance();
            }
        }

        public void Restart()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            ShowC01();
        }

        public void StartNewGameFromTitle()
        {
            ShowC01();
        }

        private void PrepareTitle()
        {
            currentStep = Step.Title;
            SetGroup(c01Group, false);
            SetGroup(c02Group, false);
            SetGroup(c03Group, false);
            SetGroup(c04Group, false);
            SetC03Frame(false, false);
            c02Sequence?.StopSequence();
            c04Sequence?.StopSequence();
            SetFade(0f);
            BurningContinuePrompt.Hide();
        }

        private void ShowC01()
        {
            currentStep = Step.C01;
            SetGroup(c01Group, true);
            SetGroup(c02Group, false);
            SetGroup(c03Group, false);
            SetGroup(c04Group, false);
            SetC03Frame(false, false);
            c02Sequence?.StopSequence();
            c04Sequence?.StopSequence();
            SetFade(0f);
            onC01Started?.Invoke();
            BurningContinuePrompt.Show(Advance);
        }

        private IEnumerator TransitionToC02()
        {
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C01", "C02", fadeOutDuration, fadeHoldDuration, fadeInDuration);
            currentStep = Step.C01ToC02;
            yield return FadeTo(1f, timing.FadeOutDuration);
            yield return Wait(timing.BlackHoldDuration);
            ShowC02();
            yield return FadeTo(0f, timing.FadeInDuration);
            transitionRoutine = null;
        }

        private void ShowC02()
        {
            BurningContinuePrompt.Hide();
            currentStep = Step.C02Playing;
            SetGroup(c01Group, false);
            SetGroup(c02Group, true);
            SetGroup(c03Group, false);
            SetGroup(c04Group, false);
            onC02Started?.Invoke();
            c02Sequence?.Play();
        }

        private void ShowC03()
        {
            currentStep = Step.C03First;
            SetGroup(c01Group, false);
            SetGroup(c02Group, false);
            SetGroup(c03Group, true);
            SetGroup(c04Group, false);
            SetC03Frame(true, false);
            c02Sequence?.StopSequence();
            c04Sequence?.StopSequence();
            SetFade(0f);
            onC03Started?.Invoke();
            BurningContinuePrompt.Hide();
        }

        private void ShowC04()
        {
            BurningContinuePrompt.Hide();
            currentStep = Step.C04Playing;
            SetGroup(c01Group, false);
            SetGroup(c02Group, false);
            SetGroup(c03Group, false);
            SetGroup(c04Group, true);
            SetC03Frame(false, false);
            c02Sequence?.StopSequence();
            SetFade(0f);
            onC04Started?.Invoke();
            c04Sequence?.Play();
        }

        private void HandleC02Finished()
        {
            if (currentStep == Step.C02Playing)
            {
                currentStep = Step.C02Complete;
                BurningContinuePrompt.Show(Advance);
            }
        }

        private IEnumerator TransitionToC03First()
        {
            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C02", "C03", fadeOutDuration, fadeHoldDuration, fadeInDuration);
            currentStep = Step.C02ToC03;
            yield return FadeTo(1f, timing.FadeOutDuration);
            yield return Wait(timing.BlackHoldDuration);
            SetGroup(c01Group, false);
            SetGroup(c02Group, false);
            SetGroup(c03Group, true);
            SetGroup(c04Group, false);
            SetC03Frame(true, false);
            onC03Started?.Invoke();
            yield return FadeTo(0f, timing.FadeInDuration);
            currentStep = Step.C03First;
            transitionRoutine = null;
        }

        private IEnumerator TransitionToC03Second()
        {
            currentStep = Step.C03ToSecond;
            yield return FadeTo(1f, fadeOutDuration);
            yield return new WaitForSecondsRealtime(fadeHoldDuration);
            SetC03Frame(false, true);
            yield return FadeTo(0f, fadeInDuration);
            currentStep = Step.C03Second;
            transitionRoutine = null;
            BurningContinuePrompt.Show(Advance);
        }

        private IEnumerator TransitionToC04()
        {
            if (c04Group == null || c04Sequence == null)
            {
                Debug.LogError("C04 화면 연결 필요. Burning 1막 플레이 씬을 다시 생성하세요.", this);
                transitionRoutine = null;
                yield break;
            }

            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C03", "C04", fadeOutDuration, fadeHoldDuration, fadeInDuration);
            currentStep = Step.C03ToC04;
            yield return FadeTo(1f, timing.FadeOutDuration);
            yield return Wait(timing.BlackHoldDuration);
            SetGroup(c01Group, false);
            SetGroup(c02Group, false);
            SetGroup(c03Group, false);
            SetGroup(c04Group, true);
            onC04Started?.Invoke();
            yield return FadeTo(0f, timing.FadeInDuration);
            currentStep = Step.C04Playing;
            c04Sequence.Play();
            transitionRoutine = null;
        }

        private void HandleC04Finished()
        {
            if (currentStep == Step.C04Playing)
            {
                currentStep = Step.C04Complete;
                BurningContinuePrompt.Show(Advance);
            }
        }

        private IEnumerator TransitionToC06()
        {
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"다음 씬을 불러올 수 없습니다: {nextSceneName}", this);
                transitionRoutine = null;
                yield break;
            }

            BurningChapterSettings.ResolvedTiming timing = BurningChapterSettings.Resolve(
                "C04", "C06", nextSceneFadeDuration, nextSceneFadeHold, 0f);
            currentStep = Step.C04ToC06;
            yield return FadeTo(1f, timing.FadeOutDuration);
            yield return Wait(timing.BlackHoldDuration);
            CompleteAct();
            SceneManager.LoadScene(nextSceneName);
        }

        private void CompleteAct()
        {
            currentStep = Step.Complete;
            onActCompleted?.Invoke();
            ActCompleted?.Invoke();
        }

        private void SetC03Frame(bool firstVisible, bool secondVisible)
        {
            if (c03FirstFrame != null)
            {
                c03FirstFrame.SetActive(firstVisible);
            }

            if (c03SecondFrame != null)
            {
                c03SecondFrame.SetActive(secondVisible);
            }
        }

        private static void SetGroup(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (transitionFade == null)
            {
                yield break;
            }

            float start = transitionFade.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transitionFade.alpha = Mathf.LerpUnclamped(start, target, t);
                yield return null;
            }

            transitionFade.alpha = target;
        }

        private void SetFade(float alpha)
        {
            if (transitionFade != null)
            {
                transitionFade.alpha = alpha;
            }
        }

        private static IEnumerator Wait(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
        }
    }
}
