using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    [Header("Debug Flags")]
    public bool logCollision = true;
    public bool logMovement = true;
    public bool logBehavior = true;
    public bool logTriggers = true;

    [Header("Settings")]
    public float movementLogInterval = 0.5f;

    private PlayerController player;
    private Rigidbody2D rb;
    private float moveLogTimer;
    private Vector2 lastMoveInput;
    private Vector2 lastVelocity;
    private int lastThrowCount = -1;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        Debug.Log("[PlayerDebug] Component initialized on " + gameObject.name);
        if (player == null) Debug.LogWarning("[PlayerDebug] PlayerController not found on this GameObject");
    }

    void Update()
    {
        if (!logBehavior || player == null) return;

        if (player.GetThrowableCount() != lastThrowCount)
        {
            lastThrowCount = player.GetThrowableCount();
            Debug.Log($"[PlayerDebug] Throwable count changed: {lastThrowCount}");
        }
    }

    void FixedUpdate()
    {
        if (!logMovement || rb == null) return;

        moveLogTimer += Time.fixedDeltaTime;
        if (moveLogTimer >= movementLogInterval)
        {
            moveLogTimer = 0f;
            Vector2 vel = rb.linearVelocity;
            float speed = vel.magnitude;
            string dir = speed > 0.1f ? $" dir=({vel.x:F3}, {vel.y:F3})" : " (stationary)";
            Debug.Log($"[PlayerDebug] Velocity=({vel.x:F3}, {vel.y:F3}) speed={speed:F3}{dir}  pos=({transform.position.x:F2},{transform.position.y:F2})");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!logCollision) return;

        string name = collision.gameObject.name;
        string tag = collision.gameObject.tag;
        string layer = LayerMask.LayerToName(collision.gameObject.layer);
        Vector2 normal = collision.GetContact(0).normal;
        Vector2 point = collision.GetContact(0).point;
        Debug.Log($"[PlayerDebug] COLLISION ENTER with '{name}' tag={tag} layer={layer} normal=({normal.x:F2},{normal.y:F2}) point=({point.x:F2},{point.y:F2})");
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!logCollision) return;
        Debug.Log($"[PlayerDebug] COLLISION STAY with '{collision.gameObject.name}' ({collision.contactCount} contacts)");
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!logCollision) return;
        Debug.Log($"[PlayerDebug] COLLISION EXIT with '{collision.gameObject.name}'");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!logTriggers) return;
        string name = other.gameObject.name;
        string tag = other.gameObject.tag;
        string layer = LayerMask.LayerToName(other.gameObject.layer);
        Debug.Log($"[PlayerDebug] TRIGGER ENTER with '{name}' tag={tag} layer={layer}");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!logTriggers) return;
        Debug.Log($"[PlayerDebug] TRIGGER STAY with '{other.gameObject.name}'");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!logTriggers) return;
        Debug.Log($"[PlayerDebug] TRIGGER EXIT with '{other.gameObject.name}'");
    }
}
