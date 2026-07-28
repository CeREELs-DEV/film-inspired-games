using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning
{
    public sealed class BurningTitleScreen : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new(0.69f, 0.61f, 0.51f, 1f);
        private static readonly Color CharcoalColor = new(0.12f, 0.115f, 0.105f, 1f);
        private static readonly Color CreamColor = new(0.94f, 0.91f, 0.84f, 1f);

        private BurningAct1FlowController flowController;
        private GameObject titlePage;
        private GameObject chapterPage;
        private GameObject introPage;
        private Button chapterCardButton;
        private RectTransform chapterCardGraphic;
        private EventTrigger chapterCardHoverTrigger;
        private RawImage introImage;
        private CanvasGroup rootGroup;
        private TMP_FontAsset regularFont;
        private TMP_FontAsset boldFont;
        private TMP_FontAsset titleFont;
        private Coroutine introRoutine;

        public static void Show(BurningAct1FlowController flow)
        {
            BurningTitleScreen existing = FindFirstObjectByType<BurningTitleScreen>();
            if (existing != null)
            {
                existing.ShowInternal(flow);
                return;
            }

            GameObject root = new("BurningTitleScreen", typeof(RectTransform));
            BurningTitleScreen screen = root.AddComponent<BurningTitleScreen>();
            screen.BuildUi();
            screen.ShowInternal(flow);
        }

        private void BuildUi()
        {
            EnsureEventSystem();
            LoadFonts();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540f, 960f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = GetComponent<RectTransform>();
            Stretch(rootRect);
            rootGroup = gameObject.AddComponent<CanvasGroup>();

            Image background = CreateImage("Background", transform, BackgroundColor);
            Stretch(background.rectTransform);

            titlePage = CreatePage("TitlePage");
            BuildTitlePage(titlePage.transform);

            chapterPage = CreatePage("ChapterPage");
            BuildChapterPage(chapterPage.transform);

            introPage = CreatePage("ChapterIntro");
            BuildIntroPage(introPage.transform);

            SetPage(titlePage);
        }

        private void BuildTitlePage(Transform parent)
        {
            Texture2D titleBackground =
                Resources.Load<Texture2D>("UI/Burning_Title_Background");
            if (titleBackground != null)
            {
                RawImage background = CreateRawImage(
                    "TitleBackground", parent, titleBackground);
                Stretch(background.rectTransform);
            }
            else
            {
                Debug.LogError("타이틀 배경 이미지를 찾을 수 없음.");
            }

            TMP_Text title = CreateText(
                "Title",
                parent,
                "Burn my nest",
                titleFont,
                68f,
                CreamColor,
                TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0.11f, 0.68f), new Vector2(0.9f, 0.88f));
            title.fontStyle = FontStyles.Bold;
            ApplyReadableLightStyle(title);

            RectTransform menu = CreateRect("Menu", parent);
            SetRect(menu, new Vector2(0.11f, 0.28f), new Vector2(0.65f, 0.6f));

            CreateMenuButton("NewGame", menu, "새로운 게임", 0.7f, StartNewGame);
            CreateMenuButton("Chapter", menu, "챕터", 0.4f, OpenChapters);
            CreateMenuButton("Exit", menu, "종료", 0.1f, ExitGame);
        }

        private void BuildChapterPage(Transform parent)
        {
            Texture2D chapterBackground =
                Resources.Load<Texture2D>("UI/Burning_ChapterSelect_Background");
            Texture2D chapterCard =
                Resources.Load<Texture2D>("UI/Burning_Chapter01_Card");
            if (chapterBackground == null || chapterCard == null)
            {
                Debug.LogError("Chapter 1 선택 화면 이미지를 찾을 수 없음.");
                return;
            }

            RawImage background = CreateRawImage(
                "ChapterBackground", parent, chapterBackground);
            Stretch(background.rectTransform);

            RawImage cardGraphic = CreateRawImage(
                "Chapter01Card", parent, chapterCard);
            chapterCardGraphic = cardGraphic.rectTransform;
            Stretch(chapterCardGraphic);

            Image cardHitArea = CreateImage("Chapter01HitArea", parent, Color.clear);
            SetRect(
                cardHitArea.rectTransform,
                new Vector2(0.11f, 0.23f),
                new Vector2(0.91f, 0.79f));

            chapterCardButton = cardHitArea.gameObject.AddComponent<Button>();
            chapterCardButton.targetGraphic = cardHitArea;
            chapterCardButton.transition = Selectable.Transition.None;
            chapterCardButton.onClick.AddListener(ContinueChapter);
            chapterCardHoverTrigger = AddHoverScale(
                cardHitArea.gameObject, chapterCardGraphic, 1.04f);

            Button backButton = CreateTextButton(
                "Back",
                parent,
                "<",
                44f,
                new Vector2(0.08f, 0.84f),
                new Vector2(0.2f, 0.92f),
                CloseChapters);
            backButton.GetComponent<Image>().color = Color.clear;
        }

        private void BuildIntroPage(Transform parent)
        {
            Image background = CreateImage("IntroBackground", parent, Color.black);
            Stretch(background.rectTransform);

            Texture2D chapterTexture =
                Resources.Load<Texture2D>("UI/Burning_Chapter01_Title");
            if (chapterTexture == null)
            {
                Debug.LogError("Chapter 1 전환 이미지를 찾을 수 없음.");
                return;
            }

            GameObject imageObject = new(
                "Chapter01Image",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(parent, false);
            Stretch(imageRect);

            introImage = imageObject.GetComponent<RawImage>();
            introImage.texture = chapterTexture;
            introImage.color = Color.white;
            introImage.raycastTarget = false;
        }

        private void ShowInternal(BurningAct1FlowController flow)
        {
            flowController = flow;
            gameObject.SetActive(true);
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
            SetPage(titlePage);
        }

        private void StartNewGame()
        {
            if (introRoutine != null)
            {
                return;
            }

            BurningProgress.Reset();
            introRoutine = StartCoroutine(PlayChapterIntro());
        }

        private IEnumerator PlayChapterIntro()
        {
            SetPage(introPage);
            introImage.color = Color.white;
            yield return new WaitForSecondsRealtime(1.25f);

            float elapsed = 0f;
            const float fadeDuration = 0.55f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                introImage.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            introRoutine = null;
            flowController?.StartNewGameFromTitle();
        }

        private void OpenChapters()
        {
            bool hasSave = BurningProgress.HasSavedGame;
            chapterCardButton.interactable = hasSave;
            chapterCardHoverTrigger.enabled = hasSave;
            chapterCardGraphic.localScale = Vector3.one;
            SetPage(chapterPage);
        }

        private void CloseChapters()
        {
            SetPage(titlePage);
        }

        private void ContinueChapter()
        {
            BurningProgress.LoadSavedGame();
        }

        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private GameObject CreatePage(string name)
        {
            RectTransform rect = CreateRect(name, transform);
            Stretch(rect);
            return rect.gameObject;
        }

        private void CreateMenuButton(
            string name,
            Transform parent,
            string label,
            float centerY,
            UnityEngine.Events.UnityAction action)
        {
            CreateTextButton(
                name,
                parent,
                label,
                38f,
                new Vector2(0f, centerY - 0.14f),
                new Vector2(1f, centerY + 0.14f),
                action,
                TextAlignmentOptions.Left,
                1.08f,
                true);
        }

        private Button CreateTextButton(
            string name,
            Transform parent,
            string label,
            float fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityEngine.Events.UnityAction action,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            float hoverFontScale = 1f,
            bool lightText = false)
        {
            Image target = CreateImage(name, parent, Color.clear);
            SetRect(target.rectTransform, anchorMin, anchorMax);

            Button button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.72f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;

            TMP_Text text = CreateText(
                "Label",
                target.transform,
                label,
                regularFont,
                fontSize,
                lightText ? CreamColor : CharcoalColor,
                alignment);
            Stretch(text.rectTransform);
            if (lightText)
            {
                ApplyReadableLightStyle(text);
            }

            AddHoverFontSize(target.gameObject, text, hoverFontScale);
            return button;
        }

        private static EventTrigger AddHoverScale(
            GameObject target,
            RectTransform graphic,
            float hoverScale)
        {
            EventTrigger trigger = target.AddComponent<EventTrigger>();
            AddEventTrigger(
                trigger,
                EventTriggerType.PointerEnter,
                _ => graphic.localScale = Vector3.one * hoverScale);
            AddEventTrigger(
                trigger,
                EventTriggerType.PointerExit,
                _ => graphic.localScale = Vector3.one);
            AddEventTrigger(
                trigger,
                EventTriggerType.PointerClick,
                _ => graphic.localScale = Vector3.one);
            return trigger;
        }

        private static void AddHoverFontSize(
            GameObject target,
            TMP_Text text,
            float hoverFontScale)
        {
            if (hoverFontScale <= 1f)
            {
                return;
            }

            float normalSize = text.fontSize;
            EventTrigger trigger = target.AddComponent<EventTrigger>();
            AddEventTrigger(
                trigger,
                EventTriggerType.PointerEnter,
                _ => text.fontSize = normalSize * hoverFontScale);
            AddEventTrigger(
                trigger,
                EventTriggerType.PointerExit,
                _ => text.fontSize = normalSize);
            AddEventTrigger(
                trigger,
                EventTriggerType.PointerClick,
                _ => text.fontSize = normalSize);
        }

        private static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyReadableLightStyle(TMP_Text text)
        {
            text.outlineColor = new Color(0f, 0f, 0f, 0.78f);
            text.outlineWidth = 0.12f;
        }

        private static RawImage CreateRawImage(
            string name,
            Transform parent,
            Texture texture)
        {
            GameObject imageObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject rectObject = new(name, typeof(RectTransform));
            RectTransform rect = rectObject.GetComponent<RectTransform>();
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

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetPage(GameObject page)
        {
            titlePage.SetActive(page == titlePage);
            chapterPage.SetActive(page == chapterPage);
            introPage.SetActive(page == introPage);
        }

        private void LoadFonts()
        {
            Font regularSource = Resources.Load<Font>("Fonts/Gaegu-Regular");
            Font boldSource = Resources.Load<Font>("Fonts/Gaegu-Bold");
            Font titleSource = Resources.Load<Font>("Fonts/Pristina");
            if (regularSource == null || boldSource == null || titleSource == null)
            {
                Debug.LogError("타이틀 UI 글꼴을 찾을 수 없음. Resources/Fonts 확인 필요.");
                return;
            }

            regularFont = TMP_FontAsset.CreateFontAsset(
                regularSource,
                72,
                7,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            regularFont.name = "Gaegu Regular Runtime";

            boldFont = TMP_FontAsset.CreateFontAsset(
                boldSource,
                72,
                7,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            boldFont.name = "Gaegu Bold Runtime";

            titleFont = TMP_FontAsset.CreateFontAsset(
                titleSource,
                72,
                7,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            titleFont.name = "Pristina Runtime";
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();
        }

        private void OnDestroy()
        {
            if (regularFont != null)
            {
                Destroy(regularFont);
            }

            if (boldFont != null)
            {
                Destroy(boldFont);
            }

            if (titleFont != null)
            {
                Destroy(titleFont);
            }
        }
    }
}
