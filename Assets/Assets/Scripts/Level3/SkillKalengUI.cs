using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillKalengUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual")]
    public Color normalColor = Color.white;
    public Color activeColor = Color.yellow;
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color hoverColor = new Color(1f, 1f, 1f, 0.9f);
    public float inactiveScale = 0.8f;
    public float activeScale = 1.1f;
    public float hoverScale = 1.08f;
    public float scaleSpeed = 8f;

    [Header("Outline")]
    public bool useOutline = true;
    public Color outlineColor = Color.black;
    public Vector2 outlineDistance = new Vector2(2f, -2f);

    private PlayerController player;
    private RawImage rawImage;
    private Outline outline;
    private Vector3 targetScale;
    private bool isHovering;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        if (rawImage == null)
            rawImage = GetComponentInChildren<RawImage>();

        if (useOutline)
        {
            outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = outlineDistance;
            outline.enabled = false;
        }

        player = FindAnyObjectByType<PlayerController>();
        targetScale = transform.localScale;

        Debug.Log($"[SkillKalengUI] Awake: player={(player != null ? player.name : "NULL")}, rawImage={(rawImage != null ? "OK" : "NULL")}");
    }

    void OnEnable()
    {
        if (player != null)
        {
            player.OnThrowableChanged += RefreshVisual;
            Debug.Log("[SkillKalengUI] Subscribed to OnThrowableChanged");
        }
        else
        {
            Debug.LogWarning("[SkillKalengUI] OnEnable: player is NULL!");
        }
    }

    void OnDisable()
    {
        if (player != null)
            player.OnThrowableChanged -= RefreshVisual;
    }

    void Start()
    {
        RefreshVisual();
        Debug.Log($"[SkillKalengUI] Start: throwables={(player != null ? player.GetThrowableCount() : -1)}");
    }

    void Update()
    {
        if (player == null || rawImage == null) return;

        bool isActive = player.IsSkillActive;
        bool hasThrowables = player.GetThrowableCount() > 0;

        if (isActive)
        {
            rawImage.color = activeColor;
            targetScale = Vector3.one * activeScale;
        }
        else if (hasThrowables && isHovering)
        {
            rawImage.color = hoverColor;
            targetScale = Vector3.one * hoverScale;
        }
        else if (hasThrowables)
        {
            rawImage.color = normalColor;
            targetScale = Vector3.one;
        }
        else
        {
            rawImage.color = inactiveColor;
            targetScale = Vector3.one * inactiveScale;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }

    void RefreshVisual()
    {
        if (player == null || rawImage == null)
        {
            Debug.LogWarning($"[SkillKalengUI] RefreshVisual FAILED: player={(player != null ? "OK" : "NULL")}, rawImage={(rawImage != null ? "OK" : "NULL")}");
            return;
        }

        int count = player.GetThrowableCount();
        bool hasThrowables = count > 0;
        Debug.Log($"[SkillKalengUI] RefreshVisual: throwables={count}, isActive={player.IsSkillActive}");

        if (!hasThrowables)
        {
            rawImage.color = inactiveColor;
            targetScale = Vector3.one * inactiveScale;
            if (outline != null) outline.enabled = false;
        }
        else
        {
            rawImage.color = normalColor;
            targetScale = Vector3.one;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (player != null)
        {
            player.ActivateSkill();
            Debug.Log("[SkillKalengUI] Icon clicked");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (outline != null && player != null && player.GetThrowableCount() > 0)
            outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (outline != null)
            outline.enabled = false;
    }
}
