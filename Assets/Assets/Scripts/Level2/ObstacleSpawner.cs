using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject flyingChairPrefab;
    public float minInterval = 2f;
    public float maxInterval = 5f;
    public float spawnX = -20f;
    public float spawnYMin = -2f;
    public float spawnYMax = 2f;

    void Start()
    {
        ScheduleNext();
    }

    void ScheduleNext()
    {
        float delay = Random.Range(minInterval, maxInterval);
        Invoke(nameof(Spawn), delay);
    }

    void Spawn()
    {
        if (flyingChairPrefab == null) return;

        float y = Random.Range(spawnYMin, spawnYMax);
        Vector3 pos = new Vector3(spawnX, y, 0f);
        Instantiate(flyingChairPrefab, pos, Quaternion.identity);

        ScheduleNext();
    }
}