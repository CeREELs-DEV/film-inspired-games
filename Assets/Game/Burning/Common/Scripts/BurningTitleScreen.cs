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
        private static readonly Color DisabledColor = new(0.25f, 0.23f, 0.2f, 0.45f);

        private BurningAct1FlowController flowController;
        private GameObject titlePage;
        private GameObject chapterPage;
        private GameObject introPage;
        private Button chapterCardButton;
        private TMP_Text progressText;
        private CanvasGroup rootGroup;
        private CanvasGroup introTextGroup;
        private TMP_FontAsset regularFont;
        private TMP_FontAsset boldFont;
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
            TMP_Text title = CreateText(
                "Title",
                parent,
                "Burn my nest",
                boldFont,
                68f,
                CharcoalColor,
                TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0.11f, 0.68f), new Vector2(0.9f, 0.88f));

            RectTransform menu = CreateRect("Menu", parent);
            SetRect(menu, new Vector2(0.11f, 0.28f), new Vector2(0.65f, 0.6f));

            CreateMenuButton("NewGame", menu, "새로운 게임", 0.7f, StartNewGame);
            CreateMenuButton("Chapter", menu, "챕터", 0.4f, OpenChapters);
            CreateMenuButton("Exit", menu, "종료", 0.1f, ExitGame);
        }

        private void BuildChapterPage(Transform parent)
        {
            TMP_Text heading = CreateText(
                "Heading",
                parent,
                "챕터",
                boldFont,
                50f,
                CharcoalColor,
                TextAlignmentOptions.Left);
            SetRect(heading.rectTransform, new Vector2(0.1f, 0.82f), new Vector2(0.8f, 0.93f));

            Button backButton = CreateTextButton(
                "Back",
                parent,
                "<",
                44f,
                new Vector2(0.79f, 0.84f),
                new Vector2(0.91f, 0.92f),
                CloseChapters);
            backButton.GetComponent<Image>().color = Color.clear;

            Image card = CreateImage("Chapter01Card", parent, CreamColor);
            SetRect(card.rectTransform, new Vector2(0.16f, 0.3f), new Vector2(0.84f, 0.76f));

            Image placeholder = CreateImage("Placeholder", card.transform, CharcoalColor);
            SetRect(placeholder.rectTransform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.91f));

            TMP_Text mark = CreateText(
                "Mark",
                placeholder.transform,
                "01",
                boldFont,
                92f,
                new Color(0.72f, 0.65f, 0.56f, 0.32f),
                TextAlignmentOptions.Center);
            Stretch(mark.rectTransform);

            TMP_Text chapterName = CreateText(
                "ChapterName",
                card.transform,
                "Chapter.1",
                boldFont,
                35f,
                CharcoalColor,
                TextAlignmentOptions.Center);
            SetRect(chapterName.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.25f));

            progressText = CreateText(
                "Progress",
                parent,
                string.Empty,
                regularFont,
                28f,
                CharcoalColor,
                TextAlignmentOptions.Center);
            SetRect(progressText.rectTransform, new Vector2(0.12f, 0.2f), new Vector2(0.88f, 0.28f));

            chapterCardButton = card.gameObject.AddComponent<Button>();
            chapterCardButton.targetGraphic = card;
            chapterCardButton.onClick.AddListener(ContinueChapter);
            chapterCardButton.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = chapterCardButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.97f, 0.9f, 1f);
            colors.pressedColor = new Color(0.86f, 0.81f, 0.72f, 1f);
            colors.disabledColor = DisabledColor;
            chapterCardButton.colors = colors;
        }

        private void BuildIntroPage(Transform parent)
        {
            Image background = CreateImage("IntroBackground", parent, CharcoalColor);
            Stretch(background.rectTransform);

            TMP_Text chapter = CreateText(
                "Chapter",
                parent,
                "Chapter.1",
                boldFont,
                58f,
                CreamColor,
                TextAlignmentOptions.Center);
            SetRect(chapter.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.58f));
            introTextGroup = chapter.gameObject.AddComponent<CanvasGroup>();
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
            introTextGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.25f);

            float elapsed = 0f;
            const float fadeDuration = 0.55f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                introTextGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
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
            progressText.text = hasSave
                ? $"{BurningProgress.LatestChapter}에서 계속"
                : "저장된 기록 없음";
            progressText.color = hasSave ? CharcoalColor : DisabledColor;
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
                1.08f);
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
            float hoverFontScale = 1f)
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
                CharcoalColor,
                alignment);
            Stretch(text.rectTransform);
            AddHoverFontSize(target.gameObject, text, hoverFontScale);
            return button;
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
            if (regularSource == null || boldSource == null)
            {
                Debug.LogError("Gaegu 글꼴을 찾을 수 없음. Resources/Fonts 확인 필요.");
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
        }
    }
}
