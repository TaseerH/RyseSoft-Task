using System;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GameObject playerObject;
    public CinemachineVirtualCamera workbenchCamera;
    public MainUIManager uiManager;

    public BoxCollider WorkbenchTrigger;
    
    public GameObject InteractionCanvas;

    public Transform workbenchContent; // Parent of all placed items
    
    [Header("Data (Assign in Inspector)")]
    public InventoryData playerInventoryData;
    public StorageBoxData storageBoxData;

    [Header("UI Prefabs")]
    public GameObject inventorySlotPrefab;
    public GameObject storageSlotPrefab;

    private bool interactionActive = false;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure camera starts disabled
        if (workbenchCamera) workbenchCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (interactionActive)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                OpenWorkbench();
                InteractionCanvas.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWorkbench();
                InteractionCanvas.SetActive(true);
            }
        }
    }

    public void ShowInteractionPrompt()
    {
        InteractionCanvas.SetActive(true);
        interactionActive = true;
    }

    public void HideInteractionPrompt()
    {
        InteractionCanvas.SetActive(false);
        interactionActive = false;
    }

    public void OpenWorkbench()
    {
        if (playerObject == null || uiManager == null) return;

        // Disable player
        playerObject.SetActive(false);
        WorkbenchTrigger.enabled = (false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Enable workbench camera
        if (workbenchCamera)
            workbenchCamera.gameObject.SetActive(true);

        // Show UI and populate
        uiManager.ShowWorkbench(
            playerInventoryData.slots,
            storageBoxData.slots,
            inventorySlotPrefab,
            storageSlotPrefab
        );
    }

    public void CloseWorkbench()
    {
        DraggableItem.CancelDrag();
        
        if (playerObject == null || uiManager == null) return;

        WorkbenchTrigger.enabled = (true);
        
        // Save current UI state back to ScriptableObjects
        uiManager.SaveCurrentWorkbenchData(
            ref playerInventoryData.slots,
            ref storageBoxData.slots
        );

        // Hide UI and clear slots
        uiManager.HideWorkbench();

        // Clean up any leftover items on the workbench
        foreach (Transform child in workbenchContent)
        {
            if (child.gameObject.activeInHierarchy)
                Destroy(child.gameObject);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable player
        playerObject.SetActive(true);

        // Disable workbench camera
        if (workbenchCamera)
            workbenchCamera.gameObject.SetActive(false);
    }
}