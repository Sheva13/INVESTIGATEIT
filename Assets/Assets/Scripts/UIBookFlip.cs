using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class UIBookFlip : MonoBehaviour
{
    [Header("Book References")]
    [SerializeField] private GameObject closedBookObject;
    [SerializeField] private GameObject openBookObject;

    [Header("Item Preview References")]
    [SerializeField] private GameObject bookPreviewPanel;
    [SerializeField] private Button previewKeepButton;
    [SerializeField] private Button previewOpenButton;
    [SerializeField] private Button previewEscButton;

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI leftPageText;
    [SerializeField] private TextMeshProUGUI rightPageText;

    [Header("Navigation Button References")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float flipDuration = 0.5f;

    [Header("Book Content")]
    [SerializeField, TextArea(3, 10)] private List<string> bookPages = new List<string>()
    {
        "Halaman 1: Ini adalah contoh isi halaman sebelah kiri dari buku penyelidikan.",
        "Halaman 2: Ini adalah contoh isi halaman sebelah kanan.",
        "Halaman 3: Ini adalah halaman kedua sebelah kiri untuk melanjutkan cerita.",
        "Halaman 4: Dan ini adalah kelanjutan di halaman sebelah kanan.",
        "Halaman 5: Petunjuk penting: ",
        "Halaman 6: Selesai!"
    };

    private bool isBookOpen = false;
    private CanvasGroup closedBookCanvasGroup;
    private CanvasGroup openBookCanvasGroup;
    private Coroutine flipCoroutine;
    private float lastFlipTime = -1f;
    private int currentPageIndex = 0;

    private bool isItemKept = false;
    private CanvasGroup previewCanvasGroup;
    private GameObject darkBackgroundObj;
    private CanvasGroup darkBgCanvasGroup;

    private void Start()
    {
        InitializeComponents();
        SetupInitialState();

        // Tambahkan listener klik pada tombol buku tutup (jika berupa UI Button)
        if (closedBookObject != null)
        {
            Button closedBookButton = closedBookObject.GetComponent<Button>();
            if (closedBookButton != null)
            {
                closedBookButton.onClick.AddListener(OnBookTriggered);
            }
        }

        // Tambahkan listener tombol navigasi
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(PreviousPage);
        }
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextPage);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(FlipBook);
        }

        // Tambahkan listener preview panel
        if (previewKeepButton != null)
        {
            previewKeepButton.onClick.AddListener(KeepBookItem);
        }
        if (previewOpenButton != null)
        {
            previewOpenButton.onClick.AddListener(OpenBookFromPreview);
        }
        if (previewEscButton != null)
        {
            previewEscButton.onClick.AddListener(ClosePreview);
        }

        UpdatePageContent();
    }

    private void Update()
    {
        // Keyboard hotkeys for BookPreviewPanel
        if (bookPreviewPanel != null && bookPreviewPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                KeepBookItem();
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                OpenBookFromPreview();
            }
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePreview();
            }
        }
    }

    private void InitializeComponents()
    {
        if (closedBookObject != null)
        {
            closedBookCanvasGroup = closedBookObject.GetComponent<CanvasGroup>();
        }

        if (openBookObject != null)
        {
            openBookGroup();
        }

        CreateDarkBackground();
    }

    private void CreateDarkBackground()
    {
        Canvas parentCanvas = null;
        if (bookPreviewPanel != null)
            parentCanvas = bookPreviewPanel.GetComponentInParent<Canvas>();
        else if (openBookObject != null)
            parentCanvas = openBookObject.GetComponentInParent<Canvas>();
        
        if (parentCanvas == null) return;

        darkBackgroundObj = new GameObject("DarkBackgroundOverlay");
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

    private void openBookGroup()
    {
        openBookCanvasGroup = openBookObject.GetComponent<CanvasGroup>();
        if (openBookCanvasGroup == null)
        {
            openBookCanvasGroup = openBookObject.AddComponent<CanvasGroup>();
        }

        if (bookPreviewPanel != null)
        {
            previewCanvasGroup = bookPreviewPanel.GetComponent<CanvasGroup>();
            if (previewCanvasGroup == null)
            {
                previewCanvasGroup = bookPreviewPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetupInitialState()
    {
        isBookOpen = false;
        isItemKept = false;
        currentPageIndex = 0;

        if (closedBookObject != null)
        {
            closedBookObject.SetActive(true);
            
            // Reset alpha sprite renderer jika ada
            SpriteRenderer closedSprite = closedBookObject.GetComponent<SpriteRenderer>();
            if (closedSprite != null)
            {
                Color c = closedSprite.color;
                c.a = 1f;
                closedSprite.color = c;
            }

            if (closedBookCanvasGroup != null)
            {
                closedBookCanvasGroup.alpha = 1f;
                closedBookCanvasGroup.interactable = true;
                closedBookCanvasGroup.blocksRaycasts = true;
            }
        }

        if (openBookObject != null)
        {
            openBookObject.SetActive(false);
            if (openBookCanvasGroup != null)
            {
                openBookCanvasGroup.alpha = 0f;
                openBookCanvasGroup.interactable = false;
                openBookCanvasGroup.blocksRaycasts = false;
            }
        }

        if (darkBgCanvasGroup != null)
        {
            darkBgCanvasGroup.alpha = 0f;
            darkBgCanvasGroup.blocksRaycasts = false;
        }

        HidePreviewPanel();
        UpdatePageContent();
    }

    public void OnBookTriggered()
    {
        if (isItemKept)
        {
            FlipBook();
        }
        else
        {
            if (bookPreviewPanel != null)
            {
                bookPreviewPanel.SetActive(true);
                if (previewCanvasGroup != null)
                {
                    previewCanvasGroup.alpha = 1f;
                    previewCanvasGroup.interactable = true;
                    previewCanvasGroup.blocksRaycasts = true;
                }
                if (darkBgCanvasGroup != null)
                {
                    darkBgCanvasGroup.alpha = 1f;
                    darkBgCanvasGroup.blocksRaycasts = true;
                }
                if (closedBookObject != null)
                {
                    closedBookObject.SetActive(false);
                }
            }
        }
    }

    public void KeepBookItem()
    {
        isItemKept = true;
        HidePreviewPanel();
        if (closedBookObject != null)
        {
            closedBookObject.SetActive(true);
        }
    }

    public void OpenBookFromPreview()
    {
        HidePreviewPanel();
        FlipBook();
    }

    public void ClosePreview()
    {
        HidePreviewPanel();
        if (closedBookObject != null)
        {
            closedBookObject.SetActive(true);
        }
    }

    private void HidePreviewPanel()
    {
        if (bookPreviewPanel != null)
        {
            if (previewCanvasGroup != null)
            {
                previewCanvasGroup.alpha = 0f;
                previewCanvasGroup.interactable = false;
                previewCanvasGroup.blocksRaycasts = false;
            }
            if (!isBookOpen && darkBgCanvasGroup != null)
            {
                darkBgCanvasGroup.alpha = 0f;
                darkBgCanvasGroup.blocksRaycasts = false;
            }
            bookPreviewPanel.SetActive(false);
        }
    }

    public void FlipBook()
    {
        if (openBookObject == null)
            return;

        // Cegah flip ganda jika animasi sedang berlangsung
        if (flipCoroutine != null)
            return;

        // Cegah flip ganda dalam waktu yang sangat singkat (debounce 0.2 detik)
        if (Time.time - lastFlipTime < 0.2f)
            return;

        lastFlipTime = Time.time;
        isBookOpen = !isBookOpen;

        if (isBookOpen)
        {
            currentPageIndex = 0;
            UpdatePageContent();
        }

        flipCoroutine = StartCoroutine(AnimateFlip());
    }

    public void NextPage()
    {
        if (currentPageIndex + 2 < bookPages.Count)
        {
            currentPageIndex += 2;
            UpdatePageContent();
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex - 2 >= 0)
        {
            currentPageIndex -= 2;
            UpdatePageContent();
        }
    }

    private void UpdatePageContent()
    {
        // Tampilkan teks halaman kiri
        if (leftPageText != null)
        {
            if (currentPageIndex < bookPages.Count)
            {
                leftPageText.text = bookPages[currentPageIndex];
            }
            else
            {
                leftPageText.text = "";
            }
        }

        // Tampilkan teks halaman kanan
        if (rightPageText != null)
        {
            if (currentPageIndex + 1 < bookPages.Count)
            {
                rightPageText.text = bookPages[currentPageIndex + 1];
            }
            else
            {
                rightPageText.text = "";
            }
        }

        // Atur keaktifan tombol navigasi
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentPageIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentPageIndex + 2 < bookPages.Count);
        }
    }

    private IEnumerator AnimateFlip()
    {
        float closedStartAlpha = isBookOpen ? 1f : 0f;
        float closedEndAlpha = isBookOpen ? 0f : 1f;

        float openStartAlpha = isBookOpen ? 0f : 1f;
        float openEndAlpha = isBookOpen ? 1f : 0f;

        float bgStartAlpha = darkBgCanvasGroup != null ? darkBgCanvasGroup.alpha : (isBookOpen ? 0f : 1f);
        float bgEndAlpha = isBookOpen ? 1f : 0f;

        // Aktifkan objek buku buka jika sedang membuka
        if (isBookOpen && openBookObject != null)
        {
            openBookObject.SetActive(true);
        }

        // Aktifkan objek buku tutup saat mulai menutup agar proses memudarnya terlihat
        if (!isBookOpen && closedBookObject != null)
        {
            closedBookObject.SetActive(true);
        }

        // Nonaktifkan interaksi selama transisi animasi berjalan
        if (closedBookCanvasGroup != null)
        {
            closedBookCanvasGroup.interactable = false;
            closedBookCanvasGroup.blocksRaycasts = false;
        }
        if (openBookCanvasGroup != null)
        {
            openBookCanvasGroup.interactable = false;
            openBookCanvasGroup.blocksRaycasts = false;
        }

        SpriteRenderer closedSprite = closedBookObject != null ? closedBookObject.GetComponent<SpriteRenderer>() : null;

        float elapsedTime = 0f;
        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flipDuration);

            // Lerp alpha buku tutup
            if (closedBookCanvasGroup != null)
            {
                closedBookCanvasGroup.alpha = Mathf.Lerp(closedStartAlpha, closedEndAlpha, t);
            }
            else if (closedSprite != null)
            {
                Color c = closedSprite.color;
                c.a = Mathf.Lerp(closedStartAlpha, closedEndAlpha, t);
                closedSprite.color = c;
            }

            // Lerp alpha buku buka
            if (openBookCanvasGroup != null)
            {
                openBookCanvasGroup.alpha = Mathf.Lerp(openStartAlpha, openEndAlpha, t);
            }

            if (darkBgCanvasGroup != null)
            {
                darkBgCanvasGroup.alpha = Mathf.Lerp(bgStartAlpha, bgEndAlpha, t);
            }

            yield return null;
        }

        // Terapkan nilai akhir secara presisi
        if (darkBgCanvasGroup != null)
        {
            darkBgCanvasGroup.alpha = bgEndAlpha;
            darkBgCanvasGroup.blocksRaycasts = isBookOpen;
        }

        if (closedBookObject != null)
        {
            if (closedBookCanvasGroup != null)
            {
                closedBookCanvasGroup.alpha = closedEndAlpha;
                closedBookCanvasGroup.interactable = !isBookOpen && isItemKept;
                closedBookCanvasGroup.blocksRaycasts = !isBookOpen && isItemKept;
            }
            else if (closedSprite != null)
            {
                Color c = closedSprite.color;
                c.a = closedEndAlpha;
                closedSprite.color = c;
            }
            
            // Sembunyikan buku tutup jika sedang dibuka, ATAU jika buku ditutup tapi belum disimpan
            closedBookObject.SetActive(!isBookOpen && isItemKept);
        }

        if (openBookObject != null)
        {
            if (openBookCanvasGroup != null)
            {
                openBookCanvasGroup.alpha = openEndAlpha;
                openBookCanvasGroup.interactable = isBookOpen;
                openBookCanvasGroup.blocksRaycasts = isBookOpen;
            }
            
            // Sembunyikan buku buka jika ditutup
            if (!isBookOpen)
            {
                openBookObject.SetActive(false);
                
                // Navigasikan kembali ke preview panel jika belum disimpan
                if (!isItemKept)
                {
                    OnBookTriggered();
                }
            }
        }

        flipCoroutine = null;
    }
}