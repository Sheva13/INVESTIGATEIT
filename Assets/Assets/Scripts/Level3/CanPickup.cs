using UnityEngine;

public class CanPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
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
                    gm.ShowNotification("+1 KALENG DIPEROLEH", Color.white, 2.0f);
                }

                Destroy(gameObject);
            }
        }
    }
}
