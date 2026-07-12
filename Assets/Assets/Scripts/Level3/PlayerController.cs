using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public int maxJumps = 2;
    public float fallMultiplier = 2.5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Throw")]
    public int maxThrowables = 3;
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
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float jumpDuration = 0.35f;
    private bool isPlatformer = false;
    private bool isGrounded = true;
    private bool wasGrounded = true;
    private int jumpCount = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        isPlatformer = rb.gravityScale > 0.01f;
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.7f;
        }
        else Debug.LogError("PlayerController: AudioSource not found!");
        if (footstepClips == null || footstepClips.Length == 0)
            Debug.LogWarning("PlayerController: footstepClips not assigned!");
        currentThrowables = maxThrowables;
    }

    void Start()
    {
        var listener = FindAnyObjectByType<AudioListener>();
        Debug.Log($"PlayerController: AudioListener={(listener != null ? listener.gameObject.name : "NULL")}, AudioSource={audioSource}, clips={footstepClips?.Length}");
        if (footstepClips != null && footstepClips.Length > 0 && footstepClips[0] != null)
        {
            AudioSource.PlayClipAtPoint(footstepClips[0], transform.position, 0.7f);
            Debug.Log($"PlayerController: test footstep played (clip={footstepClips[0].name}, len={footstepClips[0].samples})");
        }
        else Debug.LogWarning("PlayerController: cannot play test footstep — clips missing");
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");

        if (isPlatformer)
        {
            moveInput.y = 0;
        }
        else
        {
            moveInput.y = Input.GetAxisRaw("Vertical");
        }
        moveInput.Normalize();

        float speed = isPlatformer ? Mathf.Abs(moveInput.x) : moveInput.magnitude;

        if (isPlatformer && groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            if (isGrounded && !wasGrounded) jumpCount = 0;
            wasGrounded = isGrounded;
        }

        bool isMoving = speed > 0.01f;
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
            if (sprite != null)
                facingDir = sprite.flipX ? Vector2.left : Vector2.right;
        }

        if (animator != null)
            animator.SetFloat("Speed", speed);

        if (moveInput.x < 0)
            sprite.flipX = true;
        else if (moveInput.x > 0)
            sprite.flipX = false;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (isPlatformer)
            {
                if (jumpCount < maxJumps)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    jumpCount++;
                    isJumping = true;
                    jumpTimer = 0f;
                    if (animator != null)
                    {
                        animator.SetBool("isJumping", false);
                        animator.SetBool("isJumping", true);
                    }
                }
            }
            else
            {
                if (!isJumping)
                {
                    isJumping = true;
                    jumpTimer = 0f;
                    if (animator != null)
                        animator.SetBool("isJumping", true);
                }
            }
        }

        if (isJumping)
        {
            jumpTimer += Time.deltaTime;

            if (isPlatformer)
            {
                if (isGrounded && jumpTimer > 0.1f)
                {
                    isJumping = false;
                    if (animator != null)
                        animator.SetBool("isJumping", false);
                }
            }
            else if (jumpTimer >= jumpDuration)
            {
                isJumping = false;
                if (animator != null)
                    animator.SetBool("isJumping", false);
            }
        }

        if (Input.GetMouseButtonDown(1))
            TryThrow();
    }

    void FixedUpdate()
    {
        if (isPlatformer)
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
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
        Vector3 targetPos = spawnPos + (Vector3)facingDir * 4.0f;

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
