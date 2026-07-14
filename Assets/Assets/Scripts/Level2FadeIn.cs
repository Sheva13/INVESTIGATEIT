using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Level2FadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        CleanupExternalOverlays();
        var overlay = GetComponentInChildren<Image>();
        if (overlay != null)
            StartCoroutine(FadeIn(overlay));
    }

    private void CleanupExternalOverlays()
    {
        var all = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c.gameObject != gameObject && c.gameObject.name.Contains("Fade"))
                Destroy(c.gameObject);
        }
    }

    private IEnumerator FadeIn(Image overlay)
    {
        var c = overlay.color;
        c.a = 1f;
        overlay.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            var col = overlay.color;
            col.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            overlay.color = col;
            yield return null;
        }

        c = overlay.color;
        c.a = 0f;
        overlay.color = c;
        Destroy(gameObject);
    }
}
