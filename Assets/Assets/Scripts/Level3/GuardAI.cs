using UnityEngine;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 2.5f;
    public float waypointWaitTime = 1f;

    [Header("Detection")]
    public float hearingRange = 6f;
    public float sightRange = 10f;
    [Range(0, 360)]
    public float sightAngle = 100f;
    public LayerMask obstacleMask;

    [Header("Footstep Audio")]
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.6f;
    public float footstepVolume = 0.8f;

    [Header("State")]
    public GuardState currentState = GuardState.Patrol;

    public int PreviousWaypointIndex { get; private set; }
    public int CurrentWaypointIndex { get; private set; }

    private NavMeshAgent agent;
    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource audioSource;
    private Vector2 animMoveDir;
    private Vector2 smoothMoveDir;
    private float waitTimer;
    private float footstepTimer;
    private float chaseUpdateTimer;
    private int patrolDirection = 1;
    private Vector3 lastKnownPosition;
    private Vector3 chaseTargetPosition;
    private bool hasTarget;
    private bool playerWasVisible;
    private float noSightTimer;
    private float searchWaitTimer;
    private static int closestGuardFrame = -1;
    private static GuardAI closestGuardCache;
    private const float movementSmoothSpeed = 6f;
    private const float chaseStoppingDistance = 1.2f;
    private const float patrolStoppingDistance = 0.1f;
    private const float chaseUpdateInterval = 0.3f;
    private const float chaseUpdateDistThreshold = 2f;
    private const float captureDistance = 0.8f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = patrolStoppingDistance;
        agent.acceleration = 6f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.High;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.volume = footstepVolume;
        }

        if (waypoints == null || waypoints.Length == 0)
            FindWaypoints();
        closestGuardFrame = -1;
        closestGuardCache = null;
    }

    void Start()
    {
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = new Vector3(waypoints[0].position.x, waypoints[0].position.y, transform.position.z);
        }

        if (agent != null && agent.isActiveAndEnabled)
        {
            Vector3 originalPos = transform.position;
            agent.Warp(new Vector3(originalPos.x, 0f, originalPos.y));
            transform.position = originalPos;
            SetDestination(waypoints[0].position);
        }
    }

    void FixedUpdate()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (currentState == GuardState.Trapped)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        agent.nextPosition = new Vector3(transform.position.x, 0f, transform.position.y);

        Vector2 rawDir = new Vector2(agent.desiredVelocity.x, agent.desiredVelocity.z);

        if (rawDir.sqrMagnitude < 0.01f && agent.hasPath)
        {
            Vector3 target3D = agent.destination;
            rawDir = new Vector2(target3D.x - transform.position.x, target3D.z - transform.position.y);
        }

        rawDir = rawDir.normalized;

        smoothMoveDir = Vector2.MoveTowards(smoothMoveDir, rawDir, movementSmoothSpeed * Time.fixedDeltaTime);
        animMoveDir = smoothMoveDir;
        float currentSpeed = (currentState == GuardState.Chase) ? chaseSpeed : patrolSpeed;

        if (rb != null)
        {
            rb.linearVelocity = smoothMoveDir * currentSpeed;
        }
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (currentState == GuardState.Trapped)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckForPlayer();

        switch (currentState)
        {
            case GuardState.Patrol:
            case GuardState.Return:
                UpdatePatrol();
                break;
            case GuardState.Chase:
                UpdateChase();
                break;
            case GuardState.Search:
                UpdateSearch();
                break;
        }

        if (animator != null)
        {
            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
            animator.SetFloat("Speed", speed);
            if (speed > 0.1f)
            {
                animator.SetFloat("MoveX", animMoveDir.x);
                animator.SetFloat("MoveY", animMoveDir.y);
            }
        }

        UpdateFootstep();
    }

    void CheckForPlayer()
    {
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm == null || gm.player == null) return;

        Vector2 guardPos = transform.position;
        Vector2 playerPos = gm.player.transform.position;
        Vector2 dirToPlayer = playerPos - guardPos;
        float distToPlayer = dirToPlayer.magnitude;

        if (distToPlayer > sightRange) return;

        bool inCone = false;
        if (currentState == GuardState.Search)
        {
            inCone = true;
        }
        else if (smoothMoveDir.sqrMagnitude > 0.01f)
        {
            float dot = Vector2.Dot(smoothMoveDir.normalized, dirToPlayer.normalized);
            float coneThreshold = Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad);
            inCone = dot >= coneThreshold;
        }
        else
        {
            inCone = distToPlayer <= 4f;
        }

        if (!inCone) return;

        RaycastHit2D hit = Physics2D.Raycast(guardPos, dirToPlayer.normalized, distToPlayer, obstacleMask);
        if (hit.collider != null) return;

        lastKnownPosition = playerPos;
        hasTarget = true;

        if (currentState != GuardState.Chase)
        {
            currentState = GuardState.Chase;
            agent.stoppingDistance = chaseStoppingDistance;
            chaseTargetPosition = playerPos;
            chaseUpdateTimer = 0f;
        }
    }

    void FindWaypoints()
    {
        string wpName = gameObject.name + "_WPs";

        Transform wpParent = null;
        if (transform.parent != null)
        {
            wpParent = transform.parent.Find(wpName);
        }

        if (wpParent == null)
        {
            var go = GameObject.Find(wpName);
            if (go != null) wpParent = go.transform;
        }

        if (wpParent != null)
        {
            waypoints = new Transform[wpParent.childCount];
            for (int i = 0; i < wpParent.childCount; i++)
                waypoints[i] = wpParent.GetChild(i);
        }
    }

    void UpdatePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.speed = patrolSpeed;

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                waitTimer = 0f;
                PreviousWaypointIndex = CurrentWaypointIndex;

                int nextIndex = CurrentWaypointIndex + patrolDirection;
                if (nextIndex >= waypoints.Length || nextIndex < 0)
                {
                    patrolDirection *= -1;
                    nextIndex = CurrentWaypointIndex + patrolDirection;
                }

                CurrentWaypointIndex = nextIndex;
                SetDestination(waypoints[CurrentWaypointIndex].position);
            }
        }
    }

    void UpdateChase()
    {
        agent.speed = chaseSpeed;

        if (!hasTarget) return;

        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null && gm.player != null)
        {
            Vector2 playerPos = gm.player.transform.position;
            Vector2 guardPos = transform.position;
            Vector2 dirToPlayer = playerPos - guardPos;
            float dist = dirToPlayer.magnitude;

            if (dist <= captureDistance)
            {
                gm.LoseGame();
                return;
            }

            bool stillVisible = dist <= sightRange;
            if (stillVisible)
            {
                Vector2 lookDir = (smoothMoveDir.sqrMagnitude > 0.01f) ? smoothMoveDir.normalized : dirToPlayer.normalized;
                float dot = Vector2.Dot(lookDir, dirToPlayer.normalized);
                float coneThreshold = Mathf.Cos(sightAngle * 0.5f * Mathf.Deg2Rad);
                if (dot >= coneThreshold || smoothMoveDir.sqrMagnitude <= 0.01f)
                {
                    RaycastHit2D hit = Physics2D.Raycast(guardPos, dirToPlayer.normalized, dist, obstacleMask);
                    if (hit.collider != null) stillVisible = false;
                }
                else
                {
                    stillVisible = false;
                }
            }

            if (stillVisible)
            {
                lastKnownPosition = playerPos;
                noSightTimer = 0f;
            }
            else
            {
                noSightTimer += Time.deltaTime;
            }

            if (noSightTimer >= 2f)
            {
                currentState = GuardState.Search;
                return;
            }
        }

        chaseUpdateTimer += Time.deltaTime;
        float distToTarget = Vector2.Distance(transform.position, chaseTargetPosition);
        if (chaseUpdateTimer >= chaseUpdateInterval || distToTarget > chaseUpdateDistThreshold)
        {
            chaseTargetPosition = lastKnownPosition;
            SetDestination(lastKnownPosition);
            chaseUpdateTimer = 0f;
        }

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            currentState = GuardState.Search;
        }
    }

    void UpdateSearch()
    {
        CheckForPlayer();

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            searchWaitTimer += Time.deltaTime;
            if (searchWaitTimer >= 1.5f)
            {
                ReturnToPatrol();
            }
        }
    }

    void UpdateFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        if (audioSource == null) return;

        float currentSpeed = (currentState == GuardState.Chase) ? chaseSpeed : patrolSpeed;
        float effectiveSpeed = smoothMoveDir.magnitude * currentSpeed;
        if (effectiveSpeed <= 0.01f) return;
        if (currentState == GuardState.Chase) return;

        if (!IsClosestGuardToPlayerCached()) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            PlayFootstep();
            footstepTimer = footstepInterval;
        }
    }

    bool IsClosestGuardToPlayerCached()
    {
        int currentFrame = Time.frameCount;
        if (currentFrame != closestGuardFrame)
        {
            closestGuardFrame = currentFrame;
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm == null || gm.player == null)
            {
                closestGuardCache = null;
                return false;
            }

            GuardAI best = null;
            float bestDist = float.MaxValue;
            Vector2 playerPos = gm.player.transform.position;

            var guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
            foreach (var guard in guards)
            {
                if (guard == null) continue;
                float d = Vector2.Distance(guard.transform.position, playerPos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = guard;
                }
            }
            closestGuardCache = best;
        }
        return closestGuardCache == this;
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) return;
        audioSource.pitch = 1f + Random.Range(-0.08f, 0.08f);
        audioSource.volume = footstepVolume;
        audioSource.PlayOneShot(clip);
    }

    void SetDestination(Vector3 position)
    {
        if (agent != null && agent.isOnNavMesh)
            agent.SetDestination(new Vector3(position.x, 0f, position.y));
    }

    public void HearNoise(Vector3 position)
    {
        if (currentState == GuardState.Chase) return;

        lastKnownPosition = position;
        hasTarget = true;
        searchWaitTimer = 0f;
        currentState = GuardState.Search;
        SetDestination(position);
    }

    public void ReturnToPatrol()
    {
        currentState = GuardState.Patrol;
        patrolDirection = 1;
        hasTarget = false;
        waitTimer = 0f;
        noSightTimer = 0f;
        searchWaitTimer = 0f;
        playerWasVisible = false;
        agent.stoppingDistance = patrolStoppingDistance;
        smoothMoveDir = Vector2.zero;
        if (waypoints != null && waypoints.Length > 0)
            SetDestination(waypoints[CurrentWaypointIndex].position);
    }
}
