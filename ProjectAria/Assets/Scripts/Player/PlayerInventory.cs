// ============================================================
// PlayerInventory.cs
// Hotbar + main inventory. Hooks into Save/Load.
// ============================================================
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Inventory;
using ProjectAria.Building;

namespace ProjectAria.Player
{
    [RequireComponent(typeof(PlayerInteraction))]
    public class PlayerInventory : MonoBehaviour
    {
        public int HotbarSize = 8;
        public int MainSize = 24;
        public ItemStack[] Hotbar;
        public ItemStack[] Main;

        public int SelectedHotbarIndex { get; private set; }
        public ItemStack SelectedItem => Hotbar[SelectedHotbarIndex];
        public BuildingSystem Building;

        private void Awake()
        {
            Hotbar = new ItemStack[HotbarSize];
            Main = new ItemStack[MainSize];
            for (int i = 0; i < HotbarSize; i++) Hotbar[i] = ItemStack.Empty();
            for (int i = 0; i < MainSize; i++) Main[i] = ItemStack.Empty();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<InventoryChangedEvent>(OnInvChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInvChanged);
        }

        private void OnInvChanged(InventoryChangedEvent e) { /* UI listens via ServiceLocator */ }

        public void SelectHotbar(int index)
        {
            if (index < 0 || index >= HotbarSize) return;
            SelectedHotbarIndex = index;
            if (Building != null) Building.SetHeldItem(Hotbar[index]);
        }

        public bool AddItem(int itemId, int amount = 1)
        {
            // Stack to existing first
            for (int i = 0; i < MainSize; i++)
                if (Main[i].itemId == itemId)
                {
                    int space = ItemDatabase.GetMaxStack(itemId) - Main[i].amount;
                    if (space <= 0) continue;
                    int add = Mathf.Min(space, amount);
                    Main[i] = Main[i].WithAmount(Main[i].amount + add);
                    amount -= add;
                    if (amount <= 0) break;
                }
            for (int i = 0; i < MainSize && amount > 0; i++)
            {
                if (Main[i].IsEmpty)
                {
                    int add = Mathf.Min(ItemDatabase.GetMaxStack(itemId), amount);
                    Main[i] = new ItemStack(itemId, add);
                    amount -= add;
                }
            }
            for (int i = 0; i < HotbarSize && amount > 0; i++)
            {
                if (Hotbar[i].IsEmpty)
                {
                    int add = Mathf.Min(ItemDatabase.GetMaxStack(itemId), amount);
                    Hotbar[i] = new ItemStack(itemId, add);
                    amount -= add;
                }
            }
            EventBus.Publish(new InventoryChangedEvent(GetComponent<PlayerStats>().PlayerId));
            return amount <= 0;
        }

        public bool RemoveItem(int itemId, int amount = 1)
        {
            for (int i = 0; i < HotbarSize && amount > 0; i++)
                if (Hotbar[i].itemId == itemId)
                {
                    int take = Mathf.Min(Hotbar[i].amount, amount);
                    Hotbar[i] = Hotbar[i].WithAmount(Hotbar[i].amount - take);
                    if (Hotbar[i].amount <= 0) Hotbar[i] = ItemStack.Empty();
                    amount -= take;
                }
            for (int i = 0; i < MainSize && amount > 0; i++)
                if (Main[i].itemId == itemId)
                {
                    int take = Mathf.Min(Main[i].amount, amount);
                    Main[i] = Main[i].WithAmount(Main[i].amount - take);
                    if (Main[i].amount <= 0) Main[i] = ItemStack.Empty();
                    amount -= take;
                }
            EventBus.Publish(new InventoryChangedEvent(GetComponent<PlayerStats>().PlayerId));
            return amount <= 0;
        }

        public bool HasItem(int itemId, int amount = 1)
        {
            int count = 0;
            for (int i = 0; i < HotbarSize; i++) if (Hotbar[i].itemId == itemId) count += Hotbar[i].amount;
            for (int i = 0; i < MainSize; i++) if (Main[i].itemId == itemId) count += Main[i].amount;
            return count >= amount;
        }

        public int CountItem(int itemId)
        {
            int count = 0;
            for (int i = 0; i < HotbarSize; i++) if (Hotbar[i].itemId == itemId) count += Hotbar[i].amount;
            for (int i = 0; i < MainSize; i++) if (Main[i].itemId == itemId) count += Main[i].amount;
            return count;
        }

        public void GatherSave(System.Collections.Generic.List<PlayerSave> target)
        {
            var stats = GetComponent<PlayerStats>();
            var save = stats.ToSave();
            save.inventory = new InventorySaveData
            {
                hotbar = ItemStack.ToSaveArray(Hotbar),
                main = ItemStack.ToSaveArray(Main)
            };
            target.Add(save);
        }
    }
}
