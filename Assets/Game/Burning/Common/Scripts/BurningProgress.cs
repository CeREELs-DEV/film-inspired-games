using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning
{
    public static class BurningProgress
    {
        private const string LatestChapterKey = "FilmInspiredGames.Burning.LatestChapter";

        private static readonly string[] ChapterOrder =
        {
            "C01", "C02", "C03", "C04",
            "C06", "C07", "C08", "C09",
            "C10", "C11", "C12", "C13",
            "C14", "C15", "C16", "C17",
            "C18", "C19"
        };

        public static bool HasSavedGame => PlayerPrefs.HasKey(LatestChapterKey);

        public static string LatestChapter =>
            HasSavedGame ? PlayerPrefs.GetString(LatestChapterKey, "C01") : "C01";

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(LatestChapterKey);
            PlayerPrefs.Save();
        }

        public static void SaveChapter(string chapter)
        {
            int nextIndex = Array.IndexOf(ChapterOrder, chapter);
            if (nextIndex < 0)
            {
                return;
            }

            int savedIndex = HasSavedGame
                ? Array.IndexOf(ChapterOrder, LatestChapter)
                : -1;
            if (nextIndex < savedIndex)
            {
                return;
            }

            PlayerPrefs.SetString(LatestChapterKey, chapter);
            PlayerPrefs.Save();
        }

        public static bool LoadSavedGame()
        {
            if (!HasSavedGame)
            {
                return false;
            }

            string chapter = LatestChapter;
            string sceneName = GetSceneName(chapter);
            if (string.IsNullOrEmpty(sceneName)
                || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"저장된 장면을 불러올 수 없음: {chapter} / {sceneName}");
                return false;
            }

            if (IsGroupedChapter(chapter))
            {
                BurningChapterDebugRequest.Set(chapter);
            }
            else
            {
                BurningChapterDebugRequest.Clear();
            }

            SceneManager.LoadScene(sceneName);
            return true;
        }

        private static string GetSceneName(string chapter)
        {
            if (chapter is "C01" or "C02" or "C03" or "C04")
            {
                return "Burning_Act1_Playable";
            }

            if (chapter is "C06" or "C07")
            {
                return "Burning_C06_C07_Playable";
            }

            if (chapter is "C08" or "C09" or "C10" or "C11" or "C12")
            {
                return "Burning_C08_C12_Playable";
            }

            if (chapter == "C13")
            {
                return "Burning_C13_Playable";
            }

            if (chapter == "C14")
            {
                return "Burning_C14_Playable";
            }

            if (chapter == "C15")
            {
                return "Burning_C15_Playable";
            }

            if (chapter is "C16" or "C17" or "C18")
            {
                return "Burning_C16_C18_Playable";
            }

            return chapter == "C19" ? "Burning_C19_Playable" : string.Empty;
        }

        private static bool IsGroupedChapter(string chapter)
        {
            return chapter is "C01" or "C02" or "C03" or "C04"
                or "C06" or "C07"
                or "C08" or "C09" or "C10" or "C11" or "C12"
                or "C16" or "C17" or "C18";
        }
    }

    public sealed class BurningProgressTracker : MonoBehaviour
    {
        private float nextReadTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BurningProgressTracker>() != null)
            {
                return;
            }

            GameObject trackerObject = new("BurningProgressTracker");
            DontDestroyOnLoad(trackerObject);
            trackerObject.AddComponent<BurningProgressTracker>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextReadTime)
            {
                return;
            }

            nextReadTime = Time.unscaledTime + 0.25f;
            string controllerName = GetControllerTypeName(SceneManager.GetActiveScene().name);
            if (string.IsNullOrEmpty(controllerName))
            {
                return;
            }

            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.GetType().Name != controllerName)
                {
                    continue;
                }

                PropertyInfo property = behaviour.GetType().GetProperty(
                    "CurrentChapter", BindingFlags.Instance | BindingFlags.Public);
                if (property?.PropertyType != typeof(string))
                {
                    continue;
                }

                BurningProgress.SaveChapter(property.GetValue(behaviour) as string);
                return;
            }
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
    }
}
