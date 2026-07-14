using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class PetaPuzzleController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("World Object")]
    [SerializeField] private Sprite puzzleSprite;

    [Header("Puzzle UI")]
    [SerializeField] private Canvas puzzleCanvas;
    [SerializeField] private RectTransform puzzleContainer;
    [SerializeField] private int puzzleRows = 3;
    [SerializeField] private int puzzleCols = 3;

    [Header("Photo")]
    [SerializeField] private Button cameraButton;

    [Header("Timer")]
    [SerializeField] private float puzzleTimeLimit = 180f;
    [SerializeField] private Text timerDisplay;

    [Header("Animation")]
    [SerializeField] private float transitionDuration = 0.3f;

    private GameObject hoverOutline;
    private BoxCollider2D boxCollider;
    private MonoBehaviour coroutineRunner;
    private int lastTriggerFrame = -1;
    private bool isHovered = false;
    private bool puzzleActive = false;
    private bool puzzleSolved = false;
    private bool isPhotoMode = false;

    private List<TileSlot> tileSlots = new List<TileSlot>();
    private int totalTiles;
    private Texture2D sourceTexture;
    private GameObject darkBackground;
    private CanvasGroup puzzleGroup;
    private float timeRemaining;
    private bool timerRunning;
    private Coroutine timerCoroutine;

    private class TileSlot
    {
        public int correctIndex;
        public RectTransform slotRect;
        public DragTile currentTile;
    }

    private class DragTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int tileIndex;
        public int correctSlotIndex;
        public int currentSlotIndex = -1;
        public Image image;
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform;
        private PetaPuzzleController controller;

        private void Awake()
        {
            image = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Init(int index, int slotIndex, PetaPuzzleController ctrl)
        {
            tileIndex = index;
            correctSlotIndex = slotIndex;
            controller = ctrl;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (controller == null || !controller.puzzleActive || controller.puzzleSolved) return;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (controller == null || !controller.puzzleActive || controller.puzzleSolved) return;
            rectTransform.anchoredPosition += eventData.delta / controller.GetCanvasScale();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (controller == null || !controller.puzzleActive || controller.puzzleSolved) return;

            RaycastResult rayResult = eventData.pointerCurrentRaycast;
            GameObject droppedOn = rayResult.gameObject;
            if (droppedOn != null)
            {
                DragTile otherTile = droppedOn.GetComponentInParent<DragTile>();
                if (otherTile != null && otherTile != this)
                {
                    controller.SwapTiles(this, otherTile);
                    return;
                }

                TileDropZone zone = droppedOn.GetComponentInParent<TileDropZone>();
                if (zone != null)
                {
                    DragTile occupant = controller.tileSlots[zone.slotIndex].currentTile;
                    if (occupant != null && occupant != this)
                    {
                        controller.SwapTiles(this, occupant);
                        return;
                    }
                }
            }

            ReturnToStart();
        }

        public void ReturnToStart()
        {
            if (controller != null && currentSlotIndex >= 0)
            {
                TileSlot slot = controller.tileSlots[currentSlotIndex];
                rectTransform.anchoredPosition = slot.slotRect.anchoredPosition;
            }
        }
    }

    private class TileDropZone : MonoBehaviour
    {
        public int slotIndex;
    }

    private void Start()
    {
        var outlineTransform = transform.Find("HoverOutline");
        if (outlineTransform != null)
        {
            hoverOutline = outlineTransform.gameObject;
            SpriteRenderer sr = hoverOutline.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white;
                sr.sortingOrder = 0;
                outlineTransform.localScale = new Vector3(1.15f, 1.15f, 1f);
            }
        }

        boxCollider = GetComponent<BoxCollider2D>();

        if (puzzleCanvas != null)
        {
            puzzleGroup = puzzleCanvas.GetComponent<CanvasGroup>();
            if (puzzleGroup == null)
                puzzleGroup = puzzleCanvas.gameObject.AddComponent<CanvasGroup>();
            puzzleCanvas.gameObject.SetActive(false);
            coroutineRunner = puzzleCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (coroutineRunner == null)
                coroutineRunner = puzzleCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (cameraButton != null)
            cameraButton.onClick.AddListener(OnCameraClicked);
    }

    private void Update()
    {
        if (isHovered && Input.GetKeyDown(KeyCode.E))
        {
            OpenPuzzle();
            SetOutline(false);
            isHovered = false;
        }

        if (puzzleActive && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzle();
        }

        if (puzzleActive && !isPhotoMode && Input.GetKeyDown(KeyCode.Space))
        {
            OnCameraClicked();
        }
    }

    private string FormatTime(float t)
    {
        int min = Mathf.FloorToInt(t / 60);
        int sec = Mathf.FloorToInt(t % 60);
        return string.Format("{0}:{1:D2}", min, sec);
    }

    private IEnumerator TimerCoroutine()
    {
        while (timerRunning && puzzleActive)
        {
            timeRemaining -= Time.deltaTime;
            if (timerDisplay != null)
                timerDisplay.text = FormatTime(timeRemaining);
            if (timeRemaining <= 0)
            {
                timerRunning = false;
                ResetPuzzle();
                yield break;
            }
            yield return null;
        }
    }

    private float GetCanvasScale()
    {
        if (puzzleCanvas == null) return 1f;
        return puzzleCanvas.scaleFactor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!puzzleActive && !puzzleSolved)
        {
            OpenPuzzle();
            SetOutline(false);
            isHovered = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!puzzleActive && !puzzleSolved)
        {
            isHovered = true;
            SetOutline(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        SetOutline(false);
    }

    private void SetOutline(bool enable)
    {
        if (hoverOutline != null)
            hoverOutline.SetActive(enable);
    }

    private void OpenPuzzle()
    {
        if (Time.frameCount == lastTriggerFrame) return;
        lastTriggerFrame = Time.frameCount;

        puzzleActive = true;
        puzzleSolved = false;
        isPhotoMode = false;
        timeRemaining = puzzleTimeLimit;
        timerRunning = true;

        if (puzzleCanvas != null)
        {
            puzzleCanvas.gameObject.SetActive(true);
            if (cameraButton != null)
                cameraButton.gameObject.SetActive(true);
            CreateDarkBackground();
            GeneratePuzzle();
            if (timerDisplay != null)
                timerDisplay.text = FormatTime(timeRemaining);
            if (coroutineRunner != null)
                timerCoroutine = coroutineRunner.StartCoroutine(TimerCoroutine());
            if (puzzleGroup != null && coroutineRunner != null)
                coroutineRunner.StartCoroutine(FadeCanvasGroup(puzzleGroup, 0f, 1f, () =>
                {
                    if (puzzleGroup != null)
                    {
                        puzzleGroup.interactable = true;
                        puzzleGroup.blocksRaycasts = true;
                    }
                }));
        }

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;
    }

    public void ClosePuzzle()
    {
        if (!puzzleActive) return;

        puzzleActive = false;

        if (puzzleGroup != null && coroutineRunner != null)
            coroutineRunner.StartCoroutine(FadeCanvasGroup(puzzleGroup, 1f, 0f, () =>
            {
                if (puzzleCanvas != null) puzzleCanvas.gameObject.SetActive(false);
                DestroyTiles();
                DestroyDarkBackground();
                if (!puzzleSolved)
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null) sr.enabled = true;
                    if (boxCollider != null) boxCollider.enabled = true;
                }
            }));
    }

    private void GeneratePuzzle()
    {
        if (puzzleSprite == null || puzzleContainer == null) return;

        sourceTexture = puzzleSprite.texture;
        if (sourceTexture == null) return;

        if (!sourceTexture.isReadable)
        {
            sourceTexture = MakeReadable(sourceTexture);
            if (sourceTexture == null) return;
        }

        int rows = puzzleRows;
        int cols = puzzleCols;
        totalTiles = rows * cols;

        Rect spriteRect = puzzleSprite.rect;
        float spriteAspect = spriteRect.width / spriteRect.height;
        float containerW = Screen.width * 0.7f;
        float containerH = containerW / spriteAspect;
        puzzleContainer.anchorMin = new Vector2(0.5f, 0.5f);
        puzzleContainer.anchorMax = new Vector2(0.5f, 0.5f);
        puzzleContainer.pivot = new Vector2(0.5f, 0.5f);
        puzzleContainer.sizeDelta = new Vector2(containerW, containerH);
        puzzleContainer.anchoredPosition = Vector2.zero;
        float displayTileW = containerW / cols;
        float displayTileH = containerH / rows;

        tileSlots.Clear();
        for (int i = 0; i < totalTiles; i++)
        {
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.transform.SetParent(puzzleContainer, false);
            RectTransform slotRt = slotObj.AddComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(displayTileW, displayTileH);

            int row = i / cols;
            int col = i % cols;
            float x = col * displayTileW - containerW / 2f + displayTileW / 2f;
            float y = -(row * displayTileH - containerH / 2f + displayTileH / 2f);
            slotRt.anchoredPosition = new Vector2(x, y);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            TileDropZone zone = slotObj.AddComponent<TileDropZone>();
            zone.slotIndex = i;

            TileSlot slot = new TileSlot();
            slot.correctIndex = i;
            slot.slotRect = slotRt;
            slot.currentTile = null;
            tileSlots.Add(slot);
        }

        int[] assignedSlots = new int[totalTiles];
        for (int i = 0; i < totalTiles; i++) assignedSlots[i] = i;
        for (int i = 0; i < totalTiles; i++)
        {
            int r = Random.Range(i, totalTiles);
            int tmp = assignedSlots[i]; assignedSlots[i] = assignedSlots[r]; assignedSlots[r] = tmp;
        }

        for (int i = 0; i < totalTiles; i++)
        {
            int slotIdx = assignedSlots[i];

            GameObject tileObj = new GameObject("DragTile_" + i);
            tileObj.transform.SetParent(puzzleContainer, false);
            RectTransform rt = tileObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(displayTileW, displayTileH);
            rt.anchoredPosition = tileSlots[slotIdx].slotRect.anchoredPosition;

            Image img = tileObj.AddComponent<Image>();
            img.preserveAspect = true;
            img.sprite = CreateTileSprite(i);
            img.color = Color.white;

            CanvasGroup cg = tileObj.AddComponent<CanvasGroup>();
            DragTile dragTile = tileObj.AddComponent<DragTile>();
            dragTile.Init(i, i, this);
            dragTile.rectTransform = rt;
            dragTile.currentSlotIndex = slotIdx;
            tileSlots[slotIdx].currentTile = dragTile;
        }
    }

    private Texture2D MakeReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readable = new Texture2D(source.width, source.height);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    private Sprite CreateTileSprite(int index)
    {
        if (sourceTexture == null) return null;

        int cols = puzzleCols;
        int rows = puzzleRows;
        int row = index / cols;
        int col = index % cols;

        Rect spriteRect = puzzleSprite.rect;
        float tileW = spriteRect.width / cols;
        float tileH = spriteRect.height / rows;
        float x = spriteRect.x + col * tileW;
        float y = spriteRect.y + spriteRect.height - (row + 1) * tileH;
        Rect pixelRect = new Rect(x, y, tileW, tileH);

        return Sprite.Create(sourceTexture, pixelRect, new Vector2(0.5f, 0.5f), puzzleSprite.pixelsPerUnit);
    }

    private Sprite CreateSolidWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void SwapTiles(DragTile a, DragTile b)
    {
        Vector2 tempPos = a.rectTransform.anchoredPosition;
        a.rectTransform.anchoredPosition = b.rectTransform.anchoredPosition;
        b.rectTransform.anchoredPosition = tempPos;

        int tempSlot = a.currentSlotIndex;
        a.currentSlotIndex = b.currentSlotIndex;
        b.currentSlotIndex = tempSlot;

        tileSlots[a.currentSlotIndex].currentTile = a;
        tileSlots[b.currentSlotIndex].currentTile = b;

        CheckWin();
    }

    private void CheckWin()
    {
        for (int i = 0; i < totalTiles; i++)
        {
            if (tileSlots[i].currentTile == null || tileSlots[i].currentTile.correctSlotIndex != i)
                return;
        }

        timerRunning = false;
        puzzleActive = false;
        puzzleSolved = true;
        if (coroutineRunner != null)
            coroutineRunner.StartCoroutine(OnSolved());
    }

    private IEnumerator OnSolved()
    {
        yield return new WaitForSeconds(0.5f);

        DestroyTiles();
        for (int i = tileSlots.Count - 1; i >= 0; i--)
        {
            if (tileSlots[i].slotRect != null)
                Destroy(tileSlots[i].slotRect.gameObject);
        }
        tileSlots.Clear();

        Image resultImg = puzzleContainer.gameObject.AddComponent<Image>();
        resultImg.sprite = puzzleSprite;
        resultImg.preserveAspect = true;
        resultImg.color = Color.white;

        yield return new WaitForSeconds(0.3f);
    }

    private void OnCameraClicked()
    {
        if (isPhotoMode || CameraManager.Instance == null) return;
        isPhotoMode = true;
        if (cameraButton != null) cameraButton.gameObject.SetActive(false);
        CameraManager.Instance.Capture(
            onCaptured: (photo) =>
            {
                if (ObjectiveManager.Instance != null)
                    ObjectiveManager.Instance.RegisterPhoto("peta");
                isPhotoMode = false;
                if (cameraButton != null) cameraButton.gameObject.SetActive(true);
            },
            onCancelled: () =>
            {
                isPhotoMode = false;
                if (cameraButton != null) cameraButton.gameObject.SetActive(true);
            }
        );
    }

    private void CreateDarkBackground()
    {
        if (puzzleCanvas == null) return;

        darkBackground = new GameObject("PuzzleDarkBackground");
        darkBackground.transform.SetParent(puzzleCanvas.transform, false);
        darkBackground.transform.SetSiblingIndex(0);

        Image bgImage = darkBackground.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.85f);

        RectTransform rect = darkBackground.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    private void DestroyDarkBackground()
    {
        if (darkBackground != null)
            Destroy(darkBackground);
        darkBackground = null;
    }

    private void DestroyTiles()
    {
        var dragTiles = FindObjectsOfType<DragTile>();
        foreach (var t in dragTiles)
        {
            if (t != null && t.gameObject != null)
                Destroy(t.gameObject);
        }
    }

    private void ResetPuzzle()
    {
        DestroyTiles();
        DestroyDarkBackground();
        if (coroutineRunner != null)
            coroutineRunner.StartCoroutine(DelayedRegenerate());
    }

    private IEnumerator DelayedRegenerate()
    {
        yield return null;
        GeneratePuzzle();
        timeRemaining = puzzleTimeLimit;
        timerRunning = true;
        if (timerDisplay != null)
            timerDisplay.text = FormatTime(timeRemaining);
        if (coroutineRunner != null)
            timerCoroutine = coroutineRunner.StartCoroutine(TimerCoroutine());
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, System.Action onComplete)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / transitionDuration);
            yield return null;
        }
        cg.alpha = to;
        onComplete?.Invoke();
    }
}
