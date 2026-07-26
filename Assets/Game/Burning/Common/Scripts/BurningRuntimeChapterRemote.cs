using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FilmInspiredGames.Burning
{
    public sealed class BurningRuntimeChapterRemote : MonoBehaviour
    {
        private static readonly int[] Chapters =
        {
            1, 2, 3, 4,
            6, 7, 8, 9,
            10, 11, 12, 13,
            14, 15, 16, 17,
            18, 19
        };

        private const float ReferenceWidth = 540f;
        private const float ReferenceHeight = 960f;

        private bool visible;
        private string currentChapter = "-";
        private float nextChapterReadTime;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle infoStyle;
        private GUIStyle buttonStyle;
        private GUIStyle currentButtonStyle;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D currentButtonTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BurningRuntimeChapterRemote>() != null)
            {
                return;
            }

            GameObject remoteObject = new("BurningChapterRemote");
            DontDestroyOnLoad(remoteObject);
            remoteObject.AddComponent<BurningRuntimeChapterRemote>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.backquoteKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame)
            {
                visible = !visible;
                if (visible)
                {
                    RefreshCurrentChapter();
                }
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            EnsureStyles();
            if (Time.unscaledTime >= nextChapterReadTime)
            {
                RefreshCurrentChapter();
            }

            float scale = Mathf.Max(0.55f, Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight));
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float screenWidth = Screen.width / scale;
            float screenHeight = Screen.height / scale;
            const float panelWidth = 420f;
            const float panelHeight = 374f;
            Rect panelRect = new(
                (screenWidth - panelWidth) * 0.5f,
                (screenHeight - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 22f, panelRect.y + 18f, panelWidth - 44f, 28f),
                "BURNING CHAPTERS", titleStyle);
            GUI.Label(new Rect(panelRect.x + 22f, panelRect.y + 50f, panelWidth - 44f, 24f),
                $"CURRENT  {currentChapter}", infoStyle);

            const int columns = 4;
            const float buttonWidth = 86f;
            const float buttonHeight = 44f;
            const float gap = 10f;
            float startX = panelRect.x + 23f;
            float startY = panelRect.y + 88f;
            for (int index = 0; index < Chapters.Length; index++)
            {
                string chapter = $"C{Chapters[index]:00}";
                int row = index / columns;
                int column = index % columns;
                Rect buttonRect = new(
                    startX + column * (buttonWidth + gap),
                    startY + row * (buttonHeight + gap),
                    buttonWidth,
                    buttonHeight);
                GUIStyle style = chapter == currentChapter ? currentButtonStyle : buttonStyle;
                if (GUI.Button(buttonRect, chapter, style))
                {
                    LoadChapter(Chapters[index]);
                }
            }

            GUI.matrix = previousMatrix;
        }

        private void LoadChapter(int chapter)
        {
            string chapterName = $"C{chapter:00}";
            string sceneName = chapter switch
            {
                >= 1 and <= 4 => "Burning_Act1_Playable",
                6 or 7 => "Burning_C06_C07_Playable",
                >= 8 and <= 12 => "Burning_C08_C12_Playable",
                13 => "Burning_C13_Playable",
                14 => "Burning_C14_Playable",
                15 => "Burning_C15_Playable",
                >= 16 and <= 18 => "Burning_C16_C18_Playable",
                19 => "Burning_C19_Playable",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"챕터 장면을 불러올 수 없음: {chapterName} / {sceneName}", this);
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

            visible = false;
            SceneManager.LoadScene(sceneName);
        }

        private void RefreshCurrentChapter()
        {
            nextChapterReadTime = Time.unscaledTime + 0.25f;
            string preferredType = GetControllerTypeName(SceneManager.GetActiveScene().name);
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null
                    || behaviour == this
                    || (!string.IsNullOrEmpty(preferredType) && behaviour.GetType().Name != preferredType))
                {
                    continue;
                }

                PropertyInfo property = behaviour.GetType().GetProperty(
                    "CurrentChapter", BindingFlags.Instance | BindingFlags.Public);
                if (property?.PropertyType == typeof(string))
                {
                    currentChapter = property.GetValue(behaviour) as string ?? "-";
                    return;
                }
            }

            currentChapter = GetChapterFromSceneName();
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
                : "-";
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelTexture = CreateTexture(new Color(0.07f, 0.075f, 0.08f, 0.96f));
            buttonTexture = CreateTexture(new Color(0.19f, 0.2f, 0.21f, 1f));
            buttonHoverTexture = CreateTexture(new Color(0.28f, 0.3f, 0.31f, 1f));
            currentButtonTexture = CreateTexture(new Color(0.12f, 0.38f, 0.5f, 1f));

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.94f, 0.94f, 0.92f) }
            };
            infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.63f, 0.66f, 0.67f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = buttonTexture, textColor = new Color(0.9f, 0.9f, 0.88f) },
                hover = { background = buttonHoverTexture, textColor = Color.white },
                active = { background = currentButtonTexture, textColor = Color.white }
            };
            currentButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { background = currentButtonTexture, textColor = Color.white },
                hover = { background = currentButtonTexture, textColor = Color.white },
                active = { background = currentButtonTexture, textColor = Color.white }
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            Destroy(panelTexture);
            Destroy(buttonTexture);
            Destroy(buttonHoverTexture);
            Destroy(currentButtonTexture);
        }
    }
}
