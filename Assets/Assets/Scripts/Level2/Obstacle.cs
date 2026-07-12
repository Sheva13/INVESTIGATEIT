using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public string restartMessage = "Kena rintangan!";

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player"))
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) gm.PlayerHitObstacle(transform.position, restartMessage);
        }
    }
}