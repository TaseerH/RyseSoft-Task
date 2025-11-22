// FirstRunInitializer.cs
using UnityEngine;
using System.Collections.Generic;

public class FirstRunInitializer : MonoBehaviour
{
    [Header("Default Items Setup")]
    public List<ItemData> defaultStorageItems = new List<ItemData>();
    public List<ItemData> defaultInventoryItems = new List<ItemData>();
    
    [Header("Quantity Settings")]
    public int defaultStorageQuantity = 1;
    public int defaultInventoryQuantity = 1;
    
    private void Start()
    {
        // Wait a frame to ensure other systems are initialized
        Invoke("InitializeFirstRun", 0.1f);
    }
    
    private void InitializeFirstRun()
    {
        // Check if this is the first run by looking at storage box
        if (IsFirstRun())
        {
            Debug.Log("First run detected! Setting up default items...");
            AddDefaultItems();
        }
    }
    
    private bool IsFirstRun()
    {
        // Check if storage box is empty (first run)
        if (GameManager.Instance.storageBoxData.slots.Count == 0 && 
            GameManager.Instance.playerInventoryData.slots.Count == 0)
        {
            return true;
        }
        
        return false;
    }
    
    private void AddDefaultItems()
    {
        // Add items to storage box
        foreach (ItemData item in defaultStorageItems)
        {
            if (item != null)
            {
                GameManager.Instance.storageBoxData.slots.Add(new InventorySlot(item, defaultStorageQuantity));
                Debug.Log($"Added {item.itemName} to storage box");
            }
        }
        
        // Add items to player inventory
        foreach (ItemData item in defaultInventoryItems)
        {
            if (item != null)
            {
                GameManager.Instance.playerInventoryData.slots.Add(new InventorySlot(item, defaultInventoryQuantity));
                Debug.Log($"Added {item.itemName} to player inventory");
            }
        }
        
        // Save the initial state
        RuntimeItemManager.Instance.SaveCurrentGameState();
        
        Debug.Log($"First run setup complete! Added {defaultStorageItems.Count} items to storage and {defaultInventoryItems.Count} to inventory");
    }
}