using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace Level3
{
    public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;
    public Text canCountText;
    public Text notificationText;

    [Header("References")]
    public PlayerController player;

    [Header("Audio")]
    public AudioClip loseClip;

    [Header("UI Elements To Hide")]
    public GameObject objektifText;
    public GameObject canIcon;
    public GameObject kalengCounter;

    [Header("Fade Settings")]
    public Image fadeOverlay;
    public float fadeDuration = 1.5f;

    [Header("Trap System")]
    public int totalTraps = 5;
    public int trapsTriggered = 0;

    [Header("Gameplay State")]
    public bool hasLoot = false;

    private float notifTimer = 0f;
    private bool isGameOver = false;

    void Awake()
    {
        if (!player) player = FindAnyObjectByType<PlayerController>();
        hasLoot = false;
        isGameOver = false;
    }

    void Start()
    {
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (canCountText && player)
        {
            canCountText.text = $"{player.GetThrowableCount()}";
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
        if (string.IsNullOrEmpty(message))
        {
            notificationText.gameObject.SetActive(false);
            return;
        }
        notificationText.text = message;
        notificationText.color = color;
        notificationText.gameObject.SetActive(true);
        notifTimer = duration;
    }

    public void HideNotification()
    {
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    public void RegisterTrapTriggered(string areaName)
    {
        trapsTriggered++;
        ShowNotification($"Area {areaName} berhasil dijebak! ({trapsTriggered}/{totalTraps})", Color.green, 3f);

        if (trapsTriggered >= totalTraps)
        {
            ShowNotification("Semua area aman! Mencari kemenangan...", Color.yellow, 2f);
            Invoke(nameof(WinGame), 2f);
        }
    }

    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        HideGameUI();

        if (winUI) winUI.SetActive(true);

        if (fadeOverlay != null)
            StartCoroutine(FadeInOverlay());

        Time.timeScale = 0f;
    }

    public void LoseGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        HideGameUI();

        ShowNotification("Aksa tertangkap!", Color.red, 99f);
        if (loseUI) loseUI.SetActive(true);
        if (loseClip != null)
            AudioSource.PlayClipAtPoint(loseClip, Camera.main.transform.position, 1f);
        Time.timeScale = 0f;
    }

    void HideGameUI()
    {
        if (objektifText) objektifText.SetActive(false);
        if (canIcon) canIcon.SetActive(false);
        if (kalengCounter) kalengCounter.SetActive(false);
        if (notificationText) notificationText.gameObject.SetActive(false);
        if (canCountText) canCountText.gameObject.SetActive(false);
    }

    IEnumerator FadeInOverlay()
    {
        fadeOverlay.gameObject.SetActive(true);
        Color c = fadeOverlay.color;
        c.a = 0f;
        fadeOverlay.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeOverlay.color = c;
    }

    public void RestartLevel()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
}
