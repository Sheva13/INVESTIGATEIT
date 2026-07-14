using UnityEngine;
using System;

public class PlayerController2 : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float acceleration = 12f;

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
        mainCam = Camera.main;

        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.7f;
        }
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

        // Flip sprite based on horizontal movement
        if (moveInput.x < 0)
            sprite.flipX = true;
        else if (moveInput.x > 0)
            sprite.flipX = false;

        HandleSkillInput();

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
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
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

    public void AddThrowable(int amount)
    {
        currentThrowables = Mathf.Min(currentThrowables + amount, maxThrowables);
        Debug.Log($"[SkillKaleng] AddThrowable: +{amount}, total={currentThrowables}");
        OnThrowableChanged?.Invoke();
    }

    public int GetThrowableCount() => currentThrowables;
}
