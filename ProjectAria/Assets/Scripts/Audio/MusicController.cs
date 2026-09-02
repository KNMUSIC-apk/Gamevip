// ============================================================
// MusicController.cs
// Bridges AudioManager to game state (explore/combat/boss).
// Listens to EventBus.
// ============================================================
using UnityEngine;
using ProjectAria.Core;

namespace ProjectAria.Audio
{
    public class MusicController : MonoBehaviour
    {
        public AudioSource SourceA, SourceB;
        public AudioSource AmbientSource;
        public AudioSource[] SfxPool;
        public AudioClip ExploreMusic, CombatMusic, BossMusic, MenuMusic;

        private void Awake()
        {
            var am = new AudioManager
            {
                MusicSourceA = SourceA,
                MusicSourceB = SourceB,
                AmbientSource = AmbientSource,
                SfxPool = SfxPool
            };
            am.RegisterMusic(MusicState.Explore, ExploreMusic);
            am.RegisterMusic(MusicState.Combat, CombatMusic);
            am.RegisterMusic(MusicState.Boss, BossMusic);
            am.RegisterMusic(MusicState.Menu, MenuMusic);
            ServiceLocator.Register<AudioManager>(am);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EntityKilledEvent>(OnEntityKilled);
            EventBus.Subscribe<BossPhaseChangedEvent>(OnBoss);
            EventBus.Subscribe<WeatherChangedEvent>(OnWeather);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EntityKilledEvent>(OnEntityKilled);
            EventBus.Unsubscribe<BossPhaseChangedEvent>(OnBoss);
            EventBus.Unsubscribe<WeatherChangedEvent>(OnWeather);
        }

        private void OnEntityKilled(EntityKilledEvent e)
        {
            // Could switch to peaceful after a few seconds
        }

        private void OnBoss(BossPhaseChangedEvent e) => ServiceLocator.Get<AudioManager>()?.SetState(MusicState.Boss);
        private void OnWeather(WeatherChangedEvent e) { /* could change ambient loop */ }
    }
}
