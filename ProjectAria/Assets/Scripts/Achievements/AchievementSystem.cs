// ============================================================
// AchievementSystem.cs
// Tracks player achievements. Persists to PlayerPrefs.
// Triggers via EventBus hooks.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.Achievements
{
    public enum AchievementCategory { Combat, Building, Farming, Exploration, Crafting, Social, Story, Collection }

    [CreateAssetMenu(fileName = "Achievement_", menuName = "Aria/Achievement", order = 12)]
    public class AchievementDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public AchievementCategory Category;
        public Sprite Icon;
        public int Points = 10;
        public bool Hidden; // until unlocked
    }

    public class AchievementSystem : IService
    {
        private const string PrefsKey = "ProjectAria.Achievements";
        private readonly HashSet<string> _unlocked = new();
        private readonly Dictionary<string, AchievementDefinition> _byId = new();

        public event Action<AchievementDefinition> OnUnlocked;

        public void Register(AchievementDefinition def)
        {
            if (def != null) _byId[def.Id] = def;
        }

        public void Load()
        {
            _unlocked.Clear();
            string raw = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var id in raw.Split(';'))
                if (!string.IsNullOrEmpty(id)) _unlocked.Add(id);
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join(";", _unlocked));
            PlayerPrefs.Save();
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public bool Unlock(string id)
        {
            if (_unlocked.Contains(id)) return false;
            _unlocked.Add(id);
            Save();
            if (_byId.TryGetValue(id, out var def))
            {
                OnUnlocked?.Invoke(def);
                Debug.Log($"[Achievement] Unlocked: {def.DisplayName}");
            }
            return true;
        }

        public void UnlockByEvent(ObjectiveType type)
        {
            // Stub: in real game, mapping from objective types to achievement IDs
        }
    }
}
