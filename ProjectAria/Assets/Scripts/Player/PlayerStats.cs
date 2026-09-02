// ============================================================
// PlayerStats.cs
// HP, Hunger, Stamina, Temperature, Level, XP.
// Drives survival loop. Server-authoritative in multiplayer.
// ============================================================
using System;
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.Player
{
    [Serializable]
    public class PlayerStatsData
    {
        public float maxHp = 100f;
        public float hp = 100f;
        public float maxHunger = 100f;
        public float hunger = 100f;
        public float maxStamina = 100f;
        public float stamina = 100f;
        public float temperature = 21f;       // celsius, comfortable range 15-30
        public float comfortableMin = 15f;
        public float comfortableMax = 30f;
        public float freezeRate = 1f;          // HP loss / sec below comfortable
        public float overheatRate = 1f;        // HP loss / sec above comfortable
        public float hungerDrainPerSec = 0.05f;
        public float staminaDrainPerSec = 5f;  // while sprinting
        public float staminaRegenPerSec = 15f; // when not sprinting
        public float staminaRegenDelay = 1.5f; // seconds after use
        public float hpRegenPerSec = 0.5f;     // when hunger > 60 and temp ok
        public int level = 1;
        public int xp = 0;
        public int xpToNextLevel = 100;
    }

    public class PlayerStats : MonoBehaviour, IDamageable
    {
        public PlayerStatsData Data = new();
        public int PlayerId { get; set; }

        public float Hp01 => Data.hp / Data.maxHp;
        public float Hunger01 => Data.hunger / Data.maxHunger;
        public float Stamina01 => Data.stamina / Data.maxStamina;
        public bool Alive => Data.hp > 0f;
        public bool IsComfortable => Data.temperature >= Data.comfortableMin && Data.temperature <= Data.comfortableMax;
        public float Xp01 => Data.xp / (float)Data.xpToNextLevel;

        public event Action OnDeath;
        public event Action OnLevelUp;

        private float _staminaUseTimer;
        private float _lastDamageTime;

        public void Init(int playerId)
        {
            PlayerId = playerId;
        }

        public void Tick(float dt)
        {
            if (!Alive) return;

            // Hunger drain
            Data.hunger = Mathf.Max(0f, Data.hunger - Data.hungerDrainPerSec * dt * DifficultyRules.HungerDrain);
            if (Data.hunger <= 0f)
            {
                // Starving: lose HP
                Data.hp = Mathf.Max(0f, Data.hp - 1f * dt);
                CheckDeath();
            }

            // Temperature natural drift back to 21
            float drift = (21f - Data.temperature) * 0.02f;
            Data.temperature += drift * dt;

            if (Data.temperature < Data.comfortableMin)
            {
                float cold = (Data.comfortableMin - Data.temperature) / 15f;
                Data.hp -= cold * Data.freezeRate * dt;
                CheckDeath();
            }
            else if (Data.temperature > Data.comfortableMax)
            {
                float hot = (Data.temperature - Data.comfortableMax) / 15f;
                Data.hp -= hot * Data.overheatRate * dt;
                CheckDeath();
            }

            // HP regen
            if (Data.hunger > 60f && IsComfortable && Data.hp < Data.maxHp)
                Data.hp = Mathf.Min(Data.maxHp, Data.hp + Data.hpRegenPerSec * dt);

            // Stamina regen (with delay)
            if (Time.time - _staminaUseTimer > Data.staminaRegenDelay)
                Data.stamina = Mathf.Min(Data.maxStamina, Data.stamina + Data.staminaRegenPerSec * dt);

            EventBus.Publish(new PlayerStatsChangedEvent(PlayerId, Data.hp, Data.hunger, Data.stamina, Data.temperature));
        }

        public bool TryUseStamina(float amount)
        {
            if (Data.stamina < amount) return false;
            Data.stamina -= amount;
            _staminaUseTimer = Time.time;
            return true;
        }

        public void AddHunger(float amount) => Data.hunger = Mathf.Clamp(Data.hunger + amount, 0f, Data.maxHunger);
        public void AddTemperature(float amount) => Data.temperature = Mathf.Clamp(Data.temperature + amount, -20f, 60f);
        public void Heal(float amount) => Data.hp = Mathf.Clamp(Data.hp + amount, 0f, Data.maxHp);

        public void TakeDamage(float amount, Vector3 hitPoint = default)
        {
            if (!Alive) return;
            float dmg = Mathf.Max(0f, amount * DifficultyRules.DamageTaken);
            Data.hp -= dmg;
            _lastDamageTime = Time.time;
            EventBus.Publish(new EntityDamagedEvent(PlayerId, dmg, hitPoint));
            CheckDeath();
        }

        public void Kill()
        {
            Data.hp = 0f;
            CheckDeath();
        }

        private void CheckDeath()
        {
            if (Data.hp <= 0f && Alive == false) return; // already dead
            if (Data.hp <= 0f)
            {
                EventBus.Publish(new PlayerDiedEvent(PlayerId));
                OnDeath?.Invoke();
            }
        }

        public bool AddXp(int amount)
        {
            Data.xp += Mathf.Max(0, Mathf.RoundToInt(amount * DifficultyRules.XpGain));
            if (Data.xp >= Data.xpToNextLevel)
            {
                Data.xp -= Data.xpToNextLevel;
                Data.level++;
                Data.xpToNextLevel = Mathf.RoundToInt(Data.xpToNextLevel * 1.25f);
                OnLevelUp?.Invoke();
                return true;
            }
            return false;
        }

        // For save/load
        public PlayerSave ToSave() => new PlayerSave
        {
            id = PlayerId,
            name = name,
            position = transform.position,
            rotation = transform.eulerAngles,
            hp = Data.hp,
            hunger = Data.hunger,
            stamina = Data.stamina,
            temperature = Data.temperature,
            level = Data.level,
            xp = Data.xp
        };
        public void FromSave(PlayerSave s)
        {
            Data.hp = s.hp;
            Data.hunger = s.hunger;
            Data.stamina = s.stamina;
            Data.temperature = s.temperature;
            Data.level = s.level;
            Data.xp = s.xp;
            transform.position = s.position;
            transform.eulerAngles = s.rotation;
        }
    }

    public interface IDamageable
    {
        int PlayerId { get; set; }
        void TakeDamage(float amount, Vector3 hitPoint = default);
        bool Alive { get; }
    }
}
