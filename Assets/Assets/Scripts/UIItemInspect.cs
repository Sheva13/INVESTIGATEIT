using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIItemInspect : MonoBehaviour
{
    [Header("Item References")]
    [SerializeField] private GameObject closedItemObject;
    
    [Header("Preview References")]
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private Image previewImage;
    [SerializeField] private Button previewOpenButton;
    [SerializeField] private Button previewEscButton;

    [Header("Open References")]
    [SerializeField] private GameObject openPanel;
    [SerializeField] private Image openImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button captureButton;

    [Header("Item Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;

    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.3f;

    private bool isPreviewing = false;
    private bool isInspecting = false;
    private bool showingFront = true;
    
    private CanvasGroup previewCanvasGroup;
    private CanvasGroup openCanvasGroup;
    private GameObject darkBackgroundObj;
    private CanvasGroup darkBgCanvasGroup;
    private Coroutine transitionCoroutine;

    private void Start()
    {
        InitializeComponents();
        SetupInitialState();

        if (previewOpenButton != null)
            previewOpenButton.onClick.AddListener(OpenInspectFromPreview);

        if (previewEscButton != null)
            previewEscButton.onClick.AddListener(ClosePreview);

        if (nextButton != null)
            nextButton.onClick.AddListener(FlipItem);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInspectToPreview);

        if (captureButton != null)
            captureButton.onClick.AddListener(OnCaptureClicked);

        UpdateImage();
    }

    private void Update()
    {
        if (isPreviewing && previewPanel != null && previewPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                OpenInspectFromPreview();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePreview();
            }
        }
        else if (isInspecting && openPanel != null && openPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInspectToPreview();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space))
            {
                FlipItem();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                OnCaptureClicked();
            }
        }
    }

    private void InitializeComponents()
    {
        if (previewPanel != null)
        {
            previewCanvasGroup = previewPanel.GetComponent<CanvasGroup>();
            if (previewCanvasGroup == null)
                previewCanvasGroup = previewPanel.AddComponent<CanvasGroup>();
        }

        if (openPanel != null)
        {
            openCanvasGroup = openPanel.GetComponent<CanvasGroup>();
            if (openCanvasGroup == null)
                openCanvasGroup = openPanel.AddComponent<CanvasGroup>();
        }

        CreateDarkBackground();
    }

    private void CreateDarkBackground()
    {
        Canvas parentCanvas = null;
        if (previewPanel != null)
            parentCanvas = previewPanel.GetComponentInParent<Canvas>();
        else if (openPanel != null)
            parentCanvas = openPanel.GetComponentInParent<Canvas>();
            
        if (parentCanvas == null) return;

        darkBackgroundObj = new GameObject("DarkBackgroundOverlay_Inspect_" + gameObject.name);
        darkBackgroundObj.transform.SetParent(parentCanvas.transform, false);
        darkBackgroundObj.transform.SetSiblingIndex(0);

        Image bgImage = darkBackgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.85f);

        RectTransform rect = darkBackgroundObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        darkBgCanvasGroup = darkBackgroundObj.AddComponent<CanvasGroup>();
        darkBgCanvasGroup.alpha = 0f;
        darkBgCanvasGroup.blocksRaycasts = false;
        darkBgCanvasGroup.interactable = false;

        darkBackgroundObj.SetActive(true);
    }

    private void SetupInitialState()
    {
        isPreviewing = false;
        isInspecting = false;
        showingFront = true;

        if (closedItemObject != null)
            closedItemObject.SetActive(true);

        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 0f;
            previewCanvasGroup.interactable = false;
            previewCanvasGroup.blocksRaycasts = false;
            if (previewPanel != null) previewPanel.SetActive(false);
        }

        if (openCanvasGroup != null)
        {
            openCanvasGroup.alpha = 0f;
            openCanvasGroup.interactable = false;
            openCanvasGroup.blocksRaycasts = false;
            if (openPanel != null) openPanel.SetActive(false);
        }

        UpdateImage();
    }

    public void OnItemTriggered()
    {
        if (!isPreviewing && !isInspecting)
            ShowPreviewPanel();
    }

    private void ShowPreviewPanel()
    {
        if (isPreviewing) return;
        isPreviewing = true;
        isInspecting = false;
        showingFront = true;
        UpdateImage();

        if (closedItemObject != null)
            closedItemObject.SetActive(false);

        if (previewPanel != null)
            previewPanel.SetActive(true);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(FadePreview(true));
    }

    private void ClosePreview()
    {
        if (!isPreviewing) return;
        isPreviewing = false;
        isInspecting = false;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(FadePreview(false));
    }
    
    public void OpenInspectFromPreview()
    {
        if (!isPreviewing) return;
        isPreviewing = false;
        isInspecting = true;
        showingFront = true;
        UpdateImage();

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionPreviewToOpen());
    }

    public void CloseInspectToPreview()
    {
        if (!isInspecting) return;
        isInspecting = false;
        isPreviewing = true;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionOpenToPreview());
    }

    public void FlipItem()
    {
        if (!isInspecting) return;
        showingFront = !showingFront;
        UpdateImage();
    }

    private void UpdateImage()
    {
        if (previewImage != null)
        {
            previewImage.sprite = frontSprite;
        }

        if (openImage != null)
        {
            openImage.sprite = showingFront ? frontSprite : backSprite;
        }
    }

    private void OnCaptureClicked()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.Capture(
                onCaptured: (photo) =>
                {
                    ObjectiveManager.Instance.RegisterPhoto("frame");
                },
                onCancelled: () =>
                {
                    Debug.Log("Foto frame: Photo capture cancelled");
                }
            );
        }
        else
        {
            Debug.LogWarning("CameraManager not found!");
        }
    }

    private IEnumerator FadePreview(bool show)
    {
        float startAlpha = previewCanvasGroup != null ? previewCanvasGroup.alpha : (show ? 0f : 1f);
        float endAlpha = show ? 1f : 0f;
        float bgStartAlpha = darkBgCanvasGroup != null ? darkBgCanvasGroup.alpha : startAlpha;

        if (show)
        {
            if (previewPanel != null) previewPanel.SetActive(true);
            if (darkBgCanvasGroup != null) darkBgCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            if (previewCanvasGroup != null)
            {
                previewCanvasGroup.interactable = false;
                previewCanvasGroup.blocksRaycasts = false;
            }
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            if (previewCanvasGroup != null)
                previewCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            if (darkBgCanvasGroup != null)
                darkBgCanvasGroup.alpha = Mathf.Lerp(bgStartAlpha, endAlpha, t);
                
            if (previewPanel != null)
                previewPanel.transform.localScale = Vector3.Lerp(show ? new Vector3(0.8f, 0.8f, 1f) : Vector3.one, show ? Vector3.one : new Vector3(0.8f, 0.8f, 1f), t);

            yield return null;
        }

        if (previewCanvasGroup != null)
            previewCanvasGroup.alpha = endAlpha;
        if (darkBgCanvasGroup != null)
            darkBgCanvasGroup.alpha = endAlpha;
        if (previewPanel != null)
            previewPanel.transform.localScale = show ? Vector3.one : new Vector3(0.8f, 0.8f, 1f);

        if (show)
        {
            if (previewCanvasGroup != null)
            {
                previewCanvasGroup.interactable = true;
                previewCanvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            if (previewPanel != null) previewPanel.SetActive(false);
            if (darkBgCanvasGroup != null) darkBgCanvasGroup.blocksRaycasts = false;

            if (closedItemObject != null)
                closedItemObject.SetActive(true);
        }
    }

    private IEnumerator TransitionPreviewToOpen()
    {
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.interactable = false;
            previewCanvasGroup.blocksRaycasts = false;
        }

        if (openPanel != null)
            openPanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            if (previewCanvasGroup != null)
                previewCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (openCanvasGroup != null)
                openCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                
            if (openPanel != null)
                openPanel.transform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, t);

            yield return null;
        }

        if (previewCanvasGroup != null)
            previewCanvasGroup.alpha = 0f;
        if (previewPanel != null) 
            previewPanel.SetActive(false);

        if (openCanvasGroup != null)
        {
            openCanvasGroup.alpha = 1f;
            openCanvasGroup.interactable = true;
            openCanvasGroup.blocksRaycasts = true;
        }
        if (openPanel != null)
            openPanel.transform.localScale = Vector3.one;
    }

    private IEnumerator TransitionOpenToPreview()
    {
        if (openCanvasGroup != null)
        {
            openCanvasGroup.interactable = false;
            openCanvasGroup.blocksRaycasts = false;
        }

        if (previewPanel != null)
            previewPanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            if (openCanvasGroup != null)
                openCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (previewCanvasGroup != null)
                previewCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                
            if (openPanel != null)
                openPanel.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.8f, 0.8f, 1f), t);

            yield return null;
        }

        if (openCanvasGroup != null)
            openCanvasGroup.alpha = 0f;
        if (openPanel != null) 
            openPanel.SetActive(false);

        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 1f;
            previewCanvasGroup.interactable = true;
            previewCanvasGroup.blocksRaycasts = true;
        }
    }
}
