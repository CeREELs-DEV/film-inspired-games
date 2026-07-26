using System.Linq;
using FilmInspiredGames.Burning.C04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C04.Editor
{
    public static class C04PlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Burning/C04/Scenes/Burning_C04_Playable.unity";
        private const string ArtPath = "Assets/Game/Burning/C04/Art/";
        private const float SourceWidth = 1632f;
        private const float SourceHeight = 2912f;

        [MenuItem("Tools/Burning/Build C04 Playable Scene")]
        public static void BuildAndOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("재생을 멈춘 뒤 C04 씬을 다시 생성하세요.");
                return;
            }

            PrepareSprites();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Burning_C04_Playable";

            CreateCameraAndLight();
            Canvas canvas = CreateCanvas();
            CreateEventSystem();
            GameObject systems = new("Systems");
            GameObject sequenceRoot = new("ChapterSequences");
            sequenceRoot.transform.SetParent(systems.transform);
            RectTransform c04Root = CreateRoot("C04", canvas.transform);

            CreateFullScreenImage("Background", c04Root, LoadSprite("C04_Background.png"));
            CreateSourceAlignedImage(
                "HandlePanel", c04Root, LoadSprite("C04_HandlePanel.png"), new SourceLayout(331, 1728, 956, 762));
            Image handle = CreateSourceAlignedImage(
                "Handle", c04Root, LoadSprite("C04_Handle.png"), new SourceLayout(633, 2053, 368, 79));
            Image capsuleUp = CreateFullScreenImage("CapsuleUp", c04Root, LoadSprite("C04_CapsuleUp.png"));
            Image capsuleDown = CreateFullScreenImage("CapsuleDown", c04Root, LoadSprite("C04_CapsuleDown.png"));
            Image watch = CreateFullScreenImage("Watch", c04Root, LoadSprite("C04_Watch.png"));

            CanvasGroup capsuleUpGroup = capsuleUp.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup capsuleDownGroup = capsuleDown.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup watchGroup = watch.gameObject.AddComponent<CanvasGroup>();

            GameObject controllerObject = new("C04Sequence");
            controllerObject.transform.SetParent(sequenceRoot.transform);
            C04RewardSequenceController controller = controllerObject.AddComponent<C04RewardSequenceController>();
            ConfigureController(controller, handle, capsuleUp, capsuleDown, watch, capsuleUpGroup, capsuleDownGroup, watchGroup);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = controllerObject;
            Debug.Log("C04 플레이 씬 생성 완료. Play 버튼을 누르면 보상 연출이 시작됩니다.");
        }

        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int missingScripts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            C04RewardSequenceController controller = Object.FindFirstObjectByType<C04RewardSequenceController>();
            SerializedObject serialized = controller != null ? new SerializedObject(controller) : null;
            Object handle = serialized?.FindProperty("handleRect")?.objectReferenceValue;

            if (missingScripts > 0 || controller == null || handle == null)
            {
                throw new System.InvalidOperationException(
                    $"C04 씬 검사 실패: missingScripts={missingScripts}, controller={controller != null}, handle={handle != null}");
            }

            Debug.Log("C04 씬 검사 완료: 손잡이 연결 정상, 누락 스크립트 0개");
        }

        private static void ConfigureController(
            C04RewardSequenceController controller,
            Image handle,
            Image capsuleUp,
            Image capsuleDown,
            Image watch,
            CanvasGroup capsuleUpGroup,
            CanvasGroup capsuleDownGroup,
            CanvasGroup watchGroup)
        {
            SerializedObject serialized = new(controller);
            serialized.FindProperty("handleRect").objectReferenceValue = handle.rectTransform;
            serialized.FindProperty("handleInteractionRadius").floatValue = 110f;
            serialized.FindProperty("requiredHandleRotation").floatValue = 540f;
            serialized.FindProperty("handleDownwardSensitivity").floatValue = 0.65f;
            serialized.FindProperty("handleMaxInputSpeed").floatValue = 360f;
            serialized.FindProperty("handleVisualFollowSpeed").floatValue = 300f;
            serialized.FindProperty("capsuleUp").objectReferenceValue = capsuleUpGroup;
            serialized.FindProperty("capsuleDown").objectReferenceValue = capsuleDownGroup;
            serialized.FindProperty("capsuleUpRect").objectReferenceValue = capsuleUp.rectTransform;
            serialized.FindProperty("capsuleDownRect").objectReferenceValue = capsuleDown.rectTransform;
            serialized.FindProperty("watch").objectReferenceValue = watchGroup;
            serialized.FindProperty("watchRect").objectReferenceValue = watch.rectTransform;
            serialized.FindProperty("standaloneNextSceneName").stringValue = "Burning_C06_C07_Playable";
            ConfigureTiming(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != ScenePath)
                .ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureTiming(SerializedObject serialized)
        {
            serialized.FindProperty("capsuleAppearDuration").floatValue = 0.68f;
            serialized.FindProperty("capsuleScalePop").floatValue = 0.075f;
            serialized.FindProperty("capsuleSettleDip").floatValue = 0.018f;
            serialized.FindProperty("capsuleDropDistance").floatValue = 68f;
            serialized.FindProperty("capsuleLandingSquash").floatValue = 0.09f;
            serialized.FindProperty("openAnticipationDuration").floatValue = 0.13f;
            serialized.FindProperty("openAnticipationSquash").floatValue = 0.1f;
            serialized.FindProperty("capsuleOpenDuration").floatValue = 0.46f;
            serialized.FindProperty("watchAppearDelay").floatValue = 0.08f;
            serialized.FindProperty("watchFadeDuration").floatValue = 0.42f;
            serialized.FindProperty("watchStartScale").floatValue = 0.56f;
            serialized.FindProperty("watchRiseDistance").floatValue = 42f;
            serialized.FindProperty("watchScalePop").floatValue = 0.13f;
            serialized.FindProperty("watchStartRotation").floatValue = -7f;
            serialized.FindProperty("watchSettleDuration").floatValue = 0.32f;
        }

        private static void CreateCameraAndLight()
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject lightObject = new("Main Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540f, 960f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Image CreateFullScreenImage(string name, Transform parent, Sprite sprite)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSourceAlignedImage(
            string name,
            Transform parent,
            Sprite sprite,
            SourceLayout layout)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(
                layout.Width * 540f / SourceWidth,
                layout.Height * 960f / SourceHeight);
            rect.anchoredPosition = new Vector2(
                (layout.X + layout.Width * 0.5f) * 540f / SourceWidth,
                -(layout.Y + layout.Height * 0.5f) * 960f / SourceHeight);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRoot(string name, Transform parent)
        {
            GameObject root = new(name, typeof(RectTransform));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PrepareSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtPath.TrimEnd('/') });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + fileName);
        }

        private readonly struct SourceLayout
        {
            public SourceLayout(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }
        }
    }
}
