using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning
{
    public sealed class BurningContinuePrompt : MonoBehaviour
    {
        private const string TexturePath = "UI/Burning_CheckIcon_Charcoal";
        private const float ReferenceWidth = 540f;

        private static BurningContinuePrompt instance;

        private RectTransform viewport;
        private RectTransform buttonRect;
        private CanvasGroup buttonGroup;
        private Action continueAction;
        private float shownAt;

        public static void Show(Action action)
        {
            EnsureInstance().ShowInternal(action);
        }

        public static void Hide()
        {
            if (instance != null)
            {
                instance.HideInternal();
            }
        }

        public static IEnumerator WaitForContinue()
        {
            bool continued = false;
            Show(() => continued = true);
            while (!continued)
            {
                yield return null;
            }
        }

        private static BurningContinuePrompt EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new("BurningContinuePrompt", typeof(RectTransform));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<BurningContinuePrompt>();
            instance.BuildUi();
            return instance;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            UpdateLayout();
            if (buttonGroup == null || buttonGroup.alpha <= 0f)
            {
                return;
            }

            if (Time.unscaledTime - shownAt >= 0.2f && WasPressedThisFrame())
            {
                Continue();
            }
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            RectTransform canvasRect = gameObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            GameObject viewportObject = new("PortraitViewport", typeof(RectTransform));
            viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(transform, false);
            viewport.anchorMin = new Vector2(0.5f, 0.5f);
            viewport.anchorMax = new Vector2(0.5f, 0.5f);
            viewport.pivot = new Vector2(0.5f, 0.5f);

            GameObject buttonObject = new(
                "Continue",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(CanvasGroup));
            buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(viewport, false);
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(1f, 0f);

            RawImage image = buttonObject.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>(TexturePath);
            image.color = Color.white;
            image.raycastTarget = false;

            buttonGroup = buttonObject.GetComponent<CanvasGroup>();
            HideInternal();
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            if (viewport == null || buttonRect == null)
            {
                return;
            }

            float viewportWidth = Screen.width;
            float viewportHeight = Screen.height;
            if (viewportWidth / viewportHeight > 9f / 16f)
            {
                viewportWidth = viewportHeight * 9f / 16f;
            }
            else
            {
                viewportHeight = viewportWidth * 16f / 9f;
            }
            viewport.sizeDelta = new Vector2(viewportWidth, viewportHeight);

            float scale = viewportWidth / ReferenceWidth;
            float size = 78f * scale;
            float margin = 22f * scale;
            buttonRect.sizeDelta = new Vector2(size, size);
            buttonRect.anchoredPosition = new Vector2(-margin, margin);
        }

        private void ShowInternal(Action action)
        {
            continueAction = action;
            shownAt = Time.unscaledTime;
            buttonGroup.alpha = 1f;
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;
            buttonRect.localScale = Vector3.one;
        }

        private void HideInternal()
        {
            continueAction = null;
            if (buttonGroup == null)
            {
                return;
            }

            buttonGroup.alpha = 0f;
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;
            buttonRect.localScale = Vector3.one;
        }

        private void Continue()
        {
            Action action = continueAction;
            HideInternal();
            action?.Invoke();
        }

        private static bool WasPressedThisFrame()
        {
            bool mousePressed = Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame;
            bool touchPressed = Touchscreen.current != null
                && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            return mousePressed || touchPressed;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HideInternal();
        }
    }
}
