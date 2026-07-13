using UnityEngine;

public class LootObjective : MonoBehaviour
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
                gm.hasLoot = true;
                gm.ShowNotification("[ ACCESS CARD OBTAINED ]  PROCEED TO WAREHOUSE", Color.green, 4f);
            }
            gameObject.SetActive(false);
        }
    }
}
