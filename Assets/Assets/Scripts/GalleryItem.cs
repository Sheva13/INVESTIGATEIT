using UnityEngine;
using UnityEngine.EventSystems;

public class GalleryItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GalleryItemData itemData;

    private SpriteRenderer spriteRenderer;
    private Material instanceMaterial;
    private static readonly int OutlineProperty = Shader.PropertyToID("_Outline");

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            instanceMaterial = spriteRenderer.material;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData != null && UIGalleryController.Instance != null)
            UIGalleryController.Instance.ShowItem(itemData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"OnPointerEnter: {gameObject.name}");
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(OutlineProperty, 1f);
            Debug.Log($"Outline set to 1, value: {instanceMaterial.GetFloat(OutlineProperty)}");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"OnPointerExit: {gameObject.name}");
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(OutlineProperty, 0f);
        }
    }

    public void SetData(GalleryItemData data)
    {
        itemData = data;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && data != null)
            spriteRenderer.sprite = data.thumbnail;
    }
}
