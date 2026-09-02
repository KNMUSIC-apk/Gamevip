// ============================================================
// SaveSystem.cs
// JSON-based world save/load with autosave + backup slots.
// Mobile: writes to Application.persistentDataPath.
// ============================================================
using System;
using System.IO;
using UnityEngine;

namespace ProjectAria.Core
{
    public class SaveSystem : IService
    {
        public const string SaveFolder = "Saves";
        public const string BackupFolder = "Backups";
        public const int MaxBackups = 5;
        public float AutosaveInterval = 60f;

        private string _savePath;
        private string _backupPath;
        private float _autosaveTimer;
        private string _currentSlot = "world_0";
        private bool _saving;

        public event Action<bool> OnSaveCompleted;
        public event Action<bool> OnLoadCompleted;

        public SaveSystem()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SaveFolder);
            _backupPath = Path.Combine(Application.persistentDataPath, BackupFolder);
            Directory.CreateDirectory(_savePath);
            Directory.CreateDirectory(_backupPath);
        }

        public void Tick(float dt)
        {
            if (AutosaveInterval <= 0) return;
            _autosaveTimer += dt;
            if (_autosaveTimer >= AutosaveInterval)
            {
                _autosaveTimer = 0f;
                Save(_currentSlot, isAutosave: true);
            }
        }

        public void SetSlot(string slot) => _currentSlot = slot;
        public string GetSlot() => _currentSlot;

        public bool Save(string slot = null, bool isAutosave = false)
        {
            if (_saving) return false;
            _saving = true;
            slot ??= _currentSlot;

            try
            {
                var world = new WorldSave
                {
                    version = 1,
                    savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    seed = WorldManager.Instance != null ? WorldManager.Instance.Seed : 0,
                    time = ServiceLocator.Get<TimeSystem>()?.GetSaveData() ?? default,
                    weather = ServiceLocator.Get<WeatherSystem>()?.GetSaveData() ?? default,
                    playerCount = 1
                };

                // Gather player data via event broadcast — PlayerManager subscribes.
                var gathered = new System.Collections.Generic.List<PlayerSave>();
                SaveGatherBus.Publish(gathered);
                world.players = gathered.ToArray();

                string json = JsonUtility.ToJson(world, prettyPrint: false);
                string filePath = Path.Combine(_savePath, slot + ".json");
                string tmpPath = filePath + ".tmp";
                File.WriteAllText(tmpPath, json);
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tmpPath, filePath);

                if (!isAutosave) RotateBackup(slot);
                OnSaveCompleted?.Invoke(true);
                EventBus.Publish(new SaveCompletedEvent(true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e}");
                OnSaveCompleted?.Invoke(false);
                EventBus.Publish(new SaveCompletedEvent(false));
                return false;
            }
            finally
            {
                _saving = false;
            }
        }

        public bool Load(string slot = null)
        {
            slot ??= _currentSlot;
            string filePath = Path.Combine(_savePath, slot + ".json");
            if (!File.Exists(filePath))
            {
                OnLoadCompleted?.Invoke(false);
                EventBus.Publish(new LoadCompletedEvent(false));
                return false;
            }
            try
            {
                string json = File.ReadAllText(filePath);
                var world = JsonUtility.FromJson<WorldSave>(json);
                if (world == null)
                {
                    OnLoadCompleted?.Invoke(false);
                    EventBus.Publish(new LoadCompletedEvent(false));
                    return false;
                }
                ServiceLocator.Get<TimeSystem>()?.LoadSaveData(world.time);
                ServiceLocator.Get<WeatherSystem>()?.LoadSaveData(world.weather);
                SaveGatherBus.PublishRestore(world.players);

                if (WorldManager.Instance != null)
                    WorldManager.Instance.Seed = world.seed;

                OnLoadCompleted?.Invoke(true);
                EventBus.Publish(new LoadCompletedEvent(true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e}");
                OnLoadCompleted?.Invoke(false);
                EventBus.Publish(new LoadCompletedEvent(false));
                return false;
            }
        }

        public bool DeleteSave(string slot)
        {
            string filePath = Path.Combine(_savePath, slot + ".json");
            if (File.Exists(filePath)) { File.Delete(filePath); return true; }
            return false;
        }

        public string[] ListSaves()
        {
            if (!Directory.Exists(_savePath)) return Array.Empty<string>();
            var files = Directory.GetFiles(_savePath, "*.json");
            for (int i = 0; i < files.Length; i++) files[i] = Path.GetFileNameWithoutExtension(files[i]);
            return files;
        }

        private void RotateBackup(string slot)
        {
            try
            {
                string src = Path.Combine(_savePath, slot + ".json");
                string dst = Path.Combine(_backupPath, $"{slot}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.json");
                File.Copy(src, dst, overwrite: true);
                var backups = Directory.GetFiles(_backupPath, $"{slot}_*.json");
                if (backups.Length > MaxBackups)
                {
                    Array.Sort(backups);
                    for (int i = 0; i < backups.Length - MaxBackups; i++) File.Delete(backups[i]);
                }
            }
            catch (Exception e) { Debug.LogWarning($"[SaveSystem] Backup rotate failed: {e.Message}"); }
        }

        [Serializable]
        public class WorldSave
        {
            public int version;
            public long savedAtUnix;
            public int seed;
            public TimeSystem.SaveData time;
            public WeatherSystem.SaveData weather;
            public int playerCount;
            public PlayerSave[] players;
        }
    }

    [Serializable]
    public class PlayerSave
    {
        public int id;
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public float hp, hunger, stamina, temperature;
        public int level;
        public int xp;
        public int[] skillLevels;
        public InventorySaveData inventory;
        public string[] activeQuests;
        public string[] completedQuests;
    }

    [Serializable]
    public class InventorySaveData
    {
        public ItemStackSave[] hotbar;
        public ItemStackSave[] main;
    }

    [Serializable]
    public class ItemStackSave
    {
        public int itemId;
        public int amount;
        public int durability;
    }

    // Internal bus used by SaveSystem to gather/restoration without static state.
    public static class SaveGatherBus
    {
        public static System.Collections.Generic.List<PlayerSave> _buffer;
        public static void Publish(System.Collections.Generic.List<PlayerSave> target)
        {
            _buffer = target;
            EventBus.Publish(new GatherPlayerSaveEvent());
            _buffer = null;
        }
        public static void PublishRestore(PlayerSave[] players)
        {
            EventBus.Publish(new RestorePlayerSaveEvent(players));
        }
    }

    public readonly struct GatherPlayerSaveEvent { }
    public readonly struct RestorePlayerSaveEvent
    {
        public readonly PlayerSave[] Players;
        public RestorePlayerSaveEvent(PlayerSave[] p) { Players = p; }
    }
}
