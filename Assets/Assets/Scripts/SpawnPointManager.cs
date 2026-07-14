using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPointManager : MonoBehaviour
{
    public static string NextSpawnID = "SpawnPointDefault";
    public static string NextAvailableLevel = "lvl1";

    private static SpawnPointManager _instance;
    private static readonly string[] levelNames = { "lvl1", "lvl2", "lvl3" };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "kota") return;

        string spawnName = string.IsNullOrEmpty(NextSpawnID) ? "SpawnPointDefault" : NextSpawnID;

        // Cari spawn point (termasuk child objects)
        Transform spawnPoint = FindDeep(spawnName);
        if (spawnPoint == null)
        {
            Debug.LogWarning($"SpawnPoint '{spawnName}' tidak ditemukan di scene kota. Player tetap di posisi default.");
        }

        // Cari player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player dengan tag 'Player' tidak ditemukan di scene kota.");
            return;
        }

        // Geser player ke spawn point
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            Debug.Log($"Player spawn di '{spawnName}' ({spawnPoint.position})");
        }

        // Enable/disable level colliders berdasarkan NextAvailableLevel
        foreach (string lvl in levelNames)
        {
            Transform lvlTransform = FindDeep(lvl);
            if (lvlTransform == null) continue;

            Collider2D col = lvlTransform.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = (lvl == NextAvailableLevel);
                Debug.Log($"Level '{lvl}' collider: {(lvl == NextAvailableLevel ? "ON" : "OFF")}");
            }
        }
    }

    private Transform FindDeep(string name)
    {
        // Cari di semua objects di scene aktif (termasuk children)
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
