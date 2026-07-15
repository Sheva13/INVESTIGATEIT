using UnityEngine;
using Level4;

public class CocainBoxInteraction : HoldInteraction
{
    [Header("Cocain Box")]
    public Sprite usedSprite;
    public Color usedTint = new Color(0.5f, 0.5f, 0.5f, 1f);

    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    private Color originalColor;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            originalColor = spriteRenderer.color;
        }
    }

    protected override bool CanInteract()
    {
        if (isComplete) return false;
        if (Level4State.hasAllCans) return false;
        return base.CanInteract();
    }

    protected override void OnInteractionComplete()
    {
        if (gm != null)
            gm.CollectCan();
        if (spriteRenderer != null)
        {
            if (usedSprite != null)
                spriteRenderer.sprite = usedSprite;
            spriteRenderer.color = usedTint;
        }
    }
}
