# Architecture Deep Dive

This document explains the patterns and design decisions behind Project Aria's codebase.

## 🎯 Guiding Principles

1. **Modular by system** — each major system is a folder with a clear API. You can rip out Farming and the rest keeps working.
2. **Data-driven** — designers tweak ScriptableObject assets; programmers don't touch code for balance.
3. **Loose coupling via events** — systems talk through `EventBus` (decoupled pub/sub). No `FindObjectOfType` for inter-system messages.
4. **Single source of truth** — global state lives in services registered with `ServiceLocator`. Avoid static singletons where possible.
5. **Mobile-first** — every decision considers touch input, allocation cost, and low-end hardware first.

---

## 🏛️ Pattern Catalogue

### Service Locator (`Core/ServiceLocator.cs`)
Lightweight, no DI container. Services register at boot, retrieve by interface. For larger teams swap for VContainer.

```csharp
ServiceLocator.Register<TimeSystem>(new TimeSystem());
var time = ServiceLocator.Get<TimeSystem>();
```

### Event Bus (`Core/EventBus.cs`)
Static generic pub/sub. Type-safe events are `readonly struct`s.

```csharp
EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
EventBus.Publish(new PlayerDiedEvent(playerId));
```

For high-frequency events (per-frame), use direct references — `EventBus` allocates an iterator copy per dispatch.

### Object Pool (`Core/ObjectPool.cs`)
Zero-alloc instantiation. `Spawn`/`Despawn`. Pools are pre-warmed.

### ScriptableObject Data
Every content type (items, blocks, biomes, recipes, quests, NPCs, boss patterns, world events) is a ScriptableObject. Stored in `Assets/ScriptableObjects/`.

### Service-as-Manager
Gameplay managers implement `IService` so they participate in service-locator registration. Examples: `TimeSystem`, `WeatherSystem`, `SaveSystem`, `QuestSystem`, `CraftingSystem`, `FarmSystem`, `LODManager`, `WorldEventSystem`, `WorldManager`.

### World Streaming
`WorldManager` owns chunks. Player position → chunk coord → active set → generation queue → mesh queue. Chunks unload beyond render distance.

### Server Authority
In multiplayer, every gameplay-affecting action goes through `ServerAuthority.TryApproveX()`. This is the chokepoint for anti-cheat (rate limit, distance, sanity).

---

## 🧩 Module Reference

| Module | Files | Public API |
|---|---|---|
| **Core** | GameManager, TimeSystem, WeatherSystem, SaveSystem, EventBus, ServiceLocator, ObjectPool, GameSettings, GameBootstrap | Global state + services |
| **Player** | PlayerController, PlayerStats, PlayerInput, PlayerInteraction, PlayerInventory | Player loop |
| **Controls** | VirtualJoystick, SmartActionButton, MobileButton, HotbarUI, MobileControlsUI | Touch + KB/M + gamepad |
| **World** | Chunk, ChunkMesher, WorldManager, NoiseGenerator, BlockDatabase, BiomeDatabase | Procedural world |
| **Building** | BuildingSystem | Grid-snap building |
| **Farming** | FarmSystem, FarmTile, CropDefinition | Crops, growth, seasons |
| **Inventory** | ItemDatabase, ItemSlot, WorldItem | Items + drag-drop |
| **Crafting** | CraftingSystem, RecipeDefinition | Recipe queue |
| **Combat** | CombatSystem, Enemy, BossController, Projectile | Hit detection, AI, bosses |
| **NPC** | NPCController, DialogueSystem | Schedules, dialogue |
| **Quest** | QuestSystem, QuestDefinition, QuestObjectiveDef | Quests, events |
| **Multiplayer** | NetworkGameManager, ServerAuthority | Server-authoritative net |
| **Optimization** | LODManager, AsyncAssetLoader, PerformanceMonitor | Perf + streaming |
| **UI** | HUDManager, MinimapUI, WorldEventSystem | HUD, events |
| **Audio** | AudioManager, MusicController | Adaptive music + SFX |

---

## 🔄 Data Flow

### Player Action (e.g. Mine block)
```
1. Player taps Mine button → MobileControlsUI
2. → PlayerInput.SimulateMine() (InputSystem)
3. → PlayerController.Update → PlayerInteraction.GetTargetBlock()
4. → WorldManager.RaycastBlock() (DDA voxel)
5. → WorldManager.SetBlockWorld()
6. → EventBus.Publish(BlockBrokenEvent)
7. (server) ServerAuthority.TryApproveBlockPlace() validates
8. → Chunk.SetBlock + ChunkMesher rebuilt → mesh apply on main thread
9. → PlayerInventory.AddItem(dropItemId)
10. → EventBus.Publish(ItemPickedUpEvent)
11. → HUD updates → Audio plays
```

### Quest Progress
```
1. Player kills enemy → Enemy.Die()
2. → EventBus.Publish(EntityKilledEvent)
3. → QuestSystem listens (or UI button wires UpdateObjective)
4. → QuestSystem.UpdateObjective(questId, "kill_wolves", 1)
5. → QuestRuntime.Progress[i]++
6. → EventBus.Publish(QuestObjectiveUpdatedEvent)
7. → HUD shows checkmark
8. When all done → on next NPC turn-in, QuestSystem.CompleteQuest()
9. → XP + rewards + followup quest
```

---

## 🛡️ Server-Authoritative Anti-Cheat

Every gameplay-affecting action goes through `ServerAuthority`:
- `TryApproveBlockPlace(clientId, pos, blockId)` — distance, rate limit, block id validity
- `TryApproveAttack(clientId, targetId, damage)` — distance, range, damage sanity
- `TryApproveInventoryChange(clientId, itemId, amountDelta)` — item id range, amount sanity

Client predicts (instant feedback). Server validates. Mismatch = rollback (not implemented in MVP, but the hooks are in place).

---

## 🧪 Testing Strategy

- **Unit tests** for pure logic (CraftingSystem, QuestSystem, Inventory math) — no Unity API access needed.
- **PlayMode tests** for systems that touch GameObjects (Combat, World streaming).
- **Profiler** runs at every build. PerformanceMonitor auto-downshifts on FPS drop.

---

## 🚦 Performance Budgets

| Resource | Budget |
|---|---|
| Draw calls / frame | < 100 (mobile) |
| Triangles / frame | < 500K (mobile) |
| SetPass calls / frame | < 50 |
| GC alloc / frame | 0 in hot paths |
| Chunk generation / sec | 4 chunks (2 per queue, 60 fps) |
| Memory resident | < 1.5 GB mid-range |
| Battery drain | < 5% / 30 min on mid-range |

---

## 🔌 Extension Points

- **New biome?** Create a `BiomeDefinition` asset, register in `BiomeDB`, add to `BiomeDB.SampleByClimate`.
- **New item?** Create `ItemDefinition` asset. Set `PlaceBlockId` to make it placeable.
- **New recipe?** Create `RecipeDefinition`, set `Station`, list `Ingredients`.
- **New mob?** Inherit from `Enemy`, add LootTable, drop prefab in `Resources/Prefabs/Enemies/`.
- **New boss?** Inherit from `BossController` (which inherits `Enemy`), create `BossPhase` assets, define patterns.
- **New quest?** Create `QuestDefinition` + `QuestObjectiveDef`, set triggers via `QuestSystem.NotifyEvent(type, targetId)`.
- **New world event?** Create `WorldEventDefinition`, register with `WorldEventSystem`.

---

## 📌 Known Limitations (MVP)

These are deliberately stubbed and need follow-up work:

- Async chunk generation runs on main thread (not Job System / Burst)
- No LOD on chunks themselves (same mesh all distances)
- No occlusion culling beyond frustum
- No proper rollback netcode (uses NGO default)
- No procedural dungeon generator
- No real fishing mini-game
- No NPC animation FSM (uses NavMeshAgent)
- No real save encryption (plain JSON)
- No cloud save
- No tutorial system

Each of these is a follow-up sprint in the `Polish` phase.

---

## 📐 Layering Rules

```
Presentation  →  Game Logic  →  Services  →  Data
(HUD, UI)     (Player, Combat) (Time, Save) (ScriptableObjects)
```

- UI never talks to data layer directly. UI subscribes to EventBus.
- Services can talk to each other via ServiceLocator.
- Game logic (Player) talks to services and to world.
- World doesn't know about Player (one-way dependency).
