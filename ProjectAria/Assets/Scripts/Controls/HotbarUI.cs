// ============================================================
// HotbarUI.cs
// 8-slot hotbar with item icons, swap, scroll, keybinds 1-8.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ProjectAria.Core;
using ProjectAria.Inventory;
using ProjectAria.Player;

namespace ProjectAria.Controls
{
    public class HotbarUI : MonoBehaviour
    {
        public ItemSlot[] Slots;
        public int CurrentIndex;
        public PlayerInventory PlayerInv;

        public Image SelectionIndicator;

        private void Start()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                int idx = i;
                Slots[i].OnClicked += (slotIdx) => SelectSlot(idx);
                Slots[i].SlotIndex = i;
            }
            Refresh();
        }

        private void Update()
        {
            if (PlayerInv == null) return;
            // Number keys 1-8
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < 8 && i < Slots.Length; i++)
                {
                    if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) SelectSlot(i);
                }
            }
            // Mouse wheel
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel > 0.1f) SelectSlot((CurrentIndex + 1) % Slots.Length);
                else if (wheel < -0.1f) SelectSlot((CurrentIndex - 1 + Slots.Length) % Slots.Length);
            }
            // Touch swipe up/down on hotbar
            if (Touchscreen.current != null)
            {
                var t = Touchscreen.current.primaryTouch;
                if (t.press.wasPressedThisFrame)
                {
                    Vector2 pos = t.position.ReadValue();
                    if (pos.y < Screen.height * 0.2f)
                    {
                        // Swipe gesture
                    }
                }
            }
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= Slots.Length) return;
            CurrentIndex = index;
            if (PlayerInv != null) PlayerInv.SelectHotbar(index);
            if (SelectionIndicator != null && index < Slots.Length)
            {
                SelectionIndicator.transform.position = Slots[index].transform.position;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (PlayerInv == null) return;
            for (int i = 0; i < Slots.Length && i < PlayerInv.Hotbar.Length; i++)
            {
                Slots[i].SetStack(PlayerInv.Hotbar[i]);
            }
        }
    }
}
