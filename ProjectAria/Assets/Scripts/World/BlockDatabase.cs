// ============================================================
// BlockDatabase.cs
// All block definitions, stored as ScriptableObjects.
// Indexed by integer ID for fast runtime lookup.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.World
{
    public enum BlockType { Solid, Transparent, Liquid, Plant, Decoration, Air }

    [CreateAssetMenu(fileName = "Block_", menuName = "Aria/World/Block", order = 0)]
    public class BlockDefinition : ScriptableObject
    {
        public int Id;
        public string DisplayName = "Block";
        public BlockType Type = BlockType.Solid;
        public bool Mineable = true;
        public int Hardness = 1;
        public ToolType RequiredTool = ToolType.None;
        public bool Stackable = true;
        public int MaxStack = 99;
        public Sprite Icon;
        public Mesh Mesh;            // null = use unit cube
        public Material Material;
        public bool DropsItself = true;
        public int DropItemId = 0;
        public string DropTableId;
        public bool EmitsLight;
        public Color LightColor = Color.white;
        public int LightLevel = 0;
        public bool Solid = true;
        public AudioClip MineSound;
        public AudioClip PlaceSound;

        [TextArea] public string Description;
    }

    public enum ToolType { None, Pickaxe, Axe, Shovel, Hoe, Sword, FishingRod, Hammer }

    public static class BlockDatabase
    {
        private static readonly List<BlockDefinition> _blocks = new();
        private static readonly Dictionary<int, BlockDefinition> _byId = new();
        public const int AirId = 0;

        public static int Count => _blocks.Count;

        public static int Register(BlockDefinition def)
        {
            if (def == null) return AirId;
            if (_byId.TryGetValue(def.Id, out var existing)) return existing.Id;
            _blocks.Add(def);
            _byId[def.Id] = def;
            return def.Id;
        }

        public static BlockDefinition Get(int id)
        {
            if (id == AirId) return null;
            _byId.TryGetValue(id, out var d);
            return d;
        }

        public static void Clear()
        {
            _blocks.Clear();
            _byId.Clear();
        }

        public static IEnumerable<BlockDefinition> All => _blocks;
    }
}
