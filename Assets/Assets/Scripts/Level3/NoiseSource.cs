using UnityEngine;

public class NoiseSource : MonoBehaviour
{
    [Header("Noise")]
    public float noiseRadius = 6f;
    public float lifetime = 2f;
    public LayerMask guardMask;

    private SpriteRenderer ringSprite;
    private float elapsed = 0f;

    void Start()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, noiseRadius, guardMask);
        foreach (var hit in hits)
        {
            GuardAI guard = hit.GetComponent<GuardAI>();
            if (guard) guard.HearNoise(transform.position);
        }

        ringSprite = GetComponent<SpriteRenderer>();
        if (ringSprite == null)
        {
            ringSprite = gameObject.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            ringSprite.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        ringSprite.color = new Color(1f, 1f, 0f, 0.5f);
        ringSprite.sortingOrder = 2;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (ringSprite == null) return;
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;
        float size = Mathf.Lerp(0.1f, noiseRadius * 2f, t);
        transform.localScale = Vector3.one * size;
        Color c = ringSprite.color;
        c.a = Mathf.Lerp(0.5f, 0f, t);
        ringSprite.color = c;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
