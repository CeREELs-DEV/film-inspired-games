using System.Collections.Generic;
using System.Linq;
using FilmInspiredGames.Burning.C19;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C19.Editor
{
    public static class C19PlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Burning/C19/Scenes/Burning_C19_Playable.unity";
        private const string ArtPath = "Assets/Game/Burning/C19/Art/";
        private const string ControllerPath = "Assets/Game/Burning/C19/Scripts/C19BlackoutController.cs";

        [MenuItem("Tools/Burning/Build C19 Playable Scene")]
        public static void BuildAndOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("재생을 멈춘 뒤 C19 씬을 다시 생성하세요.");
                return;
            }

            PrepareSprites();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Burning_C19_Playable";
            CreateCamera();
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            CreateImage("Background", canvas.transform, LoadSprite("C19_Background.png"), false);
            CanvasGroup clothes = CreateImage("Clothes", canvas.transform, LoadSprite("C19_Clothes.png"), true);
            Button advanceButton = CreateButton("AdvanceSurface", canvas.transform, Vector2.zero, Vector2.one);
            CanvasGroup switchOn = CreateImage("SwitchOn", canvas.transform, LoadSprite("C19_SwitchOn.png"), true);
            CanvasGroup switchOff = CreateImage("SwitchOff", canvas.transform, LoadSprite("C19_SwitchOff.png"), true);
            Button switchButton = CreateButton(
                "SwitchHitArea", canvas.transform, new Vector2(0.4f, 0.52f), new Vector2(0.62f, 0.73f));
            CanvasGroup black = CreateImage("Black", canvas.transform, LoadSprite("C19_Black.png"), true);
            black.GetComponent<Image>().sprite = null;
            black.GetComponent<Image>().color = Color.black;

            C19BlackoutController controller = canvas.gameObject.AddComponent<C19BlackoutController>();
            SerializedObject serialized = new(controller);
            MonoScript controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(ControllerPath);
            if (controllerScript == null)
            {
                throw new System.InvalidOperationException("C19 컨트롤러 스크립트를 찾을 수 없음");
            }

            serialized.FindProperty("m_Script").objectReferenceValue = controllerScript;
            serialized.FindProperty("clothes").objectReferenceValue = clothes;
            serialized.FindProperty("switchOn").objectReferenceValue = switchOn;
            serialized.FindProperty("switchOff").objectReferenceValue = switchOff;
            serialized.FindProperty("black").objectReferenceValue = black;
            serialized.FindProperty("advanceButton").objectReferenceValue = advanceButton;
            serialized.FindProperty("switchButton").objectReferenceValue = switchButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("C19 플레이 씬 생성 완료: 옷장과 스위치 진행, 스위치 소등 즉시 검정");
        }

        [MenuItem("Tools/Burning/Validate C19 Playable Scene")]
        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform[] transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();
            int missingScripts = transforms
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            string[] requiredObjects =
                { "Background", "Clothes", "SwitchOn", "SwitchOff", "SwitchHitArea", "Black" };
            bool hasAllObjects = requiredObjects.All(name => transforms.Any(item => item.name == name));

            if (missingScripts > 0 || !hasAllObjects)
            {
                throw new System.InvalidOperationException(
                    $"C19 씬 검사 실패: missingScripts={missingScripts}, objects={hasAllObjects}");
            }

            Debug.Log("C19 씬 검사 완료: 이미지와 버튼 연결 정상, 누락 스크립트 0개");
        }

        private static CanvasGroup CreateImage(string name, Transform parent, Sprite sprite, bool addGroup)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            Stretch(rect, parent);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;

            if (!addGroup)
            {
                return null;
            }

            CanvasGroup group = imageObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static Button CreateButton(
            string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new(
                "Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540f, 960f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
        }

        private static void Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void PrepareSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtPath.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.compressionQuality = 100;
                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + fileName);
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(item => item.path != ScenePath)
                .ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
