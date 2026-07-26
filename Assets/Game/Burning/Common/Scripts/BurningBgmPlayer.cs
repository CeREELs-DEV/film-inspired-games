using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning
{
    public sealed class BurningBgmPlayer : MonoBehaviour
    {
        private static readonly IReadOnlyDictionary<string, string> ClipNames =
            new Dictionary<string, string>
            {
                ["C01"] = "C01_Lonely_Walk_Loop",
                ["C02"] = "C02_Warehouse_Routine_Loop",
                ["C03"] = "C03_Recognition_Loop",
                ["C04"] = "C04_Capsule_Prize_Loop",
                ["C06"] = "C06_Broken_Memory_Puzzle_Loop",
                ["C07"] = "C07_Realization_Loop",
                ["C08"] = "C08_Only_Two_Loop",
                ["C09"] = "C09_Cigarette_Light_Loop",
                ["C10"] = "C10_Side_Glance_Loop",
                ["C11"] = "C11_The_Watch_Gift_Loop",
                ["C12"] = "C12_Her_Smile_Loop",
                ["C13"] = "C13_Time_On_Her_Wrist_Loop",
                ["C14"] = "C14_Bongcheon_Drinks_Loop",
                ["C15"] = "C15_Almost_A_Kiss_Loop",
                ["C16"] = "C16_Night_Street_Loop",
                ["C17"] = "C17_Holding_Hands_Loop",
                ["C18"] = "C18_Flickering_Lamp_Loop",
                ["C19"] = "C19_Lights_Out_Loop"
            };

        private const string ResourceFolder = "BurningBGM/";
        private const float ChapterReadInterval = 0.1f;
        private const float CrossFadeDuration = 1.5f;
        private const float MusicVolume = 0.72f;

        private AudioSource firstSource;
        private AudioSource secondSource;
        private AudioSource currentSource;
        private string requestedChapter = string.Empty;
        private string currentChapter = string.Empty;
        private float nextChapterReadTime;
        private int requestVersion;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BurningBgmPlayer>() != null)
            {
                return;
            }

            GameObject playerObject = new("BurningBGM");
            DontDestroyOnLoad(playerObject);
            playerObject.AddComponent<BurningBgmPlayer>();
        }

        private void Awake()
        {
            firstSource = CreateSource();
            secondSource = CreateSource();
        }

        private void Start()
        {
            RefreshChapter();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextChapterReadTime)
            {
                return;
            }

            RefreshChapter();
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private void RefreshChapter()
        {
            nextChapterReadTime = Time.unscaledTime + ChapterReadInterval;
            string chapter = ReadCurrentChapter();
            if (!ClipNames.ContainsKey(chapter) || chapter == requestedChapter)
            {
                return;
            }

            requestedChapter = chapter;
            requestVersion++;
            StartCoroutine(SwitchMusic(chapter, requestVersion));
        }

        private IEnumerator SwitchMusic(string chapter, int version)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>(
                ResourceFolder + ClipNames[chapter]);
            yield return request;

            AudioClip clip = request.asset as AudioClip;
            if (clip == null)
            {
                Debug.LogError($"BGM을 불러올 수 없음: {chapter}");
                yield break;
            }

            if (version != requestVersion)
            {
                UnloadIfUnused(clip);
                yield break;
            }

            AudioSource incoming = currentSource == firstSource ? secondSource : firstSource;
            ReleaseSource(incoming);
            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();

            AudioSource outgoing = currentSource;
            float outgoingStartVolume = outgoing != null ? outgoing.volume : 0f;
            float elapsed = 0f;
            while (elapsed < CrossFadeDuration && version == requestVersion)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / CrossFadeDuration));
                incoming.volume = Mathf.LerpUnclamped(0f, MusicVolume, progress);
                if (outgoing != null)
                {
                    outgoing.volume = Mathf.LerpUnclamped(
                        outgoingStartVolume, 0f, progress);
                }

                yield return null;
            }

            if (version != requestVersion)
            {
                yield break;
            }

            incoming.volume = MusicVolume;
            ReleaseSource(outgoing);
            currentSource = incoming;
            currentChapter = chapter;
            Debug.Log($"BGM 재생: {currentChapter} / {clip.name}", this);
        }

        private string ReadCurrentChapter()
        {
            string preferredType = GetControllerTypeName(SceneManager.GetActiveScene().name);
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null
                    || behaviour == this
                    || (!string.IsNullOrEmpty(preferredType)
                        && behaviour.GetType().Name != preferredType))
                {
                    continue;
                }

                PropertyInfo property = behaviour.GetType().GetProperty(
                    "CurrentChapter", BindingFlags.Instance | BindingFlags.Public);
                if (property?.PropertyType != typeof(string))
                {
                    continue;
                }

                string chapter = property.GetValue(behaviour) as string;
                if (!string.IsNullOrEmpty(chapter) && ClipNames.ContainsKey(chapter))
                {
                    return chapter;
                }
            }

            return GetChapterFromSceneName();
        }

        private static string GetControllerTypeName(string sceneName)
        {
            return sceneName switch
            {
                "Burning_Act1_Playable" => "BurningAct1FlowController",
                "Burning_C06_C07_Playable" => "C06C07SequenceController",
                "Burning_C08_C12_Playable" => "C08ToC12SequenceController",
                "Burning_C13_Playable" => "C13WatchMemoryController",
                "Burning_C14_Playable" => "C14DrinkingSequenceController",
                "Burning_C15_Playable" => "C15SequenceController",
                "Burning_C16_C18_Playable" => "C16ToC18SequenceController",
                "Burning_C19_Playable" => "C19BlackoutController",
                _ => string.Empty
            };
        }

        private static string GetChapterFromSceneName()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            int marker = sceneName.IndexOf("_C", StringComparison.Ordinal);
            return marker >= 0 && sceneName.Length >= marker + 4
                ? sceneName.Substring(marker + 1, 3)
                : string.Empty;
        }

        private void ReleaseSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            AudioClip clip = source.clip;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            UnloadIfUnused(clip);
        }

        private void UnloadIfUnused(AudioClip clip)
        {
            if (clip == null
                || firstSource != null && firstSource.clip == clip
                || secondSource != null && secondSource.clip == clip)
            {
                return;
            }

            Resources.UnloadAsset(clip);
        }

        private void OnDestroy()
        {
            ReleaseSource(firstSource);
            ReleaseSource(secondSource);
        }
    }
}
