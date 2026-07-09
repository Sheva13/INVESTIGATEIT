using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class SetupPhotoFrameUI
{
    [MenuItem("Tools/Setup Photo Frame UI")]
    static void Run()
    {
        var worldFrame = GameObject.Find("WorldPhotoFrame");
        var existingCanvas = GameObject.Find("FrameCanvas");

        if (existingCanvas != null)
            GameObject.DestroyImmediate(existingCanvas);

        var frontSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Gambar/bingkaidepan.png");
        var backSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Gambar/bingkaibelakangNT.png");

        var buttonSoft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Pixel UI Grey Kit/Sprites/button_soft.png");
        var buttonClose = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Pixel UI Grey Kit/Sprites/button_close.png");
        var arrowRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Pixel UI Grey Kit/Sprites/arrow_right.png");

        var canvasGo = new GameObject("FrameCanvas");
        canvasGo.layer = 5;

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // --- PREVIEW PANEL ---
        var previewGo = new GameObject("PreviewPanel");
        previewGo.transform.SetParent(canvasGo.transform, false);
        var previewRt = previewGo.AddComponent<RectTransform>();
        previewRt.anchorMin = Vector2.zero;
        previewRt.anchorMax = Vector2.one;
        previewRt.sizeDelta = Vector2.zero;
        var previewCg = previewGo.AddComponent<CanvasGroup>();
        previewCg.alpha = 0f;
        previewGo.SetActive(false);

        // Preview Image (Left)
        var previewImgGo = new GameObject("PreviewImage");
        previewImgGo.transform.SetParent(previewGo.transform, false);
        var previewImgRt = previewImgGo.AddComponent<RectTransform>();
        previewImgRt.anchorMin = new Vector2(0.5f, 0.5f);
        previewImgRt.anchorMax = new Vector2(0.5f, 0.5f);
        previewImgRt.sizeDelta = new Vector2(480f, 480f);
        previewImgRt.anchoredPosition = new Vector2(-350f, 0f);
        var previewImg = previewImgGo.AddComponent<Image>();
        previewImg.sprite = frontSpr;
        previewImg.preserveAspect = true;

        // Title Text
        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(previewGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(800f, 120f);
        titleRt.anchoredPosition = new Vector2(400f, 120f);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "Foto Frame";
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = Color.white;

        // Description Text
        var descGo = new GameObject("DescriptionText");
        descGo.transform.SetParent(previewGo.transform, false);
        var descRt = descGo.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.5f, 0.5f);
        descRt.anchorMax = new Vector2(0.5f, 0.5f);
        descRt.sizeDelta = new Vector2(800f, 80f);
        descRt.anchoredPosition = new Vector2(400f, 20f);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.text = "Sebuah foto frame kayu. Menampilkan foto keluarga kecil yang bahagia.";
        descText.fontSize = 24;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color = Color.white;

        // Buttons
        var btnPreviewOpen = MakeBtn(previewGo, "OpenButton", "O TO OPEN", buttonSoft, new Vector2(220f, 60f));
        btnPreviewOpen.GetComponent<RectTransform>().anchoredPosition = new Vector2(110f, -120f);

        var btnPreviewEsc = MakeBtn(previewGo, "ESCButton", "ESC", buttonSoft, new Vector2(220f, 60f));
        btnPreviewEsc.GetComponent<RectTransform>().anchoredPosition = new Vector2(451f, -120f);

        // --- OPEN PANEL ---
        var openGo = new GameObject("OpenPanel");
        openGo.transform.SetParent(canvasGo.transform, false);
        var openRt = openGo.AddComponent<RectTransform>();
        openRt.anchorMin = Vector2.zero;
        openRt.anchorMax = Vector2.one;
        openRt.sizeDelta = Vector2.zero;
        var openCg = openGo.AddComponent<CanvasGroup>();
        openCg.alpha = 0f;
        openGo.SetActive(false);

        // Open Image (Center)
        var openImgGo = new GameObject("OpenImage");
        openImgGo.transform.SetParent(openGo.transform, false);
        var openImgRt = openImgGo.AddComponent<RectTransform>();
        openImgRt.anchorMin = new Vector2(0.5f, 0.5f);
        openImgRt.anchorMax = new Vector2(0.5f, 0.5f);
        openImgRt.sizeDelta = new Vector2(800f, 800f);
        openImgRt.anchoredPosition = Vector2.zero;
        var openImg = openImgGo.AddComponent<Image>();
        openImg.sprite = frontSpr;
        openImg.preserveAspect = true;

        // Next Button (Right)
        var btnNext = MakeBtn(openGo, "NextButton", null, arrowRight, new Vector2(83f, 108f));
        btnNext.GetComponent<RectTransform>().anchoredPosition = new Vector2(500f, 0f);

        // Close Button (ESC, Top Right corner of the frame)
        var btnOpenEsc = MakeBtn(openGo, "CloseButton", null, buttonClose, new Vector2(64f, 64f));
        btnOpenEsc.GetComponent<RectTransform>().anchoredPosition = new Vector2(430f, 430f);
        
        // Capture Button (Bottom Center)
        var btnCapture = MakeBtn(openGo, "CaptureButton", "KAMERA", buttonSoft, new Vector2(240f, 50f));
        btnCapture.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -440f);

        // --- WIRE UIItemInspect ---
        var inspect = canvasGo.AddComponent<UIItemInspect>();
        SerializedObject s = new SerializedObject(inspect);
        s.FindProperty("closedItemObject").objectReferenceValue = worldFrame;
        s.FindProperty("previewPanel").objectReferenceValue = previewGo;
        s.FindProperty("previewImage").objectReferenceValue = previewImg;
        s.FindProperty("previewOpenButton").objectReferenceValue = btnPreviewOpen;
        s.FindProperty("previewEscButton").objectReferenceValue = btnPreviewEsc;
        
        s.FindProperty("openPanel").objectReferenceValue = openGo;
        s.FindProperty("openImage").objectReferenceValue = openImg;
        s.FindProperty("nextButton").objectReferenceValue = btnNext;
        s.FindProperty("closeButton").objectReferenceValue = btnOpenEsc;
        s.FindProperty("captureButton").objectReferenceValue = btnCapture;
        
        s.FindProperty("frontSprite").objectReferenceValue = frontSpr;
        s.FindProperty("backSprite").objectReferenceValue = backSpr;
        s.FindProperty("transitionDuration").floatValue = 0.3f;
        s.ApplyModifiedProperties();

        if (worldFrame != null)
        {
            var trigger = worldFrame.GetComponent<UIItemInspectTrigger>();
            if (trigger != null)
            {
                SerializedObject ts = new SerializedObject(trigger);
                ts.FindProperty("itemInspect").objectReferenceValue = inspect;
                ts.ApplyModifiedProperties();
            }
        }

        EditorUtility.SetDirty(canvasGo);
        AssetDatabase.SaveAssets();
        Debug.Log("Setup Photo Frame UI complete.");
    }

    static Button MakeBtn(GameObject parent, string name, string label, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var btn = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.fontStyle = FontStyles.Bold;
        }

        return btn;
    }
}
