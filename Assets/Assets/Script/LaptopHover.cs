using UnityEngine;

public class LaptopHover : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject targetCanvas;  // Drag GameObject 'Canvas' ke sini
    [SerializeField] private GameObject laptopPanel;   // Drag GameObject 'Panel' ke sini
    [SerializeField] private GameObject laptopOutline; // Drag 'LaptopOutline' ke sini

    private bool isPanelOpen = false;

    void Start()
    {
        // 1. Matikan Canvas di awal game
        if (targetCanvas != null) 
        {
            targetCanvas.SetActive(false);
        }

        // 2. Pastikan outline mati
        if (laptopOutline != null) laptopOutline.SetActive(false);
        
        isPanelOpen = false;
    }

    void OnMouseEnter()
    {
        // Tampilkan outline hanya jika panel belum terbuka
        if (!isPanelOpen && laptopOutline != null && targetCanvas != null)
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
        // Cegah kalau ada error null
        if (targetCanvas == null || laptopPanel == null)
        {
            Debug.LogError("Gagal: Slot Canvas atau Panel di Inspector masih kosong!");
            return;
        }

        // Jangan buka lagi kalau sudah terbuka
        if (!isPanelOpen)
        {
            // Matikan outline
            if (laptopOutline != null) laptopOutline.SetActive(false);

            // 1. Nyalakan Canvas terlebih dahulu (WAJIB)
            targetCanvas.SetActive(true);
            
            // 2. Baru nyalakan Panel spesifiknya
            laptopPanel.SetActive(true);

            Debug.Log("Canvas dan Panel berhasil dinyalakan!");
            isPanelOpen = true;
        }
    }

    // Fungsi untuk dipanggil saat user menutup laptop (Misal: klik tombol Close, atau sukses login)
    public void CloseLaptop()
    {
        // Matikan panelnya dulu
        if (laptopPanel != null) laptopPanel.SetActive(false);
        
        // Opsional: kalau mau Canvasnya dimatikan total lagi, uncomment baris di bawah:
        // if (targetCanvas != null) targetCanvas.SetActive(false);

        isPanelOpen = false; // Kembalikan state agar bisa di-klik lagi
    }
}