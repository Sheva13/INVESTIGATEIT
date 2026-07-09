using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

public static class RebuildPhoneCamera
{
    private const string PrefabPath = "Assets/Prefabs/PhoneCamera.prefab";

    [MenuItem("Tools/Rebuild Phone Camera")]
    public static void Rebuild()
    {
        // ===== BUILD HIERARCHY =====
        var root = new GameObject("PhoneCamera_Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.transform.position = Vector3.zero;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Assets/Gambar/kamera-hp.png");

        // -- Dark BG --
        var bgGO = new GameObject("DarkBackground", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(root.transform, false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;
        bgGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        // -- PhoneFrame --
        var frameGO = new GameObject("PhoneFrame", typeof(RectTransform), typeof(Image));
        frameGO.transform.SetParent(root.transform, false);
        var frameRect = frameGO.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f); frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f); frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(700, 520);
        var frameImg = frameGO.GetComponent<Image>();
        frameImg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        var outline = frameGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.7f, 1f, 0.6f);
        outline.effectDistance = new Vector2(2, -2);

        // -- Viewfinder --
        var viewGO = new GameObject("ViewfinderArea", typeof(RectTransform), typeof(Image));
        viewGO.transform.SetParent(frameGO.transform, false);
        var viewRect = viewGO.GetComponent<RectTransform>();
        viewRect.anchorMin = new Vector2(0.5f, 0.5f); viewRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewRect.pivot = new Vector2(0.5f, 0.5f); viewRect.anchoredPosition = new Vector2(0f, 20f);
        viewRect.sizeDelta = new Vector2(620, 390);
        viewGO.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1f);
        SetTextureOverlay(viewGO.transform, tex);

        // -- TopBar --
        var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(frameGO.transform, false);
        topBar.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
        topBar.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        topBar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
        topBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 45);
        topBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        var titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(topBar.transform, false);
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero; titleRect.anchorMax = Vector2.one; titleRect.sizeDelta = Vector2.zero;
        var titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "KAMERA PENYELIDIKAN";
        titleTmp.fontSize = 20; titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.8f, 0.9f, 1f); titleTmp.fontStyle = FontStyles.Bold;

        // -- BottomBar --
        var botBar = new GameObject("BottomBar", typeof(RectTransform));
        botBar.transform.SetParent(frameGO.transform, false);
        var botRect = botBar.GetComponent<RectTransform>();
        botRect.anchorMin = new Vector2(0, 0); botRect.anchorMax = new Vector2(1, 0);
        botRect.pivot = new Vector2(0.5f, 0); botRect.sizeDelta = new Vector2(0, 55);

        var galBtn = MakeButton(botBar.transform, "GalleryBtn", 0.5f, 0, 0.5f, 80, 40, new Color(0.3f, 0.3f, 0.35f), "GALERI", 16, new Color(0.7f, 0.8f, 1f));
        var capBtn = MakeButton(botBar.transform, "PhoneCaptureBtn", 0.5f, 0.5f, 0.5f, 160, 44, new Color(0.85f, 0.2f, 0.2f), "CAPTURE", 20, Color.white);
        var closeBtn = MakeButton(botBar.transform, "PhoneCloseBtn", 0.5f, 1f, 0.5f, 80, 40, new Color(0.35f, 0.35f, 0.4f), "X", 24, Color.white);

        // -- PhotoResultPanel --
        var resultGO = new GameObject("PhotoResultPanel", typeof(RectTransform), typeof(CanvasGroup));
        resultGO.transform.SetParent(frameGO.transform, false);
        var resultRect = resultGO.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero; resultRect.anchorMax = Vector2.one; resultRect.sizeDelta = Vector2.zero;

        var resCg = resultGO.GetComponent<CanvasGroup>();
        resCg.alpha = 0f; resCg.interactable = false; resCg.blocksRaycasts = false;

        var resBg = resultGO.AddComponent<Image>();
        resBg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

        var capImgGO = new GameObject("CapturedImage", typeof(RectTransform), typeof(RawImage));
        capImgGO.transform.SetParent(resultGO.transform, false);
        var capImgRect = capImgGO.GetComponent<RectTransform>();
        capImgRect.anchorMin = new Vector2(0.5f, 0.5f); capImgRect.anchorMax = new Vector2(0.5f, 0.5f);
        capImgRect.pivot = new Vector2(0.5f, 0.5f); capImgRect.anchoredPosition = new Vector2(0f, 20f);
        capImgRect.sizeDelta = new Vector2(620, 390);

        var saveBtn = MakeButton(resultGO.transform, "SaveBtn", 0.5f, 0.3f, 0f, 140, 44, new Color(0.2f, 0.7f, 0.3f), "SIMPAN", 20, Color.white);
        var retakeBtn = MakeButton(resultGO.transform, "RetakeBtn", 0.5f, 0.7f, 0f, 140, 44, new Color(0.6f, 0.6f, 0.6f), "ULANGI", 20, Color.white);

        // ===== ADD PhoneCameraController =====
        var controller = root.AddComponent<PhoneCameraController>();
        var so = new SerializedObject(controller);
        so.FindProperty("phoneCanvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
        so.FindProperty("photoResultPanel").objectReferenceValue = resultGO;
        so.FindProperty("phoneCaptureBtn").objectReferenceValue = capBtn.GetComponent<Button>();
        so.FindProperty("phoneCloseBtn").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.FindProperty("galleryBtn").objectReferenceValue = galBtn.GetComponent<Button>();
        so.FindProperty("saveBtn").objectReferenceValue = saveBtn.GetComponent<Button>();
        so.FindProperty("retakeBtn").objectReferenceValue = retakeBtn.GetComponent<Button>();
        so.FindProperty("capturedImage").objectReferenceValue = capImgGO.GetComponent<RawImage>();
        so.FindProperty("viewfinderArea").objectReferenceValue = viewRect;
        so.ApplyModifiedProperties();

        // ===== SAVE AS PREFAB =====
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log("Prefab saved: " + PrefabPath);

        // ===== ASSIGN TO CameraManager =====
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        SetupCameraManager(prefab);

        // ===== CLEANUP SCENE OBJECT =====
        GameObject.DestroyImmediate(root);

        Debug.Log("=== Phone Camera Rebuild Complete ===");
        Debug.Log("Prefab: " + PrefabPath);
        Debug.Log("All references connected to PhoneCameraController.");
    }

    private static void SetupCameraManager(GameObject prefab)
    {
        var mgr = Object.FindObjectOfType<CameraManager>();
        if (mgr == null)
        {
            var mgrGO = new GameObject("[CameraManager]");
            mgr = mgrGO.AddComponent<CameraManager>();
        }
        var so = new SerializedObject(mgr);
        so.FindProperty("phoneCameraPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);
        Debug.Log("CameraManager assigned with prefab.");
    }

    private static void SetTextureOverlay(Transform parent, Texture2D tex)
    {
        var texGO = new GameObject("TextureOverlay", typeof(RectTransform), typeof(RawImage));
        texGO.transform.SetParent(parent, false);
        var texRect = texGO.GetComponent<RectTransform>();
        texRect.anchorMin = Vector2.zero; texRect.anchorMax = Vector2.one; texRect.sizeDelta = Vector2.zero;
        var raw = texGO.GetComponent<RawImage>();
        raw.texture = tex;
        raw.color = new Color(1, 1, 1, 0.2f);
    }

    private static GameObject MakeButton(Transform parent, string name, float pivotX, float anchorX, float anchorY, float w, float h, Color color, string text, float fontSize, Color textColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(pivotX, 0.5f);
        rect.anchorMin = new Vector2(anchorX, 0.5f); rect.anchorMax = new Vector2(anchorX, 0.5f);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = color;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        Color hColor = color * 1.2f; hColor.a = 1f;
        Color pColor = color * 0.8f; pColor.a = 1f;
        colors.highlightedColor = hColor;
        colors.pressedColor = pColor;
        btn.colors = colors;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        textGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        textGO.GetComponent<RectTransform>().anchorMax = Vector2.one;
        textGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor; tmp.fontStyle = FontStyles.Bold;

        return go;
    }
}
