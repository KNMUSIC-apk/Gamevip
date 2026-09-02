# Module Map — every script, what it does, when to touch it

## Core (`Assets/Scripts/Core/`)

| File | Purpose | Touch when... |
|---|---|---|
| **GameManager.cs** | Root singleton, owns main loop tick | Changing core game flow |
| **GameBootstrap.cs** | Initial scene, registers services, seeds default content | Adding new system at boot |
| **EventBus.cs** | Global pub/sub for cross-system events | Adding a new event type |
| **ServiceLocator.cs** | Service registry | Registering a new global service |
| **ObjectPool.cs** | GameObject pooling | Adding a new pooled type |
| **TimeSystem.cs** | Day/night, season, calendar | Changing time/season rules |
| **WeatherSystem.cs** | Weather state machine | Adding weather types |
| **SaveSystem.cs** | JSON save/load, autosave, backup | Changing save format |
| **GameSettings.cs** | Persistent settings + difficulty | Adding a setting |

## Player (`Assets/Scripts/Player/`)

| File | Purpose |
|---|---|
| **PlayerController.cs** | Movement, camera, look, basic actions |
| **PlayerStats.cs** | HP, hunger, stamina, temperature, XP, level |
| **PlayerInput.cs** | Bridges Input System actions to data |
| **PlayerInteraction.cs** | Raycast for interactable targets |
| **PlayerInventory.cs** | Hotbar + main inventory, save hooks |

## Controls (`Assets/Scripts/Controls/`)

| File | Purpose |
|---|---|
| **VirtualJoystick.cs** | Touch joystick, customizable |
| **SmartActionButton.cs** | Context-aware action button |
| **MobileButton.cs** | Generic touch button |
| **HotbarUI.cs** | 8-slot hotbar with scroll/keybind |
| **MobileControlsUI.cs** | Top-level control orchestrator |

## World (`Assets/Scripts/World/`)

| File | Purpose |
|---|---|
| **Chunk.cs** | 16×128×16 voxel storage + mesh holder |
| **ChunkMesher.cs** | Builds Chunk.MeshData from blocks (culls hidden faces) |
| **WorldManager.cs** | Owns active chunks, streaming, gen queue, raycast |
| **NoiseGenerator.cs** | Perlin/FBM noise, seeded RNG |
| **BlockDatabase.cs** | Block definitions (ScriptableObject) |
| **BiomeDatabase.cs** | Biome definitions + climate sampling |
| **WorldEventSystem.cs** | Random world events (meteor, blood moon, etc.) |

## Building (`Assets/Scripts/Building/`)

| File | Purpose |
|---|---|
| **BuildingSystem.cs** | Grid-snap placement, preview ghost, validation |

## Farming (`Assets/Scripts/Farming/`)

| File | Purpose |
|---|---|
| **FarmSystem.cs** | Tiles, crops, growth, watering |
| **CropDefinition.cs** | ScriptableObject for crop data |

## Inventory (`Assets/Scripts/Inventory/`)

| File | Purpose |
|---|---|
| **ItemDatabase.cs** | Item ScriptableObject definitions |
| **ItemSlot.cs** | UI slot with touch drag-drop |
| **WorldItem.cs** | Dropped item in world, magnet pickup |

## Crafting (`Assets/Scripts/Crafting/`)

| File | Purpose |
|---|---|
| **CraftingSystem.cs** | Recipe queue, station tiers, progress |

## Combat (`Assets/Scripts/Combat/`)

| File | Purpose |
|---|---|
| **CombatSystem.cs** | Player attacks, dodge, parry, magic |
| **Enemy.cs** | Basic hostile AI (NavMesh) |
| **BossController.cs** | Multi-phase boss with patterns |
| **Projectile.cs** | Homing/straight projectile |

## NPC (`Assets/Scripts/NPC/`)

| File | Purpose |
|---|---|
| **NPCController.cs** | Schedule, personality, friendship |
| **DialogueSystem.cs** | Branching dialogue trees |

## Quest (`Assets/Scripts/Quest/`)

| File | Purpose |
|---|---|
| **QuestSystem.cs** | Quest runtime, objectives, rewards |
| **QuestDefinition.cs** | ScriptableObject for quest data |
| **QuestObjectiveDef.cs** | ScriptableObject for objectives |

## Multiplayer (`Assets/Scripts/Multiplayer/`)

| File | Purpose |
|---|---|
| **NetworkManager.cs** | NGO host/client, connection lifecycle |
| **ServerAuthority.cs** | Anti-cheat chokepoint |

## Optimization (`Assets/Scripts/Optimization/`)

| File | Purpose |
|---|---|
| **LODManager.cs** | Distance-based LOD updates |
| **AsyncAssetLoader.cs** | Addressables wrapper |
| **PerformanceMonitor.cs** | FPS / draw call tracking, auto-quality |

## UI (`Assets/Scripts/UI/`)

| File | Purpose |
|---|---|
| **HUDManager.cs** | HP/hunger/stamina/temp/time HUD |
| **MinimapUI.cs** | Top-down minimap with markers |

## Audio (`Assets/Scripts/Audio/`)

| File | Purpose |
|---|---|
| **AudioManager.cs** | Adaptive music, ambient, SFX |
| **MusicController.cs** | Listens to game state, switches music |
