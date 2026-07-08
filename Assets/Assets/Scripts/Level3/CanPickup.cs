using UnityEngine;

public class CanPickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc)
        {
            pc.AddThrowable(1);
            Destroy(gameObject);
        }
    }
}
