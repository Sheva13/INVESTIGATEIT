using UnityEngine;
using Level3;

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
                    Debug.Log("Berhasil masuk ke warehouse! Kemenangan terpicu.");
                    if (gm.canCountText != null)
                    {
                        gm.canCountText.text = "BERHASIL MASUK KE WAREHOUSE!";
                        gm.canCountText.color = Color.green;
                    }
                    gm.WinGame();
                }
                else
                {
                    Debug.Log("Pintu gudang terkunci! Butuh kartu akses.");
                    if (gm.canCountText != null)
                    {
                        gm.canCountText.text = "PINTU GUDANG TERKUNCI! TEMUKAN KARTU AKSES!";
                        gm.canCountText.color = Color.red;
                    }
                }
            }
        }
    }
}
