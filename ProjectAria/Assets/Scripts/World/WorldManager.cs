// ============================================================
// WorldManager.cs
// Owns active chunks, generates on demand, unloads far chunks.
// Coordinates biome sampling and structure placement.
// ============================================================
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.Optimization;

namespace ProjectAria.World
{
    public class WorldManager : IService
    {
        public static WorldManager Instance { get; private set; }

        public int Seed { get; set; }
        public int RenderDistance { get; set; } = 6;
        public Transform Player;
        public Material ChunkMaterial;

        private NoiseGenerator _noise;
        private readonly Dictionary<Vector2Int, Chunk> _chunks = new();
        private readonly Queue<Chunk> _generationQueue = new();
        private readonly Queue<Chunk> _meshQueue = new();
        private readonly List<Chunk> _activeChunks = new();
        private const int MaxLoadedChunks = 800; // safety cap

        public BiomeType GetBiomeAt(float worldX, float worldZ)
        {
            float t = _noise.Perlin2D(worldX, worldZ, 0.001f, 2, 0.5f, 2f);
            float h = _noise.Perlin2D(worldX + 9999, worldZ + 9999, 0.0014f, 2, 0.5f, 2f);
            float elev = _noise.FBM2D(worldX, worldZ, 0.0028f, 5);
            bool ocean = elev < 0.18f;
            return BiomeDB.SampleByClimate(t, h, elev, ocean);
        }

        public int GetHeightAt(float worldX, float worldZ)
        {
            float elev = _noise.FBM2D(worldX, worldZ, 0.0028f, 5);
            int baseHeight = Mathf.FloorToInt(Mathf.Lerp(8, Chunk.Height - 16, (elev + 1f) * 0.5f));
            BiomeType b = GetBiomeAt(worldX, worldZ);
            if (b == BiomeType.Mountain) baseHeight = Mathf.RoundToInt(baseHeight * 1.3f);
            if (b == BiomeType.Ocean) baseHeight = Mathf.Min(baseHeight, 18);
            return baseHeight;
        }

        public void Init(int seed, Transform player)
        {
            if (Instance != null && Instance != this) return;
            Instance = this;
            Seed = seed;
            Player = player;
            _noise = new NoiseGenerator(seed);
            RenderDistance = SettingsManager.Current.renderDistanceChunks;
        }

        public void Update()
        {
            if (Player == null) return;
            UpdateStreaming();
            ProcessGenerationQueue();
            ProcessMeshQueue();
        }

        private void UpdateStreaming()
        {
            Vector2Int center = WorldToChunk(Player.position);
            int r = RenderDistance;
            var wanted = new HashSet<Vector2Int>();
            for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    var c = new Vector2Int(center.x + dx, center.y + dz);
                    wanted.Add(c);
                    if (!_chunks.ContainsKey(c)) _chunks[c] = CreateChunk(c);
                }

            // Unload chunks outside
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _chunks)
                if (!wanted.Contains(kv.Key))
                {
                    toRemove.Add(kv.Key);
                }
            foreach (var key in toRemove)
            {
                var ch = _chunks[key];
                _chunks.Remove(key);
                ch.Unload();
                EventBus.Publish(new ChunkUnloadedEvent(key));
            }
        }

        private Chunk CreateChunk(Vector2Int coord)
        {
            var ch = new Chunk { Coord = coord };
            var go = new GameObject($"Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(null);
            go.transform.position = ch.WorldPosition;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var mc = go.AddComponent<MeshCollider>();
            if (ChunkMaterial != null) mr.sharedMaterial = ChunkMaterial;
            ch.GameObject = go;
            ch.MeshFilter = mf;
            ch.MeshRenderer = mr;
            ch.MeshCollider = mc;
            _generationQueue.Enqueue(ch);
            return ch;
        }

        private void ProcessGenerationQueue()
        {
            int perFrame = 2;
            while (perFrame-- > 0 && _generationQueue.Count > 0)
            {
                var ch = _generationQueue.Dequeue();
                GenerateChunkData(ch);
                _meshQueue.Enqueue(ch);
            }
        }

        private void ProcessMeshQueue()
        {
            int perFrame = 2;
            while (perFrame-- > 0 && _meshQueue.Count > 0)
            {
                var ch = _meshQueue.Dequeue();
                BuildAndApplyMesh(ch);
            }
        }

        private void GenerateChunkData(Chunk ch)
        {
            int baseX = ch.Coord.x * Chunk.Width;
            int baseZ = ch.Coord.y * Chunk.Width;
            for (int z = 0; z < Chunk.Width; z++)
            {
                for (int x = 0; x < Chunk.Width; x++)
                {
                    float wx = baseX + x;
                    float wz = baseZ + z;
                    int h = GetHeightAt(wx, wz);
                    BiomeType biome = GetBiomeAt(wx, wz);
                    var def = BiomeDB.Get(biome);
                    int surface = def != null ? def.SurfaceBlockId : 1;
                    int sub = def != null ? def.SubSurfaceBlockId : 2;
                    int filler = def != null ? def.FillerBlockId : 3;

                    for (int y = 0; y < Chunk.Height; y++)
                    {
                        int id = 0;
                        if (y == 0) id = 4; // bedrock
                        else if (y < h - 4) id = filler;
                        else if (y < h) id = sub;
                        else if (y == h) id = surface;
                        ch.SetBlock(x, y, z, id);
                    }
                }
            }
            ch.IsGenerated = true;
            ch.IsDirty = true;
        }

        private void BuildAndApplyMesh(Chunk ch)
        {
            var neighbours = GetNeighbourChunks(ch.Coord);
            var data = ChunkMesher.Build(ch, neighbours);
            ch.ApplyMesh(data, ChunkMaterial);
        }

        private Chunk[] GetNeighbourChunks(Vector2Int c)
        {
            return new[]
            {
                _chunks.TryGetValue(new Vector2Int(c.x - 1, c.y), out var nx) ? nx : null,
                _chunks.TryGetValue(new Vector2Int(c.x + 1, c.y), out var px) ? px : null,
                _chunks.TryGetValue(new Vector2Int(c.x, c.y - 1), out var nz) ? nz : null,
                _chunks.TryGetValue(new Vector2Int(c.x, c.y + 1), out var pz) ? pz : null,
            };
        }

        public static Vector2Int WorldToChunk(Vector3 pos) => new(Mathf.FloorToInt(pos.x / Chunk.Width), Mathf.FloorToInt(pos.z / Chunk.Width));

        public Vector3Int? RaycastBlock(Vector3 origin, Vector3 dir, float maxDist, out Vector3Int hitNormal)
        {
            hitNormal = Vector3Int.zero;
            // DDA voxel traversal
            Vector3 pos = origin;
            Vector3 step = dir.normalized * 0.05f;
            int maxSteps = Mathf.CeilToInt(maxDist / 0.05f);
            for (int i = 0; i < maxSteps; i++)
            {
                pos += step;
                var ci = WorldToChunk(pos);
                if (!_chunks.TryGetValue(ci, out var ch)) continue;
                int lx = Mathf.FloorToInt(pos.x) - ci.x * Chunk.Width;
                int ly = Mathf.FloorToInt(pos.y);
                int lz = Mathf.FloorToInt(pos.z) - ci.y * Chunk.Width;
                int id = ch.GetBlock(lx, ly, lz);
                if (id != 0)
                {
                    var def = BlockDatabase.Get(id);
                    if (def != null && def.Solid)
                    {
                        hitNormal = -Vector3Int.RoundToInt(dir.normalized);
                        return new Vector3Int(Mathf.FloorToInt(pos.x), ly, Mathf.FloorToInt(pos.z));
                    }
                }
            }
            return null;
        }

        public bool SetBlockWorld(Vector3Int world, int id)
        {
            var ci = new Vector2Int(Mathf.FloorToInt((float)world.x / Chunk.Width), Mathf.FloorToInt((float)world.z / Chunk.Width));
            if (!_chunks.TryGetValue(ci, out var ch)) return false;
            int lx = world.x - ci.x * Chunk.Width;
            int lz = world.z - ci.y * Chunk.Width;
            ch.SetBlock(lx, world.y, lz, id);
            ch.IsDirty = true;
            _meshQueue.Enqueue(ch);
            EventBus.Publish(new BlockPlacedEvent(world, id));
            return true;
        }

        public int GetBlockWorld(Vector3Int world)
        {
            var ci = new Vector2Int(Mathf.FloorToInt((float)world.x / Chunk.Width), Mathf.FloorToInt((float)world.z / Chunk.Width));
            if (!_chunks.TryGetValue(ci, out var ch)) return 0;
            int lx = world.x - ci.x * Chunk.Width;
            int lz = world.z - ci.y * Chunk.Width;
            return ch.GetBlock(lx, world.y, lz);
        }
    }
}
