using UnityEngine;
using Level4;

public class DokumenPickup : HoldInteraction
{
    [Header("Dokumen")]
    public Color lockedTint = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color normalTint = Color.white;

    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        holdPrompt = "Tahan E untuk mengambil dokumen";
        lockedPrompt = "Kumpulkan semua kaleng terlebih dahulu";
    }

    protected override bool CanInteract()
    {
        if (isComplete) return false;
        if (!Level4State.hasAllCans) return false;
        return base.CanInteract();
    }

    protected override void Update()
    {
        base.Update();
        if (spriteRenderer != null)
        {
            if (Level4State.hasAllCans && !isComplete)
                spriteRenderer.color = normalTint;
            else if (!isComplete)
                spriteRenderer.color = lockedTint;
        }
    }

    protected override void OnInteractionComplete()
    {
        if (gm != null)
            gm.CollectDocument();
    }
}
