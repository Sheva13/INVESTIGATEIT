using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionRadius = 1.5f;
    public bool isInteractable = true;
    public KeyCode interactionKey = KeyCode.E;

    protected GameObject player;
    private bool playerInRange = false;

    void Start()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            player = playerGo;
        else
            Debug.LogError("Interactable: Player not found! Make sure Player has 'Player' tag.");
    }

    void Update()
    {
        if (!isInteractable || player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        playerInRange = distance <= interactionRadius;

        if (playerInRange)
        {
            ShowPrompt();
            if (Input.GetKeyDown(interactionKey))
                OnInteract();
        }
        else if (playerInRange == false)
        {
            HidePrompt();
        }
    }

    protected virtual void ShowPrompt()
    {
        // Override in subclass to show UI prompt
    }

    protected virtual void HidePrompt()
    {
        // Override in subclass to hide UI prompt
    }

    public abstract void OnInteract();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
