using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C19
{
    public sealed class C19BlackoutController : MonoBehaviour
    {
        [Header("장면")]
        [SerializeField] private CanvasGroup clothes;
        [SerializeField] private CanvasGroup switchOn;
        [SerializeField] private CanvasGroup switchOff;
        [SerializeField] private CanvasGroup black;

        [Header("입력")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button switchButton;

        [Header("진행")]
        [SerializeField, Min(0.1f)] private float revealDuration = 0.65f;
        [SerializeField, Min(0f)] private float switchOffHoldDuration = 0.12f;

        public string CurrentChapter => "C19";
        public string CurrentState { get; private set; } = "옷장";

        private int step;
        private Coroutine transitionRoutine;
        private bool blackoutComplete;

        private void Start()
        {
            SetAlpha(clothes, 0f);
            SetAlpha(switchOn, 0f);
            SetAlpha(switchOff, 0f);
            SetAlpha(black, 0f);

            Image blackImage = black.GetComponent<Image>();
            blackImage.sprite = null;
            blackImage.color = Color.black;

            advanceButton.onClick.AddListener(Advance);
            switchButton.onClick.AddListener(TurnOff);
            switchButton.interactable = false;
        }

        private void OnDestroy()
        {
            advanceButton?.onClick.RemoveListener(Advance);
            switchButton?.onClick.RemoveListener(TurnOff);
        }

        private void Advance()
        {
            if (transitionRoutine != null || blackoutComplete)
            {
                return;
            }

            if (step == 0)
            {
                transitionRoutine = StartCoroutine(Reveal(clothes, "바닥의 옷"));
                return;
            }

            if (step == 1)
            {
                transitionRoutine = StartCoroutine(RevealSwitch());
            }
        }

        private void TurnOff()
        {
            if (step != 2 || transitionRoutine != null || blackoutComplete)
            {
                return;
            }

            transitionRoutine = StartCoroutine(Blackout());
        }

        private IEnumerator Reveal(CanvasGroup group, string state)
        {
            CurrentState = state;
            yield return Fade(group, 0f, 1f, revealDuration);
            step++;
            transitionRoutine = null;
        }

        private IEnumerator RevealSwitch()
        {
            CurrentState = "불 켜진 스위치";
            yield return Fade(switchOn, 0f, 1f, revealDuration);
            step = 2;
            advanceButton.interactable = false;
            advanceButton.image.raycastTarget = false;
            switchButton.interactable = true;
            transitionRoutine = null;
        }

        private IEnumerator Blackout()
        {
            switchButton.interactable = false;
            CurrentState = "스위치 끔";
            SetAlpha(switchOn, 0f);
            SetAlpha(switchOff, 1f);

            // 눌린 상태 인지 후 소등
            yield return new WaitForSecondsRealtime(switchOffHoldDuration);

            SetAlpha(black, 1f);
            CurrentState = "검은 화면";
            blackoutComplete = true;
            transitionRoutine = null;
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
