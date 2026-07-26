using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FilmInspiredGames.Burning.C06
{
    public sealed class C06C07SequenceController : MonoBehaviour
    {
        [Header("C06")]
        [SerializeField] private CanvasGroup c06Root;
        [SerializeField] private CanvasGroup c06Idle;
        [SerializeField] private CanvasGroup c06Speaking;
        [SerializeField] private CanvasGroup c06SpeechBubble;
        [SerializeField] private CanvasGroup c06PuzzleBase;
        [SerializeField] private CanvasGroup c06PuzzleRoot;
        [SerializeField] private RectTransform[] puzzlePieces;
        [SerializeField, Min(0f)] private float openingHold = 1.2f;
        [SerializeField, Min(0.01f)] private float speechFadeDuration = 0.8f;
        [SerializeField, Min(0f)] private float assembledHold = 0.55f;
        [SerializeField, Min(0.01f)] private float burstDuration = 1.25f;
        [SerializeField, Min(0f)] private float scatteredHold = 1.15f;
        [SerializeField, Min(1f)] private float puzzleSnapDistance = 38f;
        [SerializeField, Min(0f)] private float puzzleSolvedHold = 0.9f;
        [SerializeField, Min(0.01f)] private float puzzleReturnDuration = 0.32f;
        [SerializeField, Min(0f)] private float puzzleReturnMargin = 8f;

        [Header("C06 말풍선 퍼즐")]
        [SerializeField, Min(1f)] private float speechPuzzleSnapDistance = 42f;
        [SerializeField, Min(0.01f)] private float speechPuzzleFadeDuration = 0.38f;

        [Header("C07")]
        [SerializeField] private CanvasGroup c07Root;
        [SerializeField] private CanvasGroup c07Unknown;
        [SerializeField] private CanvasGroup c07Realizes;
        [SerializeField] private CanvasGroup c07Exclamation;
        [SerializeField] private RectTransform c07ExclamationRect;
        [SerializeField, Min(0f)] private float c07UnknownHold = 1.4f;
        [SerializeField, Min(0.01f)] private float c07RealizeDuration = 0.75f;
        [SerializeField, Min(0.1f)] private float c07PromptPulseDuration = 1.15f;
        [SerializeField, Range(0f, 0.15f)] private float c07PromptScaleAmount = 0.06f;
        [SerializeField, Range(0f, 0.5f)] private float c07PromptRedAmount = 0.18f;
        [SerializeField, Min(0f)] private float c07ExclamationHitPadding = 18f;

        [Header("장면 전환")]
        [SerializeField] private CanvasGroup blackOverlay;
        [SerializeField] private string nextSceneName = "Burning_C08_C12_Playable";
        [SerializeField, Min(0.01f)] private float chapterFadeDuration = 1.25f;
        [SerializeField, Min(0.01f)] private float nextSceneFadeDuration = 1.5f;
        [SerializeField] private bool playOnStart = true;

        private Vector2[] puzzleHomePositions;
        private bool[] puzzlePlaced;
        private bool[] puzzleReturning;
        private bool puzzleBreakRequested;
        private bool nextSceneRequested;
        private int draggedPuzzleIndex = -1;
        private int placedPuzzleCount;
        private Vector2 puzzleDragOffset;
        private Coroutine sequenceRoutine;
        private bool startAtC07;
        private Image c07ExclamationImage;
        private Color c07ExclamationBaseColor = Color.white;
        private CanvasGroup speechPuzzleRoot;
        private RectTransform speechPuzzleRootRect;
        private RectTransform[] speechPuzzlePieces;
        private Vector2[] speechPuzzleHomePositions;
        private bool[] speechPuzzlePlaced;
        private int draggedSpeechPuzzleIndex = -1;
        private int placedSpeechPuzzleCount;
        private Vector2 speechPuzzleDragOffset;

        private static readonly Vector2[] ScatterCentersSource =
        {
            new(1210f, 1400f), new(500f, 700f), new(1300f, 190f), new(790f, 2100f),
            new(160f, 1100f), new(1450f, 800f), new(1250f, 1040f), new(760f, 240f),
            new(1370f, 1900f), new(1000f, 700f), new(220f, 1500f), new(200f, 2600f),
            new(1480f, 2570f), new(1030f, 2580f), new(180f, 650f), new(250f, 230f),
            new(130f, 1990f), new(1510f, 1450f), new(650f, 2550f), new(360f, 2300f)
        };

        private static readonly float[] ScatterRotations =
        {
            34f, 24f, -9f, 1f, -7f, 2f, -8f, 4f, -5f, -11f,
            8f, -8f, -5f, -6f, -7f, -8f, 9f, -15f, 1f, -23f
        };

        private static readonly Rect[] SpeechPuzzleCrops =
        {
            new(1f, 1f, 439f, 203f),
            new(378f, 2f, 381f, 160f),
            new(3f, 158f, 379f, 110f),
            new(321f, 116f, 436f, 201f)
        };

        private static readonly Vector2[] SpeechPuzzleScatterPositions =
        {
            new(125f, -690f),
            new(415f, -700f),
            new(120f, -910f),
            new(410f, -900f)
        };

        private static readonly float[] SpeechPuzzleScatterRotations = { -8f, 7f, 6f, -5f };

        public string CurrentChapter { get; private set; } = "재생 대기";
        public string CurrentState { get; private set; } = "대기";
        public bool IsSpeechPuzzlePlayable { get; private set; }
        public bool IsWaitingForPuzzleBreak { get; private set; }
        public bool IsPuzzlePlayable { get; private set; }
        public int PlacedPuzzleCount => placedPuzzleCount;
        public bool IsWaitingForNextScene { get; private set; }

        private void Awake()
        {
            CreateSpeechPuzzle();

            if (c07ExclamationRect != null)
            {
                c07ExclamationImage = c07ExclamationRect.GetComponent<Image>();
                if (c07ExclamationImage != null)
                {
                    c07ExclamationBaseColor = c07ExclamationImage.color;
                }
            }

            puzzleHomePositions = new Vector2[puzzlePieces.Length];
            puzzlePlaced = new bool[puzzlePieces.Length];
            puzzleReturning = new bool[puzzlePieces.Length];
            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                puzzleHomePositions[i] = puzzlePieces[i].anchoredPosition;
            }
        }

        private void Start()
        {
            startAtC07 = BurningChapterDebugRequest.Consume() == "C07";
            Prepare();
            if (playOnStart)
            {
                Play();
            }
        }

        private void Update()
        {
            ReadPointer(out Vector2 pointerPosition, out bool pressed, out bool held, out bool released);

            if (IsSpeechPuzzlePlayable)
            {
                HandleSpeechPuzzleInput(pointerPosition, pressed, held, released);
                return;
            }

            if (IsPuzzlePlayable)
            {
                HandlePuzzleInput(pointerPosition, pressed, held, released);
                return;
            }

            if (pressed && IsWaitingForPuzzleBreak)
            {
                puzzleBreakRequested = true;
            }
            else if (pressed && IsWaitingForNextScene && IsExclamationPressed(pointerPosition))
            {
                nextSceneRequested = true;
            }
        }

        public void Play()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
            }

            Prepare();
            sequenceRoutine = StartCoroutine(PlayRoutine());
        }

        private void Prepare()
        {
            SetAlpha(c06Root, 1f);
            SetAlpha(c06Idle, 1f);
            SetAlpha(c06Speaking, 0f);
            SetAlpha(c06SpeechBubble, 0f);
            SetAlpha(speechPuzzleRoot, 0f);
            SetAlpha(c06PuzzleBase, 0f);
            SetAlpha(c06PuzzleRoot, 0f);
            SetAlpha(c07Root, 0f);
            SetAlpha(c07Unknown, 1f);
            SetAlpha(c07Realizes, 0f);
            SetAlpha(c07Exclamation, 0f);
            SetAlpha(blackOverlay, 0f);

            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                puzzlePieces[i].anchoredPosition = puzzleHomePositions[i];
                puzzlePieces[i].localRotation = Quaternion.identity;
                puzzlePieces[i].localScale = Vector3.one;
                puzzlePlaced[i] = false;
                puzzleReturning[i] = false;
            }

            if (c07ExclamationRect != null)
            {
                c07ExclamationRect.localScale = Vector3.zero;
            }

            if (c07ExclamationImage != null)
            {
                c07ExclamationImage.color = c07ExclamationBaseColor;
            }

            puzzleBreakRequested = false;
            nextSceneRequested = false;
            ResetSpeechPuzzle();
            draggedPuzzleIndex = -1;
            placedPuzzleCount = 0;
            IsWaitingForPuzzleBreak = false;
            IsSpeechPuzzlePlayable = false;
            IsPuzzlePlayable = false;
            IsWaitingForNextScene = false;
            CurrentChapter = "C06";
            CurrentState = "해미를 바라봄";
        }

        private IEnumerator PlayRoutine()
        {
            if (!startAtC07)
            {
                yield return Wait(openingHold);

                SetAlpha(speechPuzzleRoot, 1f);
                IsSpeechPuzzlePlayable = true;
                CurrentState = $"말풍선 퍼즐 맞추기 {placedSpeechPuzzleCount}/{speechPuzzlePieces.Length}";
                while (placedSpeechPuzzleCount < speechPuzzlePieces.Length)
                {
                    yield return null;
                }
                IsSpeechPuzzlePlayable = false;
                CurrentState = "말풍선 퍼즐 완성";
                yield return Wait(0.25f);
                yield return Fade(
                    speechPuzzleRoot, speechPuzzleRoot.alpha, 0f, speechPuzzleFadeDuration);

                CurrentState = "나 몰라?";
                yield return CrossFade(c06Idle, c06Speaking, speechFadeDuration);
                yield return Fade(c06SpeechBubble, 0f, 1f, speechFadeDuration * 0.8f);

                IsWaitingForPuzzleBreak = true;
                while (!puzzleBreakRequested)
                {
                    yield return null;
                }
                IsWaitingForPuzzleBreak = false;

                CurrentState = "기억의 퍼즐";
                SetAlpha(c06SpeechBubble, 0f);
                SetAlpha(c06Speaking, 0f);
                SetAlpha(c06PuzzleBase, 1f);
                SetAlpha(c06PuzzleRoot, 1f);
                yield return Wait(assembledHold);

                CurrentState = "퍼즐 조각이 흩어짐";
                yield return BurstPuzzle();
                yield return Wait(Mathf.Min(scatteredHold, 0.25f));

                IsPuzzlePlayable = true;
                CurrentState = $"퍼즐 맞추기 {placedPuzzleCount}/{puzzlePieces.Length}";
                while (placedPuzzleCount < puzzlePieces.Length)
                {
                    yield return null;
                }
                IsPuzzlePlayable = false;
                CurrentState = "퍼즐 완성";
                yield return Wait(puzzleSolvedHold);
                CurrentState = "C07 이동 대기";
                yield return BurningContinuePrompt.WaitForContinue();

                BurningChapterSettings.ResolvedTiming c06ToC07 = BurningChapterSettings.Resolve(
                    "C06", "C07", chapterFadeDuration, 0f, chapterFadeDuration);
                yield return Fade(blackOverlay, 0f, 1f, c06ToC07.FadeOutDuration);
                yield return Wait(c06ToC07.BlackHoldDuration);
                SetAlpha(c06Root, 0f);
                SetAlpha(c07Root, 1f);
                CurrentChapter = "C07";
                CurrentState = "알 수 없는 표정";
                yield return Fade(blackOverlay, 1f, 0f, c06ToC07.FadeInDuration);
            }
            else
            {
                SetAlpha(c06Root, 0f);
                SetAlpha(c07Root, 1f);
                SetAlpha(blackOverlay, 0f);
                CurrentChapter = "C07";
                CurrentState = "알 수 없는 표정";
            }

            yield return Wait(c07UnknownHold);

            CurrentState = "깨달음";
            yield return CrossFade(c07Unknown, c07Realizes, c07RealizeDuration);
            yield return PopExclamation();

            IsWaitingForNextScene = true;
            CurrentState = "느낌표 선택 대기";
            float promptElapsed = 0f;
            while (!nextSceneRequested)
            {
                promptElapsed += Time.unscaledDeltaTime;
                AnimateExclamationPrompt(promptElapsed);
                yield return null;
            }
            IsWaitingForNextScene = false;
            ResetExclamationPrompt();
            CurrentState = "C08 이동 대기";
            yield return BurningContinuePrompt.WaitForContinue();

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"다음 씬을 불러올 수 없습니다: {nextSceneName}", this);
                sequenceRoutine = null;
                yield break;
            }

            BurningChapterSettings.ResolvedTiming c07ToC08 = BurningChapterSettings.Resolve(
                "C07", "C08", nextSceneFadeDuration, 0f, 0f);
            CurrentState = "C08로 전환";
            yield return Fade(blackOverlay, 0f, 1f, c07ToC08.FadeOutDuration);
            yield return Wait(c07ToC08.BlackHoldDuration);
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator BurstPuzzle()
        {
            float elapsed = 0f;
            const float stagger = 0.018f;
            float totalDuration = burstDuration + stagger * (puzzlePieces.Length - 1);

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                for (int i = 0; i < puzzlePieces.Length; i++)
                {
                    float localTime = Mathf.Clamp01((elapsed - i * stagger) / burstDuration);
                    float movement = EaseOutBack(localTime, 0.55f);
                    Vector2 target = ScatterPosition(i);

                    puzzlePieces[i].anchoredPosition = Vector2.LerpUnclamped(
                        puzzleHomePositions[i], target, movement);
                    puzzlePieces[i].localRotation = Quaternion.Euler(
                        0f, 0f, Mathf.LerpUnclamped(0f, ScatterRotation(i), movement));
                    float scale = 1f + Mathf.Sin(localTime * Mathf.PI) * 0.08f;
                    puzzlePieces[i].localScale = Vector3.one * scale;
                }

                yield return null;
            }

            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                puzzlePieces[i].anchoredPosition = ScatterPosition(i);
                puzzlePieces[i].localRotation = Quaternion.Euler(0f, 0f, ScatterRotation(i));
                puzzlePieces[i].localScale = Vector3.one;
            }
        }

        private void CreateSpeechPuzzle()
        {
            if (c06Root == null || speechPuzzleRoot != null)
            {
                return;
            }

            GameObject rootObject = new(
                "SpeechPuzzle", typeof(RectTransform), typeof(CanvasGroup));
            speechPuzzleRootRect = rootObject.GetComponent<RectTransform>();
            speechPuzzleRootRect.SetParent(c06Root.transform, false);
            Stretch(speechPuzzleRootRect);
            speechPuzzleRoot = rootObject.GetComponent<CanvasGroup>();

            const float sourceWidth = 760f;
            const float sourceHeight = 320f;
            const float targetX = 70f;
            const float targetY = 720f;
            const float targetWidth = 400f;
            float targetHeight = targetWidth * sourceHeight / sourceWidth;
            float scaleX = targetWidth / sourceWidth;
            float scaleY = targetHeight / sourceHeight;

            CreateSpeechPuzzleImage(
                "Guide",
                Resources.Load<Texture2D>("C06/SpeechPuzzle/C06_SpeechPuzzle_Full"),
                new Rect(0f, 0f, sourceWidth, sourceHeight),
                targetX,
                targetY,
                scaleX,
                scaleY,
                1f);

            speechPuzzlePieces = new RectTransform[SpeechPuzzleCrops.Length];
            speechPuzzleHomePositions = new Vector2[SpeechPuzzleCrops.Length];
            speechPuzzlePlaced = new bool[SpeechPuzzleCrops.Length];

            for (int i = 0; i < SpeechPuzzleCrops.Length; i++)
            {
                RectTransform piece = CreateSpeechPuzzleImage(
                    $"Piece_{i + 1:00}",
                    Resources.Load<Texture2D>($"C06/SpeechPuzzle/C06_SpeechPuzzle_{i + 1:00}"),
                    SpeechPuzzleCrops[i],
                    targetX,
                    targetY,
                    scaleX,
                    scaleY,
                    0.62f);
                speechPuzzlePieces[i] = piece;
                speechPuzzleHomePositions[i] = piece.anchoredPosition;
            }

            CreateSpeechPuzzleImage(
                "Border",
                Resources.Load<Texture2D>("C06/SpeechPuzzle/C06_SpeechPuzzle_Border"),
                new Rect(0f, 0f, sourceWidth, sourceHeight),
                targetX,
                targetY,
                scaleX,
                scaleY,
                1f);
        }

        private RectTransform CreateSpeechPuzzleImage(
            string objectName,
            Texture texture,
            Rect sourceRect,
            float targetX,
            float targetY,
            float scaleX,
            float scaleY,
            float alpha)
        {
            GameObject imageObject = new(
                objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(speechPuzzleRootRect, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(sourceRect.width * scaleX, sourceRect.height * scaleY);
            rect.anchoredPosition = new Vector2(
                targetX + (sourceRect.x + sourceRect.width * 0.5f) * scaleX,
                -(targetY + (sourceRect.y + sourceRect.height * 0.5f) * scaleY));

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
            return rect;
        }

        private void ResetSpeechPuzzle()
        {
            if (speechPuzzlePieces == null)
            {
                return;
            }

            placedSpeechPuzzleCount = 0;
            draggedSpeechPuzzleIndex = -1;
            for (int i = 0; i < speechPuzzlePieces.Length; i++)
            {
                speechPuzzlePlaced[i] = false;
                speechPuzzlePieces[i].anchoredPosition = SpeechPuzzleScatterPositions[i];
                speechPuzzlePieces[i].localRotation = Quaternion.Euler(
                    0f, 0f, SpeechPuzzleScatterRotations[i]);
                speechPuzzlePieces[i].localScale = Vector3.one;
            }
        }

        private void HandleSpeechPuzzleInput(
            Vector2 screenPosition,
            bool pressed,
            bool held,
            bool released)
        {
            if (pressed)
            {
                BeginSpeechPuzzleDrag(screenPosition);
            }

            if (draggedSpeechPuzzleIndex >= 0 && held)
            {
                RectTransform piece = speechPuzzlePieces[draggedSpeechPuzzleIndex];
                piece.anchoredPosition =
                    ScreenToSpeechPuzzlePosition(screenPosition) + speechPuzzleDragOffset;
                piece.localRotation = Quaternion.Slerp(
                    piece.localRotation, Quaternion.identity, 0.25f);
            }

            if (draggedSpeechPuzzleIndex >= 0 && (released || !held))
            {
                EndSpeechPuzzleDrag();
            }
        }

        private void BeginSpeechPuzzleDrag(Vector2 screenPosition)
        {
            int selectedIndex = -1;
            int highestSibling = -1;
            for (int i = 0; i < speechPuzzlePieces.Length; i++)
            {
                if (speechPuzzlePlaced[i]
                    || !RectTransformUtility.RectangleContainsScreenPoint(
                        speechPuzzlePieces[i], screenPosition))
                {
                    continue;
                }

                int sibling = speechPuzzlePieces[i].GetSiblingIndex();
                if (sibling > highestSibling)
                {
                    highestSibling = sibling;
                    selectedIndex = i;
                }
            }

            if (selectedIndex < 0)
            {
                return;
            }

            draggedSpeechPuzzleIndex = selectedIndex;
            RectTransform selected = speechPuzzlePieces[selectedIndex];
            speechPuzzleDragOffset =
                selected.anchoredPosition - ScreenToSpeechPuzzlePosition(screenPosition);
            selected.SetAsLastSibling();
            selected.localScale = Vector3.one * 1.05f;
        }

        private void EndSpeechPuzzleDrag()
        {
            int index = draggedSpeechPuzzleIndex;
            draggedSpeechPuzzleIndex = -1;
            RectTransform piece = speechPuzzlePieces[index];
            piece.localScale = Vector3.one;

            if (IsMoreThanHalfOutside(piece))
            {
                piece.anchoredPosition = GetFullyVisiblePosition(piece);
                return;
            }

            if (Vector2.Distance(
                    piece.anchoredPosition, speechPuzzleHomePositions[index])
                > speechPuzzleSnapDistance)
            {
                return;
            }

            piece.anchoredPosition = speechPuzzleHomePositions[index];
            piece.localRotation = Quaternion.identity;
            speechPuzzlePlaced[index] = true;
            placedSpeechPuzzleCount++;
            CurrentState =
                $"말풍선 퍼즐 맞추기 {placedSpeechPuzzleCount}/{speechPuzzlePieces.Length}";
        }

        private Vector2 ScreenToSpeechPuzzlePosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                speechPuzzleRootRect, screenPosition, null, out Vector2 localPosition);
            Vector2 topLeft =
                new(speechPuzzleRootRect.rect.xMin, speechPuzzleRootRect.rect.yMax);
            return localPosition - topLeft;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void HandlePuzzleInput(Vector2 screenPosition, bool pressed, bool held, bool released)
        {
            if (pressed)
            {
                BeginPuzzleDrag(screenPosition);
            }

            if (draggedPuzzleIndex >= 0 && held)
            {
                RectTransform piece = puzzlePieces[draggedPuzzleIndex];
                piece.anchoredPosition = ScreenToPuzzlePosition(screenPosition) + puzzleDragOffset;
                piece.localRotation = Quaternion.Slerp(piece.localRotation, Quaternion.identity, 0.22f);
            }

            if (draggedPuzzleIndex >= 0 && (released || !held))
            {
                EndPuzzleDrag();
            }
        }

        private void BeginPuzzleDrag(Vector2 screenPosition)
        {
            int selectedIndex = -1;
            int highestSibling = -1;

            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                if (puzzlePlaced[i]
                    || puzzleReturning[i]
                    || !RectTransformUtility.RectangleContainsScreenPoint(puzzlePieces[i], screenPosition))
                {
                    continue;
                }

                int sibling = puzzlePieces[i].GetSiblingIndex();
                if (sibling > highestSibling)
                {
                    highestSibling = sibling;
                    selectedIndex = i;
                }
            }

            if (selectedIndex < 0)
            {
                return;
            }

            draggedPuzzleIndex = selectedIndex;
            RectTransform selected = puzzlePieces[selectedIndex];
            puzzleDragOffset = selected.anchoredPosition - ScreenToPuzzlePosition(screenPosition);
            selected.SetAsLastSibling();
            selected.localScale = Vector3.one * 1.04f;
        }

        private void EndPuzzleDrag()
        {
            int index = draggedPuzzleIndex;
            draggedPuzzleIndex = -1;
            RectTransform piece = puzzlePieces[index];
            piece.localScale = Vector3.one;

            if (IsMoreThanHalfOutside(piece))
            {
                StartCoroutine(ReturnPuzzleInside(index));
                return;
            }

            if (Vector2.Distance(piece.anchoredPosition, puzzleHomePositions[index]) > puzzleSnapDistance)
            {
                return;
            }

            piece.anchoredPosition = puzzleHomePositions[index];
            piece.localRotation = Quaternion.identity;
            puzzlePlaced[index] = true;
            placedPuzzleCount++;
            CurrentState = $"퍼즐 맞추기 {placedPuzzleCount}/{puzzlePieces.Length}";
        }

        private bool IsMoreThanHalfOutside(RectTransform piece)
        {
            Vector2 center = piece.anchoredPosition;
            return center.x < 0f || center.x > 540f || center.y > 0f || center.y < -960f;
        }

        private IEnumerator ReturnPuzzleInside(int index)
        {
            puzzleReturning[index] = true;
            RectTransform piece = puzzlePieces[index];
            Vector2 start = piece.anchoredPosition;
            Vector2 target = GetFullyVisiblePosition(piece);
            float elapsed = 0f;

            while (elapsed < puzzleReturnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / puzzleReturnDuration);
                piece.anchoredPosition = Vector2.LerpUnclamped(start, target, EaseOutBack(t, 0.35f));
                yield return null;
            }

            piece.anchoredPosition = target;
            puzzleReturning[index] = false;
        }

        private Vector2 GetFullyVisiblePosition(RectTransform piece)
        {
            float radians = piece.localEulerAngles.z * Mathf.Deg2Rad;
            float cosine = Mathf.Abs(Mathf.Cos(radians));
            float sine = Mathf.Abs(Mathf.Sin(radians));
            float halfWidth = (piece.rect.width * cosine + piece.rect.height * sine) * 0.5f;
            float halfHeight = (piece.rect.width * sine + piece.rect.height * cosine) * 0.5f;
            float margin = puzzleReturnMargin;

            return new Vector2(
                Mathf.Clamp(piece.anchoredPosition.x, halfWidth + margin, 540f - halfWidth - margin),
                Mathf.Clamp(piece.anchoredPosition.y, -960f + halfHeight + margin, -halfHeight - margin));
        }

        private Vector2 ScreenToPuzzlePosition(Vector2 screenPosition)
        {
            RectTransform puzzleRect = (RectTransform)c06PuzzleRoot.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                puzzleRect, screenPosition, null, out Vector2 localPosition);
            Vector2 topLeft = new(puzzleRect.rect.xMin, puzzleRect.rect.yMax);
            return localPosition - topLeft;
        }

        private static void ReadPointer(
            out Vector2 position,
            out bool pressed,
            out bool held,
            out bool released)
        {
            if (Touchscreen.current != null
                && (Touchscreen.current.primaryTouch.press.isPressed
                    || Touchscreen.current.primaryTouch.press.wasPressedThisFrame
                    || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                held = Touchscreen.current.primaryTouch.press.isPressed;
                released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
                return;
            }

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                return;
            }

            position = default;
            pressed = false;
            held = false;
            released = false;
        }

        private static Vector2 ScatterPosition(int index)
        {
            Vector2 source = ScatterCentersSource[index % ScatterCentersSource.Length];
            return new Vector2(source.x * 540f / 1632f, -source.y * 960f / 2912f);
        }

        private static float ScatterRotation(int index)
        {
            return ScatterRotations[index % ScatterRotations.Length];
        }

        private IEnumerator PopExclamation()
        {
            SetAlpha(c07Exclamation, 1f);
            float elapsed = 0f;
            const float duration = 0.72f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale;

                if (t < 0.55f)
                {
                    scale = EaseOutBack(t / 0.55f, 1.2f);
                }
                else
                {
                    float bounce = (t - 0.55f) / 0.45f;
                    scale = 1f + Mathf.Sin(bounce * Mathf.PI * 2f) * (1f - bounce) * 0.16f;
                }

                c07ExclamationRect.localScale = Vector3.one * scale;
                yield return null;
            }

            c07ExclamationRect.localScale = Vector3.one;
        }

        private bool IsExclamationPressed(Vector2 screenPosition)
        {
            if (c07ExclamationRect == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    c07ExclamationRect, screenPosition, null, out Vector2 localPosition))
            {
                return false;
            }

            Rect hitRect = c07ExclamationRect.rect;
            hitRect.xMin -= c07ExclamationHitPadding;
            hitRect.xMax += c07ExclamationHitPadding;
            hitRect.yMin -= c07ExclamationHitPadding;
            hitRect.yMax += c07ExclamationHitPadding;
            return hitRect.Contains(localPosition);
        }

        private void AnimateExclamationPrompt(float elapsed)
        {
            float phase = elapsed / Mathf.Max(0.1f, c07PromptPulseDuration) * Mathf.PI * 2f;
            float pulse = (1f - Mathf.Cos(phase)) * 0.5f;
            c07ExclamationRect.localScale = Vector3.one * (1f + pulse * c07PromptScaleAmount);

            if (c07ExclamationImage != null)
            {
                Color redTint = new(
                    c07ExclamationBaseColor.r,
                    c07ExclamationBaseColor.g * (1f - c07PromptRedAmount),
                    c07ExclamationBaseColor.b * (1f - c07PromptRedAmount),
                    c07ExclamationBaseColor.a);
                c07ExclamationImage.color = Color.Lerp(c07ExclamationBaseColor, redTint, pulse);
            }
        }

        private void ResetExclamationPrompt()
        {
            if (c07ExclamationRect != null)
            {
                c07ExclamationRect.localScale = Vector3.one;
            }

            if (c07ExclamationImage != null)
            {
                c07ExclamationImage.color = c07ExclamationBaseColor;
            }
        }

        private static IEnumerator CrossFade(CanvasGroup from, CanvasGroup to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                SetAlpha(from, 1f - t);
                SetAlpha(to, t);
                yield return null;
            }

            SetAlpha(from, 0f);
            SetAlpha(to, 1f);
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(group, Mathf.LerpUnclamped(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
                yield return null;
            }

            SetAlpha(group, to);
        }

        private static IEnumerator Wait(float duration)
        {
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
        }

        private static float EaseOutBack(float value, float overshoot)
        {
            float shifted = Mathf.Clamp01(value) - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted
                + overshoot * shifted * shifted;
        }

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
    }
}
