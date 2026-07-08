using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    [Header("Movement")]
    public float throwSpeed = 6f;
    public float lifetimeAfterLand = 3f;

    [Header("Noise")]
    public GameObject noiseSourcePrefab;

    private Vector2 target;
    private bool isThrown = false;
    private bool hasLanded = false;

    void Update()
    {
        if (!isThrown || hasLanded) return;

        transform.position = Vector2.MoveTowards(transform.position, target, throwSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.15f)
        {
            Landed();
        }
    }

    public void Throw(Vector2 targetPos)
    {
        target = targetPos;
        isThrown = true;
    }

    void Landed()
    {
        hasLanded = true;
        isThrown = false;

        if (noiseSourcePrefab)
            Instantiate(noiseSourcePrefab, transform.position, Quaternion.identity);

        Destroy(gameObject, lifetimeAfterLand);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isThrown && !collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Throwable"))
        {
            Landed();
        }
    }

    void Start()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.gravityScale = 0f;
    }
}
