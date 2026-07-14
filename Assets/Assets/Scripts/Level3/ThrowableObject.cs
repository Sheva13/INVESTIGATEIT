using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    [Header("Movement")]
    public float throwSpeed = 6f;
    public float lifetimeAfterLand = 3f;

    [Header("Arc Settings")]
    public float maxThrowHeight = 3f;

    [Header("Noise")]
    public GameObject noiseSourcePrefab;

    [Header("Audio")]
    public AudioClip throwClip;

    private Vector2 target;
    private Vector2 startPosition;
    private float totalDist;
    private float elapsedFlightTime = 0f;
    private float expectedDuration = 0f;
    private bool isThrown = false;
    private bool hasLanded = false;

    void Update()
    {
        if (!isThrown || hasLanded) return;

        elapsedFlightTime += Time.deltaTime;
        float t = expectedDuration > 0f ? Mathf.Clamp01(elapsedFlightTime / expectedDuration) : 1f;

        // Base 2D position
        Vector2 basePos = Vector2.Lerp(startPosition, target, t);

        // Height arc offset
        float heightOffset = Mathf.Sin(t * Mathf.PI) * maxThrowHeight;

        // Combined position
        transform.position = new Vector3(basePos.x, basePos.y + heightOffset, transform.position.z);

        if (t >= 1.0f || Vector2.Distance(basePos, target) < 0.15f)
        {
            Landed();
        }
    }

    public void Throw(Vector2 targetPos)
    {
        target = targetPos;
        startPosition = transform.position;
        totalDist = Vector2.Distance(startPosition, target);
        expectedDuration = throwSpeed > 0f ? totalDist / throwSpeed : 1f;
        elapsedFlightTime = 0f;
        isThrown = true;

        if (throwClip != null)
            AudioSource.PlayClipAtPoint(throwClip, Camera.main.transform.position, 0.8f);
    }

    void Landed()
    {
        hasLanded = true;
        isThrown = false;
        
        // Reset position to exact target ground coordinates
        transform.position = new Vector3(target.x, target.y, transform.position.z);

        if (noiseSourcePrefab)
            Instantiate(noiseSourcePrefab, transform.position, Quaternion.identity);

        Destroy(gameObject, lifetimeAfterLand);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isThrown && !collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Throwable"))
            Landed();
    }
}
