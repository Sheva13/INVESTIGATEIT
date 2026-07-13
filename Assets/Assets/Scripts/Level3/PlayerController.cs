using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float acceleration = 12f;

    [Header("Throw")]
    public int maxThrowables = 3;
    public float throwRange = 20f;
    public GameObject throwablePrefab;
    public Transform throwSpawnPoint;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    public float stepInterval = 0.4f;

    private int currentThrowables;
    private Vector2 moveInput;
    private Vector2 facingDir = Vector2.right;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    private float stepTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.7f;
        }
        else Debug.LogError("PlayerController: AudioSource not found!");
        if (footstepClips == null || footstepClips.Length == 0)
            Debug.LogWarning("PlayerController: footstepClips not assigned!");
        currentThrowables = 0;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();
        
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            facingDir = moveInput.normalized;
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
        
        if (animator != null)
        {
            animator.SetFloat("MoveX", facingDir.x);
            animator.SetFloat("MoveY", facingDir.y);
            animator.SetFloat("Speed", moveInput.magnitude);
        }
        
        if (Input.GetMouseButtonDown(1))
            TryThrow();
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) { Debug.LogWarning("footstep clip is null!"); return; }
        audioSource.pitch = 1f + Random.Range(-0.08f, 0.08f);
        audioSource.PlayOneShot(clip);
    }

    void TryThrow()
    {
        if (currentThrowables <= 0 || throwablePrefab == null) return;
        
        Vector3 spawnPos = throwSpawnPoint ? throwSpawnPoint.position : transform.position;
        Vector3 targetPos = spawnPos + (Vector3)facingDir * throwRange;
        
        RaycastHit2D hit = Physics2D.Raycast(spawnPos, facingDir, throwRange);
        if (hit.collider != null)
            targetPos = hit.point;
        
        GameObject obj = Instantiate(throwablePrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<ThrowableObject>()?.Throw(targetPos);
        
        currentThrowables--;
    }

    public void AddThrowable(int amount)
    {
        currentThrowables = Mathf.Min(currentThrowables + amount, maxThrowables);
    }

    public int GetThrowableCount() => currentThrowables;
}