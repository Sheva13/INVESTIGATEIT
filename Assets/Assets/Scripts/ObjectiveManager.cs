using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "kota";

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 3f;

    [Header("Objectives")]
    [SerializeField] private bool isFramePhotoTaken;
    [SerializeField] private bool isBookPhotoTaken;
    [SerializeField] private bool isLaptopPhotoTaken;
    [SerializeField] private bool isPetaPhotoTaken;

    private static ObjectiveManager _instance;
    public static ObjectiveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[ObjectiveManager]");
                _instance = go.AddComponent<ObjectiveManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPhoto(string id)
    {
        switch (id)
        {
            case "frame":  isFramePhotoTaken = true;  break;
            case "book":   isBookPhotoTaken = true;   break;
            case "laptop": isLaptopPhotoTaken = true;  break;
            case "peta":   isPetaPhotoTaken = true;    break;
            default:
                Debug.LogWarning("Unknown photo id: " + id);
                return;
        }

        Debug.Log($"Foto {id} tercatat! Frame:{isFramePhotoTaken} Book:{isBookPhotoTaken} Laptop:{isLaptopPhotoTaken} Peta:{isPetaPhotoTaken}");

        if (isFramePhotoTaken && isBookPhotoTaken && isLaptopPhotoTaken && isPetaPhotoTaken)
        {
            Debug.Log("Semua foto terkumpul! Pindah scene...");
            StartCoroutine(FadeAndLoad());
        }
    }

    private IEnumerator FadeAndLoad()
    {
        var overlay = fadeOverlay;
        if (overlay == null)
        {
            var all = Resources.FindObjectsOfTypeAll<CanvasGroup>();
            foreach (var cg in all)
            {
                if (cg.name == "FadeOverlay" && cg.gameObject.scene.IsValid())
                {
                    overlay = cg;
                    break;
                }
            }
        }

        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.alpha = 0f;
            overlay.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                overlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            overlay.alpha = 1f;
        }

        var transition = FindAnyObjectByType<LevelToKotaTransition>();
        if (transition != null)
            transition.LoadKota();
        else
            SceneManager.LoadScene(nextSceneName);
    }
}
