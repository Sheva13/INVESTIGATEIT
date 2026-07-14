using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VisionConeController : MonoBehaviour
{
    [Header("Cone Shape")]
    public float coneAngle = 60f;
    public float coneDistance = 5f;
    [Range(5, 100)]
    public int rayCount = 30;

    [Header("Rotation")]
    public float rotationSpeed = 8f;

    [Header("Detection")]
    public LayerMask obstacleMask;
    public string playerTag = "Player";

    private GuardAI guardAI;
    private LineRenderer lineRenderer;
    private float currentAngle;
    private Vector3[] coneVertices;

    void Awake()
    {
        guardAI = GetComponentInParent<GuardAI>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = rayCount + 1;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.numCapVertices = 0;

        if (lineRenderer.material == null)
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        coneVertices = new Vector3[rayCount + 1];

        currentAngle = transform.localEulerAngles.z;
    }

    void Update()
    {
        RotateCone();
        GenerateConeMesh();
        DetectPlayer();
    }

    void RotateCone()
    {
        if (guardAI == null) return;

        Vector2 moveDir = guardAI.AnimMoveDir;
        if (moveDir.sqrMagnitude < 0.01f) return;

        float targetAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    void GenerateConeMesh()
    {
        float startAngle = currentAngle - coneAngle * 0.5f;
        float angleStep = coneAngle / (rayCount - 1);

        Vector2 guardPos = transform.position;

        coneVertices[0] = Vector3.zero;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D hit = Physics2D.Raycast(guardPos, direction, coneDistance, obstacleMask);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;
            }
            else
            {
                endPoint = guardPos + direction * coneDistance;
            }

            coneVertices[i + 1] = transform.InverseTransformPoint(endPoint);
        }

        lineRenderer.positionCount = coneVertices.Length;
        lineRenderer.SetPositions(coneVertices);
    }

    void DetectPlayer()
    {
        if (guardAI == null) return;
        if (guardAI.currentState == GuardState.Chase) return;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        Vector2 guardPos = transform.position;
        Vector2 playerPos = player.transform.position;
        Vector2 dirToPlayer = playerPos - guardPos;
        float distToPlayer = dirToPlayer.magnitude;

        if (distToPlayer > coneDistance) return;

        float angleToPlayer = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, angleToPlayer));

        if (angleDiff > coneAngle * 0.5f) return;

        RaycastHit2D hit = Physics2D.Raycast(guardPos, dirToPlayer.normalized, distToPlayer, obstacleMask);
        if (hit.collider != null) return;

        guardAI.OnPlayerDetected();
    }

    void OnDrawGizmosSelected()
    {
        Vector2 guardPos = Application.isPlaying ? (Vector2)transform.position : (Vector2)transform.position;

        Gizmos.color = Color.yellow;
        float startAngle = currentAngle - coneAngle * 0.5f;
        float endAngle = currentAngle + coneAngle * 0.5f;

        Vector2 startDir = new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad));
        Vector2 endDir = new Vector2(Mathf.Cos(endAngle * Mathf.Deg2Rad), Mathf.Sin(endAngle * Mathf.Deg2Rad));

        Gizmos.DrawLine(guardPos, guardPos + startDir * coneDistance);
        Gizmos.DrawLine(guardPos, guardPos + endDir * coneDistance);

        int segments = 20;
        for (int i = 0; i < segments; i++)
        {
            float a1 = startAngle + (endAngle - startAngle) * i / segments;
            float a2 = startAngle + (endAngle - startAngle) * (i + 1) / segments;
            Vector2 p1 = guardPos + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * coneDistance;
            Vector2 p2 = guardPos + new Vector2(Mathf.Cos(a2 * Mathf.Deg2Rad), Mathf.Sin(a2 * Mathf.Deg2Rad)) * coneDistance;
            Gizmos.DrawLine(p1, p2);
        }
    }
}
