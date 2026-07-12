using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;
    public GameObject loseUI;

    [Header("Popup")]
    public GameObject popupTextPrefab;

    [Header("Audio")]
    public AudioSource sfxAudioSource;
    public AudioClip oofClip;

    [Header("Settings")]
    public float restartDelay = 1f;

    public void PlayerHitObstacle(Vector3 position, string message)
    {
        if (popupTextPrefab)
        {
            var popup = Instantiate(popupTextPrefab, position + Vector3.up * 2f, Quaternion.identity);
            var tmp = popup.GetComponentInChildren<TextMeshPro>();
            if (tmp) tmp.text = message;
            Destroy(popup, restartDelay);
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
