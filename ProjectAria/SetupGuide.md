# Project Aria — Setup Guide

This guide walks you through opening, configuring, and running Project Aria in Unity.

---

## 1. Prerequisites

- **Unity 2022.3 LTS** (or newer 2022.3.x)
- **Android Build Support** module (for Android target)
- **iOS Build Support** module (for iOS target)
- ~5 GB free disk for the project + Library
- Git (optional)

## 2. Open the Project

1. Launch **Unity Hub**
2. Click **Open** → navigate to `/workspace/ProjectAria`
3. Wait for first-time import (3-10 min depending on internet speed — package download)
4. Switch platform:
   - **File → Build Settings → Android** → **Switch Platform**
   - (or iOS, same flow)

## 3. Install Required Packages

If Unity didn't auto-install everything from `Packages/manifest.json`, manually install:

- `com.unity.inputsystem` (new Input System)
- `com.unity.netcode.gameobjects` (multiplayer)
- `com.unity.addressables` (async asset loading)
- `com.unity.cinemachine` (camera)
- `com.unity.localization` (i18n)
- `com.unity.render-pipelines.universal` (URP)

Then **Window → Package Manager → Search** for each, click **Install**.

## 4. Set Up URP

1. **Assets → Create → Rendering → URP Asset (with Universal Renderer)**
2. **Edit → Project Settings → Graphics → URP Asset** → assign the new URP asset
3. **Edit → Project Settings → Quality → Rendering** → also assign

## 5. Configure Player Settings

- **Player → Company Name / Product Name / Bundle Identifier** (e.g. `com.yourstudio.projectaria`)
- **Player → Other Settings → Color Space** → `Linear`
- **Player → Other Settings → Scripting Backend** → `IL2CPP`
- **Player → Other Settings → Target Architectures** → `ARM64` (mobile)
- **Player → Other Settings → API Compatibility Level** → `.NET Standard 2.1`
- **Player → Resolution and Presentation → Default Orientation** → `Auto Rotation` (or `Landscape`)

## 6. Set Up the Bootstrap Scene

1. **File → New Scene** → name it `Bootstrap`
2. Create an empty GameObject, name it `[Bootstrap]`
3. Add component **GameBootstrap** (drag from `Assets/Scripts/Core/GameBootstrap.cs`)
4. Save scene to `Assets/Scenes/Bootstrap.unity`
5. **File → Build Settings → Add Open Scenes** → make sure Bootstrap is **Scene 0**

## 7. Set Up the Game Scene

1. **File → New Scene** → name it `Game`
2. Add a **Directional Light** (set shadows → Soft if tier allows)
3. Add a **Camera** (position Y=2, parented under empty `CameraRoot` at (0, 0, 0))
4. Create empty `[Player]` GameObject at (0, 1, 0)
   - Add **CharacterController**
   - Add **PlayerController** (auto-pulls PlayerStats, PlayerInput, PlayerInteraction, PlayerInventory, CombatSystem via `RequireComponent` — add them all)
   - Add **BuildingSystem**
5. Create empty `[WorldManager]` GameObject
   - Add **WorldManagerRunner** (a thin MonoBehaviour that calls `WorldManager.Instance.Update()` from `Update()`) — see snippet below
   - Set Chunk Material to your URP/Lit material
6. Create empty `[UI]` Canvas
   - Set Render Mode → Screen Space – Overlay
   - Add **HUDManager** with HP/Hunger/Stamina/Temperature bars + text
   - Add **MobileControlsUI** with VirtualJoystick + SmartActionButton + buttons
   - Add **HotbarUI** with 8 ItemSlots
   - Add **MinimapUI** with a child Camera (orthographic, top-down) + RawImage
7. Save scene → Add to Build Settings as **Scene 1**

### WorldManagerRunner snippet

Create `Assets/Scripts/World/WorldManagerRunner.cs`:

```csharp
using UnityEngine;
namespace ProjectAria.World {
    public class WorldManagerRunner : MonoBehaviour {
        public Transform Player;
        public Material ChunkMaterial;
        private void Start() {
            WorldManager wm = new WorldManager();
            wm.Init(GameManager.Instance.Seed, Player);
            wm.ChunkMaterial = ChunkMaterial;
            ServiceLocator.Register<WorldManager>(wm);
        }
        private void Update() { ServiceLocator.Get<WorldManager>()?.Update(); }
    }
}
```

## 8. Set Up the Main Menu Scene

1. **File → New Scene** → `MainMenu`
2. Add a Canvas with:
   - Title text
   - **Play Solo** button (calls `GameManager.Instance.StartGame()` + SceneManager.LoadScene("Game"))
   - **Host Multiplayer** button (calls `NetworkGameManager.Instance.StartHost()`)
   - **Join Multiplayer** button (input field for address + port, then `StartClient()`)
   - **Settings** button (opens settings panel)
   - **Quit** button
3. Add to Build Settings as **Scene 2**

## 9. Create Sample Data Assets

The Bootstrap registers a few runtime ScriptableObjects as samples. For production:

1. **Assets → Create → Aria → World → Block** → make assets for Grass, Dirt, Stone, Wood Log, Plank, etc.
2. **Assets → Create → Aria → World → Biome** → make assets for Plains, Forest, Desert, Snow, Mountain, Ocean
3. **Assets → Create → Aria → Inventory → Item** → make assets for tools, weapons, food, materials
4. **Assets → Create → Aria → Crafting → Recipe** → recipes
5. **Assets → Create → Aria → NPC → Definition** → NPCs
6. **Assets → Create → Aria → NPC → DialogueTree** → dialogues
7. **Assets → Create → Aria → Quest → Definition** → quests
8. **Assets → Create → Aria → Combat → BossPattern / BossPhase** → boss data

For now, the bootstrap registers a few in-memory samples so the game runs.

## 10. Set Up Input Actions

The PlayerInput component expects an `InputActionAsset` with these action maps:
- `Player`: Move, Look, Jump, Attack, Dodge, Interact, BuildMode, Sprint, Crouch
- `UI`: Inventory, Map, Pause, Hotbar1-8

To create:

1. **Project window → right-click → Create → Input Actions**
2. Add the action maps above
3. Bindings:
   - Move: WASD + Left Stick
   - Look: Mouse Delta + Right Stick
   - Jump: Space + South Button
   - Attack: Mouse Left + West Button
   - Dodge: Shift + East Button
   - Interact: E + South Button (held)
   - BuildMode: B + DPad Up
   - Sprint: Left Shift + Left Trigger
   - Crouch: Left Ctrl + Right Stick Click
   - Pause: Escape + Start
4. Drag the asset onto the **PlayerInput** component's `actions` field

## 11. Run

1. Open `Bootstrap` scene
2. Press **Play**
3. You should see the player spawn in the world with mobile controls UI

## 12. Build

### Android
1. **File → Build Settings → Android → Player Settings**
2. Set keystore (or accept default for testing)
3. Click **Build And Run**
4. APK installs to connected device

### iOS
1. **File → Build Settings → iOS → Switch Platform**
2. **Build** → generates Xcode project
3. Open in Xcode → set signing team → Run

## 13. Profiling

- **Window → Analysis → Profiler** → attach to device
- Look for:
  - **GC.Alloc** spikes in Update → ObjectPool miss
  - **CPU Main** spikes during chunk generation → offload to Job System
  - **Render** draw call count → should stay < 100
- **Window → Analysis → Frame Debugger** → inspect overdraw

---

## 🐛 Common Issues

| Issue | Fix |
|---|---|
| Input doesn't work | Install Input System package; Player Settings → Active Input Handling = "Both" |
| URP pink shaders | Reassign materials after URP install |
| Chunks don't generate | WorldManagerRunner not in scene; Player reference not set |
| Netcode fails | Netcode package not installed; NetworkConfig not initialized |
| Mobile UI invisible | Canvas in Overlay but Resolution Scaling wrong; set CanvasScaler to "Scale With Screen Size", reference 1920x1080 |
| High GC alloc | ObjectPool not pre-warmed; missing `Reset()` on entities |

---

## 📚 Next Steps

1. Read [Architecture.md](Architecture.md) to understand the patterns
2. Read [PerformanceGuide.md](PerformanceGuide.md) for mobile optimization tips
3. Read [Networking.md](Networking.md) for multiplayer design
4. Add 3D models, textures, animations (art pipeline)
5. Add sound effects, music (audio pipeline)
6. Polish: add animations, VFX, juice

---

## 🎮 Quick Sanity Test

After setup, you should be able to:

✅ Press Play → see player + chunks + sky
✅ Joystick → character moves
✅ Smart Action button → changes context (Attack when enemy in range, Mine when block in range, etc.)
✅ Hotbar at bottom → 8 slots
✅ HP/Hunger/Stamina bars drain slowly over time
✅ Day/night cycle visible
✅ Save with `F5` (if you wire it), load with `F9`
✅ Host → second instance joins → both see each other
