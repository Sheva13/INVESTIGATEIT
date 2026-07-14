using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Level2
{
    public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;
    public TextMeshProUGUI restartMessageText;

    [Header("Audio")]
    public AudioSource sfxAudioSource;
    public AudioClip oofClip;

    [Header("Settings")]
    public float restartDelay = 1f;

    void Start()
    {
        if (restartMessageText) restartMessageText.gameObject.SetActive(false);
    }

    public void PlayerHitObstacle(Vector3 position, string message)
    {
        if (restartMessageText)
        {
            restartMessageText.text = message;
            restartMessageText.gameObject.SetActive(true);
        }

        if (sfxAudioSource && oofClip)
            sfxAudioSource.PlayOneShot(oofClip);

        Invoke(nameof(RestartLevel), restartDelay);
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
        CancelInvoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
}
