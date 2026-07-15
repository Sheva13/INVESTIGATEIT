using UnityEngine;
using UnityEngine.Events;
using Level3;

public enum GuardState
{
    Patrol,
    Chase,
    Search,
    Trapped,
    Return
}

public class GuardAI : MonoBehaviour
{
    [Header("Movement")]
    public Transform[] waypoints;
    public bool loopPatrol = false;
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 3.2f;
    public float investigateSpeed = 2.2f;
    public float patrolAccel = 8f;
    public float chaseAccel = 12f;
    public float arrivalThreshold = 0.2f;

    [Header("Detection")]
    public float sightRange = 3.5f;
    [Range(0, 360)]
    public float sightAngle = 60f;
    public LayerMask obstacleMask;

    [Header("Timing")]
    public float waypointWaitTime = 1f;
    public float investigateDuration = 3f;
    public float chaseUpdateInterval = 0.3f;
    public float noSightTimeout = 2f;

    [Header("Capture")]
    public float captureDistance = 0.8f;
    public UnityEvent onPlayerCaptured;

    [Header("Footstep Audio")]
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.5f;
    public float footstepVolume = 0.8f;

    [Header("State")]
    public Vector2 AnimMoveDir => animMoveDir;
    public GuardState currentState = GuardState.Patrol;

    public int PreviousWaypointIndex { get; private set; }
    public int CurrentWaypointIndex { get; private set; }

    private Animator animator;
    private AudioSource audioSource;
    private Transform playerTransform;
    private Vector2 animMoveDir;
    private Vector2 currentVelocity;
    private float waitTimer;
    private float footstepTimer;
    private float chaseUpdateTimer;
    private float noSightTimer;
    private float investigateTimer;
    private int patrolDirection = 1;
    private bool hasTarget;
    private static int closestGuardFrame = -1;
    private static GuardAI closestGuardCache;
    private GameManager gmCache;
    private Vector3 lastKnownPosition;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

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
    }

    void Start()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
            playerTransform = playerGo.transform;

        gmCache = FindAnyObjectByType<GameManager>();
        closestGuardFrame = -1;
        closestGuardCache = null;
    }

    void FixedUpdate()
    {
        if (currentState == GuardState.Trapped)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPos = GetTargetPosition();
        Vector2 guardPos = transform.position;
        Vector2 dir = targetPos - guardPos;

        float currentSpeed = GetCurrentSpeed();
        bool shouldMove = dir.magnitude > arrivalThreshold;

        currentVelocity = shouldMove ? dir.normalized * currentSpeed : Vector2.zero;

        Vector2 newPos = Vector2.MoveTowards(guardPos, targetPos, currentSpeed * Time.fixedDeltaTime);
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

        if (currentVelocity.sqrMagnitude > 0.01f)
            animMoveDir = currentVelocity.normalized;
    }

    void Update()
    {
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

        UpdateAnimator();
        UpdateFootstep();
    }

    Vector2 GetTargetPosition()
    {
        switch (currentState)
        {
            case GuardState.Patrol:
            case GuardState.Return:
                if (waypoints != null && waypoints.Length > 0)
                    return waypoints[CurrentWaypointIndex].position;
                return transform.position;
            case GuardState.Chase:
                return playerTransform != null ? playerTransform.position : transform.position;
            case GuardState.Search:
                return hasTarget ? new Vector2(lastKnownPosition.x, lastKnownPosition.y) : transform.position;
            default:
                return transform.position;
        }
    }

    float GetCurrentSpeed()
    {
        switch (currentState)
        {
            case GuardState.Chase: return chaseSpeed;
            case GuardState.Search: return investigateSpeed;
            default: return patrolSpeed;
        }
    }

    void FindWaypoints()
    {
        string wpName = gameObject.name + "_WPs";

        Transform wpParent = null;
        if (transform.parent != null)
            wpParent = transform.parent.Find(wpName);

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

        float distToTarget = Vector2.Distance(transform.position, waypoints[CurrentWaypointIndex].position);

        if (distToTarget < arrivalThreshold)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointWaitTime)
            {
                waitTimer = 0f;
                PreviousWaypointIndex = CurrentWaypointIndex;

                if (loopPatrol)
                {
                    CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypoints.Length;
                }
                else
                {
                    int nextIndex = CurrentWaypointIndex + patrolDirection;
                    if (nextIndex >= waypoints.Length || nextIndex < 0)
                    {
                        patrolDirection *= -1;
                        nextIndex = CurrentWaypointIndex + patrolDirection;
                    }
                    CurrentWaypointIndex = nextIndex;
                }
            }
        }
    }

    void UpdateChase()
    {
        if (playerTransform == null) return;

        Vector2 playerPos = playerTransform.position;
        Vector2 guardPos = transform.position;
        float dist = Vector2.Distance(guardPos, playerPos);

        if (dist <= captureDistance)
        {
            if (gmCache != null) gmCache.LoseGame();
            onPlayerCaptured?.Invoke();
            return;
        }

        if (dist > sightRange)
        {
            noSightTimer += Time.deltaTime;
        }
        else
        {
            noSightTimer = 0f;
        }

        if (noSightTimer >= noSightTimeout)
        {
            currentState = GuardState.Search;
            hasTarget = true;
            investigateTimer = 0f;
            return;
        }

        chaseUpdateTimer += Time.deltaTime;
        if (chaseUpdateTimer >= chaseUpdateInterval)
        {
            chaseUpdateTimer = 0f;
        }
    }

    void UpdateSearch()
    {
        float distToTarget = Vector2.Distance(transform.position, new Vector2(lastKnownPosition.x, lastKnownPosition.y));

        if (distToTarget < arrivalThreshold)
        {
            investigateTimer += Time.deltaTime;
            if (investigateTimer >= investigateDuration)
            {
                ReturnToPatrol();
            }
        }
    }

    void UpdateFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        if (audioSource == null) return;

        float spd = currentVelocity.magnitude;
        if (spd <= 0.01f) return;
        if (currentState == GuardState.Chase) return;

        if (!IsClosestGuardToPlayerCached()) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            PlayFootstep();
            footstepTimer = footstepInterval;
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = currentVelocity.magnitude;
        animator.SetFloat("Speed", speed);

        if (speed > 0.1f)
        {
            animator.SetFloat("MoveX", animMoveDir.x);
            animator.SetFloat("MoveY", animMoveDir.y);
        }
    }

    bool IsClosestGuardToPlayerCached()
    {
        int currentFrame = Time.frameCount;
        if (currentFrame != closestGuardFrame)
        {
            closestGuardFrame = currentFrame;
            if (playerTransform == null)
            {
                closestGuardCache = null;
                return false;
            }

            GuardAI best = null;
            float bestDist = float.MaxValue;

            var guards = FindObjectsByType<GuardAI>();
            foreach (var guard in guards)
            {
                if (guard == null) continue;
                float d = Vector2.Distance(guard.transform.position, playerTransform.position);
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

    public void HearNoise(Vector3 position)
    {
        lastKnownPosition = position;
        hasTarget = true;
        investigateTimer = 0f;

        if (currentState == GuardState.Chase)
        {
            noSightTimer = 0f;
            chaseUpdateTimer = 0f;
        }

        currentState = GuardState.Search;
    }

    public void ReturnToPatrol()
    {
        currentState = GuardState.Patrol;
        patrolDirection = 1;
        hasTarget = false;
        waitTimer = 0f;
        noSightTimer = 0f;
        investigateTimer = 0f;
    }

    public void OnPlayerDetected()
    {
        if (currentState == GuardState.Chase) return;

        currentState = GuardState.Chase;
        hasTarget = true;
        chaseUpdateTimer = 0f;
        noSightTimer = 0f;
    }
}
