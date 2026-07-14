using UnityEngine;

public class NoiseSource : MonoBehaviour
{
    [Header("Detection")]
    public float noiseRadius = 6f;
    public LayerMask obstacleMask;

    [Header("Visual")]
    public float lifetime = 2f;
    public bool showRing = true;
    public bool showFill = true;
    public Color ringColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    public Color fillColor = new Color(1f, 0.9f, 0.2f, 0.15f);

    private LineRenderer ringRenderer;
    private SpriteRenderer fillRenderer;
    private float elapsed = 0f;
    private const int segmentCount = 40;

    void Start()
    {
        AlertGuards();
        SetupVisuals();
    }

    void AlertGuards()
    {
        var guards = FindObjectsByType<GuardAI>();
        int alerted = 0;

        foreach (var guard in guards)
        {
            Vector2 dir = (Vector2)guard.transform.position - (Vector2)transform.position;
            float dist = dir.magnitude;

            if (dist > noiseRadius) continue;

            if (obstacleMask.value != 0)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(transform.position, dir.normalized, dist, obstacleMask);
                if (wallHit.collider != null) continue;
            }

            guard.HearNoise(transform.position);
            alerted++;
            Debug.Log($"[NoiseSource] Guard '{guard.name}' alerted at distance {dist:F1}");
        }

        Debug.Log($"[NoiseSource] AlertGuards: {alerted}/{guards.Length} guards alerted (radius={noiseRadius})");
    }

    void SetupVisuals()
    {
        if (showRing)
            SetupRing();
        if (showFill)
            SetupFill();
    }

    void SetupRing()
    {
        ringRenderer = GetComponent<LineRenderer>();
        if (ringRenderer == null)
            ringRenderer = gameObject.AddComponent<LineRenderer>();

        ringRenderer.useWorldSpace = true;
        ringRenderer.positionCount = segmentCount + 1;
        ringRenderer.loop = true;
        ringRenderer.startWidth = 0.06f;
        ringRenderer.endWidth = 0.06f;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            ringRenderer.material = new Material(shader);
    }

    void SetupFill()
    {
        fillRenderer = gameObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreateCircleSprite();
        fillRenderer.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0f);
        fillRenderer.sortingOrder = -1;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            fillRenderer.material = new Material(shader);
    }

    Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = size * 0.5f - 1f;

        Color transparent = new Color(0, 0, 0, 0);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = dist <= radius ? Color.white : transparent;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        float currentRadius = Mathf.Lerp(0.1f, noiseRadius, t);
        float alpha = Mathf.Lerp(1f, 0f, t);

        UpdateRing(currentRadius, alpha);
        UpdateFill(currentRadius, alpha);
    }

    void UpdateRing(float currentRadius, float alpha)
    {
        if (ringRenderer == null) return;

        Color currentRing = new Color(ringColor.r, ringColor.g, ringColor.b, ringColor.a * alpha);
        ringRenderer.startColor = currentRing;
        ringRenderer.endColor = currentRing;

        for (int i = 0; i <= segmentCount; i++)
        {
            float angle = i * (360f / segmentCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * currentRadius, Mathf.Sin(angle) * currentRadius, 0f);
            ringRenderer.SetPosition(i, new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, -1f));
        }
    }

    void UpdateFill(float currentRadius, float alpha)
    {
        if (fillRenderer == null) return;

        float scale = currentRadius * 2f;
        fillRenderer.transform.position = new Vector3(transform.position.x, transform.position.y, -1f);
        fillRenderer.transform.localScale = new Vector3(scale, scale, 1f);

        Color currentFill = new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * alpha);
        fillRenderer.color = currentFill;
    }

    void OnDestroy()
    {
        if (ringRenderer != null) Destroy(ringRenderer);
        if (fillRenderer != null)
        {
            if (fillRenderer.sprite != null)
            {
                if (fillRenderer.sprite.texture != null)
                    Destroy(fillRenderer.sprite.texture);
                Destroy(fillRenderer.sprite);
            }
            Destroy(fillRenderer);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
