using System.Collections.Generic;
using System.Linq;
using FilmInspiredGames.Burning.C16;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C16.Editor
{
    public static class C16ToC18PlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Burning/C16/Scenes/Burning_C16_C18_Playable.unity";
        private const string ArtPath = "Assets/Game/Burning/C16/Art/";
        private const string ControllerPath = "Assets/Game/Burning/C16/Scripts/C16ToC18SequenceController.cs";

        [MenuItem("Tools/Burning/Build C16-C18 Playable Scene")]
        public static void BuildAndOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("재생을 멈춘 뒤 C16-C18 씬을 다시 생성하세요.");
                return;
            }

            PrepareSprites();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Burning_C16_C18_Playable";
            CreateCamera();
            Canvas canvas = CreateCanvas();
            CreateBlackBackground(canvas.transform);
            CanvasGroup c16 = CreateImage("C16", canvas.transform, LoadSprite("C16_NightStreet.png"));
            CanvasGroup c17 = CreateImage("C17", canvas.transform, LoadSprite("C17_HoldingHands.png"));
            CanvasGroup c18Off = CreateImage("C18_LampOff", canvas.transform, LoadSprite("C18_LampOff.png"));
            CanvasGroup c18On = CreateImage("C18_LampOn", canvas.transform, LoadSprite("C18_LampOn.png"));

            C16ToC18SequenceController controller = canvas.gameObject.AddComponent<C16ToC18SequenceController>();
            SerializedObject serialized = new(controller);
            MonoScript controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(ControllerPath);
            if (controllerScript == null)
            {
                throw new System.InvalidOperationException("C16-C18 컨트롤러 스크립트를 찾을 수 없음");
            }
            serialized.FindProperty("m_Script").objectReferenceValue = controllerScript;
            serialized.FindProperty("c16").objectReferenceValue = c16;
            serialized.FindProperty("c17").objectReferenceValue = c17;
            serialized.FindProperty("c18LampOff").objectReferenceValue = c18Off;
            serialized.FindProperty("c18LampOn").objectReferenceValue = c18On;
            serialized.FindProperty("nextSceneName").stringValue = "Burning_C19_Playable";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = canvas.gameObject;
            Debug.Log("C16-C18 플레이 씬 생성 완료: 장면 사이 검정, C18 가로등 깜빡임");
        }

        [MenuItem("Tools/Burning/Validate C16-C18 Playable Scene")]
        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform[] transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();
            int missingScripts = transforms
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            string[] requiredObjects = { "C16", "C17", "C18_LampOff", "C18_LampOn" };
            bool hasAllImages = requiredObjects.All(name => transforms.Any(item => item.name == name));

            if (missingScripts > 0 || !hasAllImages)
            {
                throw new System.InvalidOperationException(
                    $"C16-C18 씬 검사 실패: missingScripts={missingScripts}, images={hasAllImages}");
            }

            Debug.Log("C16-C18 씬 검사 완료: 파싱 정상, 이미지 연결 정상, 누락 스크립트 0개");
        }

        private static CanvasGroup CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject imageObject = new(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            Stretch(rect, parent);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;

            CanvasGroup group = imageObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static void CreateBlackBackground(Transform parent)
        {
            GameObject backgroundObject = new("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = backgroundObject.GetComponent<RectTransform>();
            Stretch(rect, parent);
            Image image = backgroundObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
