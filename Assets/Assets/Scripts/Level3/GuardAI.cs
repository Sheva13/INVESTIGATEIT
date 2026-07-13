using UnityEngine;
using Level3;

public enum GuardState
{
    Patrol,
    Alert,
    Investigate,
    Return
}

public class GuardAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float investigateSpeed = 2.5f;

    [Header("Detection Settings")]
    public float visionRange = 5f;
    public float visionAngle = 90f;
    public float hearingRange = 7f;
    public float catchDistance = 0.8f;
    public float alertDuration = 3f;
    public float investigateDuration = 2f;
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;

    [Header("Directional Sprites")]
    public Sprite[] directionSprites;

    [Header("Animator")]
    public RuntimeAnimatorController guardAnimatorController;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    public AudioClip caughtClip;
    public float stepInterval = 0.5f;

    [Header("References")]
    public Transform player;
    public GameManager gameManager;

    private Animator anim;
    private float stepTimer = 0f;
    private int wpIndex = 0;
    private Vector2 lastKnownPlayerPos;
    private Vector2 investigateTarget;
    private float stateTimer = 0f;
    private float chaseTimer = 0f; // Tracks continuous chase duration
    private float waypointTime = 0f; // Stuck timeout timer
    private Vector2 lastPosition; // Tracks last position to detect physical stuckness
    private float stuckCheckTimer = 0f; // Stuck check interval timer
    private Vector2 alertSteerOffset = Vector2.zero; // Sideways nudge to get unstuck in Alert state
    private float alertSteerTimer = 0f; // Remaining duration of steering nudge
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Vector2 facingDir = Vector2.right;
    private LineRenderer visionLineRenderer;
    private int frameCounter = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.6f;
        }
        else Debug.LogWarning($"{gameObject.name}: AudioSource missing!");

        // Create or find child GameObject for Vision Cone rendering
        Transform existingCone = transform.Find("VisionCone");
        GameObject coneObj;
        if (existingCone != null)
        {
            coneObj = existingCone.gameObject;
        }
        else
        {
            coneObj = new GameObject("VisionCone");
            coneObj.transform.SetParent(transform);
            coneObj.transform.localPosition = Vector3.zero;
            coneObj.transform.localRotation = Quaternion.identity;
        }

        visionLineRenderer = coneObj.GetComponent<LineRenderer>();
        if (visionLineRenderer == null)
        {
            visionLineRenderer = coneObj.AddComponent<LineRenderer>();
        }

        visionLineRenderer.useWorldSpace = false;
        visionLineRenderer.positionCount = 3;
        visionLineRenderer.loop = true;
        visionLineRenderer.startWidth = 0.05f;
        visionLineRenderer.endWidth = 0.05f;
        
        var defaultMat = new Material(Shader.Find("Sprites/Default"));
        visionLineRenderer.material = defaultMat;
        visionLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visionLineRenderer.receiveShadows = false;
        visionLineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
    }

    void Start()
    {
        transform.rotation = Quaternion.identity;
        facingDir = transform.right;
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }
        lastPosition = transform.position;

        // Set up Animator
        anim = GetComponent<Animator>();
        if (anim == null) anim = gameObject.AddComponent<Animator>();
        if (guardAnimatorController != null)
        {
            anim.runtimeAnimatorController = guardAnimatorController;
        }

        UpdateSpriteDirection();
    }

    void Update()
    {
        // Universal physical stuck detection (1.0s interval)
        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer >= 1.0f)
        {
            bool isTryingToMove = currentState == GuardState.Patrol || 
                                  currentState == GuardState.Return || 
                                  currentState == GuardState.Investigate || 
                                  currentState == GuardState.Alert;

            if (isTryingToMove && Vector2.Distance(transform.position, lastPosition) < 0.15f)
            {
                if (currentState == GuardState.Patrol)
                {
                    wpIndex = (wpIndex + 1) % waypoints.Length;
                    Debug.Log($"Guard {gameObject.name} stuck in Patrol. Skipping to next waypoint.");
                }
                else if (currentState == GuardState.Return)
                {
                    currentState = GuardState.Patrol;
                    Debug.Log($"Guard {gameObject.name} stuck in Return. Forcing patrol.");
                }
                else if (currentState == GuardState.Investigate)
                {
                    currentState = GuardState.Return;
                    Debug.Log($"Guard {gameObject.name} stuck in Investigate. Returning to patrol.");
                }
                else if (currentState == GuardState.Alert && player != null)
                {
                    // Pick a random perpendicular direction relative to player heading to steer around obstacle
                    Vector2 targetDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    alertSteerOffset = new Vector2(-targetDir.y, targetDir.x) * (Random.value < 0.5f ? 1.5f : -1.5f);
                    alertSteerTimer = 0.8f;
                    Debug.Log($"Guard {gameObject.name} stuck in Alert. Applying sideways steer offset.");
                }
                waypointTime = 0f;
            }
            lastPosition = transform.position;
            stuckCheckTimer = 0f;
        }

        switch (currentState)
        {
            case GuardState.Patrol: PatrolUpdate(); break;
            case GuardState.Alert: AlertUpdate(); break;
            case GuardState.Investigate: InvestigateUpdate(); break;
            case GuardState.Return: ReturnUpdate(); break;
        }

        CheckVision();
        UpdateColor();
        UpdateSpriteDirection();
        if (anim != null) anim.SetFloat("Speed", rb.linearVelocity.magnitude);

        // Footstep audio
        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }

        frameCounter++;
        if (frameCounter % 3 == 0)
            UpdateVisionCone();
    }

    void UpdateVisionCone()
    {
        if (visionLineRenderer == null) return;

        Vector3 facing3 = new Vector3(facingDir.x, facingDir.y, 0f);
        if (facing3.sqrMagnitude < 0.001f) facing3 = Vector3.right;

        float halfAngle = visionAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0, 0, -halfAngle) * facing3;
        Vector3 rightDir = Quaternion.Euler(0, 0, halfAngle) * facing3;

        visionLineRenderer.useWorldSpace = true;
        visionLineRenderer.SetPosition(0, transform.position);
        visionLineRenderer.SetPosition(1, transform.position + leftDir * visionRange);
        visionLineRenderer.SetPosition(2, transform.position + rightDir * visionRange);

        Color coneColor = currentState switch
        {
            GuardState.Patrol => new Color(0f, 1f, 0f, 0.4f), // Translucent green
            GuardState.Alert => (chaseTimer > 1.5f) ? new Color(1f, 0f, 0f, 0.8f) : new Color(1f, 0f, 0f, 0.4f), // VERY red when angry
            GuardState.Investigate => new Color(1f, 0.5f, 0f, 0.4f), // Translucent orange
            GuardState.Return => new Color(1f, 1f, 0f, 0.4f), // Translucent yellow
            _ => new Color(1f, 1f, 1f, 0.4f)
        };

        visionLineRenderer.startColor = coneColor;
        visionLineRenderer.endColor = coneColor;
    }

    void PatrolUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        waypointTime += Time.deltaTime;
        if (waypointTime > 8.0f)
        {
            wpIndex = (wpIndex + 1) % waypoints.Length;
            waypointTime = 0f;
            Debug.Log($"Guard {gameObject.name} stuck timeout! Moving to next waypoint.");
        }

        Transform target = waypoints[wpIndex];
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Vector2 avoidDir = AvoidObstacles(dir);
        rb.linearVelocity = avoidDir * patrolSpeed;
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            facingDir = rb.linearVelocity.normalized;

        if (Vector2.Distance(transform.position, target.position) < 0.3f)
        {
            wpIndex = (wpIndex + 1) % waypoints.Length;
            waypointTime = 0f;
        }
    }

    void AlertUpdate()
    {
        if (!player) return;

        chaseTimer += Time.deltaTime;
        float currentSpeed = chaseSpeed;
        if (chaseTimer > 1.5f)
        {
            currentSpeed = chaseSpeed * 1.25f; // Runs slightly faster (25% boost) when vision is VERY red
        }

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (alertSteerTimer > 0f)
        {
            alertSteerTimer -= Time.deltaTime;
            dir = (dir + alertSteerOffset).normalized;
        }

        Vector2 avoidDir = AvoidObstacles(dir);
        rb.linearVelocity = avoidDir * currentSpeed;
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            facingDir = rb.linearVelocity.normalized;

        if (Vector2.Distance(transform.position, player.position) < catchDistance)
        {
            if (caughtClip != null && audioSource != null)
                audioSource.PlayOneShot(caughtClip);
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
                chaseTimer = 0f; // Reset chase timer
            }
        }
    }

    void InvestigateUpdate()
    {
        chaseTimer = 0f;
        Vector2 dir = (investigateTarget - (Vector2)transform.position).normalized;
        Vector2 avoidDir = AvoidObstacles(dir);
        rb.linearVelocity = avoidDir * investigateSpeed;
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            facingDir = rb.linearVelocity.normalized;

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
        chaseTimer = 0f; // Reset chase timer
        if (waypoints == null || waypoints.Length == 0) return;

        waypointTime += Time.deltaTime;
        if (waypointTime > 8.0f)
        {
            currentState = GuardState.Patrol;
            waypointTime = 0f;
            Debug.Log($"Guard {gameObject.name} stuck timeout on return! Forcing patrol state.");
        }

        Transform target = waypoints[wpIndex];
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Vector2 avoidDir = AvoidObstacles(dir);
        rb.linearVelocity = avoidDir * patrolSpeed;
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
            facingDir = rb.linearVelocity.normalized;

        if (Vector2.Distance(transform.position, target.position) < 0.3f)
        {
            currentState = GuardState.Patrol;
            waypointTime = 0f;
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
            chaseTimer = 0f; // Reset chase timer
        }
    }

    bool CanSeePlayer()
    {
        if (!player) return false;
        Vector2 dirToPlayer = (Vector2)player.position - (Vector2)transform.position;
        float dist = dirToPlayer.magnitude;
        if (dist > visionRange) return false;

        float angle = Vector2.Angle(facingDir, dirToPlayer.normalized);
        if (angle > visionAngle * 0.5f) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer.normalized, dist, obstacleMask | playerMask);
        if (hit.collider == null) return false;
        if (hit.collider.gameObject == gameObject) return false;
        return hit.collider.CompareTag("Player");
    }

    void UpdateSpriteDirection()
    {
        if (directionSprites == null || directionSprites.Length < 8 || sprite == null) return;
        if (facingDir.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(facingDir.y, facingDir.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        int index;
        if (angle >= 337.5f || angle < 22.5f) index = 2;      // E
        else if (angle >= 22.5f && angle < 67.5f) index = 1;   // NE
        else if (angle >= 67.5f && angle < 112.5f) index = 0;  // N
        else if (angle >= 112.5f && angle < 157.5f) index = 7; // NW
        else if (angle >= 157.5f && angle < 202.5f) index = 6; // W
        else if (angle >= 202.5f && angle < 247.5f) index = 5; // SW
        else if (angle >= 247.5f && angle < 292.5f) index = 4; // S
        else index = 3; // SE

        sprite.sprite = directionSprites[index];
    }

    void RotateTowards(Vector2 dir)
    {
        // Transform rotation is no longer used — sprite direction is handled
        // by UpdateSpriteDirection() and vision cone by UpdateVisionCone()
    }

    void UpdateColor()
    {
        if (!sprite) return;
        if (directionSprites != null && directionSprites.Length >= 8)
        {
            sprite.color = Color.white;
            return;
        }
        sprite.color = currentState switch
        {
            GuardState.Patrol => Color.green,
            GuardState.Alert => (chaseTimer > 1.5f) ? new Color(0.7f, 0f, 0f) : Color.red,
            GuardState.Investigate => new Color(1f, 0.5f, 0f),
            GuardState.Return => Color.yellow,
            _ => Color.white
        };
    }

    // Projects the desired direction to slide smoothly around obstacles
    Vector2 AvoidObstacles(Vector2 desiredDir)
    {
        // Start raycast slightly in front of the guard's body (0.4f radius offset)
        Vector2 origin = (Vector2)transform.position + desiredDir * 0.4f;
        RaycastHit2D hit = Physics2D.Raycast(origin, desiredDir, 0.8f, obstacleMask);
        if (hit.collider != null)
        {
            Vector2 hitNormal = hit.normal;
            Vector2 slideDir = desiredDir - Vector2.Dot(desiredDir, hitNormal) * hitNormal;
            if (slideDir.sqrMagnitude > 0.01f)
            {
                return slideDir.normalized;
            }
            else
            {
                // Fallback to perpendicular direction
                return new Vector2(-hitNormal.y, hitNormal.x).normalized;
            }
        }
        return desiredDir;
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) { Debug.LogWarning($"{gameObject.name} footstep clip is null!"); return; }
        audioSource.pitch = 1f + Random.Range(-0.1f, 0.1f);
        audioSource.volume = Random.Range(0.7f, 1f);
        audioSource.PlayOneShot(clip);
    }

    public void HearNoise(Vector2 noisePos)
    {
        float dist = Vector2.Distance(transform.position, noisePos);
        if (dist > hearingRange) return;

        if (currentState == GuardState.Patrol || currentState == GuardState.Return || currentState == GuardState.Investigate)
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
