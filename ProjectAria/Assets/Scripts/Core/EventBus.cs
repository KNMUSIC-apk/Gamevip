// ============================================================
// EventBus.cs
// Global event bus for decoupled inter-system communication.
// Systems publish events; others subscribe. Zero coupling.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAria.Core
{
    /// <summary>
    /// Static event bus. Lightweight pub/sub. Use for cross-system signals
    /// (e.g. "PlayerDied", "ItemPickedUp", "BlockPlaced").
    /// For high-frequency events (per-frame), use direct references instead.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private static readonly Dictionary<Type, List<Delegate>> _onceSubscribers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>(8);
                _subscribers[type] = list;
            }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
            if (_onceSubscribers.TryGetValue(typeof(T), out var onceList))
                onceList.Remove(handler);
        }

        public static void SubscribeOnce<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_onceSubscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>(4);
                _onceSubscribers[type] = list;
            }
            list.Add(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
            {
                // Iterate copy in case handlers unsubscribe during dispatch.
                var copy = list.ToArray();
                for (int i = 0; i < copy.Length; i++)
                {
                    try { ((Action<T>)copy[i]).Invoke(evt); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
            if (_onceSubscribers.TryGetValue(typeof(T), out var onceList))
            {
                var copy = onceList.ToArray();
                onceList.Clear();
                for (int i = 0; i < copy.Length; i++)
                {
                    try { ((Action<T>)copy[i]).Invoke(evt); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
        }

        public static void Clear()
        {
            _subscribers.Clear();
            _onceSubscribers.Clear();
        }
    }

    // ============================================================
    // Core event payloads — declare all events as readonly structs.
    // ============================================================

    public readonly struct GameInitializedEvent { }
    public readonly struct GamePausedEvent { public readonly bool Paused; public GamePausedEvent(bool p) { Paused = p; } }
    public readonly struct GameQuittedEvent { }

    public readonly struct PlayerSpawnedEvent { public readonly int PlayerId; public PlayerSpawnedEvent(int id) { PlayerId = id; } }
    public readonly struct PlayerDiedEvent { public readonly int PlayerId; public PlayerDiedEvent(int id) { PlayerId = id; } }
    public readonly struct PlayerRespawnedEvent { public readonly int PlayerId; public PlayerRespawnedEvent(int id) { PlayerId = id; } }
    public readonly struct PlayerStatsChangedEvent
    {
        public readonly int PlayerId; public readonly float Hp, Hunger, Stamina, Temperature;
        public PlayerStatsChangedEvent(int id, float hp, float h, float s, float t) { PlayerId = id; Hp = hp; Hunger = h; Stamina = s; Temperature = t; }
    }

    public readonly struct TimeOfDayChangedEvent { public readonly float DayTime01; public readonly int Day; public TimeOfDayChangedEvent(float t, int d) { DayTime01 = t; Day = d; } }
    public readonly struct WeatherChangedEvent { public readonly WeatherType Weather; public WeatherChangedEvent(WeatherType w) { Weather = w; } }

    public readonly struct BlockPlacedEvent { public readonly Vector3Int Pos; public readonly int BlockId; public BlockPlacedEvent(Vector3Int p, int id) { Pos = p; BlockId = id; } }
    public readonly struct BlockBrokenEvent { public readonly Vector3Int Pos; public readonly int BlockId; public BlockBrokenEvent(Vector3Int p, int id) { Pos = p; BlockId = id; } }
    public readonly struct ChunkLoadedEvent { public readonly Vector2Int Coord; public ChunkLoadedEvent(Vector2Int c) { Coord = c; } }
    public readonly struct ChunkUnloadedEvent { public readonly Vector2Int Coord; public ChunkUnloadedEvent(Vector2Int c) { Coord = c; } }

    public readonly struct ItemPickedUpEvent { public readonly int ItemId; public readonly int Amount; public ItemPickedUpEvent(int id, int a) { ItemId = id; Amount = a; } }
    public readonly struct ItemCraftedEvent { public readonly int RecipeId; public ItemCraftedEvent(int id) { RecipeId = id; } }
    public readonly struct InventoryChangedEvent { public readonly int PlayerId; public InventoryChangedEvent(int id) { PlayerId = id; } }

    public readonly struct EntityDamagedEvent { public readonly int EntityId; public readonly float Amount; public readonly Vector3 HitPoint; public EntityDamagedEvent(int id, float a, Vector3 hp) { EntityId = id; Amount = a; HitPoint = hp; } }
    public readonly struct EntityKilledEvent { public readonly int EntityId; public readonly int KillerId; public EntityKilledEvent(int id, int k) { EntityId = id; KillerId = k; } }
    public readonly struct BossPhaseChangedEvent { public readonly int BossId; public readonly int Phase; public BossPhaseChangedEvent(int id, int p) { BossId = id; Phase = p; } }

    public readonly struct QuestStartedEvent { public readonly string QuestId; public QuestStartedEvent(string id) { QuestId = id; } }
    public readonly struct QuestCompletedEvent { public readonly string QuestId; public QuestCompletedEvent(string id) { QuestId = id; } }
    public readonly struct QuestObjectiveUpdatedEvent { public readonly string QuestId; public readonly string ObjectiveId; public readonly int Progress; public QuestObjectiveUpdatedEvent(string q, string o, int p) { QuestId = q; ObjectiveId = o; Progress = p; } }

    public readonly struct DialogueStartedEvent { public readonly int NpcId; public DialogueStartedEvent(int id) { NpcId = id; } }
    public readonly struct DialogueEndedEvent { public readonly int NpcId; public DialogueEndedEvent(int id) { NpcId = id; } }

    public readonly struct WorldEventStartedEvent { public readonly string EventId; public WorldEventStartedEvent(string id) { EventId = id; } }
    public readonly struct WorldEventEndedEvent { public readonly string EventId; public WorldEventEndedEvent(string id) { EventId = id; } }

    public readonly struct SaveCompletedEvent { public readonly bool Success; public SaveCompletedEvent(bool s) { Success = s; } }
    public readonly struct LoadCompletedEvent { public readonly bool Success; public LoadCompletedEvent(bool s) { Success = s; } }
}
