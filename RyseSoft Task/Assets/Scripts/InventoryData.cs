using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory/Player Inventory")]
public class InventoryData : ScriptableObject
{
    public List<InventorySlot> slots = new List<InventorySlot>();
}

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity = 0;

    public InventorySlot(ItemData item = null, int quantity = 0)
    {
        this.item = item;
        this.quantity = quantity;
    }
}