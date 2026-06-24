using UnityEngine;
using UnityEngine.EventSystems;

public class UIItemInspectTrigger : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Item Inspect Reference")]
    [SerializeField] private UIItemInspect itemInspect;

    private SpriteRenderer spriteRenderer;
    private int lastTriggerFrame = -1;
    private static readonly int OutlineProperty = Shader.PropertyToID("_Outline");
    private bool isHovered = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Keyboard trigger support
        if (isHovered && Input.GetKeyDown(KeyCode.E))
        {
            TriggerInspect();
            SetOutline(false);
            isHovered = false; // Reset hover state to avoid multiple triggers if UI blocks raycasts
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerInspect();
        SetOutline(false);
        isHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        SetOutline(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        SetOutline(false);
    }

    private void SetOutline(bool enable)
    {
        if (spriteRenderer != null && spriteRenderer.sharedMaterial != null)
        {
            spriteRenderer.sharedMaterial.SetFloat(OutlineProperty, enable ? 1f : 0f);
        }
    }

    private void TriggerInspect()
    {
        if (Time.frameCount == lastTriggerFrame)
            return; // Prevent double trigger in same frame

        lastTriggerFrame = Time.frameCount;

        if (itemInspect != null)
        {
            itemInspect.OnItemTriggered();
        }
        else
        {
            Debug.LogWarning("UIItemInspect reference is missing on " + gameObject.name, this);
        }
    }
}
