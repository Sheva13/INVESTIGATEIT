using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "kota";

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 3f;

    private string[] requiredPhotos = { "frame", "book", "laptop" };
    private HashSet<string> registeredPhotos = new HashSet<string>();

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

    public void SetRequiredPhotos(string[] photos)
    {
        requiredPhotos = photos;
        registeredPhotos.Clear();
        Debug.Log($"ObjectiveManager: Required photos set to [{string.Join(", ", photos)}]");
    }

    public void ResetObjectives()
    {
        registeredPhotos.Clear();
        Debug.Log("ObjectiveManager: Objectives reset.");
    }

    public void RegisterPhoto(string id)
    {
        registeredPhotos.Add(id);

        int count = registeredPhotos.Count;
        int total = requiredPhotos.Length;
        Debug.Log($"Foto '{id}' tercatat! ({count}/{total})");

        bool allDone = true;
        foreach (string required in requiredPhotos)
        {
            if (!registeredPhotos.Contains(required))
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
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
