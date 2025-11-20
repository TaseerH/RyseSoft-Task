using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUIManager : MonoBehaviour
{
    [SerializeField] private PlayerInventoryUI playerInventory;
    [SerializeField] private StorageBoxUI storageBox;

    private List<GameObject> createdSlots = new List<GameObject>();

    public void ShowWorkbench(
        List<InventorySlot> playerSlots,
        List<InventorySlot> storageSlots,
        GameObject playerSlotPrefab,
        GameObject storageSlotPrefab)
    {
        ClearAllSlots();

        playerInventory.inventoryPanel.SetActive(true);
        storageBox.storagePanel.SetActive(true);

        PopulateSlots(playerInventory.itemSlotParent, playerSlots, playerSlotPrefab);
        PopulateSlots(storageBox.itemSlotParent, storageSlots, storageSlotPrefab);
    }

    public void HideWorkbench()
    {
        playerInventory.inventoryPanel.SetActive(false);
        storageBox.storagePanel.SetActive(false);
        ClearAllSlots();
    }

    private void PopulateSlots(Transform parent, List<InventorySlot> slots, GameObject slotPrefab)
    {
        foreach (var slot in slots)
        {
            if (slot.item == null) continue;

            GameObject slotObj = Instantiate(slotPrefab, parent);
            createdSlots.Add(slotObj);

            var uiSlot = slotObj.GetComponent<InventoryUISlot>();
            if (uiSlot != null)
            {
                uiSlot.Setup(slot.item, slot.quantity);
            }
            else
            {
                // Fallback: simple setup with Image + Text
                var img = slotObj.GetComponentInChildren<Image>();
                var text = slotObj.GetComponentInChildren<Text>();
                if (img) img.sprite = slot.item.icon;
                if (text) text.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
            }
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in createdSlots)
        {
            if (slot != null) Destroy(slot);
        }
        createdSlots.Clear();
    }

    // Called when closing to save drag-and-drop changes
    public void SaveCurrentWorkbenchData(
        ref List<InventorySlot> playerInventoryData,
        ref List<InventorySlot> storageBoxData)
    {
        // Use the UI parents, NOT the data lists
        playerInventoryData = ReadSlotsFromUI(playerInventory.itemSlotParent);
        storageBoxData      = ReadSlotsFromUI(storageBox.itemSlotParent);
    }

    private List<InventorySlot> ReadSlotsFromUI(Transform parent)
    {
        var slots = new List<InventorySlot>();

        foreach (Transform child in parent)
        {
            var uiSlot = child.GetComponent<InventoryUISlot>();
            if (uiSlot != null && uiSlot.CurrentItem != null)
            {
                slots.Add(new InventorySlot(uiSlot.CurrentItem, uiSlot.Quantity));
            }
        }

        return slots;
    }
}

[System.Serializable]
public class PlayerInventoryUI
{
    public GameObject inventoryPanel;
    public Transform itemSlotParent;
}

[System.Serializable]
public class StorageBoxUI
{
    public GameObject storagePanel;
    public Transform itemSlotParent;
}