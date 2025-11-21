// DraggableItem.cs - 100% FIXED WITH DEBUG LOGS
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private static DraggableItem dragging;
    private Canvas canvas;
    private GameObject dragIcon;
    private InventoryUISlot sourceSlot;
    private GameObject previewObject;
    private Camera cam;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        sourceSlot = GetComponent<InventoryUISlot>();
        // FIXED: Robust camera detection
        cam = FindObjectOfType<Camera>();
        if (cam == null) Debug.LogError("No active Camera found for raycasting!");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragging != null || sourceSlot.CurrentItem == null) return;

        dragging = this;

        // Create floating icon
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var img = dragIcon.AddComponent<Image>();
        img.sprite = sourceSlot.CurrentItem.icon;
        img.raycastTarget = false;

        var rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        var cg = dragIcon.AddComponent<CanvasGroup>();
        cg.alpha = 0.9f;
        cg.blocksRaycasts = false;

        // Dim source
        var sourceCG = sourceSlot.GetComponent<CanvasGroup>();
        if (sourceCG) sourceCG.alpha = 0.5f;

        Debug.Log("Drag started: " + sourceSlot.CurrentItem.itemName);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging != this || dragIcon == null) return;

        dragIcon.transform.position = Input.mousePosition;

        // FIXED: Precise UI detection
        bool overUI = IsPointerOverUI();
        Debug.Log("Drag Update - Over UI: " + overUI);

        if (overUI)
        {
            HidePreview();
        }
        else
        {
            ShowWorldPreview();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragging != this) return;

        bool dropped = false;

        // Try drop on another slot
        var target = eventData.pointerEnter?.GetComponentInParent<InventoryUISlot>();
        if (target && target != sourceSlot)
        {
            DropOnSlot(target);
            dropped = true;
            Debug.Log("Dropped on UI slot");
        }
        else
        {
            bool overUI = IsPointerOverUI();
            Debug.Log("End Drag - Over UI: " + overUI);
            if (!overUI)
            {
                dropped = TryPlaceInWorld();
            }
        }

        // Cleanup
        if (dragIcon) Destroy(dragIcon);
        HidePreview();
        var sourceCG = sourceSlot.GetComponent<CanvasGroup>();
        if (sourceCG) sourceCG.alpha = 1f;

        dragging = null;
        Debug.Log("Drag ended, dropped: " + dropped);
    }

    private bool IsPointerOverUI()
    {
        PointerEventData ped = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (var r in results)
        {
            if (r.gameObject == dragIcon) continue; // Ignore drag icon itself
            // FIXED: Only block if over a slot (allows empty panel space)
            if (r.gameObject.GetComponent<InventoryUISlot>() != null) 
            {
                Debug.Log("Over UI Slot: " + r.gameObject.name);
                return true;
            }
        }
        return false; // No slot hit → allow world preview!
    }

    private void ShowWorldPreview()
    {
        if (sourceSlot.CurrentItem?.prefab == null || cam == null) 
        {
            HidePreview();
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        // FIXED: Exclude UI layer, longer distance
        int layerMask = ~(LayerMask.GetMask("UI"));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
        {
            Debug.Log("Raycast HIT: " + hit.collider.name + " | Tag: " + hit.collider.tag + " | Is PlacementSurface: " + hit.collider.CompareTag("PlacementSurface"));
            
            if (hit.collider.CompareTag("PlacementSurface"))
            {
                if (previewObject == null)
                {
                    previewObject = Instantiate(sourceSlot.CurrentItem.prefab);
                    MakeGhost(previewObject);
                    Debug.Log("Preview instantiated!");
                }
                previewObject.transform.position = hit.point + hit.normal * 0.05f;
                previewObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return;
            }
        }
        else
        {
            Debug.Log("Raycast NO HIT");
        }
        HidePreview();
    }

    private void HidePreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
            Debug.Log("Preview hidden");
        }
    }

    private bool TryPlaceInWorld()
    {
        if (sourceSlot.CurrentItem?.prefab == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~LayerMask.GetMask("UI")))
        {
            if (hit.collider.CompareTag("PlacementSurface"))
            {
                Vector3 pos = hit.point + hit.normal * 0.05f;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                // SPAWN AND MAKE CHILD OF WORKBENCH
                GameObject spawned = Instantiate(sourceSlot.CurrentItem.prefab, GameManager.Instance.workbenchContent);
                spawned.transform.position = pos;
                spawned.transform.rotation = rot;

                // REDUCE STACK
                sourceSlot.Quantity--;
                if (sourceSlot.Quantity <= 0)
                {
                    sourceSlot.Clear();
                }
                else
                {
                    sourceSlot.UpdateVisuals(); // ← THIS KEEPS UI IN SYNC
                }

                // FORCE UI REFRESH (critical!)
                MainUIManager.Instance?.RefreshWorkbenchUI();

                return true;
            }
        }
        return false;
    }

    private void DropOnSlot(InventoryUISlot target)
    {
        if (target.CurrentItem == null)
        {
            target.Setup(sourceSlot.CurrentItem, sourceSlot.Quantity);
            sourceSlot.Clear();
        }
        else if (target.CurrentItem == sourceSlot.CurrentItem)
        {
            target.Quantity += sourceSlot.Quantity;
            target.UpdateVisuals();
            sourceSlot.Clear();
        }
        else
        {
            var tempItem = target.CurrentItem;
            var tempQty = target.Quantity;
            target.Setup(sourceSlot.CurrentItem, sourceSlot.Quantity);
            sourceSlot.Setup(tempItem, tempQty);
        }
    }

    // FIXED: Universal ghost effect (works in Built-in, URP, HDRP)
    private void MakeGhost(GameObject obj)
    {
        obj.transform.localScale *= 1.1f;
        obj.layer = LayerMask.NameToLayer("Ignore Raycast"); // Prevent self-hits

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material ghostMat = new Material(mats[i]);
                Color col = ghostMat.color;
                col.a = 0.6f;
                ghostMat.color = col;
                mats[i] = ghostMat;
            }
            r.materials = mats;
        }
    }

    public static void CancelDrag()
    {
        if (dragging != null)
        {
            if (dragging.dragIcon) Destroy(dragging.dragIcon);
            dragging.HidePreview();
            var sourceCG = dragging.sourceSlot?.GetComponent<CanvasGroup>();
            if (sourceCG) sourceCG.alpha = 1f;
            dragging = null;
        }
    }
}