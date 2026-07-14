using UnityEngine;
using UnityEngine.UI;

public class UIButtonHandler : MonoBehaviour
{
    public enum ActionType { RestartGame }
    public ActionType action;

    private GameManager gm;

    void Awake()
    {
        gm = FindAnyObjectByType<GameManager>();

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (gm == null) gm = FindAnyObjectByType<GameManager>();
        if (gm == null) return;

        switch (action)
        {
            case ActionType.RestartGame:
                gm.RestartLevel();
                break;
        }
    }
}
