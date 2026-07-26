using System;
using System.Collections.Generic;
using UnityEngine;

namespace FilmInspiredGames.Burning
{
    [CreateAssetMenu(fileName = "BurningChapterSettings", menuName = "Burning/Chapter Settings")]
    public sealed class BurningChapterSettings : ScriptableObject
    {
        [Serializable]
        public sealed class TransitionTiming
        {
            [SerializeField] private string fromChapter;
            [SerializeField] private string toChapter;
            [SerializeField, Min(0f)] private float fadeOutDuration;
            [SerializeField, Min(0f)] private float blackHoldDuration;
            [SerializeField, Min(0f)] private float fadeInDuration;

            public string FromChapter => fromChapter;
            public string ToChapter => toChapter;
            public float FadeOutDuration => fadeOutDuration;
            public float BlackHoldDuration => blackHoldDuration;
            public float FadeInDuration => fadeInDuration;

            public TransitionTiming(
                string fromChapter,
                string toChapter,
                float fadeOutDuration,
                float blackHoldDuration,
                float fadeInDuration)
            {
                this.fromChapter = fromChapter;
                this.toChapter = toChapter;
                this.fadeOutDuration = fadeOutDuration;
                this.blackHoldDuration = blackHoldDuration;
                this.fadeInDuration = fadeInDuration;
            }
        }

        public readonly struct ResolvedTiming
        {
            public float FadeOutDuration { get; }
            public float BlackHoldDuration { get; }
            public float FadeInDuration { get; }

            public ResolvedTiming(float fadeOutDuration, float blackHoldDuration, float fadeInDuration)
            {
                FadeOutDuration = fadeOutDuration;
                BlackHoldDuration = blackHoldDuration;
                FadeInDuration = fadeInDuration;
            }
        }

        private const string ResourceName = "BurningChapterSettings";

        [SerializeField] private List<TransitionTiming> transitions = new();

        private static BurningChapterSettings cached;

        public static BurningChapterSettings Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<BurningChapterSettings>(ResourceName);
            }

            return cached;
        }

        public static ResolvedTiming Resolve(
            string fromChapter,
            string toChapter,
            float fallbackFadeOut,
            float fallbackBlackHold,
            float fallbackFadeIn)
        {
            BurningChapterSettings settings = Load();
            if (settings != null)
            {
                foreach (TransitionTiming transition in settings.transitions)
                {
                    if (transition.FromChapter == fromChapter && transition.ToChapter == toChapter)
                    {
                        return new ResolvedTiming(
                            transition.FadeOutDuration,
                            transition.BlackHoldDuration,
                            transition.FadeInDuration);
                    }
                }
            }

            return new ResolvedTiming(fallbackFadeOut, fallbackBlackHold, fallbackFadeIn);
        }

        public void ResetToDefaults()
        {
            transitions = new List<TransitionTiming>
            {
                New("C01", "C02", 0.65f, 0.18f, 0.8f),
                New("C02", "C03", 0.65f, 0.18f, 0.8f),
                New("C03", "C04", 0.65f, 0.18f, 0.8f),
                New("C04", "C06", 0.65f, 0.18f, 0.8f),
                New("C06", "C07", 0.65f, 0.18f, 0.8f),
                New("C07", "C08", 0.65f, 0.18f, 0.8f),
                New("C08", "C09", 0.65f, 0.18f, 0.8f),
                New("C09", "C10", 0.65f, 0.18f, 0.8f),
                New("C10", "C11", 0.65f, 0.18f, 0.8f),
                New("C11", "C12", 0.65f, 0.18f, 0.8f),
                New("C12", "C13", 0.65f, 0.18f, 0.8f),
                New("C13", "C14", 0.65f, 0.18f, 0.8f),
                New("C14", "C15", 0.65f, 0.18f, 0.8f),
                New("C15", "C16", 0.65f, 0.18f, 0.8f),
                New("C16", "C17", 0.65f, 0.18f, 0.8f),
                New("C17", "C18", 0.65f, 0.18f, 0.8f),
                New("C18", "C19", 0.65f, 0.18f, 0.8f)
            };
        }

        private static TransitionTiming New(string from, string to, float fadeOut, float hold, float fadeIn)
        {
            return new TransitionTiming(from, to, fadeOut, hold, fadeIn);
        }
    }
}
