using System.Collections.Generic;
using System.Linq;
using FilmInspiredGames.Burning.C14;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C14.Editor
{
    public static class C14PlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Burning/C14/Scenes/Burning_C14_Playable.unity";
        private const string Part1Path = "Assets/Game/Burning/C14/Art/Part1/";
        private const string Part2Path = "Assets/Game/Burning/C14/Art/Part2/";
        private const float SourceWidth = 1632f;
        private const float SourceHeight = 4308f;
        private const float ViewWidth = 540f;
        private const float ViewHeight = 960f;
        private const float Scale = ViewWidth / SourceWidth;

        [MenuItem("Tools/Burning/Build C14 Playable Scene")]
        public static void BuildAndOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("재생을 멈춘 뒤 C14 씬을 다시 생성하세요.");
                return;
            }

            PrepareSprites();
            EnsureFolder("Assets/Game/Burning/C14/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Burning_C14_Playable";
            CreateCamera();
            Canvas canvas = CreateCanvas();
            RectTransform viewport = CreateViewport(canvas.transform);
            RectTransform content = CreateContent(viewport);

            CreateFullContentImage("BackgroundBefore", content, LoadPart1("C14_BackgroundBefore.png"));
            CanvasGroup backgroundAfter = CreateFullContentImage(
                "BackgroundAfter", content, LoadPart1("C14_BackgroundAfter.png")).gameObject.AddComponent<CanvasGroup>();
            CanvasGroup couple = CreateLayer(
                "FacingEachOther", content, LoadPart1("C14_Couple.png"), new Layout(0, 191, 1632, 955));

            Prop bottle = CreateProp(
                "SojuBottle", content,
                LoadPart1("C14_Bottle.png"), new Layout(631, 2033, 864, 1941),
                LoadPart1("C14_BottleLiquid.png"), new Layout(1013, 2229, 436, 1622));
            Prop glass1 = CreateProp(
                "SojuGlass1", content,
                LoadPart1("C14_Glass1.png"), new Layout(557, 3344, 384, 629),
                LoadPart1("C14_Glass1Liquid.png"), new Layout(592, 3457, 317, 480));
            Prop glass2 = CreateProp(
                "SojuGlass2", content,
                LoadPart1("C14_Glass2.png"), new Layout(188, 3290, 365, 603),
                LoadPart1("C14_Glass2Liquid.png"), new Layout(221, 3401, 304, 451));

            CanvasGroup part2Set = CreateLayer(
                "PaleSojuSet", content, LoadPart2("C14_Part2_SojuSet.png"), new Layout(188, 2033, 1307, 1941));
            CanvasGroup[] cuts =
            {
                CreateLayer("Part2Cut01", content, LoadPart2("C14_Part2_Image01.png"), new Layout(67, 2128, 1505, 879)),
                CreateLayer("Part2Cut02", content, LoadPart2("C14_Part2_Image02.png"), new Layout(0, 3337, 1450, 722)),
                CreateLayer("Part2Cut03", content, LoadPart2("C14_Part2_Image03.png"), new Layout(67, 1586, 1512, 1607)),
                CreateLayer("Part2Cut04", content, LoadPart2("C14_Part2_Image04.png"), new Layout(67, 1586, 1512, 1607)),
                CreateLayer("Part2Cut05", content, LoadPart2("C14_Part2_Image05.png"), new Layout(281, 3337, 1351, 737)),
                CreateLayer("Part2Cut06", content, LoadPart2("C14_Part2_Image06.png"), new Layout(67, 1586, 1512, 1607))
            };

            C14DrinkingSequenceController controller = viewport.gameObject.AddComponent<C14DrinkingSequenceController>();
            SerializedObject serialized = new(controller);
            Set(serialized, "scrollContent", content);
            serialized.FindProperty("scrollDistance").floatValue = SourceHeight * Scale - ViewHeight;
            Set(serialized, "couple", couple);
            Set(serialized, "backgroundAfter", backgroundAfter);
            Set(serialized, "bottle", bottle.Group);
            Set(serialized, "glass1", glass1.Group);
            Set(serialized, "glass2", glass2.Group);
            Set(serialized, "bottleRect", bottle.Rect);
            Set(serialized, "glass1Rect", glass1.Rect);
            Set(serialized, "glass2Rect", glass2.Rect);
            Set(serialized, "bottleLiquid", bottle.Liquid);
            Set(serialized, "glass1Liquid", glass1.Liquid);
            Set(serialized, "glass2Liquid", glass2.Liquid);
            Set(serialized, "holdHintSprite", LoadPart1("C14_HoldHint.png"));
            Set(serialized, "part2SojuSet", part2Set);
            serialized.FindProperty("nextSceneName").stringValue = "Burning_C15_Playable";
            serialized.FindProperty("transitionToBlackDuration").floatValue = 1.1f;
            SerializedProperty cutArray = serialized.FindProperty("part2Images");
            cutArray.arraySize = cuts.Length;
            for (int i = 0; i < cuts.Length; i++)
            {
                cutArray.GetArrayElementAtIndex(i).objectReferenceValue = cuts[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = viewport.gameObject;
            Debug.Log("C14 플레이 씬 생성 완료. 클릭해 포차로 이동한 뒤 잔을 길게 눌러 술을 채우세요.");
        }

        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int missingScripts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
            C14DrinkingSequenceController controller = Object.FindFirstObjectByType<C14DrinkingSequenceController>();

            if (missingScripts > 0 || controller == null)
            {
                throw new System.InvalidOperationException(
                    $"C14 씬 검사 실패: missingScripts={missingScripts}, controller={controller != null}");
            }

            Debug.Log("C14 씬 검사 완료: 파싱 정상, 누락 스크립트 0개");
        }

        private static Prop CreateProp(
            string name,
            Transform parent,
            Sprite baseSprite,
            Layout baseLayout,
            Sprite liquidSprite,
            Layout liquidLayout)
        {
            GameObject propObject = new(name, typeof(RectTransform), typeof(CanvasGroup));
            RectTransform propRect = propObject.GetComponent<RectTransform>();
            propRect.SetParent(parent, false);
            Place(propRect, baseLayout);

            Image baseImage = CreateStretchedImage("Base", propRect, baseSprite);
            baseImage.raycastTarget = false;

            GameObject liquidObject = new("Liquid", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform liquidRect = liquidObject.GetComponent<RectTransform>();
            liquidRect.SetParent(propRect, false);
            liquidRect.anchorMin = new Vector2(0f, 1f);
            liquidRect.anchorMax = new Vector2(0f, 1f);
            liquidRect.pivot = new Vector2(0.5f, 0.5f);
            liquidRect.sizeDelta = new Vector2(liquidLayout.Width * Scale, liquidLayout.Height * Scale);
            liquidRect.anchoredPosition = new Vector2(
                (liquidLayout.X - baseLayout.X + liquidLayout.Width * 0.5f) * Scale,
                -(liquidLayout.Y - baseLayout.Y + liquidLayout.Height * 0.5f) * Scale);

            Image liquid = liquidObject.GetComponent<Image>();
            liquid.sprite = liquidSprite;
            liquid.type = Image.Type.Filled;
            liquid.fillMethod = Image.FillMethod.Vertical;
            liquid.fillOrigin = (int)Image.OriginVertical.Bottom;
            liquid.fillAmount = 0f;
            liquid.raycastTarget = false;
            return new Prop(propObject.GetComponent<CanvasGroup>(), propRect, liquid);
        }

        private static CanvasGroup CreateLayer(string name, Transform parent, Sprite sprite, Layout layout)
        {
            GameObject layerObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform rect = layerObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, layout);
            Image image = layerObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return layerObject.GetComponent<CanvasGroup>();
        }

        private static Image CreateFullContentImage(string name, Transform parent, Sprite sprite)
        {
            return CreateStretchedImage(name, parent, sprite);
        }

        private static Image CreateStretchedImage(string name, Transform parent, Sprite sprite)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, Layout layout)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(layout.Width * Scale, layout.Height * Scale);
            rect.anchoredPosition = new Vector2(
                (layout.X + layout.Width * 0.5f) * Scale,
                -(layout.Y + layout.Height * 0.5f) * Scale);
        }

        private static RectTransform CreateViewport(Transform parent)
        {
            GameObject viewportObject = new("C14", typeof(RectTransform), typeof(RectMask2D));
            RectTransform rect = viewportObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        private static RectTransform CreateContent(Transform parent)
        {
            GameObject contentObject = new("StoryCanvas", typeof(RectTransform));
            RectTransform rect = contentObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(ViewWidth, SourceHeight * Scale);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ViewWidth, ViewHeight);
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
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
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
            foreach (string folder in new[] { Part1Path, Part2Path })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') }))
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
                    importer.SaveAndReimport();
                }
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

        private static Sprite LoadPart1(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>(Part1Path + fileName);
        private static Sprite LoadPart2(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>(Part2Path + fileName);

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

        private readonly struct Layout
        {
            public Layout(float x, float y, float width, float height)
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

        private readonly struct Prop
        {
            public Prop(CanvasGroup group, RectTransform rect, Image liquid)
            {
                Group = group;
                Rect = rect;
                Liquid = liquid;
            }

            public CanvasGroup Group { get; }
            public RectTransform Rect { get; }
            public Image Liquid { get; }
        }
    }
}
