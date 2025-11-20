using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory/Player Inventory")]
public class InventoryData : ScriptableObject
{
    public List<InventorySlot> slots = new List<InventorySlot>();
}

[CreateAssetMenu(fileName = "New Storage Box", menuName = "Inventory/Storage Box")]
public class StorageBoxData : ScriptableObject
{
    public List<InventorySlot> slots = new List<InventorySlot>();
}

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity = 1;

    public InventorySlot(ItemData item, int quantity = 1)
    {
        this.item = item;
        this.quantity = quantity;
    }
}