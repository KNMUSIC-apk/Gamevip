// ============================================================
// CraftingSystem.cs
// Recipe-based crafting. Stations (hand/workbench/furnace/anvil).
// Queue + crafting time + level requirements.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Inventory;

namespace ProjectAria.Crafting
{
    public enum CraftingStation { Hand, Workbench, Furnace, Anvil, AlchemyTable, EnchantingTable, CookingPot }

    [CreateAssetMenu(fileName = "Recipe_", menuName = "Aria/Crafting/Recipe", order = 3)]
    public class RecipeDefinition : ScriptableObject
    {
        public int Id;
        public string DisplayName;
        public CraftingStation Station = CraftingStation.Hand;
        public int ResultItemId;
        public int ResultAmount = 1;
        public Ingredient[] Ingredients;
        public float CraftTime = 1f;
        public int RequiredLevel = 1;
        public int SkillXpAmount = 10;
        public string SkillType = "Crafting";
        [TextArea] public string Description;
    }

    [System.Serializable]
    public struct Ingredient
    {
        public int itemId;
        public int amount;
    }

    public class CraftingJob
    {
        public RecipeDefinition Recipe;
        public float Progress;
        public bool Complete;
    }

    public static class RecipeDatabase
    {
        private static readonly List<RecipeDefinition> _all = new();
        private static readonly Dictionary<int, RecipeDefinition> _byId = new();

        public static void Register(RecipeDefinition r)
        {
            if (r == null || _byId.ContainsKey(r.Id)) return;
            _all.Add(r);
            _byId[r.Id] = r;
        }
        public static RecipeDefinition Get(int id) => _byId.TryGetValue(id, out var r) ? r : null;
        public static IEnumerable<RecipeDefinition> All => _all;
        public static IEnumerable<RecipeDefinition> ForStation(CraftingStation s)
        {
            foreach (var r in _all) if (r.Station == s) yield return r;
        }
        public static void Clear() { _all.Clear(); _byId.Clear(); }
    }

    public class CraftingSystem : IService
    {
        public int MaxQueue = 4;
        public List<CraftingJob> Queue = new();
        public CraftingStation CurrentStation { get; set; } = CraftingStation.Hand;

        public event Action<RecipeDefinition> OnCrafted;
        public event Action OnQueueChanged;

        public bool CanCraft(RecipeDefinition r, PlayerInventory inv)
        {
            if (r == null || inv == null) return false;
            if (r.Station > CurrentStation) return false;
            for (int i = 0; i < r.Ingredients.Length; i++)
                if (!inv.HasItem(r.Ingredients[i].itemId, r.Ingredients[i].amount)) return false;
            return Queue.Count < MaxQueue;
        }

        public bool Enqueue(RecipeDefinition r, PlayerInventory inv)
        {
            if (!CanCraft(r, inv)) return false;
            for (int i = 0; i < r.Ingredients.Length; i++)
                inv.RemoveItem(r.Ingredients[i].itemId, r.Ingredients[i].amount);
            Queue.Add(new CraftingJob { Recipe = r, Progress = 0f });
            OnQueueChanged?.Invoke();
            return true;
        }

        public void Tick(float dt, PlayerInventory inv)
        {
            if (Queue.Count == 0) return;
            var job = Queue[0];
            job.Progress += dt;
            if (job.Progress >= job.Recipe.CraftTime)
            {
                inv.AddItem(job.Recipe.ResultItemId, job.Recipe.ResultAmount);
                Queue.RemoveAt(0);
                job.Complete = true;
                OnCrafted?.Invoke(job.Recipe);
                EventBus.Publish(new ItemCraftedEvent(job.Recipe.Id));
                OnQueueChanged?.Invoke();
            }
        }

        public float GetProgress() => Queue.Count == 0 ? 0f : Queue[0].Progress / Queue[0].Recipe.CraftTime;

        public void Cancel(int index, PlayerInventory inv)
        {
            if (index < 0 || index >= Queue.Count) return;
            var job = Queue[index];
            // Refund
            for (int i = 0; i < job.Recipe.Ingredients.Length; i++)
                inv.AddItem(job.Recipe.Ingredients[i].itemId, job.Recipe.Ingredients[i].amount);
            Queue.RemoveAt(index);
            OnQueueChanged?.Invoke();
        }
    }
}
