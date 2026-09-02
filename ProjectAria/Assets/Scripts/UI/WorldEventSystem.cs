// ============================================================
// WorldEventSystem.cs
// Random world events: Meteor, Blood Moon, Monster Invasion, etc.
// Each event has a duration, effects, and reward modifiers.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.World
{
    public enum WorldEventType { None, MeteorShower, BloodMoon, MonsterInvasion, GoldenHour, MerchantCaravan, BossRush, Aurora }

    [CreateAssetMenu(fileName = "Event_", menuName = "Aria/World/Event", order = 11)]
    public class WorldEventDefinition : ScriptableObject
    {
        public string Id;
        public WorldEventType Type;
        public string DisplayName;
        [TextArea] public string Description;
        public float MinInterval = 300f;  // min seconds between
        public float Duration = 120f;
        public float SpawnChancePerCheck = 0.05f;
        public int DifficultyTier = 1;
        public AudioClip MusicOverride;
        public Color SkyTint = Color.white;
        public int[] ExtraMobIds;
        public int XpMultiplier = 1;
        public int LootMultiplier = 1;
    }

    public class WorldEventSystem : IService
    {
        public WorldEventDefinition Current { get; private set; }
        public float EventTimeRemaining { get; private set; }

        public event System.Action<WorldEventDefinition> OnEventStarted;
        public event System.Action<WorldEventDefinition> OnEventEnded;

        private readonly List<WorldEventDefinition> _all = new();
        private float _nextCheck;
        private float _cooldown;
        private readonly System.Random _rng = new();

        public void Register(WorldEventDefinition def) { if (def != null && !_all.Contains(def)) _all.Add(def); }
        public IEnumerable<WorldEventDefinition> All => _all;

        public void Tick(float dt)
        {
            _cooldown -= dt;
            if (Current == null)
            {
                _nextCheck -= dt;
                if (_nextCheck <= 0f)
                {
                    TryStartEvent();
                    _nextCheck = 60f; // check every minute
                }
            }
            else
            {
                EventTimeRemaining -= dt;
                if (EventTimeRemaining <= 0f) EndEvent();
            }
        }

        private void TryStartEvent()
        {
            if (_cooldown > 0f) return;
            if (_all.Count == 0) return;
            foreach (var e in _all)
            {
                if (_rng.NextDouble() < e.SpawnChancePerCheck)
                {
                    StartEvent(e);
                    return;
                }
            }
        }

        public void StartEvent(WorldEventDefinition def)
        {
            Current = def;
            EventTimeRemaining = def.Duration;
            _cooldown = def.MinInterval;
            OnEventStarted?.Invoke(def);
            EventBus.Publish(new WorldEventStartedEvent(def.Id));
        }

        public void EndEvent()
        {
            if (Current == null) return;
            var prev = Current;
            Current = null;
            OnEventEnded?.Invoke(prev);
            EventBus.Publish(new WorldEventEndedEvent(prev.Id));
        }
    }
}
