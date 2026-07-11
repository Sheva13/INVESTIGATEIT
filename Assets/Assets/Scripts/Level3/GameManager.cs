using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;
    public TextMeshProUGUI canCountText;
    public TextMeshProUGUI notificationText;

    [Header("References")]
    public PlayerController player;

    [Header("Gameplay State")]
    public bool hasLoot = false;

    private float notifTimer = 0f;

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

        if (notifTimer > 0f)
        {
            notifTimer -= Time.deltaTime;
            if (notifTimer <= 0f && notificationText != null)
                notificationText.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.R))
            RestartLevel();
    }

    public void ShowNotification(string message, Color color, float duration = 3f)
    {
        if (notificationText == null) return;
        notificationText.text = message;
        notificationText.color = color;
        notificationText.gameObject.SetActive(true);
        notifTimer = duration;
    }

    public void WinGame()
    {
        ShowNotification("Aksa berhasil masuk ke dalam gudang!", Color.green, 99f);
        if (winUI) winUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void LoseGame()
    {
        ShowNotification("Aksa tertangkap!", Color.red, 99f);
        if (loseUI) loseUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
