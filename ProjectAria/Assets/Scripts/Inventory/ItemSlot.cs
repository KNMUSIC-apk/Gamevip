// ============================================================
// ItemSlot.cs
// UI slot for inventory/hotbar with touch drag-and-drop support.
// ============================================================
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectAria.Core;
using ProjectAria.Inventory;

namespace ProjectAria.UI
{
    public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Image Icon;
        public Text AmountText;
        public Image DurabilityBar;
        public int SlotIndex;
        public bool IsHotbar;

        public ItemStack Stack { get; private set; }
        public System.Action<int> OnClicked;
        public System.Action<int, ItemSlot> OnDroppedOn;
        public System.Action<int, ItemSlot> OnDraggedFrom;

        public void SetStack(ItemStack stack)
        {
            Stack = stack;
            if (Icon != null)
            {
                Icon.enabled = !stack.IsEmpty;
                if (!stack.IsEmpty)
                {
                    var def = ItemDatabase.Get(stack.itemId);
                    if (def != null && def.Icon != null) Icon.sprite = def.Icon;
                }
            }
            if (AmountText != null) AmountText.text = stack.amount > 1 ? stack.amount.ToString() : "";
            if (DurabilityBar != null)
            {
                var def = ItemDatabase.Get(stack.itemId);
                DurabilityBar.gameObject.SetActive(!stack.IsEmpty && def != null && (def.Category == ItemCategory.Tool || def.Category == ItemCategory.Weapon || def.Category == ItemCategory.Armor) && def.MaxStack > 0 && stack.durability > 0);
                if (DurabilityBar.gameObject.activeSelf)
                    DurabilityBar.fillAmount = stack.durability / (float)def.MaxStack;
            }
        }

        public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(SlotIndex);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Stack.IsEmpty) return;
            ItemDragHandler.DraggedFrom = this;
            OnDraggedFrom?.Invoke(SlotIndex, this);
        }

        public void OnDrag(PointerEventData eventData) { /* ghost handled by handler */ }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ItemDragHandler.DraggedFrom == null) return;
            if (ItemDragHandler.DraggedFrom == this) { ItemDragHandler.DraggedFrom = null; return; }
            OnDroppedOn?.Invoke(SlotIndex, ItemDragHandler.DraggedFrom);
            ItemDragHandler.DraggedFrom = null;
        }
    }

    public static class ItemDragHandler
    {
        public static ItemSlot DraggedFrom;
    }
}
