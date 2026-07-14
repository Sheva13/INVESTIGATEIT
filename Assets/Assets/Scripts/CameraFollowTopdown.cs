using UnityEngine;

public class CameraFollowTopdown : MonoBehaviour
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

    void LateUpdate()
    {
        if (!target) return;

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
}
