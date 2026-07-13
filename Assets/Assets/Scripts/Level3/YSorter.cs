using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSorter : MonoBehaviour
{
    private SpriteRenderer sr;
    public int baseSortingOrder = 10000;
    public float offset = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (sr != null)
        {
            // The Y coordinate is multiplied by -100 to sort objects from top to bottom.
            // Lower Y coordinates (closer to bottom of screen) render in front (higher sortingOrder).
            sr.sortingOrder = baseSortingOrder - Mathf.RoundToInt((transform.position.y + offset) * 100f);
        }
    }
}
