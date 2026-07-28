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
        private const string TitleRequest = "TITLE";
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

        private static readonly string[] AvailableChapterNames =
        {
            "C01", "C02", "C03", "C04",
            "C06", "C07", "C08", "C09",
            "C10", "C11", "C12", "C13",
            "C14", "C15", "C16", "C17",
            "C18", "C19"
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
        private int savedChapterIndex;
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
            window.minSize = new Vector2(360f, 430f);
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
            EditorApplication.delayCall += () =>
            {
                if (pendingChapter == TitleRequest)
                {
                    StartTitle();
                    return;
                }

                StartChapter(pendingChapter);
            };
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            bool titleVisible = IsTitleVisible();
            string currentChapter = titleVisible
                ? "Title"
                : GetCurrentValue("CurrentChapter", GetChapterFromSceneName());
            string currentState = titleVisible
                ? "Title Screen"
                : GetCurrentValue("CurrentState", Application.isPlaying ? "-" : "Idle");

            EditorGUILayout.LabelField("Current Progress", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Playable", SceneManager.GetActiveScene().name);
            EditorGUILayout.LabelField("Chapter", currentChapter);
            EditorGUILayout.LabelField("State", currentState);

            EditorGUILayout.Space(8f);
            DrawTitleAndSaveControls();

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

        private void DrawTitleAndSaveControls()
        {
            bool hasSave = BurningProgress.HasSavedGame;
            string latestChapter = hasSave ? BurningProgress.LatestChapter : "-";

            EditorGUILayout.LabelField("Title & Save", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Saved", hasSave ? "Yes" : "No");
            EditorGUILayout.LabelField("Latest", latestChapter);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Title", GUILayout.Height(28f)))
                {
                    RequestTitle();
                }

                using (new EditorGUI.DisabledScope(!hasSave))
                {
                    if (GUILayout.Button("Continue Saved", GUILayout.Height(28f)))
                    {
                        RequestChapterName(latestChapter);
                    }
                }
            }

            savedChapterIndex = EditorGUILayout.Popup(
                "Saved Chapter",
                savedChapterIndex,
                AvailableChapterNames);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Set Save"))
                    {
                        BurningProgress.Reset();
                        BurningProgress.SaveChapter(AvailableChapterNames[savedChapterIndex]);
                    }

                    using (new EditorGUI.DisabledScope(!hasSave))
                    {
                        if (GUILayout.Button("Clear Save"))
                        {
                            BurningProgress.Reset();
                        }
                    }
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "저장 위치 변경과 초기화는 플레이 종료 후 사용",
                    MessageType.Info);
            }
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
            RequestChapterName($"C{chapter:00}");
        }

        private static void RequestChapterName(string chapterName)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(PendingChapterKey, chapterName);
                EditorApplication.isPlaying = false;
                return;
            }

            StartChapter(chapterName);
        }

        private static void RequestTitle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetString(PendingChapterKey, TitleRequest);
                EditorApplication.isPlaying = false;
                return;
            }

            StartTitle();
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

        private static void StartTitle()
        {
            const string titleScenePath =
                "Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity";

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BurningChapterDebugRequest.Clear();
            EditorSceneManager.OpenScene(titleScenePath);
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

        private static bool IsTitleVisible()
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            BurningTitleScreen titleScreen =
                FindFirstObjectByType<BurningTitleScreen>(FindObjectsInactive.Include);
            return titleScreen != null && titleScreen.isActiveAndEnabled;
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
