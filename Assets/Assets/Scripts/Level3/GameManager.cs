using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;
    public Text canCountText;

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

    [Header("Restart")]
    public float loseDelay = 3f;

    [Header("Trap System")]
    public int totalTraps = 5;
    public int trapsTriggered = 0;

    [Header("Gameplay State")]
    public bool hasLoot = false;

    private bool isGameOver = false;
    private bool isRestarting = false;

    void Awake()
    {
        if (!player) player = FindAnyObjectByType<PlayerController>();
        hasLoot = false;
        isGameOver = false;
        isRestarting = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
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
    }

    public void ShowNotification(string message, Color color, float duration = 3f)
    {
    }

    public void RegisterTrapTriggered(string areaName)
    {
        trapsTriggered++;

        if (trapsTriggered >= totalTraps)
        {
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

        if (loseUI) loseUI.SetActive(true);
        if (loseClip != null)
            AudioSource.PlayClipAtPoint(loseClip, Camera.main.transform.position, 1f);
        Time.timeScale = 0f;

        StartCoroutine(AutoRestartAfterDelay());
    }

    IEnumerator AutoRestartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(loseDelay);
        RestartLevel();
    }

    void HideGameUI()
    {
        if (objektifText) objektifText.SetActive(false);
        if (canIcon) canIcon.SetActive(false);
        if (kalengCounter) kalengCounter.SetActive(false);
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
        if (isRestarting) return;
        StartCoroutine(RestartWithFade());
    }

    IEnumerator RestartWithFade()
    {
        isRestarting = true;
        Time.timeScale = 1f;

        if (fadeOverlay != null)
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

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
