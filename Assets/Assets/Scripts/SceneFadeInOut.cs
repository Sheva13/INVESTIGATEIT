using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFadeInOut : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1f;

    private void Start()
    {
        if (fadeOverlay == null)
            fadeOverlay = GetComponentInChildren<Image>();

        if (fadeOverlay != null)
            StartCoroutine(FadeIn());

        var dialogManager = FindFirstObjectByType<OpeningDialogManager>();
        if (dialogManager != null)
            dialogManager.onDialogFinished.AddListener(() => FadeOutAndLoadScene("SampleScene"));
    }

    private IEnumerator FadeIn()
    {
        Color color = fadeOverlay.color;
        color.a = 1f;
        fadeOverlay.color = color;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            fadeOverlay.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeOverlay.color = color;
    }

    public void FadeOutAndLoadScene(string sceneName)
    {
        StartCoroutine(FadeOutRoutine(sceneName));
    }

    private IEnumerator FadeOutRoutine(string sceneName)
    {
        if (fadeOverlay == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        Color color = fadeOverlay.color;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            fadeOverlay.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeOverlay.color = color;

        SceneManager.LoadScene(sceneName);
    }
}
