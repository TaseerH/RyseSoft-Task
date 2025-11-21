// InventoryUISlot.cs (UPDATED)
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUISlot : MonoBehaviour
{
    public ItemData CurrentItem { get; set; }
    public int Quantity { get; set; } = 0;

    public bool IsEmpty => CurrentItem == null || Quantity <= 0;

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image backgroundImage;

    // Add this at the top
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (iconImage == null) iconImage = GetComponentInChildren<Image>();
        if (quantityText == null) quantityText = GetComponentInChildren<TMP_Text>();
    }

    public void Setup(ItemData item, int quantity = 0)
    {
        CurrentItem = item;
        Quantity = quantity;
        UpdateVisuals();
    }

    public void Clear()
    {
        Setup(null, 0);
    }

    public void UpdateVisuals()
    {
        if (iconImage != null)
        {
            iconImage.sprite = CurrentItem?.icon;
            iconImage.enabled = !IsEmpty;
        }

        if (quantityText != null)
        {
            quantityText.text = Quantity > 1 ? Quantity.ToString() : "";
            quantityText.enabled = !IsEmpty && Quantity > 1;
        }
    }

    public void SetDragState(bool dragging)
    {
        canvasGroup.alpha = dragging ? 0.5f : 1f;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Interface required, handled by DraggableItem
    }
}