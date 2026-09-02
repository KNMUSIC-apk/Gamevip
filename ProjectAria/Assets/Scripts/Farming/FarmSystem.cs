// ============================================================
// FarmSystem.cs
// Farmland tiles, crops, growth stages, seasons, tilling/watering.
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using ProjectAria.Core;
using ProjectAria.World;

namespace ProjectAria.Farming
{
    public enum CropStage { Seed, Sprout, Growing, Mature, Harvestable, Dead }

    [CreateAssetMenu(fileName = "Crop_", menuName = "Aria/Farming/Crop", order = 4)]
    public class CropDefinition : ScriptableObject
    {
        public int Id;
        public string DisplayName;
        public Season[] PlantableSeasons;
        public BiomeType[] PlantableBiomes;
        public float GrowthSecondsPerStage = 60f;
        public int Stages = 4;
        public int ResultItemId;
        public int ResultMinAmount = 1;
        public int ResultMaxAmount = 3;
        public bool Regrows = false;
        public float RegrowSeconds = 30f;
        public int SeedItemId;
        public Sprite[] StageIcons;
        public GameObject[] StagePrefabs;
        [TextArea] public string Description;
    }

    public static class CropDatabase
    {
        private static readonly List<CropDefinition> _all = new();
        private static readonly Dictionary<int, CropDefinition> _byId = new();

        public static void Register(CropDefinition c)
        {
            if (c == null || _byId.ContainsKey(c.Id)) return;
            _all.Add(c); _byId[c.Id] = c;
        }
        public static CropDefinition Get(int id) => _byId.TryGetValue(id, out var c) ? c : null;
        public static IEnumerable<CropDefinition> All => _all;
    }

    public class FarmTile : MonoBehaviour
    {
        public Vector3Int Grid;
        public bool Tilled;
        public bool Watered;
        public int CropId;
        public float PlantedTime;
        public float LastWateredTime;
        public CropStage Stage;
    }

    public class FarmSystem : IService
    {
        public float WaterDrySeconds = 600f; // 10 min
        public float GrowthMultiplier = 1f;

        private readonly List<FarmTile> _tiles = new();

        public IReadOnlyList<FarmTile> Tiles => _tiles;

        public void Tick(float dt, TimeSystem time)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var t = _tiles[i];
                if (!t.Tilled) continue;
                if (t.CropId == 0) continue;
                var def = CropDatabase.Get(t.CropId);
                if (def == null) continue;

                // Water evaporation
                if (t.Watered && time.CurrentDay != Mathf.FloorToInt(t.LastWateredTime))
                {
                    t.Watered = false;
                }

                // Growth only if watered
                if (t.Watered && time.CurrentSeason.InArray(def.PlantableSeasons))
                {
                    float elapsed = time.CurrentDay * 86400f + (float)time.CurrentHour * 3600f - t.PlantedTime;
                    float stageTime = def.GrowthSecondsPerStage / GrowthMultiplier;
                    int newStage = Mathf.Min(def.Stages, Mathf.FloorToInt(elapsed / stageTime));
                    t.Stage = (CropStage)newStage;
                }
                else if (t.Stage == CropStage.Sprout)
                {
                    t.Stage = CropStage.Dead;
                }
            }
        }

        public FarmTile Till(Vector3Int grid, Vector3 pos)
        {
            var existing = _tiles.Find(t => t.Grid == grid);
            if (existing != null) return existing;
            var go = new GameObject($"Farm_{grid.x}_{grid.y}_{grid.z}");
            go.transform.position = pos;
            var tile = go.AddComponent<FarmTile>();
            tile.Grid = grid;
            tile.Tilled = true;
            _tiles.Add(tile);
            return tile;
        }

        public bool Plant(FarmTile tile, int cropId, float time)
        {
            if (tile == null || !tile.Tilled) return false;
            tile.CropId = cropId;
            tile.PlantedTime = time;
            tile.Stage = CropStage.Seed;
            return true;
        }

        public bool Water(FarmTile tile, float time)
        {
            if (tile == null) return false;
            tile.Watered = true;
            tile.LastWateredTime = time;
            return true;
        }

        public int Harvest(FarmTile tile)
        {
            if (tile == null || tile.Stage != CropStage.Harvestable) return 0;
            var def = CropDatabase.Get(tile.CropId);
            if (def == null) return 0;
            int amount = Random.Range(def.ResultMinAmount, def.ResultMaxAmount + 1);
            if (def.Regrows)
            {
                tile.Stage = CropStage.Growing;
            }
            else
            {
                tile.CropId = 0;
                tile.Stage = CropStage.Seed;
            }
            return amount;
        }
    }

    public static class SeasonExtensions
    {
        public static bool InArray(this Season s, Season[] arr)
        {
            if (arr == null || arr.Length == 0) return true;
            for (int i = 0; i < arr.Length; i++) if (arr[i] == s) return true;
            return false;
        }
    }
}
