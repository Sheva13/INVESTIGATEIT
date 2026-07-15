using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class EndingDialogEntry
{
    public string speaker;
    public Sprite ketosExpression;
    public Sprite aksaExpression;
    [TextArea(2, 5)] public string text;
}

public class EndingDialogManager : MonoBehaviour
{
    [Header("Dialog UI")]
    [SerializeField] private CanvasGroup dialogRootGroup;
    [SerializeField] private Image ketosImage;
    [SerializeField] private Image aksaImage;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Panel Background")]
    [SerializeField] private SpriteRenderer panelDialogRenderer;

    [Header("Fade Screen")]
    [SerializeField] private Graphic fadeOverlay;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Background Music")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private float bgmFadeInDuration = 2.0f;
    [SerializeField] private float bgmFadeOutDuration = 2.0f;
    [SerializeField, Range(0f, 1f)] private float bgmTargetVolume = 0.5f;

    [Header("Dialog Data")]
    [SerializeField] private List<EndingDialogEntry> dialogEntries = new List<EndingDialogEntry>();

    [Header("Events")]
    public UnityEvent onDialogFinished;

    [Header("Animation Settings")]
    [SerializeField] private float overlayFadeDuration = 0.5f;
    [SerializeField] private float speakerScale = 1.15f;
    [SerializeField] private float listenerScale = 0.85f;
    [SerializeField, Range(0f, 1f)] private float listenerDimValue = 0.4f;

    [Header("Fade Settings")]
    [SerializeField] private float endFadeDuration = 1.0f;

    private int currentEntryIndex = 0;
    private bool isDialogActive = false;
    private bool hasStarted = false;

    private void Start()
    {
        if (dialogRootGroup != null)
        {
            dialogRootGroup.alpha = 0f;
            dialogRootGroup.interactable = false;
            dialogRootGroup.blocksRaycasts = false;
        }

        if (panelDialogRenderer != null)
        {
            Color c = panelDialogRenderer.color;
            c.a = 1f;
            panelDialogRenderer.color = c;
        }

        if (fadeOverlay != null)
        {
            var c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            fadeOverlay.raycastTarget = true;
        }

        if (bgmSource != null && bgmSource.clip != null)
        {
            bgmSource.volume = 0f;
            bgmSource.Play();
        }

        StartCoroutine(BlackFadeIn());
    }

    private IEnumerator BlackFadeIn()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            var c = fadeOverlay.color;
            c.a = 1f - t;
            fadeOverlay.color = c;
            yield return null;
        }

        var c2 = fadeOverlay.color;
        c2.a = 0f;
        fadeOverlay.color = c2;
        fadeOverlay.raycastTarget = false;

        if (bgmSource != null && bgmSource.clip != null)
        {
            elapsed = 0f;
            while (elapsed < bgmFadeInDuration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Clamp01(elapsed / bgmFadeInDuration) * bgmTargetVolume;
                yield return null;
            }
            bgmSource.volume = bgmTargetVolume;
        }
    }

    private void Update()
    {
        if (!hasStarted && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(StartDialog());
        }
        else if (isDialogActive && dialogRootGroup != null && dialogRootGroup.blocksRaycasts && Input.GetMouseButtonDown(0))
        {
            ShowNextEntry();
        }
    }

    private IEnumerator StartDialog()
    {
        hasStarted = true;

        if (dialogRootGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < overlayFadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogRootGroup.alpha = Mathf.Clamp01(elapsed / overlayFadeDuration);
                yield return null;
            }
            dialogRootGroup.alpha = 1f;
            dialogRootGroup.interactable = true;
            dialogRootGroup.blocksRaycasts = true;
        }

        isDialogActive = true;
        currentEntryIndex = 0;
        ShowEntry(currentEntryIndex);
    }

    private void ShowNextEntry()
    {
        currentEntryIndex++;
        if (currentEntryIndex >= dialogEntries.Count)
        {
            isDialogActive = false;
            StartCoroutine(FadeAndEnd());
            return;
        }

        ShowEntry(currentEntryIndex);
    }

    private IEnumerator FadeAndEnd()
    {
        if (dialogRootGroup != null)
        {
            dialogRootGroup.interactable = false;
            dialogRootGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < endFadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogRootGroup.alpha = 1f - Mathf.Clamp01(elapsed / endFadeDuration);
                yield return null;
            }
            dialogRootGroup.alpha = 0f;
        }

        yield return StartCoroutine(BlackFadeOut());

        onDialogFinished?.Invoke();
    }

    private IEnumerator BlackFadeOut()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            float elapsedBgm = 0f;
            float startVol = bgmSource.volume;
            while (elapsedBgm < bgmFadeOutDuration)
            {
                elapsedBgm += Time.deltaTime;
                bgmSource.volume = startVol * (1f - Mathf.Clamp01(elapsedBgm / bgmFadeOutDuration));
                yield return null;
            }
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.raycastTarget = true;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
                var c = fadeOverlay.color;
                c.a = t;
                fadeOverlay.color = c;
                yield return null;
            }
            var c2 = fadeOverlay.color;
            c2.a = 1f;
            fadeOverlay.color = c2;
        }
    }

    private void ShowEntry(int index)
    {
        if (index < 0 || index >= dialogEntries.Count) return;

        EndingDialogEntry entry = dialogEntries[index];

        if (dialogueText != null)
            dialogueText.text = entry.text;

        if (ketosImage != null)
            ketosImage.sprite = entry.ketosExpression;

        if (aksaImage != null)
            aksaImage.sprite = entry.aksaExpression;

        bool isKetosSpeaking = entry.speaker.ToLower() == "ketos";
        UpdateCharacterAnimation(ketosImage, isKetosSpeaking);
        UpdateCharacterAnimation(aksaImage, !isKetosSpeaking);
    }

    private void UpdateCharacterAnimation(Image characterImage, bool isSpeaking)
    {
        if (characterImage == null) return;

        float targetScale = isSpeaking ? speakerScale : listenerScale;
        Color targetColor = isSpeaking ? Color.white : new Color(listenerDimValue, listenerDimValue, listenerDimValue, 1f);

        characterImage.transform.localScale = Vector3.one * targetScale;
        characterImage.color = targetColor;
    }
}
