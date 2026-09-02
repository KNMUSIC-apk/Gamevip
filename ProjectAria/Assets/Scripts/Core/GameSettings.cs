// ============================================================
// GameSettings.cs
// Runtime settings: graphics, audio, controls, accessibility, difficulty.
// Persisted to PlayerPrefs. Auto-detects device tier on first launch.
// ============================================================
using System;
using UnityEngine;

namespace ProjectAria.Core
{
    public enum GraphicsTier { VeryLow, Low, Medium, High, Ultra }
    public enum Difficulty { Peaceful, Easy, Normal, Hard, Hardcore }
    public enum ControlLayout { Default, LeftHanded, Custom }
    public enum FpsTarget { Fps30, Fps40, Fps60, Fps90, Fps120 }

    [Serializable]
    public class GameSettings
    {
        // ---- Graphics
        public GraphicsTier graphicsTier = GraphicsTier.Medium;
        public FpsTarget fpsTarget = FpsTarget.Fps60;
        public int renderDistanceChunks = 6;
        public bool shadows = true;
        public int textureQuality = 1;     // 0..3
        public int effectsQuality = 1;     // 0..3
        public bool vsync = false;
        public bool fovDynamic = false;
        public float fovBase = 60f;

        // ---- Audio
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;
        public float ambientVolume = 0.8f;
        public bool muteWhenBackground = true;

        // ---- Controls
        public ControlLayout controlLayout = ControlLayout.Default;
        public float joystickOpacity = 0.6f;
        public float buttonOpacity = 0.8f;
        public float buttonScale = 1f;
        public bool hapticFeedback = true;
        public bool aimAssist = true;
        public bool lockOnEnabled = true;

        // ---- Gameplay
        public Difficulty difficulty = Difficulty.Normal;
        public bool showDamageNumbers = true;
        public bool showHungerHud = true;
        public bool showTemperatureHud = true;
        public float mouseSensitivity = 1f;
        public float joystickSensitivity = 1f;
        public bool invertY = false;

        // ---- Accessibility
        public bool colorblindMode = false;
        public int colorblindType = 0; // 0=Protanopia 1=Deuteranopia 2=Tritanopia
        public bool highContrastUi = false;
        public float uiScale = 1f;
        public bool textToSpeech = false;
        public bool reducedMotion = false;
        public bool subtitleEnabled = true;
        public float subtitleSize = 1f;

        // ---- Misc
        public string language = "en";
        public bool showFps = false;
        public int cloudSyncInterval = 300; // seconds; 0 = disabled
    }

    public static class SettingsManager
    {
        private const string PrefsKey = "ProjectAria.Settings";
        public static GameSettings Current { get; private set; }

        public static void Init()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { Current = JsonUtility.FromJson<GameSettings>(json); }
                catch { Current = new GameSettings(); }
            }
            else Current = new GameSettings();

            AutoDetectTier();
        }

        public static void Apply()
        {
            // Apply FPS target
            int target = Current.fpsTarget switch
            {
                FpsTarget.Fps30 => 30,
                FpsTarget.Fps40 => 40,
                FpsTarget.Fps60 => 60,
                FpsTarget.Fps90 => 90,
                FpsTarget.Fps120 => 120,
                _ => 60
            };
            Application.targetFrameRate = target;
            QualitySettings.vSyncCount = Current.vsync ? 1 : 0;

            // Apply difficulty multipliers
            DifficultyRules.Apply(Current.difficulty);
        }

        public static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        private static void AutoDetectTier()
        {
            int sysMem = SystemInfo.systemMemorySize; // MB
            string gfx = SystemInfo.graphicsDeviceName ?? "";
            int gfxMem = SystemInfo.graphicsMemorySize;

            if (sysMem < 3000 || gfxMem < 1000)
                Current.graphicsTier = GraphicsTier.VeryLow;
            else if (sysMem < 5000 || gfxMem < 2000)
                Current.graphicsTier = GraphicsTier.Low;
            else if (sysMem < 8000 || gfxMem < 4000)
                Current.graphicsTier = GraphicsTier.Medium;
            else if (sysMem < 12000)
                Current.graphicsTier = GraphicsTier.High;
            else
                Current.graphicsTier = GraphicsTier.Ultra;
        }
    }

    public static class DifficultyRules
    {
        public static float DamageTaken { get; private set; } = 1f;
        public static float DamageDealt { get; private set; } = 1f;
        public static float HungerDrain { get; private set; } = 1f;
        public static float XpGain { get; private set; } = 1f;
        public static float LootDrop { get; private set; } = 1f;
        public static bool EnemiesSpawn { get; private set; } = true;
        public static bool Permadeath { get; private set; } = false;

        public static void Apply(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Peaceful:
                    DamageTaken = 0.1f; DamageDealt = 1f; HungerDrain = 0.5f;
                    XpGain = 1f; LootDrop = 1.5f; EnemiesSpawn = false; Permadeath = false; break;
                case Difficulty.Easy:
                    DamageTaken = 0.6f; DamageDealt = 1.1f; HungerDrain = 0.8f;
                    XpGain = 1.2f; LootDrop = 1.2f; EnemiesSpawn = true; Permadeath = false; break;
                case Difficulty.Normal:
                    DamageTaken = 1f; DamageDealt = 1f; HungerDrain = 1f;
                    XpGain = 1f; LootDrop = 1f; EnemiesSpawn = true; Permadeath = false; break;
                case Difficulty.Hard:
                    DamageTaken = 1.5f; DamageDealt = 0.9f; HungerDrain = 1.5f;
                    XpGain = 1f; LootDrop = 0.9f; EnemiesSpawn = true; Permadeath = false; break;
                case Difficulty.Hardcore:
                    DamageTaken = 2f; DamageDealt = 0.8f; HungerDrain = 2f;
                    XpGain = 1.5f; LootDrop = 1.5f; EnemiesSpawn = true; Permadeath = true; break;
            }
        }
    }
}
