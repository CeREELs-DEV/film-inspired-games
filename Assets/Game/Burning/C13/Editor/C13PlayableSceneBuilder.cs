using System.Collections.Generic;
using System.Linq;
using FilmInspiredGames.Burning.C13;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C13.Editor
{
    public static class C13PlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Burning/C13/Scenes/Burning_C13_Playable.unity";
        private const string ArtPath = "Assets/Game/Burning/C13/Art/";
        private const float SourceWidth = 1632f;
        private const float SourceHeight = 2913f;
        private static readonly Vector2 WatchCenterSource = new(591.3f, 410.2f);

        [MenuItem("Tools/Burning/Build C13 Playable Scene")]
        public static void BuildAndOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("재생을 멈춘 뒤 C13 씬을 다시 생성하세요.");
                return;
            }

            PrepareSprites();
            EnsureFolder("Assets/Game/Burning/C13/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Burning_C13_Playable";
            CreateCamera();
            Canvas canvas = CreateCanvas();
            RectTransform root = CreateRoot("C13", canvas.transform);

            CanvasGroup walk = CreateFrame("WalkMemory", root, "C13_Walk.png");
            CanvasGroup glance1 = CreateFrame("GlanceMemory1", root, "C13_Glance1.png");
            CanvasGroup glance2 = CreateFrame("GlanceMemory2", root, "C13_Glance2.png");
            CanvasGroup stop = CreateFrame("StoppedFeet", root, "C13_Stop.png");
            Image stopMark1Image = CreateSourceAlignedImage(
                "StopMark1", root, "C13_StopMark1.png", new SourceLayout(1016, 1913, 60, 48));
            Image stopMark2Image = CreateSourceAlignedImage(
                "StopMark2", root, "C13_StopMark2.png", new SourceLayout(829, 1947, 136, 188));
            CanvasGroup stopMark1 = stopMark1Image.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup stopMark2 = stopMark2Image.gameObject.AddComponent<CanvasGroup>();
            CanvasGroup pub = CreateFrame("BongcheondongPub", root, "C13_Pub.png");

            CreateFullScreenImage("WristAndWatch", root, LoadSprite("C13_WristAndWatch.png"));
            RectTransform watchCenter = CreateWatchCenter(root);
            Image minute = CreateRotatableHand(
                "MinuteHand", root, "C13_MinuteHand.png", new SourceLayout(594, 364, 96, 48));
            Image hour = CreateRotatableHand(
                "HourHand", root, "C13_HourHand.png", new SourceLayout(591, 413, 24, 48));
            Image transitionImage = CreateFullScreenImage("TransitionToC14", root, null);
            transitionImage.color = Color.black;
            CanvasGroup transitionOverlay = transitionImage.gameObject.AddComponent<CanvasGroup>();
            transitionOverlay.alpha = 0f;

            C13WatchMemoryController controller = root.gameObject.AddComponent<C13WatchMemoryController>();
            SerializedObject serialized = new(controller);
            Set(serialized, "minuteHand", minute.rectTransform);
            Set(serialized, "hourHand", hour.rectTransform);
            Set(serialized, "watchCenter", watchCenter);
            Set(serialized, "rotateHintSprite", LoadSprite("C13_RotateHint.png"));
            Set(serialized, "walkImage", walk);
            Set(serialized, "glance1Image", glance1);
            Set(serialized, "glance2Image", glance2);
            Set(serialized, "stopImage", stop);
            Set(serialized, "stopMark1", stopMark1);
            Set(serialized, "stopMark2", stopMark2);
            Set(serialized, "pubImage", pub);
            Set(serialized, "transitionOverlay", transitionOverlay);
            serialized.FindProperty("nextSceneName").stringValue = "Burning_C14_Playable";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = root.gameObject;
            Debug.Log("C13 플레이 씬 생성 완료. 시곗바늘 주변을 드래그하면 기억 이미지가 순서대로 나타납니다.");
        }

        private static RectTransform CreateWatchCenter(Transform parent)
        {
            GameObject centerObject = new("WatchCenter", typeof(RectTransform));
            RectTransform rect = centerObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one;
            rect.anchoredPosition = new Vector2(
                WatchCenterSource.x * 540f / SourceWidth,
                -WatchCenterSource.y * 960f / SourceHeight);
            return rect;
        }

        private static Image CreateRotatableHand(
            string name,
            Transform parent,
            string fileName,
            SourceLayout layout)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(
                (WatchCenterSource.x - layout.X) / layout.Width,
                1f - (WatchCenterSource.y - layout.Y) / layout.Height);
            rect.sizeDelta = new Vector2(layout.Width * 540f / SourceWidth, layout.Height * 960f / SourceHeight);
            rect.anchoredPosition = new Vector2(
                WatchCenterSource.x * 540f / SourceWidth,
                -WatchCenterSource.y * 960f / SourceHeight);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = LoadSprite(fileName);
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSourceAlignedImage(
            string name,
            Transform parent,
            string fileName,
            SourceLayout layout)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(layout.Width * 540f / SourceWidth, layout.Height * 960f / SourceHeight);
            rect.anchoredPosition = new Vector2(
                (layout.X + layout.Width * 0.5f) * 540f / SourceWidth,
                -(layout.Y + layout.Height * 0.5f) * 960f / SourceHeight);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = LoadSprite(fileName);
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static CanvasGroup CreateFrame(string name, Transform parent, string fileName)
        {
            return CreateFullScreenImage(name, parent, LoadSprite(fileName)).gameObject.AddComponent<CanvasGroup>();
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

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
            GameObject cameraObject = new("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void PrepareSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtPath.TrimEnd('/') });
            foreach (string guid in guids)
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
                importer.maxTextureSize = path.Contains("Hand.png")
                    || path.Contains("Mark")
                    || path.Contains("Hint")
                    ? 512
                    : 4096;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }
                current = next;
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + fileName);
        }

        private static void Set(SerializedObject serialized, string property, Object value)
        {
            serialized.FindProperty(property).objectReferenceValue = value;
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
