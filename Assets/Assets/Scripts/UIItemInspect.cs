using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIItemInspect : MonoBehaviour
{
    [Header("Item References")]
    [SerializeField] private GameObject closedItemObject; // Object in world
    [SerializeField] private GameObject inspectPanel;     // Panel to show the big item
    [SerializeField] private Image inspectImage;          // The UI Image component

    [Header("Item Sprites")]
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;

    [Header("Navigation Buttons")]
    [SerializeField] private Button flipButton;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.3f;

    private bool isInspecting = false;
    private bool showingFront = true;
    private CanvasGroup inspectCanvasGroup;
    private GameObject darkBackgroundObj;
    private CanvasGroup darkBgCanvasGroup;
    private Coroutine transitionCoroutine;

    private void Start()
    {
        InitializeComponents();
        SetupInitialState();

        if (flipButton != null)
            flipButton.onClick.AddListener(FlipItem);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInspect);
    }

    private void Update()
    {
        if (isInspecting && inspectPanel != null && inspectPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInspect();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || 
                     Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || 
                     Input.GetKeyDown(KeyCode.Space))
            {
                FlipItem();
            }
        }
    }

    private void InitializeComponents()
    {
        if (inspectPanel != null)
        {
            inspectCanvasGroup = inspectPanel.GetComponent<CanvasGroup>();
            if (inspectCanvasGroup == null)
                inspectCanvasGroup = inspectPanel.AddComponent<CanvasGroup>();
            
            CreateDarkBackground();
        }
    }

    private void CreateDarkBackground()
    {
        if (inspectPanel == null) return;
        Canvas parentCanvas = inspectPanel.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        darkBackgroundObj = new GameObject("DarkBackgroundOverlay_Inspect_" + gameObject.name);
        darkBackgroundObj.transform.SetParent(parentCanvas.transform, false);
        darkBackgroundObj.transform.SetSiblingIndex(0); // put behind

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
        isInspecting = false;
        showingFront = true;

        if (closedItemObject != null)
            closedItemObject.SetActive(true);

        if (inspectPanel != null)
        {
            inspectCanvasGroup.alpha = 0f;
            inspectCanvasGroup.interactable = false;
            inspectCanvasGroup.blocksRaycasts = false;
            inspectPanel.SetActive(false);
        }

        UpdateImage();
    }

    public void OnItemTriggered()
    {
        if (!isInspecting)
            OpenInspect();
    }

    private void OpenInspect()
    {
        if (isInspecting) return;
        isInspecting = true;
        showingFront = true;
        UpdateImage();

        if (closedItemObject != null)
            closedItemObject.SetActive(false);

        if (inspectPanel != null)
            inspectPanel.SetActive(true);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(FadeTransition(true));
    }

    private void CloseInspect()
    {
        if (!isInspecting) return;
        isInspecting = false;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(FadeTransition(false));
    }

    private void FlipItem()
    {
        if (!isInspecting) return;
        showingFront = !showingFront;
        UpdateImage();
        
        // Optional: play a flip sound or small animation here
    }

    private void UpdateImage()
    {
        if (inspectImage != null)
        {
            inspectImage.sprite = showingFront ? frontSprite : backSprite;
            // set native size to ensure proper aspect ratio if needed
            inspectImage.SetNativeSize();
        }
    }

    private IEnumerator FadeTransition(bool show)
    {
        float startAlpha = inspectCanvasGroup != null ? inspectCanvasGroup.alpha : (show ? 0f : 1f);
        float endAlpha = show ? 1f : 0f;
        
        float bgStartAlpha = darkBgCanvasGroup != null ? darkBgCanvasGroup.alpha : startAlpha;
        
        float elapsed = 0f;

        if (show)
        {
            if (inspectPanel != null) inspectPanel.SetActive(true);
            if (darkBgCanvasGroup != null) darkBgCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            if (inspectCanvasGroup != null)
            {
                inspectCanvasGroup.interactable = false;
                inspectCanvasGroup.blocksRaycasts = false;
            }
        }

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            if (inspectCanvasGroup != null)
                inspectCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            if (darkBgCanvasGroup != null)
                darkBgCanvasGroup.alpha = Mathf.Lerp(bgStartAlpha, endAlpha, t);

            yield return null;
        }

        if (inspectCanvasGroup != null)
            inspectCanvasGroup.alpha = endAlpha;
        if (darkBgCanvasGroup != null)
            darkBgCanvasGroup.alpha = endAlpha;

        if (show)
        {
            if (inspectCanvasGroup != null)
            {
                inspectCanvasGroup.interactable = true;
                inspectCanvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            if (inspectPanel != null) inspectPanel.SetActive(false);
            if (darkBgCanvasGroup != null) darkBgCanvasGroup.blocksRaycasts = false;
            
            if (closedItemObject != null)
                closedItemObject.SetActive(true);
        }
    }
}
