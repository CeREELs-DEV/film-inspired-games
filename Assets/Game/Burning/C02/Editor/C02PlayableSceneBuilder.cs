using System.Collections.Generic;
using System.Linq;
using FilmInspiredGames.Burning.C02;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C02.Editor
{
    [InitializeOnLoad]
    public static class C02PlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Burning/C02/Scenes/Burning_C02_Playable.unity";
        private const string AutoBuildKey = "Burning.C02.AutoBuild.1";
        private const string ArtPath = "Assets/Game/Burning/C02/Art/";
        private static readonly Vector2 LayerSize = new(1250.5f, 960f);
        private static readonly Vector2 JongsuPosition = new(326.75f, 0f);
        private static readonly Vector2 WarehousePosition = new(-324.25f, 0f);

        static C02PlayableSceneBuilder()
        {
            EditorApplication.delayCall += BuildOnceAfterImport;
        }

        [MenuItem("Tools/Burning/Build C02 Playable Scene")]
        public static void BuildAndOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("재생을 멈춘 뒤 C02 씬을 다시 생성하세요.");
                return;
            }

            BuildScene();
        }

        private static void BuildOnceAfterImport()
        {
            if (SessionState.GetBool(AutoBuildKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SessionState.SetBool(AutoBuildKey, true);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildScene();
            }
        }

        private static void BuildScene()
        {
            PrepareSprites();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Burning_C02_Playable";
            SceneManager.SetActiveScene(scene);

            CreateCameraAndLight();
            Canvas canvas = CreateCanvas();
            CreateEventSystem();
            GameObject systems = new("Systems");
            GameObject sequenceRoot = new("ChapterSequences");
            sequenceRoot.transform.SetParent(systems.transform);
            RectTransform c02Root = CreateRect("C02", canvas.transform);
            Stretch(c02Root);

            RectTransform sceneViewport = CreateRect("SceneViewport", c02Root);
            Stretch(sceneViewport);
            sceneViewport.gameObject.AddComponent<RectMask2D>();

            Image sceneImage = CreateImage("SceneImage", sceneViewport, LoadSprite("C02_Jongsu.png"));
            sceneImage.preserveAspect = false;
            SetCenteredRect(sceneImage.rectTransform, LayerSize, JongsuPosition);

            Image darknessImage = CreateImage("Darkness", c02Root, null);
            Stretch(darknessImage.rectTransform);
            darknessImage.color = Color.black;
            darknessImage.raycastTarget = false;
            CanvasGroup darkness = darknessImage.gameObject.AddComponent<CanvasGroup>();

            RectTransform stackRoot = CreateRect("StackGame", c02Root);
            Stretch(stackRoot);
            CanvasGroup stackGroup = stackRoot.gameObject.AddComponent<CanvasGroup>();

            Image shelf = CreateImage("Shelf", stackRoot, LoadSprite("C02_Shelf.png"));
            shelf.preserveAspect = false;
            shelf.raycastTarget = false;
            SetCenteredRect(shelf.rectTransform, LayerSize, WarehousePosition);

            RectTransform guidesRoot = CreateRect("PlacementGuides", stackRoot);
            Stretch(guidesRoot);
            List<Image> placementGuides = CreatePlacementGuides(guidesRoot);

            RectTransform slotsRoot = CreateRect("Slots", stackRoot);
            Stretch(slotsRoot);
            List<RectTransform> slots = CreateSlots(slotsRoot);

            RectTransform boxTray = CreateRect("Boxes", stackRoot);
            Stretch(boxTray);
            List<C02DraggableBox> boxes = CreateBoxes(boxTray);

            RectTransform dragLayer = CreateRect("DragLayer", stackRoot);
            Stretch(dragLayer);
            dragLayer.SetAsLastSibling();

            C02BoxStackGame boxGame = stackRoot.gameObject.AddComponent<C02BoxStackGame>();
            ConfigureBoxGame(boxGame, canvas, dragLayer, boxes, slots, placementGuides);

            GameObject controllerObject = new GameObject("C02Sequence");
            controllerObject.transform.SetParent(sequenceRoot.transform);
            C02SequenceController controller = controllerObject.AddComponent<C02SequenceController>();
            ConfigureSequence(controller, sceneImage, stackGroup, boxGame, darkness);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = controllerObject;

            Debug.Log("C02 플레이 씬 생성 완료. Play 버튼을 누르면 자동으로 시작됩니다.");
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

                bool changed = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || !importer.alphaIsTransparency;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
        }

        private static void CreateCameraAndLight()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.07f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject lightObject = new GameObject("Main Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

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
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static List<RectTransform> CreateSlots(Transform parent)
        {
            Vector2[] positions =
            {
                new(-119.5f, 15.5f), new(20.25f, -19.25f), new(146.25f, -20f),
                new(-119.25f, 110.75f), new(20.5f, 47.25f), new(144.25f, 45.75f),
                new(135.75f, 108.25f)
            };

            Vector2[] sizes =
            {
                new(115.5f, 135f), new(164f, 67.5f), new(80f, 66f),
                new(115f, 60.5f), new(163.5f, 65.5f), new(84f, 66.5f),
                new(97f, 57.5f)
            };

            List<RectTransform> slots = new();

            for (int index = 0; index < positions.Length; index++)
            {
                RectTransform slot = CreateRect($"Slot{index + 1:00}", parent);
                SetCenteredRect(slot, sizes[index], positions[index]);
                slots.Add(slot);
            }

            return slots;
        }

        private static List<C02DraggableBox> CreateBoxes(Transform parent)
        {
            Vector2[] positions =
            {
                new(-197.5f, -330f), new(-46f, -330f), new(88f, -330f), new(197.5f, -330f),
                new(-102.75f, -430f), new(33f, -430f), new(135.5f, -430f)
            };

            Vector2[] layerCenters =
            {
                new(204.75f, 15.5f), new(344.5f, -19.25f), new(470.5f, -20f),
                new(205f, 110.75f), new(344.75f, 47.25f), new(468.5f, 45.75f),
                new(460f, 108.25f)
            };

            Vector2[] raycastMins =
            {
                new(0.6178f, 0.4458f), new(0.7101f, 0.4448f), new(0.8445f, 0.4448f),
                new(0.6182f, 0.5839f), new(0.7105f, 0.5151f), new(0.8413f, 0.5130f),
                new(0.8293f, 0.5828f)
            };

            Vector2[] raycastMaxs =
            {
                new(0.7101f, 0.5865f), new(0.8413f, 0.5151f), new(0.9084f, 0.5135f),
                new(0.7101f, 0.6469f), new(0.8413f, 0.5833f), new(0.9084f, 0.5823f),
                new(0.9068f, 0.6427f)
            };

            List<C02DraggableBox> boxes = new();

            for (int index = 0; index < positions.Length; index++)
            {
                Image image = CreateImage($"Box{index + 1:00}", parent, LoadSprite($"C02_Box{index + 1:00}.png"));
                image.preserveAspect = false;
                image.raycastTarget = true;
                SetCenteredRect(image.rectTransform, LayerSize, positions[index] - layerCenters[index]);
                C02DraggableBox box = image.gameObject.AddComponent<C02DraggableBox>();
                SerializedObject serialized = new(box);
                serialized.FindProperty("normalizedRaycastMin").vector2Value = raycastMins[index];
                serialized.FindProperty("normalizedRaycastMax").vector2Value = raycastMaxs[index];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                boxes.Add(box);
            }

            return boxes;
        }

        private static List<Image> CreatePlacementGuides(Transform parent)
        {
            List<Image> guides = new();

            for (int index = 0; index < 7; index++)
            {
                Image guide = CreateImage(
                    $"BoxGuide{index + 1:00}",
                    parent,
                    LoadSprite($"C02_Box{index + 1:00}.png"));
                guide.preserveAspect = false;
                guide.raycastTarget = false;
                guide.color = new Color(1f, 1f, 1f, 0.1f);
                SetCenteredRect(guide.rectTransform, LayerSize, WarehousePosition);
                guides.Add(guide);
            }

            return guides;
        }

        private static void ConfigureBoxGame(
            C02BoxStackGame game,
            Canvas canvas,
            RectTransform dragLayer,
            IReadOnlyList<C02DraggableBox> boxes,
            IReadOnlyList<RectTransform> slots,
            IReadOnlyList<Image> placementGuides)
        {
            SerializedObject serialized = new(game);
            serialized.FindProperty("rootCanvas").objectReferenceValue = canvas;
            serialized.FindProperty("dragLayer").objectReferenceValue = dragLayer;
            serialized.FindProperty("requiredPlacementOverlap").floatValue = 0.8f;
            serialized.FindProperty("fitBoxToSlot").boolValue = false;
            serialized.FindProperty("preserveLayerAlignment").boolValue = true;
            serialized.FindProperty("alignedTargetPosition").vector2Value = WarehousePosition;
            serialized.FindProperty("placementGuideMode").enumValueIndex =
                (int)C02BoxStackGame.PlacementGuideMode.GhostBoxes;
            serialized.FindProperty("placementGuideOpacity").floatValue = 0.1f;
            AssignObjectArray(serialized.FindProperty("boxes"), boxes.Cast<Object>().ToArray());
            AssignObjectArray(serialized.FindProperty("targetSlots"), slots.Cast<Object>().ToArray());
            AssignObjectArray(serialized.FindProperty("placementGuides"), placementGuides.Cast<Object>().ToArray());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSequence(
            C02SequenceController controller,
            Image sceneImage,
            CanvasGroup stackGroup,
            C02BoxStackGame boxGame,
            CanvasGroup darkness)
        {
            SerializedObject serialized = new(controller);
            serialized.FindProperty("sceneImage").objectReferenceValue = sceneImage;
            serialized.FindProperty("jongsuSprite").objectReferenceValue = LoadSprite("C02_Jongsu.png");
            serialized.FindProperty("jongsuLookSprite").objectReferenceValue = LoadSprite("C02_JongsuLook.png");
            serialized.FindProperty("sceneRect").objectReferenceValue = sceneImage.rectTransform;
            serialized.FindProperty("jongsuFramePosition").vector2Value = JongsuPosition;
            serialized.FindProperty("warehouseFramePosition").vector2Value = WarehousePosition;
            serialized.FindProperty("stackGameGroup").objectReferenceValue = stackGroup;
            serialized.FindProperty("stackGame").objectReferenceValue = boxGame;
            serialized.FindProperty("darkness").objectReferenceValue = darkness;
            serialized.FindProperty("openingHold").floatValue = 0.8f;
            serialized.FindProperty("cutDuration").floatValue = 0.76f;
            serialized.FindProperty("warehouseDarkness").floatValue = 0.38f;
            serialized.FindProperty("playOnStart").boolValue = true;
            serialized.FindProperty("standaloneNextSceneName").stringValue = "Burning_C04_Playable";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObjectArray(SerializedProperty property, IReadOnlyList<Object> values)
        {
            property.arraySize = values.Count;

            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Sprite LoadSprite(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + fileName);

            if (sprite == null)
            {
                Debug.LogError($"C02 이미지 로드 실패: {fileName}");
            }

            return sprite;
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != ScenePath)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
