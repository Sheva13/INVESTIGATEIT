using UnityEngine;

[RequireComponent(typeof(GuardAI))]
public class PatrolPathVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color pathColor = new Color(0f, 1f, 0.5f, 1f); // Neon green/cyan
    public float lineWidth = 0.15f;
    public float scrollSpeed = 2f;
    public float pulseSpeed = 3f;
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f;
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;

    [Header("Fade Settings")]
    public float stateFadeSpeed = 4f; // Speed of fading in/out when changing states

    private GuardAI guard;
    private LineRenderer lineRenderer;
    private Material pathMat;
    private Texture2D dashedTex;
    private float currentOverallAlpha = 0f; // For smooth fading between states

    void Start()
    {
        guard = GetComponent<GuardAI>();
        
        // Create child GameObject for the path line
        GameObject pathObj = new GameObject("PatrolPathVisual");
        pathObj.transform.SetParent(transform);
        pathObj.transform.localPosition = Vector3.zero;
        pathObj.transform.localRotation = Quaternion.identity;

        lineRenderer = pathObj.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 3;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.alignment = LineAlignment.TransformZ; // Better for 2D top-down

        // Create unlit material
        pathMat = new Material(Shader.Find("Sprites/Default"));
        
        // Generate dashed texture programmatically
        dashedTex = CreateDashedTexture();
        pathMat.mainTexture = dashedTex;
        
        lineRenderer.material = pathMat;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        
        // Start fully visible if in Patrol state
        currentOverallAlpha = (guard.currentState == GuardState.Patrol || guard.currentState == GuardState.Return) ? 1f : 0f;
    }

    void Update()
    {
        if (guard == null || guard.waypoints == null || guard.waypoints.Length < 2 || lineRenderer == null)
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            return;
        }

        // Determine if we should show the patrol path
        bool showPath = guard.currentState == GuardState.Patrol || guard.currentState == GuardState.Return;
        float targetAlpha = showPath ? 1f : 0f;
        currentOverallAlpha = Mathf.MoveTowards(currentOverallAlpha, targetAlpha, Time.deltaTime * stateFadeSpeed);

        if (currentOverallAlpha <= 0.01f)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Get active waypoints
        int prevIdx = guard.PreviousWaypointIndex;
        int nextIdx = guard.CurrentWaypointIndex;

        // Boundary check to prevent array index out of bounds
        if (prevIdx < 0 || prevIdx >= guard.waypoints.Length || nextIdx < 0 || nextIdx >= guard.waypoints.Length)
        {
            return;
        }

        Transform prevWp = guard.waypoints[prevIdx];
        Transform nextWp = guard.waypoints[nextIdx];

        if (prevWp == null || nextWp == null) return;

        // Set path positions: Prev -> Guard -> Next
        lineRenderer.SetPosition(0, new Vector3(prevWp.position.x, prevWp.position.y, -0.5f));
        lineRenderer.SetPosition(1, new Vector3(transform.position.x, transform.position.y, -0.5f));
        lineRenderer.SetPosition(2, new Vector3(nextWp.position.x, nextWp.position.y, -0.5f));

        // Animate dashed line scrolling
        // Scroll texture offset in the negative direction so dashes move towards the target
        float offset = -Time.time * scrollSpeed;
        pathMat.mainTextureOffset = new Vector2(offset, 0f);

        // Animate soft pulsing glow
        float pulseRange = (maxAlpha - minAlpha) * 0.5f;
        float pulseBase = minAlpha + pulseRange;
        float currentPulseAlpha = pulseBase + Mathf.Sin(Time.time * pulseSpeed) * pulseRange;
        
        // Combine pulse alpha with state fade alpha
        float finalAlpha = currentPulseAlpha * currentOverallAlpha;

        // Configure gradient (Alpha: fade at ends, peak at Guard position)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(pathColor, 0f),
                new GradientColorKey(pathColor, 0.5f),
                new GradientColorKey(pathColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),              // Fades to 0 at prev waypoint
                new GradientAlphaKey(finalAlpha, 0.5f),   // Fully visible at guard
                new GradientAlphaKey(0f, 1f)              // Fades to 0 at next waypoint
            }
        );

        lineRenderer.colorGradient = gradient;
    }

    Texture2D CreateDashedTexture()
    {
        Texture2D tex = new Texture2D(32, 1);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] colors = new Color[32];
        for (int i = 0; i < 32; i++)
        {
            // 16 pixels white, 16 pixels transparent for a 50% duty cycle dash pattern
            colors[i] = (i < 16) ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
        
        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    void OnDestroy()
    {
        if (dashedTex != null) Destroy(dashedTex);
        if (pathMat != null) Destroy(pathMat);
    }
}
