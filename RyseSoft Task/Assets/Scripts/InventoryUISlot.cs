// InventoryUISlot.cs (updated)
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUISlot : MonoBehaviour, IDropHandler
{
    public ItemData CurrentItem { get; private set; }
    public int Quantity { get; set; } = 0;

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image backgroundImage; // Optional: for highlighting

    private void Awake()
    {
        // Auto-assign if missing
        if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (quantityText == null) quantityText = GetComponentInChildren<TMP_Text>();
    }

    public void Setup(ItemData item, int quantity = 1)
    {
        CurrentItem = item;
        Quantity = quantity;
        UpdateVisuals();
    }

    public void Clear()
    {
        CurrentItem = null;
        Quantity = 0;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (iconImage != null)
            iconImage.sprite = CurrentItem ? CurrentItem.icon : null;

        if (quantityText != null)
            quantityText.text = (Quantity > 1) ? Quantity.ToString() : "";

        iconImage.enabled = CurrentItem != null;
        if (quantityText) quantityText.enabled = CurrentItem != null;
    }

    // Called when something is dropped on this slot
    public void OnDrop(PointerEventData eventData)
    {
        // Handled by DraggableItem.OnEndDrag → no need here
        // But we implement interface to accept drops
    }

    // Optional: Visual feedback on hover
    private void OnEnable() => AddDraggableIfMissing();
    private void AddDraggableIfMissing()
    {
        if (CurrentItem != null && GetComponent<DraggableItem>() == null)
            gameObject.AddComponent<DraggableItem>();
    }
}