using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

public class OpeningDialogManager : MonoBehaviour
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

    [Header("Camera Transition")]
    [SerializeField] private bool useCameraTransition = true;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Graphic blackOverlay;
    [SerializeField] private float cameraFadeDuration = 0.5f;
    [SerializeField] private Vector3 hallwayCameraPosition = new Vector3(35.7f, -28.5f, -10f);
    [SerializeField] private Vector3 kelasCameraPosition = new Vector3(18.41f, 0.97f, -10);

    [Header("Scene Transition")]
    [SerializeField] private LevelToKotaTransition levelToKotaTransition;

    private int currentEntryIndex = 0;
    private bool isDialogActive = false;
    private bool hasTransitioned = false;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null && useCameraTransition)
            mainCamera.transform.position = hallwayCameraPosition;

        if (blackOverlay != null)
        {
            if (useCameraTransition)
            {
                var fc = blackOverlay.transform.parent;
                if (fc != null)
                    DontDestroyOnLoad(fc.gameObject);
            }

            var c = blackOverlay.color;
            c.a = 0f;
            blackOverlay.color = c;
            blackOverlay.raycastTarget = useCameraTransition;
        }

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

    private void Update()
    {
        if (!hasTransitioned && Input.GetMouseButtonDown(0))
            StartCoroutine(CameraTransitionThenDialog());
        else if (isDialogActive && dialogRootGroup != null && dialogRootGroup.blocksRaycasts && Input.GetMouseButtonDown(0))
            ShowNextEntry();
    }

    private IEnumerator CameraTransitionThenDialog()
    {
        hasTransitioned = true;

        if (phoneAudioSource != null)
            phoneAudioSource.Stop();

        if (useCameraTransition)
        {
            if (blackOverlay != null)
            {
                float elapsed = 0f;
                while (elapsed < cameraFadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Lerp(0f, 1f, elapsed / cameraFadeDuration);
                    var c = blackOverlay.color;
                    c.a = t;
                    blackOverlay.color = c;
                    yield return null;
                }
                var c2 = blackOverlay.color;
                c2.a = 1f;
                blackOverlay.color = c2;
            }

            if (mainCamera != null)
                mainCamera.transform.position = kelasCameraPosition;

            yield return new WaitForSeconds(0.1f);

            if (blackOverlay != null)
            {
                float elapsed = 0f;
                while (elapsed < cameraFadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Lerp(1f, 0f, elapsed / cameraFadeDuration);
                    var c = blackOverlay.color;
                    c.a = t;
                    blackOverlay.color = c;
                    yield return null;
                }
                var c2 = blackOverlay.color;
                c2.a = 0f;
                blackOverlay.color = c2;
                blackOverlay.raycastTarget = false;
            }
        }

        isDialogActive = true;
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
            isDialogActive = false;
            StartCoroutine(FadeAndLoadScene());
            return;
        }

        ShowEntry(currentEntryIndex);
    }

    private IEnumerator FadeAndLoadScene()
    {
        if (blackOverlay != null)
        {
            blackOverlay.raycastTarget = true;
            float elapsed = 0f;
            while (elapsed < cameraFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Lerp(0f, 1f, elapsed / cameraFadeDuration);
                var c = blackOverlay.color;
                c.a = t;
                blackOverlay.color = c;
                yield return null;
            }
            var c2 = blackOverlay.color;
            c2.a = 1f;
            blackOverlay.color = c2;
        }

        yield return new WaitForSeconds(0.2f);

        if (levelToKotaTransition != null)
            levelToKotaTransition.LoadKota();
        else
            SceneManager.LoadScene("SampleScene");
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

public class SceneFadeHandler : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.5f;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var rawImg = GetComponentInChildren<UnityEngine.UI.RawImage>();
        if (rawImg == null) return;

        var c = rawImg.color;
        c.a = 1f;
        rawImg.color = c;
        rawImg.raycastTarget = true;

        StartCoroutine(FadeIn(rawImg));
    }

    private System.Collections.IEnumerator FadeIn(UnityEngine.UI.RawImage rawImg)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            var c = rawImg.color;
            c.a = t;
            rawImg.color = c;
            yield return null;
        }
        var c2 = rawImg.color;
        c2.a = 0f;
        rawImg.color = c2;
        rawImg.raycastTarget = false;
    }
}
