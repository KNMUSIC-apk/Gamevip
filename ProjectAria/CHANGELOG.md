# Changelog

All notable changes to Project Aria will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Project scaffolding and architecture
- Modular system folders (Core, Player, Controls, World, Building, Farming, Inventory, Crafting, Combat, NPC, Quest, Multiplayer, Optimization, UI, Audio, Achievements)
- CI/CD pipeline via GitHub Actions (Android + iOS)
- Build automation scripts (Editor + local shell)
- Comprehensive documentation (Architecture, Setup, Performance, Networking, Module Map)

## [0.1.0] - 2026-09-02

### Added — MVP Foundation
- **Core:** GameManager, EventBus, ServiceLocator, ObjectPool, TimeSystem, WeatherSystem, SaveSystem (autosave + backup), GameSettings (5 tiers + accessibility + difficulty)
- **Player:** CharacterController movement, HP/Hunger/Stamina/Temperature, XP/Level, Inventory
- **Mobile Controls:** VirtualJoystick, SmartActionButton (context-aware), Hotbar, customizable layout, left-handed mode
- **World:** Procedural chunk streaming (16×128×16), 12 biomes (plains, forest, desert, mountain, snow, ocean, beach, swamp, volcano, cave, jungle, taiga), Perlin/FBM noise, biome sampling by climate
- **Building:** Grid-snap placement with ghost preview, validation
- **Farming:** Tilling, watering, growth stages, seasonal crops
- **Inventory:** Drag-drop UI, world pickups with magnet effect
- **Crafting:** Recipe queue, 6 station tiers
- **Combat:** Melee attacks, dodge, parry, Enemy AI (NavMesh), multi-phase Boss, projectiles
- **NPC:** Schedule by hour, dialogue tree with choices, friendship system
- **Quest:** Main/Side/Daily/Event types, objectives, rewards
- **Multiplayer:** NGO host/client (2-20 players), server-authoritative anti-cheat, chat
- **Optimization:** LOD manager, ObjectPool, Addressables async loader, auto-quality downshift
- **UI:** HUD (HP/hunger/stamina/temp), minimap with markers
- **Audio:** Adaptive music (explore/combat/boss), crossfade, ambient loops
- **Achievements:** Persisted, categorized

### Notes
- 51 C# scripts (~5,300 lines)
- 6 markdown documentation files
- Unity 2022.3 LTS, URP, NGO, Addressables, Input System
- Targets: Android 8.0+ (ARM64), iOS 13+

[Unreleased]: https://github.com/<user>/project-aria/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/<user>/project-aria/releases/tag/v0.1.0
