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
                // Display feedback
                if (gm.canCountText != null)
                {
                    gm.canCountText.text = "AKSES GUDANG DIPEROLEH! SEGERA MASUK!";
                    gm.canCountText.color = Color.green;
                }
                Debug.Log("Kartu Akses diperoleh! Segera masuk ke pintu gudang!");
            }
            // Deactivate visual representation of Access Card
            gameObject.SetActive(false);
        }
    }
}
