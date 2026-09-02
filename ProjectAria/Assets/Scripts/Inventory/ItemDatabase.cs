// ============================================================
// ItemDatabase.cs
// All item types as ScriptableObjects.
// Tools, weapons, consumables, materials, placeables, quest items.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.World;

namespace ProjectAria.Inventory
{
    public enum ItemCategory { Material, Tool, Weapon, Armor, Consumable, Placeable, Seed, Food, Quest, Currency, Furniture, Magic }

    [CreateAssetMenu(fileName = "Item_", menuName = "Aria/Inventory/Item", order = 2)]
    public class ItemDefinition : ScriptableObject
    {
        public int Id;
        public string DisplayName = "Item";
        public ItemCategory Category = ItemCategory.Material;
        public Sprite Icon;
        public GameObject WorldPrefab;
        public int MaxStack = 99;
        public int Value; // base shop price
        public bool Stackable = true;
        public float Weight = 0.1f;
        public ToolType ToolType = ToolType.None;
        public int ToolTier = 0;
        public int Damage = 0;
        public int Defense = 0;
        public float HealAmount = 0f;
        public float HungerRestored = 0f;
        public float StaminaRestored = 0f;
        public float TemperatureRestored = 0f;
        public float UseTime = 1f;
        public int PlaceBlockId = 0;
        public AudioClip UseSound;
        [TextArea] public string Description;
    }

    public static class ItemDatabase
    {
        private static readonly List<ItemDefinition> _all = new();
        private static readonly Dictionary<int, ItemDefinition> _byId = new();

        public static void Register(ItemDefinition def)
        {
            if (def == null) return;
            if (_byId.ContainsKey(def.Id)) return;
            _all.Add(def);
            _byId[def.Id] = def;
        }

        public static ItemDefinition Get(int id) => _byId.TryGetValue(id, out var d) ? d : null;
        public static int GetMaxStack(int id) => Get(id) != null ? Get(id).MaxStack : 99;
        public static IEnumerable<ItemDefinition> All => _all;
        public static void Clear() { _all.Clear(); _byId.Clear(); }
    }

    [System.Serializable]
    public struct ItemStack
    {
        public int itemId;
        public int amount;
        public int durability;
        public bool IsEmpty => itemId == 0 || amount <= 0;
        public static ItemStack Empty() => new ItemStack { itemId = 0, amount = 0, durability = 0 };
        public ItemStack(int id, int amt, int dur = 0) { itemId = id; amount = amt; durability = dur; }
        public ItemStack WithAmount(int newAmount) => new ItemStack(itemId, newAmount, durability);

        public static ItemStackSave[] ToSaveArray(ItemStack[] arr)
        {
            var result = new ItemStackSave[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = new ItemStackSave
                {
                    itemId = arr[i].itemId,
                    amount = arr[i].amount,
                    durability = arr[i].durability
                };
            }
            return result;
        }

        public static ItemStack[] FromSaveArray(ItemStackSave[] arr)
        {
            var result = new ItemStack[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = new ItemStack(arr[i].itemId, arr[i].amount, arr[i].durability);
            }
            return result;
        }
    }
}
