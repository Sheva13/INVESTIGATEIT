using UnityEngine;

[System.Serializable]
public struct CameraZone
{
    public string zoneName;
    public float playerThresholdX;
    public float playerExitThresholdX;
    public Vector3 camPosition;
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public float targetZoom;
}

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Zoom Settings")]
    public float targetZoom = 3.8f;
    public float zoomSpeed = 2.0f;

    [Header("Camera Bounds")]
    public bool enableBounds = true;
    public float minX = -1.39f;
    public float maxX = 53.13f;
    public float minY = 0.25f;
    public float maxY = 1.95f;

    [Header("Zone System")]
    public CameraZone[] zones;
    [Tooltip("Min delay between zone transitions (prevents oscillation)")]
    public float zoneTransitionDelay = 0.5f;
    private int currentZone = -1;
    private bool snapQueued = false;
    private float zoneCooldownTimer = 0f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!target)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void Start()
    {
        if (zones != null && zones.Length > 0)
        {
            ApplyZone(0);
            snapQueued = true;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        if (snapQueued)
        {
            if (currentZone >= 0 && currentZone < zones.Length)
            {
                transform.position = zones[currentZone].camPosition;
            }
            snapQueued = false;
            return;
        }

        if (zoneCooldownTimer > 0f)
        {
            zoneCooldownTimer -= Time.deltaTime;
        }
        else if (zones != null && zones.Length > 0)
        {
            if (currentZone < zones.Length - 1 && target.position.x >= zones[currentZone].playerThresholdX)
            {
                ApplyZone(currentZone + 1);
                zoneCooldownTimer = zoneTransitionDelay;
                return;
            }
            if (currentZone > 0 && target.position.x < zones[currentZone].playerExitThresholdX)
            {
                ApplyZone(currentZone - 1);
                zoneCooldownTimer = zoneTransitionDelay;
                return;
            }
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        if (enableBounds)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }
    }

    void ApplyZone(int index)
    {
        currentZone = index;
        var z = zones[index];
        minX = z.minX;
        maxX = z.maxX;
        minY = z.minY;
        maxY = z.maxY;
        targetZoom = z.targetZoom;
    }
}
