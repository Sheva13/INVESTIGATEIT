using UnityEngine;
using TMPro;

public class PlayerController4 : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

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
    private TextMeshProUGUI canCountText;
    private GameManager gameManager;

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
        else Debug.LogError("PlayerController4: AudioSource not found!");
        if (footstepClips == null || footstepClips.Length == 0)
            Debug.LogWarning("PlayerController4: footstepClips not assigned!");

        gameManager = FindAnyObjectByType<GameManager>();
        var canvasObj = GameObject.Find("CansCountText");
        if (canvasObj != null) canCountText = canvasObj.GetComponent<TextMeshProUGUI>();

        currentThrowables = maxThrowables;
    }

    void Start()
    {
        var listener = FindAnyObjectByType<AudioListener>();
        Debug.Log($"PlayerController4: AudioListener={(listener != null ? listener.gameObject.name : "NULL")}, AudioSource={audioSource}, clips={footstepClips?.Length}");
        if (footstepClips != null && footstepClips.Length > 0 && footstepClips[0] != null)
        {
            AudioSource.PlayClipAtPoint(footstepClips[0], transform.position, 0.7f);
            Debug.Log($"PlayerController4: test footstep played (clip={footstepClips[0].name}, len={footstepClips[0].samples})");
        }
        else Debug.LogWarning("PlayerController4: cannot play test footstep - clips missing");
    }

    void Update()
    {
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
            animator.SetFloat("Speed", moveInput.magnitude);

        UpdateRotation();

        if (canCountText != null && gameManager != null && !gameManager.hasLoot)
            canCountText.text = $"[Kaleng: {currentThrowables}]";

        if (Input.GetMouseButtonDown(1))
            TryThrow();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void UpdateRotation()
    {
        if (moveInput.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
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
        Vector3 targetPos = spawnPos + transform.right * 4.0f;

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
