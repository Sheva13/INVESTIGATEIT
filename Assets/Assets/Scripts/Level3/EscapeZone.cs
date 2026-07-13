using UnityEngine;

public class EscapeZone : MonoBehaviour
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
            if (gm != null)
            {
                if (gm.hasLoot)
                {
                    gm.WinGame();
                }
                else
                {
                    gm.ShowNotification("[ LOCKED ]  FIND ACCESS CARD", Color.red, 3f);
                }
            }
        }
    }
}
