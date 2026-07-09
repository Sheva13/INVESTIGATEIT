using UnityEngine;
using UnityEngine.EventSystems;

public class UIItemInspectTrigger : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Inspect Reference")]
    [SerializeField] private UIItemInspect itemInspect;

    private int lastTriggerFrame = -1;
    private bool isHovered = false;
    private GameObject hoverOutline;

    private void Start()
    {
        var outlineTransform = transform.Find("HoverOutline");
        if (outlineTransform != null)
        {
            hoverOutline = outlineTransform.gameObject;
        }
    }

    private void Update()
    {
        // Keyboard trigger support
        if (isHovered && Input.GetKeyDown(KeyCode.E))
        {
            TriggerInspect();
            SetOutline(false);
            isHovered = false;
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
        if (hoverOutline != null)
        {
            hoverOutline.SetActive(enable);
        }
    }

    private void TriggerInspect()
    {
        if (Time.frameCount == lastTriggerFrame)
            return;

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
