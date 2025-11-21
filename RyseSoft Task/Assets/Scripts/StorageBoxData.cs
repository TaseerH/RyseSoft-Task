using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Storage Box", menuName = "Inventory/Storage Box")]
public class StorageBoxData : ScriptableObject
{
    public List<InventorySlot> slots = new List<InventorySlot>();
}