using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;
    public TextMeshProUGUI canCountText;

    [Header("References")]
    public PlayerController player;

    void Awake()
    {
        if (!player) player = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (canCountText && player)
        {
            canCountText.text = $"[Cans: {player.GetThrowableCount()}]";
        }
    }

    public void WinGame()
    {
        if (winUI) winUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void LoseGame()
    {
        if (loseUI) loseUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
