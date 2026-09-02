// ============================================================
// HUDManager.cs
// In-game HUD: HP bar, hunger, stamina, temperature, hotbar, minimap.
// Mobile-friendly layout with safe area support.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using ProjectAria.Core;
using ProjectAria.Player;

namespace ProjectAria.UI
{
    public class HUDManager : MonoBehaviour
    {
        public Image HpBar, HungerBar, StaminaBar, TemperatureBar;
        public Text HpText, HungerText, StaminaText, TemperatureText;
        public Text TimeText, DayText, SeasonText;
        public Text LocationText;
        public Image Crosshair;
        public MinimapUI Minimap;

        private PlayerStats _stats;
        private TimeSystem _time;
        private WeatherSystem _weather;

        private void Start()
        {
            _stats = FindObjectOfType<PlayerStats>();
            _time = ServiceLocator.Get<TimeSystem>();
            _weather = ServiceLocator.Get<WeatherSystem>();
        }

        private void Update()
        {
            if (_stats == null) return;
            if (HpBar != null) HpBar.fillAmount = _stats.Hp01;
            if (HungerBar != null) HungerBar.fillAmount = _stats.Hunger01;
            if (StaminaBar != null) StaminaBar.fillAmount = _stats.Stamina01;
            if (TemperatureBar != null) TemperatureBar.fillAmount = Mathf.InverseLerp(-20f, 50f, _stats.Data.temperature);
            if (HpText != null) HpText.text = $"{Mathf.CeilToInt(_stats.Data.hp)}/{Mathf.CeilToInt(_stats.Data.maxHp)}";
            if (HungerText != null) HungerText.text = $"{Mathf.CeilToInt(_stats.Data.hunger)}";
            if (StaminaText != null) StaminaText.text = $"{Mathf.CeilToInt(_stats.Data.stamina)}";
            if (TemperatureText != null) TemperatureText.text = $"{Mathf.CeilToInt(_stats.Data.temperature)}°C";
            if (TimeText != null && _time != null) TimeText.text = _time.GetTimeString();
            if (DayText != null && _time != null) DayText.text = $"Day {_time.CurrentDay}";
            if (SeasonText != null && _time != null) SeasonText.text = _time.GetSeasonString();
            if (Minimap != null) Minimap.Refresh(_stats.transform.position);
        }
    }
}
