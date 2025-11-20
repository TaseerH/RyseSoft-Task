using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public GameObject prefab;     // World object
    public Mesh mesh;             // For 3D preview in UI (optional)
    public Sprite icon;           // 2D icon in inventory
    [TextArea] public string description;
}