using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class DialogEntry
{
    public string speaker;
    public Sprite wicakExpression;
    public Sprite aksaExpression;
    [TextArea(2, 5)] public string text;
}

public class OpeningDialogManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Panel References")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip phoneVibrateClip;

    [Header("Dialog UI")]
    [SerializeField] private CanvasGroup dialogRootGroup;
    [SerializeField] private Image wicakImage;
    [SerializeField] private Image aksaImage;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Dialog Data")]
    [SerializeField] private List<DialogEntry> dialogEntries = new List<DialogEntry>();

    [Header("Events")]
    public UnityEvent onDialogFinished;

    [Header("Animation Settings")]
    [SerializeField] private float overlayFadeDuration = 0.5f;
    [SerializeField] private float speakerScale = 1.15f;
    [SerializeField] private float listenerScale = 0.85f;
    [SerializeField, Range(0f, 1f)] private float listenerDimValue = 0.4f;

    private int currentEntryIndex = 0;
    private bool isDialogActive = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        if (dialogRootGroup != null)
        {
            dialogRootGroup.alpha = 0f;
            dialogRootGroup.interactable = false;
            dialogRootGroup.blocksRaycasts = false;
        }

        if (phoneAudioSource != null && phoneVibrateClip != null)
        {
            phoneAudioSource.loop = true;
            phoneAudioSource.clip = phoneVibrateClip;
            phoneAudioSource.Play();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDialogActive)
            StartCoroutine(StartDialogSequence());
        else
            ShowNextEntry();
    }

    private IEnumerator StartDialogSequence()
    {
        isDialogActive = true;

        if (phoneAudioSource != null)
            phoneAudioSource.Stop();

        if (dialogRootGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < overlayFadeDuration)
            {
                elapsed += Time.deltaTime;
                dialogRootGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / overlayFadeDuration);
                yield return null;
            }
            dialogRootGroup.alpha = 1f;
            dialogRootGroup.interactable = true;
            dialogRootGroup.blocksRaycasts = true;
        }

        currentEntryIndex = 0;
        ShowEntry(currentEntryIndex);
    }

    private void ShowNextEntry()
    {
        currentEntryIndex++;
        if (currentEntryIndex >= dialogEntries.Count)
        {
            onDialogFinished?.Invoke();
            return;
        }

        ShowEntry(currentEntryIndex);
    }

    private void ShowEntry(int index)
    {
        if (index < 0 || index >= dialogEntries.Count) return;

        DialogEntry entry = dialogEntries[index];

        if (dialogueText != null)
            dialogueText.text = entry.text;

        if (wicakImage != null)
            wicakImage.sprite = entry.wicakExpression;

        if (aksaImage != null)
            aksaImage.sprite = entry.aksaExpression;

        bool isWicakSpeaking = entry.speaker.ToLower() == "wicak";
        UpdateCharacterAnimation(wicakImage, isWicakSpeaking);
        UpdateCharacterAnimation(aksaImage, !isWicakSpeaking);
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
