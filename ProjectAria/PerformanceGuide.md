# Performance Guide

Mobile game performance is the #1 priority for Project Aria. This guide explains every optimization in place and the budgets we hit.

## 🎯 Targets

| Device | FPS | Notes |
|---|---|---|
| Low-end (2GB RAM, Mali-G52) | 30 | Very Low / Low graphics |
| Mid-range (4GB RAM, SD6-gen1) | 60 | Medium / High graphics |
| High-end (6GB+, A14 / SD8) | 90-120 | Ultra graphics |

The `PerformanceMonitor` auto-downshifts quality when FPS drops below 85% of target for 5+ seconds.

## 🛠️ Implemented Optimizations

### Chunk Streaming
- Chunks within `RenderDistance` are kept loaded
- Generation runs max 2 chunks/frame (avoids hitches)
- Mesh building runs max 2 chunks/frame
- Chunks beyond distance → unload, destroy GameObject
- WorldManager caps total loaded chunks at 800 (safety)

### Object Pooling
- `ObjectPool` for projectiles, particles, items, FX
- Pre-warmed in `ObjectPool.Register(prefab, prewarmCount, maxSize)`
- Auto-despawn via `PoolDespawnTimer`

### LOD
- `LODManager` updates each entity's LOD every 0.5s (not every frame)
- 4 levels: High / Medium / Low / Culled
- Distance thresholds in `LODManager`

### Mesh Optimization
- `ChunkMesher` culls hidden faces (greedy culling — TODO: full greedy mesher)
- Single mesh per chunk instead of per-block GameObjects
- Vertex colors for AO/light tinting (no shader branch)

### Async Asset Loading
- `AsyncAssetLoader` wraps Addressables
- `Prewarm` for hot assets (avoids first-use hitches)

### Frustum Culling
- Built into Unity. Chunks with no visible faces are still culled by Unity's frustum culler.

### GPU Instancing
- URP supports GPU Instancing on Lit shader — turn on for block/foliage materials
- Reduces draw calls by 5-50x

### Texture Compression
- ASTC for mobile (best quality/size ratio)
- Per-platform: ASTC 6x6 for Android, ASTC 4x4 for iOS
- Set in **Import Settings → Default → Android/iOS → Texture Compression**

### Job System (Roadmap)
- Chunk generation can be moved to Burst-compiled jobs
- Mesh building can run on worker threads
- Path: refactor `ChunkMesher.Build` to take arrays, run via `IJobParallelFor`

### Memory
- Chunks beyond distance → fully unloaded, mesh destroyed
- Object pool trims to `MaxSize` when over
- Save system uses streaming JSON (no full-file load if too big)

## 📊 Profiling Checklist

Before each release, profile on a real device:

- [ ] **GC Alloc** = 0 in Update (use Profiler → GC.Alloc)
- [ ] **Draw calls** < 100 (Frame Debugger)
- [ ] **Triangles** < 500K (Frame Debugger)
- [ ] **SetPass calls** < 50
- [ ] **CPU Main** < 16ms (60 FPS target)
- [ ] **Memory** < 1.5 GB mid-range
- [ ] **Battery drain** < 5% / 30 min
- [ ] **Thermal** no throttling after 15 min

## 🔧 Hot Spots to Watch

| Hot spot | Mitigation |
|---|---|
| Chunk generation spikes | Pre-generate rings around player as they move |
| Mesh building spikes | Move to Job System |
| Inventory drag-drop allocations | Cache Image components, no `new` in OnDrag |
| Audio loading | Stream from disk, pre-warm SFX pool |
| Texture memory | Mipmap streaming, compress aggressively |
| GC from LINQ | Replace with `for` loops in hot paths |

## 📱 Mobile-Specific Tips

- **Avoid `foreach`** over `List<T>` (allocates) — use indexed `for`
- **Avoid `new`** in `Update` — pool everything
- **Avoid `FindObjectOfType`** in `Update` — cache references
- **Avoid `Camera.main`** in `Update` — cache once
- **Avoid LINQ** in hot paths
- **Use `Mathf` instead of `System.Math`** (faster)
- **Use `Vector3.SqrMagnitude`** instead of `Vector3.Distance` when comparing
- **Batch similar materials** → fewer SetPass calls
- **Disable shadows** on tiny props
- **Use LOD aggressively** — even mobile enemies get 2-3 LODs
- **Turn off vsync** (we set `Application.targetFrameRate` instead)
- **Disable MSAA** on mobile (use FXAA or SMAA instead)

## 🧪 Test Matrix

Profile on at least these devices before launch:
- Low-end: Samsung A13, Redmi 10
- Mid-range: Pixel 6a, iPhone 11
- High-end: iPhone 14 Pro, Pixel 7 Pro

## 🛠️ PerformanceMonitor Auto-Downshift

The `PerformanceMonitor` script:
1. Samples FPS every 1 second
2. If avg FPS < 85% of target for 5+ consecutive samples
3. Reduces `GraphicsTier` by 1
4. Reduces `renderDistanceChunks` by 1 (min 2)
5. Re-applies via `SettingsManager.Apply()`
6. Saves the new tier to PlayerPrefs

This means a struggling device automatically falls back to a playable state without user intervention.

## 📦 Build Settings for Mobile

- **IL2CPP scripting backend** (faster, smaller, no Mono GC spikes)
- **ARM64 target architecture** only (drop 32-bit)
- **Managed Stripping** = High
- **Engine Code Stripping** = On
- **Mesh Optimizing** = On
- **Texture Compression** = ASTC
- **ETC2 Fallback** = Off
- **GPU Skinning** = On
- **Graphics Jobs** = On (URP only)
- **Static Batching** = On
- **Dynamic Batching** = Off (URP handles batching)
