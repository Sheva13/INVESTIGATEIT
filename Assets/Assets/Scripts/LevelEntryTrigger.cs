using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEntryTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad;

    [Header("UI")]
    [SerializeField] private GameObject promptCanvas;

    private bool playerInside = false;

    private void Start()
    {
        if (promptCanvas != null)
            promptCanvas.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            if (promptCanvas != null)
                promptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (promptCanvas != null)
                promptCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("LevelEntryTrigger: sceneToLoad kosong!");
            }
        }
    }
}
