using UnityEngine;
using UnityEngine.SceneManagement;
using Level2;

public class RuangOsisInteraction : MonoBehaviour
{
    [Header("UI")]
    public TMPro.TextMeshProUGUI interactionPromptText;

    [Header("Settings")]
    public float interactionRange = 3f;
    public string targetSceneName = "level2_ruang";
    public string promptMessage = "Tekan [M] untuk masuk";

    private Transform player;
    private bool playerInRange = false;

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo) player = playerGo.transform;

        if (interactionPromptText)
            interactionPromptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = dist <= interactionRange;

        if (playerInRange != wasInRange)
        {
            if (interactionPromptText)
                interactionPromptText.gameObject.SetActive(playerInRange);
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.M))
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.enabled = false;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(targetSceneName);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
