using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIGalleryController : MonoBehaviour
{
    private static UIGalleryController _instance;
    public static UIGalleryController Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }
    [Header("Dark Background")]
    [SerializeField] private CanvasGroup darkBg;

    [Header("Info Panel")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Image infoImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button openBtn;
    [SerializeField] private Button escBtn;

    [Header("Large Panel")]
    [SerializeField] private GameObject largePanel;
    [SerializeField] private Image largeImage;
    [SerializeField] private Button largeCloseBtn;

    [Header("World Object References")]
    [SerializeField] private GameObject bookWorld;
    [SerializeField] private GameObject laptopWorld;
    [SerializeField] private GameObject frameWorld;

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.3f;

    private void Start()
    {
        if (escBtn != null) escBtn.onClick.AddListener(Close);
        if (openBtn != null) openBtn.onClick.AddListener(OpenLarge);
        if (largeCloseBtn != null) largeCloseBtn.onClick.AddListener(Close);

        HideAll();
    }

    private void Update()
    {
        if (infoPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
        if (largePanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
        if (infoPanel.activeSelf && Input.GetKeyDown(KeyCode.O)) OpenLarge();
    }

    public void ShowItem(GalleryItemData data)
    {
        if (data == null) return;

        if (infoImage != null) infoImage.sprite = data.thumbnail ?? data.fullImage;
        if (titleText != null) titleText.text = data.title;
        if (descText != null) descText.text = data.description;
        if (largeImage != null) largeImage.sprite = data.fullImage ?? data.thumbnail;

        StopAllCoroutines();
        StartCoroutine(AnimateShow());
    }

    public void OpenLarge()
    {
        infoPanel.SetActive(false);
        largePanel.SetActive(true);
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateHide());
    }

    private void SetWorldColliders(bool enable)
    {
        foreach (var obj in new[] { bookWorld, laptopWorld, frameWorld })
        {
            if (obj != null)
            {
                var col = obj.GetComponent<Collider2D>();
                if (col != null) col.enabled = enable;
            }
        }
    }

    private void HideAll()
    {
        if (darkBg != null) { darkBg.alpha = 0f; darkBg.blocksRaycasts = false; darkBg.gameObject.SetActive(false); }
        if (infoPanel != null) infoPanel.SetActive(false);
        if (largePanel != null) largePanel.SetActive(false);

        SetWorldColliders(true);
    }

    private IEnumerator AnimateShow()
    {
        SetWorldColliders(false);

        if (darkBg != null)
        {
            darkBg.gameObject.SetActive(true);
            darkBg.alpha = 0f;
            darkBg.blocksRaycasts = true;
        }

        infoPanel.SetActive(true);
        infoPanel.transform.localScale = Vector3.one * 0.8f;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            if (darkBg != null)
                darkBg.alpha = Mathf.Lerp(0f, 0.85f, t);

            infoPanel.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);

            yield return null;
        }

        if (darkBg != null) darkBg.alpha = 0.85f;
        infoPanel.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateHide()
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            if (darkBg != null)
                darkBg.alpha = Mathf.Lerp(darkBg.alpha, 0f, t);

            yield return null;
        }

        HideAll();
    }
}
