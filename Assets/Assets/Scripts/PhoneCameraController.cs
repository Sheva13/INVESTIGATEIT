using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

public class PhoneCameraController : MonoBehaviour
{
    [Header("Camera UI References")]
    [SerializeField] private CanvasGroup phoneCanvasGroup;
    [SerializeField] private GameObject photoResultPanel;
    [SerializeField] private Button phoneCaptureBtn;
    [SerializeField] private Button phoneCloseBtn;
    [SerializeField] private Button galleryBtn;
    [SerializeField] private Button saveBtn;
    [SerializeField] private Button retakeBtn;
    [SerializeField] private RawImage capturedImage;

    [Header("Viewfinder")]
    [SerializeField] private RectTransform viewfinderArea;

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.3f;

    private System.Action<Texture2D> onCaptureComplete;
    private System.Action onCaptureCancelled;
    private bool isResultMode = false;

    private void Start()
    {
        if (phoneCaptureBtn != null) phoneCaptureBtn.onClick.AddListener(OnCaptureClicked);
        if (phoneCloseBtn != null) phoneCloseBtn.onClick.AddListener(OnCloseClicked);
        if (galleryBtn != null) galleryBtn.onClick.AddListener(OnGalleryClicked);
        if (saveBtn != null) saveBtn.onClick.AddListener(OnSaveClicked);
        if (retakeBtn != null) retakeBtn.onClick.AddListener(OnRetakeClicked);
    }

    public void Show(System.Action<Texture2D> onCaptured, System.Action onCancelled = null)
    {
        onCaptureComplete = onCaptured;
        onCaptureCancelled = onCancelled;
        isResultMode = false;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (photoResultPanel != null) photoResultPanel.SetActive(false);

        StartCoroutine(AnimateShow());
    }

    public void Hide()
    {
        StartCoroutine(AnimateHide());
    }

    private IEnumerator AnimateShow()
    {
        if (phoneCanvasGroup == null) yield break;

        phoneCanvasGroup.interactable = false;
        phoneCanvasGroup.blocksRaycasts = false;
        phoneCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            phoneCanvasGroup.alpha = t;
            transform.localScale = Vector3.Lerp(new Vector3(1.1f, 1.1f, 1f), Vector3.one, t);
            yield return null;
        }

        phoneCanvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
        phoneCanvasGroup.interactable = true;
        phoneCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateHide()
    {
        if (phoneCanvasGroup == null) yield break;

        phoneCanvasGroup.interactable = false;
        phoneCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            phoneCanvasGroup.alpha = 1f - t;
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.9f, 0.9f, 1f), t);
            yield return null;
        }

        phoneCanvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnCaptureClicked()
    {
        if (isResultMode) return;
        StartCoroutine(CaptureSequence());
    }

    private IEnumerator CaptureSequence()
    {
        isResultMode = true;
        if (phoneCaptureBtn != null) phoneCaptureBtn.interactable = false;

        FlashEffect();

        yield return new WaitForSeconds(0.15f);

        string tempPath = Application.dataPath + "/Captures/_temp_capture.png";
        string dir = Path.GetDirectoryName(tempPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        if (phoneCanvasGroup != null)
        {
            phoneCanvasGroup.alpha = 0f;
            phoneCanvasGroup.interactable = false;
            phoneCanvasGroup.blocksRaycasts = false;
        }

        yield return new WaitForEndOfFrame();

        ScreenCapture.CaptureScreenshot(tempPath);

        yield return new WaitForSeconds(0.1f);

        if (phoneCaptureBtn != null) phoneCaptureBtn.interactable = true;

        var tex = LoadTempCapture(tempPath);
        StartCoroutine(FinalizeCapture(tex));
    }

    private Texture2D LoadTempCapture(string path)
    {
        if (!File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        return tex;
    }

    private IEnumerator FinalizeCapture(Texture2D result)
    {
        yield return new WaitForEndOfFrame();
        if (result != null)
            onCaptureComplete?.Invoke(result);
        else
            onCaptureCancelled?.Invoke();
    }

    private void FlashEffect()
    {
        var flash = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        flash.transform.SetParent(transform, false);
        var rect = flash.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var img = flash.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        Destroy(flash, 0.15f);
    }

    private void OnSaveClicked()
    {
        string srcPath = Application.dataPath + "/Captures/_temp_capture.png";
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string destPath = Application.dataPath + "/Captures/Capture_" + timestamp + ".png";

        if (File.Exists(srcPath))
        {
            File.Copy(srcPath, destPath, true);
            Debug.Log("Photo saved: " + destPath);
        }

        var tex = capturedImage != null ? capturedImage.texture as Texture2D : null;
        StartCoroutine(HideAndCallback(tex));
    }

    private void OnRetakeClicked()
    {
        if (photoResultPanel != null)
        {
            var cg = photoResultPanel.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
            photoResultPanel.SetActive(false);
        }

        isResultMode = false;
        Show(onCaptureComplete, onCaptureCancelled);
    }

    private void OnCloseClicked()
    {
        StartCoroutine(HideAndCallback(null));
    }

    private void OnGalleryClicked()
    {
        Debug.Log("Gallery feature coming soon.");
    }

    private IEnumerator HideAndCallback(Texture2D result)
    {
        yield return StartCoroutine(AnimateHide());

        if (result != null)
            onCaptureComplete?.Invoke(result);
        else
            onCaptureCancelled?.Invoke();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (isResultMode)
        {
            if (Input.GetKeyDown(KeyCode.S)) OnSaveClicked();
            else if (Input.GetKeyDown(KeyCode.R)) OnRetakeClicked();
            else if (Input.GetKeyDown(KeyCode.Escape)) OnRetakeClicked();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.C)) OnCaptureClicked();
            else if (Input.GetKeyDown(KeyCode.Escape)) OnCloseClicked();
            else if (Input.GetKeyDown(KeyCode.G)) OnGalleryClicked();
        }
    }
}
