using UnityEngine;
using Level4;

[RequireComponent(typeof(Collider2D))]
public abstract class HoldInteraction : MonoBehaviour
{
    [Header("Hold Settings")]
    public float holdDuration = 2f;
    public string holdPrompt = "Tahan E untuk berinteraksi";
    public string lockedPrompt = "Terkunci";

    protected Transform playerTransform;
    protected bool isInRange = false;
    protected float holdTimer = 0f;
    protected bool isHolding = false;
    protected bool isComplete = false;

    protected Level4GameManager gm;

    public static HoldInteraction ActiveInteraction { get; private set; }
    public bool IsHolding => isInRange && isHolding && !isComplete;
    public float HoldProgress => holdDuration > 0f ? Mathf.Clamp01(holdTimer / holdDuration) : 0f;
    public string CurrentPrompt
    {
        get
        {
            if (!CanInteract()) return lockedPrompt;
            if (isHolding) return holdPrompt;
            return holdPrompt;
        }
    }

    protected virtual void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    protected virtual void Start()
    {
        gm = FindAnyObjectByType<Level4GameManager>();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        isInRange = true;
        playerTransform = pc.transform;
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        isInRange = false;
        playerTransform = null;
        holdTimer = 0f;
        isHolding = false;
        if (ActiveInteraction == this)
            ActiveInteraction = null;
    }

    protected virtual void Update()
    {
        if (isComplete || !isInRange || playerTransform == null) return;

        if (gm == null)
            gm = FindAnyObjectByType<Level4GameManager>();

        bool pressing = Input.GetKey(KeyCode.E);
        if (pressing && CanInteract())
        {
            if (!isHolding)
            {
                isHolding = true;
                ActiveInteraction = this;
                OnHoldStarted();
            }
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdDuration)
            {
                holdTimer = holdDuration;
                isHolding = false;
                isComplete = true;
                ActiveInteraction = null;
                OnInteractionComplete();
            }
        }
        else
        {
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;
                if (ActiveInteraction == this)
                    ActiveInteraction = null;
                OnHoldCancelled();
            }
        }
    }

    protected virtual void OnHoldStarted() { }
    protected virtual void OnHoldCancelled() { }
    protected virtual bool CanInteract() => !isComplete;
    protected abstract void OnInteractionComplete();

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
