using UnityEngine;
using System;

public class CameraManager : MonoBehaviour
{
    [Header("Camera Prefab")]
    [SerializeField] private GameObject phoneCameraPrefab;

    private static CameraManager _instance;
    private PhoneCameraController activeCamera;

    public static CameraManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[CameraManager]");
                _instance = go.AddComponent<CameraManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public event Action<Texture2D> OnPhotoCaptured;
    public event Action OnCaptureCancelled;
    public bool IsActive => activeCamera != null && activeCamera.isActiveAndEnabled;

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

    public void Capture()
    {
        Capture(null, null);
    }

    public void Capture(Action<Texture2D> onCaptured, Action onCancelled = null)
    {
        if (IsActive)
        {
            Debug.LogWarning("Camera is already active.");
            return;
        }

        if (phoneCameraPrefab == null)
        {
            Debug.LogError("Phone Camera Prefab not assigned in CameraManager!");
            OnCaptureCancelled?.Invoke();
            onCancelled?.Invoke();
            return;
        }

        var cameraGO = Instantiate(phoneCameraPrefab, transform);
        cameraGO.name = "PhoneCameraInstance";
        activeCamera = cameraGO.GetComponent<PhoneCameraController>();

        if (activeCamera == null)
        {
            Debug.LogError("PhoneCameraController not found on prefab!");
            Destroy(cameraGO);
            return;
        }

        activeCamera.Show(
            (photo) =>
            {
                OnPhotoCaptured?.Invoke(photo);
                onCaptured?.Invoke(photo);
                Cleanup();
            },
            () =>
            {
                OnCaptureCancelled?.Invoke();
                onCancelled?.Invoke();
                Cleanup();
            }
        );
    }

    public void Hide()
    {
        if (activeCamera != null)
        {
            activeCamera.Hide();
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (activeCamera != null)
        {
            Destroy(activeCamera.gameObject);
            activeCamera = null;
        }
    }

    public void SetPrefab(GameObject prefab)
    {
        phoneCameraPrefab = prefab;
    }
}
