using UnityEngine;
using System;

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
    public float throwRange = 20f;
    public float throwMaxHeight = 3f;
    public GameObject throwablePrefab;
    public Transform throwSpawnPoint;

    [Header("Skill Circle")]
    public float skillCircleRadius = 5f;
    public float skillCircleWidth = 0.08f;
    public int skillCircleSegments = 60;
    public Color skillCircleColor = new Color(0f, 1f, 1f, 0.3f);

    [Header("Skill Landing Marker")]
    public Color skillMarkerColor = new Color(1f, 1f, 0f, 0.8f);
    public float skillMarkerSize = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    public float stepInterval = 0.4f;

    public event Action OnThrowableChanged;

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
    private Camera mainCam;

    private bool isSkillActive;
    private bool hasSetPosition;
    private Vector3 landingPosition;
    private GameObject skillCircle;
    private LineRenderer skillCircleLine;
    private GameObject skillMarker;
    private SpriteRenderer skillMarkerSprite;

    public bool IsSkillActive => isSkillActive;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        isPlatformer = rb.gravityScale > 0.01f;
        mainCam = Camera.main;

        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.7f;
        }
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
        if (Time.timeScale == 0f) return;

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

        HandleSkillInput();

        if (!isSkillActive && Input.GetMouseButtonDown(1))
            TryThrow();

        if (isSkillActive && hasSetPosition)
        {
            float distToLanding = Vector2.Distance(transform.position, landingPosition);
            if (distToLanding > skillCircleRadius)
            {
                hasSetPosition = false;
                HideLandingMarker();
                Debug.Log($"[SkillKaleng] Landing auto-hide: player out of range ({distToLanding:F2} > {skillCircleRadius})");
            }
        }

        if (isSkillActive)
            UpdateSkillCirclePosition();
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

    void HandleSkillInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isSkillActive)
                ActivateSkill();
            else
                DeactivateSkill();
        }

        if (!isSkillActive) return;

        if (Input.GetMouseButtonDown(1))
        {
            DeactivateSkill();
            Debug.Log("[SkillKaleng] Skill cancelled");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!hasSetPosition)
                SetLandingPosition();
            else
                ExecuteThrow();
        }
    }

    public void ActivateSkill()
    {
        if (currentThrowables <= 0)
        {
            Debug.Log("[SkillKaleng] No throwables available");
            return;
        }

        isSkillActive = true;
        hasSetPosition = false;
        ShowSkillCircle();
        Debug.Log("[SkillKaleng] Skill activated");
    }

    void DeactivateSkill()
    {
        isSkillActive = false;
        hasSetPosition = false;
        HideSkillCircle();
        HideLandingMarker();
        Debug.Log("[SkillKaleng] Skill deactivated");
    }

    void SetLandingPosition()
    {
        Vector3 mousePos = GetMouseWorldPos();
        float dist = Vector2.Distance(transform.position, mousePos);

        if (dist > throwRange)
        {
            Debug.Log($"[SkillKaleng] Out of range: {dist:F2} > {throwRange}");
            return;
        }

        landingPosition = mousePos;
        hasSetPosition = true;
        ShowLandingMarker();
        Debug.Log($"[SkillKaleng] Position set at: {mousePos}");
    }

    void ExecuteThrow()
    {
        if (currentThrowables <= 0 || throwablePrefab == null) return;

        Vector3 spawnPos = throwSpawnPoint != null ? throwSpawnPoint.position : (Vector3)transform.position;

        GameObject obj = Instantiate(throwablePrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<ThrowableObject>()?.Throw(landingPosition);

        currentThrowables--;
        Debug.Log($"[SkillKaleng] Can thrown to: {landingPosition}, remaining: {currentThrowables}");

        DeactivateSkill();
        OnThrowableChanged?.Invoke();
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCam.nearClipPlane;
        return mainCam.ScreenToWorldPoint(mouseScreenPos);
    }

    void ShowSkillCircle()
    {
        if (skillCircle == null)
            CreateSkillCircle();

        skillCircle.SetActive(true);
        UpdateSkillCirclePosition();
        Debug.Log("[SkillKaleng] Skill circle shown");
    }

    void CreateSkillCircle()
    {
        skillCircle = new GameObject("SkillCircle");

        skillCircleLine = skillCircle.AddComponent<LineRenderer>();
        skillCircleLine.useWorldSpace = false;
        skillCircleLine.loop = true;
        skillCircleLine.startWidth = skillCircleWidth;
        skillCircleLine.endWidth = skillCircleWidth;
        skillCircleLine.numCornerVertices = 0;
        skillCircleLine.numCapVertices = 0;
        skillCircleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        skillCircleLine.receiveShadows = false;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            skillCircleLine.material = new Material(shader);

        skillCircleLine.startColor = skillCircleColor;
        skillCircleLine.endColor = skillCircleColor;

        skillCircleLine.positionCount = skillCircleSegments;

        for (int i = 0; i < skillCircleSegments; i++)
        {
            float angle = i * (360f / skillCircleSegments) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * skillCircleRadius;
            skillCircleLine.SetPosition(i, pos);
        }

        Debug.Log($"[SkillKaleng] Circle created: radius={skillCircleRadius}, width={skillCircleWidth}, segments={skillCircleSegments}");
    }

    void UpdateSkillCirclePosition()
    {
        if (skillCircle == null) return;

        skillCircle.transform.position = transform.position;
    }

    void HideSkillCircle()
    {
        if (skillCircle != null)
            skillCircle.SetActive(false);

        Debug.Log("[SkillKaleng] Skill circle hidden");
    }

    void ShowLandingMarker()
    {
        if (skillMarker == null)
            CreateLandingMarker();

        skillMarker.SetActive(true);
        skillMarker.transform.position = new Vector3(
            landingPosition.x,
            landingPosition.y,
            transform.position.z
        );
        Debug.Log("[SkillKaleng] Landing marker shown");
    }

    void CreateLandingMarker()
    {
        skillMarker = new GameObject("LandingMarker");
        skillMarkerSprite = skillMarker.AddComponent<SpriteRenderer>();

        skillMarkerSprite.sprite = CreateCircleSprite();
        skillMarkerSprite.color = skillMarkerColor;
        skillMarkerSprite.sortingOrder = 100;

        skillMarker.transform.localScale = Vector3.one * skillMarkerSize;
    }

    void HideLandingMarker()
    {
        if (skillMarker != null)
            skillMarker.SetActive(false);

        Debug.Log("[SkillKaleng] Landing marker hidden");
    }

    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = size * 0.5f - 2f;

        Color transparent = new Color(0, 0, 0, 0);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = transparent;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];
        if (clip == null) return;
        audioSource.pitch = 1f + UnityEngine.Random.Range(-0.08f, 0.08f);
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
        Debug.Log($"[SkillKaleng] AddThrowable: +{amount}, total={currentThrowables}");
        OnThrowableChanged?.Invoke();
    }

    public int GetThrowableCount() => currentThrowables;
}
