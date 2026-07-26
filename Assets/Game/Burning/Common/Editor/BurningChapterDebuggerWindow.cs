using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning.Editor
{
    public sealed class BurningChapterDebuggerWindow : EditorWindow
    {
        private const string PendingChapterKey = "FilmInspiredGames.Burning.PendingDebugChapter";
        private const string SettingsFolderPath = "Assets/Game/Burning/Resources";
        private const string SettingsAssetPath = SettingsFolderPath + "/BurningChapterSettings.asset";

        private static readonly int[] AvailableChapters =
        {
            1, 2, 3, 4,
            6, 7, 8, 9,
            10, 11, 12, 13,
            14, 15, 16, 17,
            18, 19
        };

        private static readonly string[] ScenePaths =
        {
            "",
            "Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity",
            "Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity",
            "Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity",
            "Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity",
            "",
            "Assets/Game/Burning/C06/Scenes/Burning_C06_C07_Playable.unity",
            "Assets/Game/Burning/C06/Scenes/Burning_C06_C07_Playable.unity",
            "Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity",
            "Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity",
            "Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity",
            "Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity",
            "Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity",
            "Assets/Game/Burning/C13/Scenes/Burning_C13_Playable.unity",
            "Assets/Game/Burning/C14/Scenes/Burning_C14_Playable.unity",
            "Assets/Game/Burning/C15/Scenes/Burning_C15_Playable.unity",
            "Assets/Game/Burning/C16/Scenes/Burning_C16_C18_Playable.unity",
            "Assets/Game/Burning/C16/Scenes/Burning_C16_C18_Playable.unity",
            "Assets/Game/Burning/C16/Scenes/Burning_C16_C18_Playable.unity",
            "Assets/Game/Burning/C19/Scenes/Burning_C19_Playable.unity"
        };

        private Vector2 scrollPosition;
        private bool settingsExpanded;
        private UnityEditor.Editor settingsEditor;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.delayCall += EnsureSettingsAsset;
        }

        [MenuItem("Tools/Burning/Chapter Debugger")]
        public static void Open()
        {
            BurningChapterDebuggerWindow window = GetWindow<BurningChapterDebuggerWindow>();
            window.titleContent = new GUIContent("Burning Chapters");
            window.minSize = new Vector2(360f, 310f);
            window.Show();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Open();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            string pendingChapter = SessionState.GetString(PendingChapterKey, string.Empty);
            if (string.IsNullOrEmpty(pendingChapter))
            {
                return;
            }

            SessionState.EraseString(PendingChapterKey);
            EditorApplication.delayCall += () => StartChapter(pendingChapter);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            string currentChapter = GetCurrentValue("CurrentChapter", GetChapterFromSceneName());

            EditorGUILayout.LabelField("Current Progress", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Playable", SceneManager.GetActiveScene().name);
            EditorGUILayout.LabelField("Chapter", currentChapter);
            EditorGUILayout.LabelField(
                "State", GetCurrentValue("CurrentState", Application.isPlaying ? "-" : "Idle"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Start Chapter", EditorStyles.boldLabel);

            const int columns = 4;
            for (int first = 0; first < AvailableChapters.Length; first += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    int last = Mathf.Min(first + columns, AvailableChapters.Length);
                    for (int index = first; index < last; index++)
                    {
                        int chapter = AvailableChapters[index];
                        string label = $"C{chapter:00}";
                        Color previousColor = GUI.backgroundColor;
                        if (currentChapter == label)
                        {
                            GUI.backgroundColor = new Color(0.35f, 0.72f, 1f);
                        }

                        if (GUILayout.Button(label, GUILayout.Height(34f)))
                        {
                            RequestChapter(chapter);
                        }

                        GUI.backgroundColor = previousColor;
                    }
                }
            }

            EditorGUILayout.Space(10f);
            settingsExpanded = EditorGUILayout.Foldout(settingsExpanded, "Chapter Transition Timing", true);
            if (settingsExpanded)
            {
                BurningChapterSettings settings = LoadSettingsAsset();
                if (settings != null)
                {
                    EditorGUILayout.Space(3f);
                    UnityEditor.Editor.CreateCachedEditor(settings, null, ref settingsEditor);
                    settingsEditor.OnInspectorGUI();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnDisable()
        {
            if (settingsEditor != null)
            {
                DestroyImmediate(settingsEditor);
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private static void RequestChapter(int chapter)
        {
            string chapterName = $"C{chapter:00}";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(PendingChapterKey, chapterName);
                EditorApplication.isPlaying = false;
                return;
            }

            StartChapter(chapterName);
        }

        private static void EnsureSettingsAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<BurningChapterSettings>(SettingsAssetPath) != null)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(SettingsFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/Game/Burning", "Resources");
            }

            BurningChapterSettings settings = CreateInstance<BurningChapterSettings>();
            settings.ResetToDefaults();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
        }

        private static BurningChapterSettings LoadSettingsAsset()
        {
            EnsureSettingsAsset();
            return AssetDatabase.LoadAssetAtPath<BurningChapterSettings>(SettingsAssetPath);
        }

        private static void StartChapter(string chapterName)
        {
            if (!int.TryParse(chapterName.Substring(1), out int chapter)
                || chapter <= 0
                || chapter >= ScenePaths.Length
                || string.IsNullOrEmpty(ScenePaths[chapter]))
            {
                Debug.LogError($"시작할 수 없는 챕터: {chapterName}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            bool groupedChapter = chapter is >= 1 and <= 4
                || chapter is >= 6 and <= 12
                || chapter is >= 16 and <= 18;

            if (groupedChapter)
            {
                BurningChapterDebugRequest.Set(chapterName);
            }
            else
            {
                BurningChapterDebugRequest.Clear();
            }

            EditorSceneManager.OpenScene(ScenePaths[chapter]);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        private static string GetCurrentValue(string propertyName, string fallback)
        {
            if (!Application.isPlaying)
            {
                return fallback;
            }

            string preferredType = GetControllerTypeName(SceneManager.GetActiveScene().name);
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || (!string.IsNullOrEmpty(preferredType) && behaviour.GetType().Name != preferredType))
                {
                    continue;
                }

                PropertyInfo property = behaviour.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                if (property?.PropertyType == typeof(string))
                {
                    return property.GetValue(behaviour) as string ?? fallback;
                }
            }

            return fallback;
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
                : "Idle";
        }
    }
}
