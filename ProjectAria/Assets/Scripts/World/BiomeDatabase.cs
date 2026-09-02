// ============================================================
// BiomeDatabase.cs
// Biome definitions: temperature, humidity, blocks, mobs, color.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.World
{
    public enum BiomeType
    {
        Plains, Forest, Desert, Mountain, Snow, Ocean, Beach, Swamp, Volcano, Cave, Jungle, Taiga
    }

    [CreateAssetMenu(fileName = "Biome_", menuName = "Aria/World/Biome", order = 1)]
    public class BiomeDefinition : ScriptableObject
    {
        public BiomeType Type;
        public string DisplayName;
        [Range(-1f, 1f)] public float Temperature = 0.4f;
        [Range(-1f, 1f)] public float Humidity = 0.4f;
        public Color FogColor = new Color(0.7f, 0.8f, 1f);
        public Color SkyTint = new Color(0.5f, 0.7f, 1f);
        public Color GrassColor = new Color(0.4f, 0.7f, 0.3f);
        public int SurfaceBlockId = 1;
        public int SubSurfaceBlockId = 2;
        public int FillerBlockId = 3;
        public int[] TreeIds;
        public int[] PlantIds;
        public int[] StructureIds;
        public float TreeDensity = 0.02f;
        public float HeightMultiplier = 1f;
        public bool AllowRain = true;
        public bool AllowSnow = false;
        public int[] Mobs;
        public int[] HostileMobs;
        public AudioClip AmbientLoop;
    }

    public static class BiomeDB
    {
        private static readonly List<BiomeDefinition> _all = new();
        private static readonly Dictionary<BiomeType, BiomeDefinition> _byType = new();

        public static void Register(BiomeDefinition def)
        {
            if (def == null) return;
            if (_byType.ContainsKey(def.Type)) return;
            _all.Add(def);
            _byType[def.Type] = def;
        }

        public static BiomeDefinition Get(BiomeType t) => _byType.TryGetValue(t, out var b) ? b : null;
        public static IEnumerable<BiomeDefinition> All => _all;

        public static BiomeType SampleByClimate(float temp, float humidity, float elevation, bool isOcean)
        {
            if (isOcean) return BiomeType.Ocean;
            if (elevation > 0.85f) return BiomeType.Mountain;
            if (temp < -0.4f) return BiomeType.Snow;
            if (temp > 0.5f && humidity < -0.2f) return BiomeType.Desert;
            if (temp > 0.3f && humidity > 0.5f) return BiomeType.Jungle;
            if (humidity < -0.3f) return BiomeType.Plains;
            if (humidity > 0.4f) return BiomeType.Forest;
            if (temp < 0.0f) return BiomeType.Taiga;
            return BiomeType.Plains;
        }
    }
}
