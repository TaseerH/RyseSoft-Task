using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUISlot : MonoBehaviour
{
    public ItemData CurrentItem { get; private set; }
    public int Quantity { get; private set; } = 1;

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;

    public void Setup(ItemData item, int quantity = 1)
    {
        CurrentItem = item;
        Quantity = quantity;

        if (iconImage) iconImage.sprite = item.icon;
        if (quantityText) quantityText.text = quantity > 1 ? quantity.ToString() : "";
    }

    public void Clear()
    {
        CurrentItem = null;
        Quantity = 0;
        if (iconImage) iconImage.sprite = null;
        if (quantityText) quantityText.text = "";
    }
}