using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button continueButton;
    public Button exitButton;

    void Start()
    {
        EnsureSpawnManager();

        // Hook up button listeners dynamically
        if (startButton != null) startButton.onClick.AddListener(StartNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        // Only enable Continue button if there's a saved level
        if (continueButton != null)
        {
            bool hasSave = PlayerPrefs.HasKey("SavedLevel");
            continueButton.interactable = hasSave;
            
            // Adjust opacity of continue button if not interactable for clean visual feedback
            if (!hasSave)
            {
                var colors = continueButton.colors;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.25f);
                continueButton.colors = colors;
            }
        }
    }

    public void StartNewGame()
    {
        // Clear progress on starting new game
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();

        SpawnPointManager.NextSpawnID = "SpawnPointDefault";
        SpawnPointManager.NextAvailableLevel = "lvl1";
        SceneManager.LoadScene("kota");
    }

    public void ContinueGame()
    {
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            string savedScene = PlayerPrefs.GetString("SavedLevel");
            Debug.Log("Loading saved scene: " + savedScene);
            SceneManager.LoadScene(savedScene);
        }
        else
        {
            // Fallback
            SpawnPointManager.NextSpawnID = "SpawnPointDefault";
            SpawnPointManager.NextAvailableLevel = "lvl1";
            SceneManager.LoadScene("kota");
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }

    private void EnsureSpawnManager()
    {
        if (FindAnyObjectByType<SpawnPointManager>() == null)
        {
            var go = new GameObject("[SpawnPointManager]");
            go.AddComponent<SpawnPointManager>();
        }
    }
}
