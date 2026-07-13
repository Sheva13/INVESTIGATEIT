using UnityEngine;

[System.Serializable]
public class GalleryItemData
{
    public string title;
    [TextArea(3, 5)] public string description;
    public Sprite thumbnail;
    public Sprite fullImage;
}
