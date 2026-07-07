using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class ConnectPhoneReferences
{
    [MenuItem("Tools/Connect Phone References")]
    public static void Connect()
    {
        var bc = GameObject.Find("BookCanvas");
        if (bc == null) { Debug.LogError("BookCanvas not found"); return; }

        var uibf = bc.GetComponent<UIBookFlip>();
        if (uibf == null) { Debug.LogError("UIBookFlip not found"); return; }

        var so = new SerializedObject(uibf);

        var phoneGO = bc.transform.Find("PhoneCameraOverlay");
        if (phoneGO == null) { Debug.LogError("PhoneCameraOverlay not found"); return; }

        var capBtn = phoneGO.Find("PhoneFrame/PhoneCaptureBtn")?.GetComponent<Button>();
        var closeBtn = phoneGO.Find("PhoneFrame/PhoneCloseBtn")?.GetComponent<Button>();

        so.FindProperty("phoneCameraOverlay").objectReferenceValue = phoneGO.gameObject;
        so.FindProperty("phoneCaptureBtn").objectReferenceValue = capBtn;
        so.FindProperty("phoneCloseBtn").objectReferenceValue = closeBtn;
        so.ApplyModifiedProperties();

        Debug.Log($"Connected: phoneCameraOverlay={phoneGO.name}, phoneCaptureBtn={(capBtn != null ? "OK" : "NULL")}, phoneCloseBtn={(closeBtn != null ? "OK" : "NULL")}");
    }
}
