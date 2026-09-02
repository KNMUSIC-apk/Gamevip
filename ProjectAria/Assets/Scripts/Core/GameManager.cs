// ============================================================
// GameManager.cs
// Root singleton. Boots all systems, owns main loop tick.
// Holds references to active world, players, and global state.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAria.Core
{
    public enum GameState { Boot, MainMenu, Loading, Playing, Paused, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Boot")]
        [SerializeField] private string _bootstrapScene = "Bootstrap";
        [SerializeField] private string _mainMenuScene = "MainMenu";
        [SerializeField] private string _gameScene = "Game";
        [SerializeField] private bool _autoStartOnPlay = true;
        [SerializeField] private int _defaultSeed = 0;
        [SerializeField] private Difficulty _defaultDifficulty = Difficulty.Normal;

        public GameState State { get; private set; } = GameState.Boot;
        public int Seed { get; set; }
        public int LocalPlayerId { get; private set; } = 0;

        private TimeSystem _time;
        private WeatherSystem _weather;
        private SaveSystem _save;
        private ObjectPool _pool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = false; // mobile battery
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            // Initialize core services
            SettingsManager.Init();
            SettingsManager.Apply();

            _time = new TimeSystem();
            _weather = new WeatherSystem();
            _save = new SaveSystem();
            _pool = new ObjectPool(transform);

            ServiceLocator.Register<TimeSystem>(_time);
            ServiceLocator.Register<WeatherSystem>(_weather);
            ServiceLocator.Register<SaveSystem>(_save);
            ServiceLocator.Register<ObjectPool>(_pool);

            Seed = _defaultSeed;
            EventBus.Publish(new GameInitializedEvent());

            if (_autoStartOnPlay) StartGame();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (State != GameState.Playing) return;
            _time?.Tick(dt);
            _weather?.Tick(dt);
            _save?.Tick(dt);
        }

        public void StartGame()
        {
            State = GameState.Loading;
            // Scene load handled elsewhere; for MVP, just flip state
            State = GameState.Playing;
        }

        public void Pause(bool paused)
        {
            if (State == GameState.Playing && paused) State = GameState.Paused;
            else if (State == GameState.Paused && !paused) State = GameState.Playing;
            _time?.Pause(paused);
            EventBus.Publish(new GamePausedEvent(paused));
        }

        public void GameOver()
        {
            State = GameState.GameOver;
            EventBus.Publish(new GameQuittedEvent());
        }

        public void SetLocalPlayer(int id) => LocalPlayerId = id;
    }
}
