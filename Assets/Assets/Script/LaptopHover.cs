using UnityEngine;
using UnityEngine.EventSystems; // tambahkan ini

public class LaptopHover : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject targetCanvas;
    [SerializeField] private GameObject laptopPanel;
    [SerializeField] private GameObject laptopOutline;

    private bool isPanelOpen = false;

    void Start()
    {
        if (targetCanvas != null) targetCanvas.SetActive(false);
        if (laptopOutline != null) laptopOutline.SetActive(false);
        isPanelOpen = false;
    }

    void Update()
    {
        // Tutup panel dengan ESC
        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLaptop();
        }
    }

    void OnMouseEnter()
    {
        // Jangan tampilkan outline kalau panel sudah terbuka ATAU pointer sedang di atas UI
        if (!isPanelOpen && !EventSystem.current.IsPointerOverGameObject() 
            && laptopOutline != null && targetCanvas != null)
        {
            laptopOutline.SetActive(true);
        }
    }

    void OnMouseExit()
    {
        if (laptopOutline != null) laptopOutline.SetActive(false);
    }

    void OnMouseDown()
    {
        // Blokir klik kalau panel sedang terbuka ATAU pointer di atas UI (mencegah klik tembus)
        if (isPanelOpen || EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (targetCanvas == null || laptopPanel == null)
        {
            Debug.LogError("Gagal: Slot Canvas atau Panel di Inspector masih kosong!");
            return;
        }

        if (laptopOutline != null) laptopOutline.SetActive(false);

        targetCanvas.SetActive(true);
        laptopPanel.SetActive(true);

        Debug.Log("Canvas dan Panel berhasil dinyalakan!");
        isPanelOpen = true;
    }

    public void CloseLaptop()
    {
        if (laptopPanel != null) laptopPanel.SetActive(false);
        if (targetCanvas != null) targetCanvas.SetActive(false);

        isPanelOpen = false;
    }
}