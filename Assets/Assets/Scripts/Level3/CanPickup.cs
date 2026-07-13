using UnityEngine;

public class CanPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.timeSinceLevelLoad < 0.5f) return;
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc == null) pc = FindAnyObjectByType<PlayerController>();

            if (pc != null)
            {
                pc.AddThrowable(1);

                // Show notification on screen
                GameManager gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    gm.ShowNotification("+1 Kaleng", Color.white, 2.0f);
                }

                Destroy(gameObject);
            }
        }
    }
}
