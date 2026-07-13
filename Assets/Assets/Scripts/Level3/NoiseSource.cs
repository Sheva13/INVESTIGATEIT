using UnityEngine;

public class NoiseSource : MonoBehaviour
{
    [Header("Noise")]
    public float noiseRadius = 6f;
    public float lifetime = 2f;
    public LayerMask guardMask;

    private LineRenderer lineRenderer;
    private float elapsed = 0f;
    private const int segmentCount = 40; // High resolution circle segments

    void Start()
    {
        // 1. Alert guards within radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardMask);
        foreach (var hit in hits)
        {
            GuardAI guard = hit.GetComponent<GuardAI>();
            if (guard) guard.HearNoise(transform.position);
        }

        // 2. Setup LineRenderer for the Sonar Wave Ring
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = segmentCount + 1; // +1 to close circle
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.06f;
        lineRenderer.endWidth = 0.06f;
        
        // Setup sprite material so it has clean color flat rendering
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }
        
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (lineRenderer == null) return;
        
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        
        // Interpolate radius and alpha
        float currentRadius = Mathf.Lerp(0.1f, noiseRadius, t);
        float alpha = Mathf.Lerp(0.6f, 0f, t);
        
        Color waveColor = new Color(1f, 0.9f, 0.2f, alpha); // Sonar yellow-orange
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
        
        // Calculate circle positions
        for (int i = 0; i <= segmentCount; i++)
        {
            float angle = i * (360f / segmentCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * currentRadius, Mathf.Sin(angle) * currentRadius, 0f);
            
            // Keep Z coordinate at -1.0 so it renders on same plane as gameplay
            lineRenderer.SetPosition(i, new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, -1.0f));
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
