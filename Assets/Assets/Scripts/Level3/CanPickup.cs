using UnityEngine;
using TMPro;

public class CanPickup : MonoBehaviour
{
    private TextMeshProUGUI subtitleText;
    private bool isPlayerInRange = false;

    void Start()
    {
        // Find SubtitleText in the scene Canvas
        GameObject subObj = GameObject.Find("SubtitleText");
        if (subObj != null)
        {
            subtitleText = subObj.GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetMouseButtonDown(0))
        {
            // Convert mouse screen position to world coordinates
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(mousePos))
            {
                // Access player controller and add can to inventory
                PlayerController pc = FindAnyObjectByType<PlayerController>();
                if (pc != null)
                {
                    pc.AddThrowable(1);
                    HideSubtitle();
                    Destroy(gameObject);
                }
            }
        }
    }

    void ShowSubtitle()
    {
        if (subtitleText != null)
        {
            subtitleText.text = "Klik kiri pada kaleng untuk mengambilnya";
        }
    }

    void HideSubtitle()
    {
        if (subtitleText != null && subtitleText.text == "Klik kiri pada kaleng untuk mengambilnya")
        {
            subtitleText.text = "";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            ShowSubtitle();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            HideSubtitle();
        }
    }

    void OnDestroy()
    {
        // Safety cleanup if object is destroyed while player is in range
        if (isPlayerInRange)
        {
            HideSubtitle();
        }
    }
}
