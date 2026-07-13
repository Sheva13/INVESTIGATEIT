using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Level3
{
    public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;
    public TextMeshProUGUI canCountText;

    [Header("References")]
    public PlayerController player;

    [Header("Gameplay State")]
    public bool hasLoot = false;

    void Awake()
    {
        if (!player) player = FindAnyObjectByType<PlayerController>();
        hasLoot = false;
    }

    void Update()
    {
        if (canCountText && player)
        {
            if (!hasLoot)
            {
                canCountText.text = $"[Kaleng: {player.GetThrowableCount()}]";
                canCountText.color = Color.white;
            }
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
}
