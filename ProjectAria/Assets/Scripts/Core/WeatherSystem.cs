// ============================================================
// WeatherSystem.cs
// Weather state machine. Drives skybox, particles, audio, lighting.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAria.Core
{
    public enum WeatherType { Clear, Cloudy, Rain, Storm, Snow, Fog, Heatwave, Sandstorm }

    [Serializable]
    public class WeatherData
    {
        public WeatherType type;
        public float weight = 1f;
        public Color skyTint = Color.white;
        public Color lightTint = Color.white;
        public float lightIntensity = 1f;
        public float fogDensity = 0f;
        public Color fogColor = Color.gray;
        public bool playParticles;
        public bool playAudio;
        public string audioLoopId;
    }

    public class WeatherSystem : IService
    {
        public WeatherType Current { get; private set; } = WeatherType.Clear;
        public float Intensity { get; private set; } = 1f; // 0..1
        public WeatherData CurrentData { get; private set; }

        private readonly List<WeatherData> _table = new();
        private float _timeToNextChange;
        private readonly System.Random _rng = new();

        public event Action<WeatherType> OnWeatherChanged;

        public WeatherSystem()
        {
            _timeToNextChange = 60f; // first change after 1 minute
        }

        public void RegisterWeather(WeatherData data)
        {
            if (data == null || _table.Contains(data)) return;
            _table.Add(data);
        }

        public void Tick(float dt)
        {
            _timeToNextChange -= dt;
            if (_timeToNextChange <= 0f)
            {
                RollNewWeather();
                _timeToNextChange = UnityEngine.Random.Range(180f, 480f); // 3-8 min
            }
        }

        private void RollNewWeather()
        {
            if (_table.Count == 0) return;
            float total = 0f;
            for (int i = 0; i < _table.Count; i++) total += _table[i].weight;
            float r = (float)_rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < _table.Count; i++)
            {
                acc += _table[i].weight;
                if (r <= acc)
                {
                    SetWeather(_table[i].type);
                    return;
                }
            }
        }

        public void SetWeather(WeatherType type)
        {
            Current = type;
            CurrentData = _table.Find(w => w.type == type) ?? new WeatherData { type = type };
            OnWeatherChanged?.Invoke(type);
            EventBus.Publish(new WeatherChangedEvent(type));
        }

        [Serializable]
        public struct SaveData { public int current; }
        public SaveData GetSaveData() => new SaveData { current = (int)Current };
        public void LoadSaveData(SaveData d)
        {
            SetWeather((WeatherType)d.current);
        }
    }
}
