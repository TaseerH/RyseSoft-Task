// DraggableItem.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private CanvasGroup canvasGroup;
    private Image iconImage;
    private TMP_Text quantityText;
    private RectTransform rectTransform;

    private static DraggableItem currentlyDragging;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();
        quantityText = GetComponentInChildren<TMP_Text>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentlyDragging != null) return; // Prevent multiple drags

        var uiSlot = GetComponent<InventoryUISlot>();
        if (uiSlot == null || uiSlot.CurrentItem == null) return;

        currentlyDragging = this;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        transform.SetParent(canvas.transform, true); // Top-level during drag
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentlyDragging != this) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Try to drop on a valid slot
        var dropTarget = eventData.pointerEnter;
        var targetSlot = dropTarget != null ? dropTarget.GetComponentInParent<InventoryUISlot>() : null;

        if (targetSlot != null && targetSlot != GetComponent<InventoryUISlot>())
        {
            HandleDrop(targetSlot);
        }
        else
        {
            // Return to original position
            ReturnToOriginalPosition();
        }

        currentlyDragging = null;
    }

    private void HandleDrop(InventoryUISlot targetSlot)
    {
        var sourceSlot = GetComponent<InventoryUISlot>();

        // Case 1: Target is empty
        if (targetSlot.CurrentItem == null)
        {
            MoveItem(sourceSlot, targetSlot);
        }
        // Case 2: Same item → stack
        else if (targetSlot.CurrentItem == sourceSlot.CurrentItem)
        {
            targetSlot.Quantity += sourceSlot.Quantity;
            targetSlot.UpdateVisuals();
            sourceSlot.Clear();
            Destroy(gameObject);
        }
        // Case 3: Different item → swap
        else
        {
            SwapItems(sourceSlot, targetSlot);
        }
    }

    private void MoveItem(InventoryUISlot from, InventoryUISlot to)
    {
        to.Setup(from.CurrentItem, from.Quantity);
        from.Clear();
        Destroy(gameObject);
    }

    private void SwapItems(InventoryUISlot a, InventoryUISlot b)
    {
        var tempItem = a.CurrentItem;
        var tempQty = a.Quantity;

        a.Setup(b.CurrentItem, b.Quantity);
        b.Setup(tempItem, tempQty);

        // Move this GameObject to target's parent
        transform.SetParent(b.transform.parent);
        transform.SetSiblingIndex(b.transform.GetSiblingIndex());

        // Update visuals
        a.UpdateVisuals();
        b.UpdateVisuals();
    }

    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
    }

    public static void CancelCurrentDrag()
    {
        if (currentlyDragging != null)
        {
            currentlyDragging.ReturnToOriginalPosition();
            currentlyDragging.canvasGroup.blocksRaycasts = true;
            currentlyDragging.canvasGroup.alpha = 1f;
            currentlyDragging = null;
        }
    }
}