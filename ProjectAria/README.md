# 🎮 Project Aria

> An open-world 3D survival game for Android/iOS, blending Minecraft's building freedom with Stardew Valley's life-simulation depth.

[![Unity](https://img.shields.io/badge/Unity-2022.3_LTS-000?logo=unity)](https://unity.com/)
[![URP](https://img.shields.io/badge/Render-URP-blue)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/index.html)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-lightgrey)]()
[![C#](https://img.shields.io/badge/C%23-9.0-239120?logo=csharp)]()
[![Mobile](https://img.shields.io/badge/Target-Mobile%20First-orange)]()

[![Build Android](https://github.com/<user>/project-aria/actions/workflows/build-android.yml/badge.svg)](.github/workflows/build-android.yml)
[![Build iOS](https://github.com/<user>/project-aria/actions/workflows/build-ios.yml/badge.svg)](.github/workflows/build-ios.yml)
[![Lint](https://github.com/<user>/project-aria/actions/workflows/lint.yml/badge.svg)](.github/workflows/lint.yml)

> **Status:** 🚧 MVP Foundation (v0.1.0) — Core systems implemented, awaiting art pipeline

---

## ✨ Features

| System | Description |
|---|---|
| 🌍 **Procedural World** | Seeded infinite world, 12 biomes, day/night, weather, seasons |
| ⛏️ **Mining & Building** | Grid-snap block placement, ghost preview, structure validation |
| 🌾 **Farming** | Seasonal crops, tilling, watering, growth stages, harvest |
| 🐄 **Livestock** | Animal husbandry and breeding |
| 🎣 **Fishing** | Biome/season/weather-dependent rare fish |
| 🔨 **Crafting** | Recipe queue, 6 station tiers, skill progression |
| ⚔️ **Combat** | Melee/ranged/magic, dodge, parry, multi-phase bosses |
| 🏰 **Dungeons** | Procedural dungeons, boss arenas |
| 🧙 **RPG** | Skill tree, level, equipment upgrades |
| 🏘️ **NPCs** | Schedules, dialogue, friendship, quest givers |
| 📜 **Quests** | Main / Side / Daily / Event with objectives and rewards |
| 💰 **Economy** | Shops, currency, trading |
| 🌩️ **World Events** | Meteor, Blood Moon, Monster Invasion, Aurora |
| 👥 **Multiplayer** | 2-20 players coop, server-authoritative anti-cheat |
| 📱 **Mobile-First** | Joystick, smart action button, drag-drop inventory, customizable layout |
| ♿ **Accessibility** | Colorblind modes, large UI, reduced motion, TTS |
| 🎨 **5 Graphics Tiers** | Very Low → Ultra, auto-detected, auto-downshift on FPS drop |

---

## 🚀 Quick Start

### 1. Use this template

Click **Use this template** → **Create a new repository** on GitHub.

Or clone directly:
```bash
git clone https://github.com/<user>/project-aria.git
cd project-aria
```

### 2. Set up CI/CD (one-time)

In your GitHub repo: **Settings → Secrets and variables → Actions**:
- `UNITY_EMAIL` — your Unity ID email
- `UNITY_PASSWORD` — your Unity ID password
- `UNITY_LICENSE` — contents of `.ulf` license file
  - Windows: `%APPDATA%\Unity\Unity_lic.ulf`
  - macOS: `~/Library/Application Support/Unity/Unity_lic.ulf`
  - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

### 3. First push

```bash
./scripts/setup.sh   # initial bootstrap (optional)
git push origin main
```

→ **Actions** tab → `Build Android APK` workflow runs → download `ProjectAria-<sha>` artifact.

### 4. Local development (optional)

Requires Unity 2022.3.20f1 + Android Build Support module.

```bash
export UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity"
./BuildTools/build-android.sh
# → Builds/Android/ProjectAria.apk
```

---

## 🏗️ Architecture

Modular, event-driven, data-driven. Read **[Architecture.md](Architecture.md)** for the deep dive.

```
Bootstrap → GameManager → ServiceLocator
                          ├─ TimeSystem
                          ├─ WeatherSystem
                          ├─ SaveSystem
                          ├─ WorldManager (chunks + biomes)
                          ├─ QuestSystem
                          ├─ CraftingSystem
                          ├─ FarmSystem
                          ├─ LODManager
                          ├─ AudioManager
                          └─ NetworkGameManager
```

- **EventBus** for decoupled system communication
- **ServiceLocator** for global state (no static singletons in gameplay code)
- **ScriptableObject** for all designer-editable content
- **ObjectPool** for zero-allocation hot paths

---

## 📁 Project Structure

```
ProjectAria/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/          # GameManager, EventBus, ServiceLocator, Time, Weather, Save
│   │   ├── Player/        # Movement, Stats, Input, Inventory
│   │   ├── Controls/      # Mobile UI, joystick, smart action button
│   │   ├── World/         # Chunks, procedural gen, biomes
│   │   ├── Building/      # Grid-snap placement
│   │   ├── Farming/       # Crops, soil, seasons
│   │   ├── Inventory/     # Items, drag-drop, world pickups
│   │   ├── Crafting/      # Recipes, stations
│   │   ├── Combat/        # Attacks, enemies, bosses, projectiles
│   │   ├── NPC/           # Schedule, dialogue
│   │   ├── Quest/         # Main/Side/Daily, objectives
│   │   ├── Multiplayer/   # NGO, server authority
│   │   ├── Optimization/  # LOD, async loading, perf monitor
│   │   ├── UI/            # HUD, minimap
│   │   ├── Audio/         # Adaptive music
│   │   ├── Achievements/  # Persistent unlocks
│   │   └── Editor/        # Build automation
│   ├── ScriptableObjects/ # Designer-editable content
│   ├── Scenes/            # Bootstrap, Game, MainMenu
│   └── Resources/         # Runtime-loaded assets
├── BuildTools/            # Local build scripts (.sh + .bat)
├── .github/
│   ├── workflows/         # CI/CD pipelines
│   ├── ISSUE_TEMPLATE/    # Bug, feature, perf templates
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── CODEOWNERS
├── Packages/manifest.json # URP, NGO, Input System, Addressables
├── Architecture.md        # Pattern deep dive
├── SetupGuide.md          # Full setup walkthrough
├── PerformanceGuide.md    # Mobile optimization
├── Networking.md          # Server-authoritative design
├── ModuleMap.md           # Every file explained
├── CHANGELOG.md
└── LICENSE
```

---

## 📊 Performance Targets

| Device | FPS | Tier |
|---|---|---|
| Low-end (2GB RAM) | 30 | Very Low / Low |
| Mid-range (4GB) | 60 | Medium / High |
| High-end (6GB+) | 90-120 | Ultra |

`PerformanceMonitor` auto-downshifts quality when FPS drops below 85% of target.

See **[PerformanceGuide.md](PerformanceGuide.md)** for full optimization details.

---

## 🤝 Contributing

We welcome contributions! Please read **[Architecture.md](Architecture.md)** first to understand the patterns.

- 🐛 **Bug?** Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md)
- 💡 **Feature?** Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md)
- 📉 **Perf issue?** Use the [performance template](.github/ISSUE_TEMPLATE/performance_issue.md)
- 🔧 **PR?** Use the [PR template](.github/PULL_REQUEST_TEMPLATE.md)

---

## 📜 License

[MIT](LICENSE) — use freely, attribution appreciated.

---

## 🌟 Roadmap

### v0.1.0 (Current)
- ✅ Modular architecture
- ✅ All 14 core systems scaffolded
- ✅ CI/CD pipeline

### v0.2.0 — Art Pipeline
- [ ] Stylized low-poly block meshes
- [ ] Character model + animations
- [ ] Particle FX (mining, combat, weather)
- [ ] UI art + icons
- [ ] Sound effects + music tracks

### v0.3.0 — Gameplay Depth
- [ ] Job System / Burst chunk generation
- [ ] Greedy meshing
- [ ] Procedural dungeons
- [ ] Fishing minigame
- [ ] Tutorial system

### v0.4.0 — Polish
- [ ] Rollback netcode
- [ ] Cloud save
- [ ] Encrypted saves
- [ ] Localization (i18n)
- [ ] Steam Workshop

### v1.0.0 — Release
- [ ] All biomes + dungeons populated
- [ ] 50+ quests (main + side + daily)
- [ ] 20+ bosses
- [ ] 100+ items
- [ ] 30+ NPCs
- [ ] Soft launch

---

## 📞 Support

- 💬 **Discussions:** [GitHub Discussions](../../discussions)
- 🐛 **Issues:** [GitHub Issues](../../issues)
- 📖 **Wiki:** [GitHub Wiki](../../wiki)
- 🌐 **Website:** Coming soon

---

**Built with ❤️ for mobile. Open source. Game on.**
