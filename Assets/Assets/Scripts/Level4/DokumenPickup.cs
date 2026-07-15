using UnityEngine;
using TMPro;
using Level4;

public class DokumenPickup : HoldInteraction
{
    [Header("Dokumen")]
    public Color normalTint = Color.white;

    [Header("Popup")]
    public TextMeshProUGUI popupPrompt;
    public string popupMessage = "Tekan [M] untuk ambil dokumen";

    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        holdPrompt = "";
        lockedPrompt = "";
    }

    protected override bool CanInteract()
    {
        return false;
    }

    protected override void Update()
    {
        if (isComplete) return;

        if (spriteRenderer != null && !isComplete)
            spriteRenderer.color = normalTint;

        if (isInRange && !isComplete)
        {
            if (popupPrompt != null)
            {
                popupPrompt.gameObject.SetActive(true);
                popupPrompt.text = popupMessage;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                isComplete = true;
                if (popupPrompt != null)
                    popupPrompt.gameObject.SetActive(false);
                if (gm != null)
                    gm.CollectDocument();
            }
        }
        else
        {
            if (popupPrompt != null)
                popupPrompt.gameObject.SetActive(false);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2 pc = other.GetComponentInParent<PlayerController2>();
        if (pc == null) return;
        isInRange = true;
        playerTransform = pc.transform;
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController2>() == null) return;
        isInRange = false;
        playerTransform = null;
        holdTimer = 0f;
        isHolding = false;
    }

    protected override void OnInteractionComplete()
    {
    }
}
