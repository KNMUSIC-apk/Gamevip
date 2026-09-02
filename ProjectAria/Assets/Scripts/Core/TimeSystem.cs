// ============================================================
// TimeSystem.cs
// Day/night cycle, season, calendar.
// Drives world lighting, NPC schedules, crop growth, events.
// ============================================================
using System;
using UnityEngine;

namespace ProjectAria.Core
{
    public enum Season { Spring, Summer, Fall, Winter }

    public class TimeSystem : IService
    {
        public float DayLengthSeconds = 1200f;       // 20 real minutes = 1 in-game day
        public float YearLengthDays = 28f;           // 4 seasons × 7 days each
        public float DayStartHour = 6f;              // 06:00 = sunrise
        public int CurrentDay { get; private set; } = 1;
        public Season CurrentSeason { get; private set; } = Season.Spring;
        public float CurrentHour { get; private set; } = 8f;
        public float DayTime01 => Mathf.InverseLerp(0f, DayLengthSeconds, _dayProgress);

        public event Action<int> OnDayChanged;
        public event Action<Season> OnSeasonChanged;
        public event Action<float> OnHourChanged;

        private float _dayProgress;
        private float _lastHourInt = -1f;
        private bool _paused;

        public TimeSystem()
        {
            _dayProgress = (8f - DayStartHour) * (DayLengthSeconds / 24f);
            if (_dayProgress < 0) _dayProgress += DayLengthSeconds;
        }

        public void Tick(float deltaTime)
        {
            if (_paused) return;
            _dayProgress += deltaTime;
            if (_dayProgress >= DayLengthSeconds)
            {
                _dayProgress -= DayLengthSeconds;
                CurrentDay++;
                OnDayChanged?.Invoke(CurrentDay);
                EventBus.Publish(new TimeOfDayChangedEvent(DayTime01, CurrentDay));

                if ((CurrentDay - 1) % Mathf.RoundToInt(YearLengthDays / 4f) == 0)
                {
                    CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
                    OnSeasonChanged?.Invoke(CurrentSeason);
                }
            }
            CurrentHour = DayStartHour + (_dayProgress / DayLengthSeconds) * 24f;
            if (CurrentHour >= 24f) CurrentHour -= 24f;

            int hourInt = Mathf.FloorToInt(CurrentHour);
            if (hourInt != _lastHourInt)
            {
                _lastHourInt = hourInt;
                OnHourChanged?.Invoke(CurrentHour);
                EventBus.Publish(new TimeOfDayChangedEvent(DayTime01, CurrentDay));
            }
        }

        public void Pause(bool paused) => _paused = paused;

        public string GetTimeString()
        {
            int h = Mathf.FloorToInt(CurrentHour);
            int m = Mathf.FloorToInt((CurrentHour - h) * 60f);
            return $"{h:00}:{m:00}";
        }

        public string GetSeasonString() => CurrentSeason.ToString();

        public void SetTime(float hour)
        {
            CurrentHour = Mathf.Repeat(hour, 24f);
            _dayProgress = (CurrentHour - DayStartHour) * (DayLengthSeconds / 24f);
            if (_dayProgress < 0) _dayProgress += DayLengthSeconds;
            OnHourChanged?.Invoke(CurrentHour);
            EventBus.Publish(new TimeOfDayChangedEvent(DayTime01, CurrentDay));
        }

        // Save/load
        [Serializable]
        public struct SaveData
        {
            public int currentDay;
            public int currentSeason;
            public float dayProgress;
        }
        public SaveData GetSaveData() => new SaveData
        {
            currentDay = CurrentDay,
            currentSeason = (int)CurrentSeason,
            dayProgress = _dayProgress
        };
        public void LoadSaveData(SaveData d)
        {
            CurrentDay = d.currentDay;
            CurrentSeason = (Season)d.currentSeason;
            _dayProgress = d.dayProgress;
            CurrentHour = DayStartHour + (_dayProgress / DayLengthSeconds) * 24f;
            if (CurrentHour >= 24f) CurrentHour -= 24f;
            EventBus.Publish(new TimeOfDayChangedEvent(DayTime01, CurrentDay));
        }
    }
}
