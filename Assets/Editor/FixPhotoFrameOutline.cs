using UnityEngine;
using UnityEditor;

public class FixPhotoFrameOutline
{
    [MenuItem("Tools/Fix Photo Frame Outline")]
    public static void Run()
    {
        var worldFrame = GameObject.Find("WorldPhotoFrame");
        if (worldFrame == null) return;

        // Clean up old outline if exists
        var oldOutline = worldFrame.transform.Find("HoverOutline");
        if (oldOutline != null)
            GameObject.DestroyImmediate(oldOutline.gameObject);

        var sr = worldFrame.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        
        // Remove the SpriteOutline material, revert to Default
        sr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

        // Create Outline Child
        var outlineGo = new GameObject("HoverOutline");
        outlineGo.transform.SetParent(worldFrame.transform, false);
        outlineGo.transform.localPosition = Vector3.zero;
        
        // Scale it slightly up
        outlineGo.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
        
        var outlineSr = outlineGo.AddComponent<SpriteRenderer>();
        outlineSr.sprite = sr.sprite;
        outlineSr.color = Color.black; // Changed from yellow to black!
        outlineSr.sortingLayerID = sr.sortingLayerID;
        outlineSr.sortingOrder = sr.sortingOrder - 1; // Behind the main sprite
        
        outlineGo.SetActive(false);

        EditorUtility.SetDirty(worldFrame);
        AssetDatabase.SaveAssets();
        Debug.Log("Photo Frame Outline fixed!");
    }
}
