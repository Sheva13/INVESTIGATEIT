using UnityEngine;
using UnityEngine.EventSystems;

public class UIBookFlipTrigger : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Book Flip Reference")]
    [SerializeField] private UIBookFlip bookFlip;

    private SpriteRenderer spriteRenderer;
    private int lastTriggerFrame = -1;
    private static readonly int OutlineProperty = Shader.PropertyToID("_Outline");

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerFlip();
        // Deactivate outline on click to ensure clean transitions
        SetOutline(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetOutline(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetOutline(false);
    }

    private void SetOutline(bool enable)
    {
        if (spriteRenderer != null && spriteRenderer.sharedMaterial != null)
        {
            spriteRenderer.sharedMaterial.SetFloat(OutlineProperty, enable ? 1f : 0f);
        }
    }

    private void TriggerFlip()
    {
        if (Time.frameCount == lastTriggerFrame)
            return; // Cegah double trigger dalam frame yang sama

        lastTriggerFrame = Time.frameCount;

        if (bookFlip != null)
        {
            bookFlip.OnBookTriggered();
        }
        else
        {
            Debug.LogWarning("UIBookFlip reference is missing on " + gameObject.name, this);
        }
    }
}
