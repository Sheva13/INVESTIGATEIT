using UnityEngine;

public class WinZone : MonoBehaviour
{
    private GameManager gm;

    void Awake()
    {
        gm = FindAnyObjectByType<GameManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gm?.WinGame();
        }
    }
}
