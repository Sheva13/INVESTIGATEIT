using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Throw")]
    public int maxThrowables = 3;
    public GameObject throwablePrefab;
    public Transform throwSpawnPoint;

    private int currentThrowables;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        currentThrowables = maxThrowables;
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        if (Input.GetMouseButtonDown(1))
        {
            TryThrow();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    void TryThrow()
    {
        if (currentThrowables <= 0 || throwablePrefab == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 spawnPos = throwSpawnPoint ? throwSpawnPoint.position : transform.position;
        GameObject obj = Instantiate(throwablePrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<ThrowableObject>()?.Throw(mousePos);

        currentThrowables--;
    }

    public void AddThrowable(int amount)
    {
        currentThrowables = Mathf.Min(currentThrowables + amount, maxThrowables);
    }

    public int GetThrowableCount() => currentThrowables;
}
