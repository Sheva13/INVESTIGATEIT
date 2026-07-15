using UnityEngine;
using Level4;

public class EscapeZoneHandler : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        var gm = FindAnyObjectByType<Level4GameManager>();
        if (gm != null)
            gm.ReachStartPoint();
    }
}
