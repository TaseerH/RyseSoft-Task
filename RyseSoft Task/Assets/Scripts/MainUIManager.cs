// MainUIManager.cs (UPDATED for fixed slots)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private PlayerInventoryUI playerInventory;
    [SerializeField] private StorageBoxUI storageBox;

    [Header("Slot Counts")]
    [SerializeField] private int playerSlotCount = 5;
    [SerializeField] private int storageSlotCount = 10;

    private List<GameObject> createdSlots = new List<GameObject>();

    public static MainUIManager Instance; // ← ADD THIS

    private void Awake()
    {
        Instance = this;
    }
    
    public void ShowWorkbench(
        List<InventorySlot> playerSlots,
        List<InventorySlot> storageSlots,
        GameObject playerSlotPrefab,
        GameObject storageSlotPrefab, bool openUI = true)
    {
        ClearAllSlots();

        playerInventory.inventoryPanel.SetActive(openUI);
        storageBox.storagePanel.SetActive(openUI);

        // Ensure drop zones
        AddDropZone(playerInventory.itemSlotParent.gameObject);
        AddDropZone(storageBox.itemSlotParent.gameObject);

        PopulateSlots(playerInventory.itemSlotParent, playerSlots, playerSlotPrefab, playerSlotCount);
        PopulateSlots(storageBox.itemSlotParent, storageSlots, storageSlotPrefab, storageSlotCount);
    }

    public void HideWorkbench()
    {
        playerInventory.inventoryPanel.SetActive(false);
        storageBox.storagePanel.SetActive(false);
        ClearAllSlots();
    }
    
    public void RefreshWorkbenchUI(bool openUI = true)
    {
        // Re-read current data and update all slots
        ShowWorkbench(
            GameManager.Instance.playerInventoryData.slots,
            GameManager.Instance.storageBoxData.slots,
            GameManager.Instance.inventorySlotPrefab,
            GameManager.Instance.storageSlotPrefab, openUI
        );
    }

    private void PopulateSlots(Transform parent, List<InventorySlot> slotsData, GameObject slotPrefab, int totalSlots)
    {
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, parent);
            createdSlots.Add(slotObj);

            InventoryUISlot uiSlot = slotObj.GetComponent<InventoryUISlot>();
            if (uiSlot == null)
            {
                Debug.LogError("Slot prefab missing InventoryUISlot!");
                continue;
            }

            ItemData item = null;
            int qty = 0;
            if (i < slotsData.Count)
            {
                item = slotsData[i].item;
                qty = slotsData[i].quantity;
            }
            uiSlot.Setup(item, qty);
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

    public void SaveCurrentWorkbenchData(
        ref List<InventorySlot> playerInventoryData,
        ref List<InventorySlot> storageBoxData)
    {
        playerInventoryData = ReadSlotsFromUI(playerInventory.itemSlotParent, playerSlotCount);
        storageBoxData = ReadSlotsFromUI(storageBox.itemSlotParent, storageSlotCount);

        GameManager.Instance.playerInventoryData.slots = playerInventoryData;
        GameManager.Instance.storageBoxData.slots = storageBoxData;
    
        // Save to persistent storage
        RuntimeItemManager.Instance?.SaveCurrentGameState();
    }

    private List<InventorySlot> ReadSlotsFromUI(Transform parent, int totalSlots)
    {
        List<InventorySlot> slots = new List<InventorySlot>();

        foreach (Transform child in parent)
        {
            InventoryUISlot uiSlot = child.GetComponent<InventoryUISlot>();
            if (uiSlot != null && !uiSlot.IsEmpty)
            {
                // ONLY SAVE NON-EMPTY SLOTS
                slots.Add(new InventorySlot(uiSlot.CurrentItem, uiSlot.Quantity));
            }
        }

        return slots; // No padding! Only real items!
    }

    private void AddDropZone(GameObject obj)
    {
        if (obj.GetComponent<DropZone>() == null)
        {
            obj.AddComponent<DropZone>();
        }
    }

    // Inner class for drop zones
    public class DropZone : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData) { /* Fallback */ }
    }
}

// PlayerInventoryUI and StorageBoxUI unchanged
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