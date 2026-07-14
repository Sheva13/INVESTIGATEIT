using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button continueButton;
    public Button exitButton;

    [Header("Scene List")]
    [SerializeField] private Transform sceneListParent;
    [SerializeField] private GameObject sceneButtonPrefab;
    [SerializeField] private string[] sceneNames = new string[]
    {
        "MainMenu", "MenuScene", "kota", "level1_opening", "level1_rumahwicak",
        "level2_kelas", "level2_ruang", "level2_platformer", "minigame_level2",
        "Level3_Warehouse", "level4_dialog", "level4", "level4_end",
        "BookFlipScene", "SampleScene"
    };

    void Start()
    {
        EnsureSpawnManager();

        if (startButton != null) startButton.onClick.AddListener(StartNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        if (continueButton != null)
        {
            bool hasSave = PlayerPrefs.HasKey("SavedLevel");
            continueButton.interactable = hasSave;

            if (!hasSave)
            {
                var colors = continueButton.colors;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.25f);
                continueButton.colors = colors;
            }
        }

        PopulateSceneList();
    }

    private void PopulateSceneList()
    {
        if (sceneListParent == null || sceneButtonPrefab == null) return;

        foreach (string sceneName in sceneNames)
        {
            if (string.IsNullOrEmpty(sceneName)) continue;

            GameObject btnGO = Instantiate(sceneButtonPrefab, sceneListParent);
            btnGO.name = "SceneBtn_" + sceneName;

            var tmp = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = sceneName;

            var btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                string capturedName = sceneName;
                btn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("SavedLevel", capturedName);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene(capturedName);
                });
            }
        }
    }

    public void StartNewGame()
    {
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
