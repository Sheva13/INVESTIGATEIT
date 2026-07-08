using UnityEngine;

public enum GuardState
{
    Patrol,
    Alert,
    Investigate,
    Return
}

public class GuardAI : MonoBehaviour
{
    [Header("Settings")]
    public GuardState currentState = GuardState.Patrol;
    public Transform[] waypoints;
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float investigateSpeed = 2f;
    public float visionRange = 5f;
    public float visionAngle = 90f;
    public float hearingRange = 7f;
    public float catchDistance = 0.8f;
    public float alertDuration = 2.5f;
    public float investigateDuration = 3f;
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    [Header("References")]
    public Transform player;
    public GameManager gameManager;

    private int wpIndex = 0;
    private Vector2 lastKnownPlayerPos;
    private Vector2 investigateTarget;
    private float stateTimer = 0f;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Vector2 facingDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!gameManager) gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case GuardState.Patrol: PatrolUpdate(); break;
            case GuardState.Alert: AlertUpdate(); break;
            case GuardState.Investigate: InvestigateUpdate(); break;
            case GuardState.Return: ReturnUpdate(); break;
        }

        CheckVision();
        UpdateColor();
    }

    void PatrolUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[wpIndex];
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        facingDir = dir;
        rb.linearVelocity = dir * patrolSpeed;
        RotateTowards(dir);

        if (Vector2.Distance(transform.position, target.position) < 0.3f)
        {
            wpIndex = (wpIndex + 1) % waypoints.Length;
        }
    }

    void AlertUpdate()
    {
        if (!player) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        facingDir = dir;
        rb.linearVelocity = dir * chaseSpeed;
        RotateTowards(dir);

        if (Vector2.Distance(transform.position, player.position) < catchDistance)
        {
            gameManager?.LoseGame();
        }

        if (CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
            stateTimer = 0f;
        }
        else
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= alertDuration)
            {
                investigateTarget = lastKnownPlayerPos;
                currentState = GuardState.Investigate;
                stateTimer = 0f;
            }
        }
    }

    void InvestigateUpdate()
    {
        Vector2 dir = (investigateTarget - (Vector2)transform.position).normalized;
        facingDir = dir;
        rb.linearVelocity = dir * investigateSpeed;
        RotateTowards(dir);

        if (Vector2.Distance(transform.position, investigateTarget) < 0.5f)
        {
            stateTimer += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            if (stateTimer >= investigateDuration)
            {
                currentState = GuardState.Return;
                stateTimer = 0f;
            }
        }
    }

    void ReturnUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[wpIndex];
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        facingDir = dir;
        rb.linearVelocity = dir * patrolSpeed;
        RotateTowards(dir);

        if (Vector2.Distance(transform.position, target.position) < 0.3f)
        {
            currentState = GuardState.Patrol;
        }
    }

    void CheckVision()
    {
        if (!player || currentState == GuardState.Alert) return;
        if (CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
            currentState = GuardState.Alert;
            stateTimer = 0f;
        }
    }

    bool CanSeePlayer()
    {
        Vector2 dirToPlayer = (Vector2)player.position - (Vector2)transform.position;
        float dist = dirToPlayer.magnitude;
        if (dist > visionRange) return false;

        float angle = Vector2.Angle(facingDir, dirToPlayer.normalized);
        if (angle > visionAngle * 0.5f) return false;

        var hits = Physics2D.RaycastAll(transform.position, dirToPlayer.normalized, dist, obstacleMask | playerMask);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    void RotateTowards(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void UpdateColor()
    {
        if (!sprite) return;
        sprite.color = currentState switch
        {
            GuardState.Patrol => Color.green,
            GuardState.Alert => Color.red,
            GuardState.Investigate => new Color(1f, 0.5f, 0f),
            GuardState.Return => Color.yellow,
            _ => Color.white
        };
    }

    public void HearNoise(Vector2 noisePos)
    {
        float dist = Vector2.Distance(transform.position, noisePos);
        if (dist > hearingRange) return;

        if (currentState == GuardState.Patrol || currentState == GuardState.Return)
        {
            investigateTarget = noisePos;
            currentState = GuardState.Investigate;
            stateTimer = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
