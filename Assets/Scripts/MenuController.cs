using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class MenuController : MonoBehaviour
{
    public TextMeshProUGUI[] menuItems;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.white;
    public Color hoverColor = Color.white;
    public float selectedScale = 1.3f;
    public float hoverScale = 1.1f;

    private int currentIndex = 0;
    private float inputCooldown;

    void Start()
    {
        if (menuItems == null || menuItems.Length == 0) return;

        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] == null) continue;

            var trigger = menuItems[i].GetComponent<EventTrigger>();
            if (trigger == null) trigger = menuItems[i].gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            var idx = i;

            var enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((data) => OnHoverEnter(idx));
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((data) => OnHoverExit(idx));
            trigger.triggers.Add(exit);

            var click = new EventTrigger.Entry();
            click.eventID = EventTriggerType.PointerClick;
            click.callback.AddListener((data) => OnClickItem(idx));
            trigger.triggers.Add(click);
        }

        UpdateVisuals();
    }

    void Update()
    {
        if (menuItems == null || menuItems.Length == 0) return;

        if (inputCooldown > 0)
            inputCooldown -= Time.deltaTime;

        if (inputCooldown <= 0)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                currentIndex = (currentIndex + 1) % menuItems.Length;
                inputCooldown = 0.2f;
                UpdateVisuals();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                currentIndex = (currentIndex - 1 + menuItems.Length) % menuItems.Length;
                inputCooldown = 0.2f;
                UpdateVisuals();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            SelectItem(currentIndex);
        }
    }

    public void OnHoverEnter(int index)
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] == null) continue;
            menuItems[i].color = i == index ? hoverColor : (i == currentIndex ? selectedColor : normalColor);
            menuItems[i].rectTransform.localScale = i == index ? Vector3.one * hoverScale : (i == currentIndex ? Vector3.one * selectedScale : Vector3.one);
        }
    }

    public void OnHoverExit(int index)
    {
        UpdateVisuals();
    }

    public void OnClickItem(int index)
    {
        currentIndex = index;
        UpdateVisuals();
        SelectItem(index);
    }

    private void SelectItem(int index)
    {
        var text = menuItems[index].text.ToUpper();
        Debug.Log("Selected: " + text);
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            if (menuItems[i] == null) continue;

            if (i == currentIndex)
            {
                menuItems[i].color = selectedColor;
                menuItems[i].rectTransform.localScale = Vector3.one * selectedScale;
            }
            else
            {
                menuItems[i].color = normalColor;
                menuItems[i].rectTransform.localScale = Vector3.one;
            }
        }
    }
}
