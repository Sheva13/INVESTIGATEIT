using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; } = false;

    [Header("UI Reference")]
    public GameObject pausePanel;

    void Awake()
    {
        IsPaused = false;
    }

    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void QuitToMainMenu()
    {
        // Resume time scale before loading scene
        Time.timeScale = 1f;
        IsPaused = false;

        // Save progress using PlayerPrefs
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedLevel", currentScene);
        PlayerPrefs.Save();
        Debug.Log("Progress saved for level: " + currentScene);

        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
