using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelToKotaTransition : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private string spawnID = "lvl1";
    [SerializeField] private string nextAvailableLevel = "lvl2";

    public void LoadKota()
    {
        SpawnPointManager.NextSpawnID = spawnID;
        SpawnPointManager.NextAvailableLevel = nextAvailableLevel;
        SceneManager.LoadScene("kota");
    }
}
