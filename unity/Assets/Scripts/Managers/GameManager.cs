using System;
using UnityEngine;
using TD.Data;
using TD.Systems;

namespace TD.Managers
{
    /// <summary>
    /// Coordinates high-level game flow and cross-manager communication.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState> GameStateChanged;
        public event Action<int> PlayerLivesChanged;
        public event Action<int> PlayerCurrencyChanged;
        public event Action<float> GameSpeedChanged;

        [SerializeField] private LevelManager levelManager;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private float defaultGameSpeed = 1f;

        public GameState CurrentState { get; private set; } = GameState.Bootstrapping;
        public int PlayerLives { get; private set; }
        public int PlayerCurrency { get; private set; }
        public float CurrentGameSpeed { get; private set; } = 1f;
        public bool IsPaused => CurrentState == GameState.Paused;

        private bool isInitialized;
        private float cachedSpeedBeforePause = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            if (saveManager != null)
            {
                saveManager.LoadCompleted += HandleSaveLoaded;
                saveManager.SaveCompleted += HandleSaveSaved;
                saveManager.Initialize();
            }

            if (towerManager != null)
            {
                towerManager.Initialize();
                towerManager.CurrencyChanged += HandleTowerCurrencyChanged;
            }

            if (enemyManager != null)
            {
                enemyManager.Initialize();
            }

            if (levelManager != null)
            {
                levelManager.LevelLoaded += HandleLevelLoaded;
                levelManager.LevelStarted += HandleLevelStarted;
                levelManager.LevelCompleted += HandleLevelCompleted;
                levelManager.LevelFailed += HandleLevelFailed;
            }

            if (waveManager != null)
            {
                waveManager.WaveStarted += HandleWaveStarted;
                waveManager.WaveCompleted += HandleWaveCompleted;
                waveManager.AllWavesCompleted += HandleAllWavesCompleted;
            }

            if (uiManager != null)
            {
                uiManager.Initialize(this, levelManager, waveManager, towerManager, saveManager);
            }

            SetGameSpeed(defaultGameSpeed);
            ChangeState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (saveManager != null)
            {
                saveManager.LoadCompleted -= HandleSaveLoaded;
                saveManager.SaveCompleted -= HandleSaveSaved;
            }

            if (towerManager != null)
            {
                towerManager.CurrencyChanged -= HandleTowerCurrencyChanged;
            }

            if (levelManager != null)
            {
                levelManager.LevelLoaded -= HandleLevelLoaded;
                levelManager.LevelStarted -= HandleLevelStarted;
                levelManager.LevelCompleted -= HandleLevelCompleted;
                levelManager.LevelFailed -= HandleLevelFailed;
            }

            if (waveManager != null)
            {
                waveManager.WaveStarted -= HandleWaveStarted;
                waveManager.WaveCompleted -= HandleWaveCompleted;
                waveManager.AllWavesCompleted -= HandleAllWavesCompleted;
            }
        }

        public void StartGame()
        {
            if (levelManager == null)
            {
                Debug.LogWarning("GameManager cannot start game – LevelManager missing.");
                return;
            }

            if (levelManager.CurrentLevel == null)
            {
                Debug.LogWarning("No level loaded. Load a level before calling StartGame().");
                return;
            }

            levelManager.StartLevel();
        }

        public void PauseGame()
        {
            if (IsPaused)
            {
                return;
            }

            cachedSpeedBeforePause = CurrentGameSpeed;
            Time.timeScale = 0f;
            ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (!IsPaused)
            {
                return;
            }

            ChangeState(GameState.Playing);
            SetGameSpeed(Mathf.Max(0.1f, cachedSpeedBeforePause));
        }

        public void EndGame(bool victory)
        {
            ChangeState(victory ? GameState.Victory : GameState.Defeat);

            if (levelManager != null)
            {
                if (victory)
                {
                    levelManager.CompleteLevel();
                }
                else
                {
                    levelManager.FailLevel();
                }
            }
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            PlayerCurrency += amount;
            PlayerCurrencyChanged?.Invoke(PlayerCurrency);
            towerManager?.AddCurrency(amount);
        }

        public void RemoveLife(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            PlayerLives = Mathf.Max(0, PlayerLives - amount);
            PlayerLivesChanged?.Invoke(PlayerLives);

            if (PlayerLives <= 0 && CurrentState != GameState.Defeat)
            {
                EndGame(false);
            }
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            GameStateChanged?.Invoke(CurrentState);
        }

        public void SetGameSpeed(float speed)
        {
            speed = Mathf.Clamp(speed, 0.1f, 5f);
            CurrentGameSpeed = speed;

            if (!IsPaused)
            {
                Time.timeScale = CurrentGameSpeed;
            }

            GameSpeedChanged?.Invoke(CurrentGameSpeed);
        }

        private void HandleTowerCurrencyChanged(int amount)
        {
            PlayerCurrency = amount;
            PlayerCurrencyChanged?.Invoke(PlayerCurrency);
        }

        private void HandleLevelLoaded(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            PlayerLives = Mathf.Max(0, level.StartingLives);
            PlayerCurrency = Mathf.Max(0, level.StartingCurrency);
            PlayerLivesChanged?.Invoke(PlayerLives);
            PlayerCurrencyChanged?.Invoke(PlayerCurrency);

            if (towerManager != null)
            {
                towerManager.SetCurrency(PlayerCurrency);
            }

            ChangeState(GameState.PreparingLevel);
        }

        private void HandleLevelStarted(LevelData level)
        {
            ChangeState(GameState.Playing);
            SetGameSpeed(CurrentGameSpeed <= 0f ? defaultGameSpeed : CurrentGameSpeed);
        }

        private void HandleLevelCompleted(LevelData level)
        {
            ChangeState(GameState.Victory);

            if (saveManager != null && level != null)
            {
                string nextLevelId = null;

                if (levelManager != null)
                {
                    var availableLevels = levelManager.AvailableLevels;
                    int nextIndex = levelManager.CurrentLevelIndex + 1;
                    if (availableLevels != null && nextIndex >= 0 && nextIndex < availableLevels.Count)
                    {
                        LevelData nextLevel = availableLevels[nextIndex];
                        if (nextLevel != null)
                        {
                            nextLevelId = nextLevel.LevelId;
                        }
                    }
                }

                saveManager.RegisterLevelCompletion(level.LevelId, PlayerCurrency, nextLevelId);
            }
        }

        private void HandleLevelFailed(LevelData level)
        {
            ChangeState(GameState.Defeat);
        }

        private void HandleWaveStarted(WaveData wave, int index)
        {
            uiManager?.ShowWaveIncoming(index + 1);
        }

        private void HandleWaveCompleted(WaveData wave, int index)
        {
            // Reserved for future expansion (rewards, UI notifications, etc.).
        }

        private void HandleAllWavesCompleted(LevelData level)
        {
            levelManager?.CompleteLevel();
        }

        private void HandleSaveLoaded(SaveData saveData)
        {
            ApplySettings(saveData?.Settings);
        }

        private void HandleSaveSaved(SaveData saveData)
        {
            ApplySettings(saveData?.Settings);
        }

        private void ApplySettings(SettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(settings.MasterVolume);

            int qualityIndex = Mathf.Clamp((int)settings.Quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(qualityIndex, true);
            }

            // Language handling would typically be forwarded to a localisation system.
        }
    }

    public enum GameState
    {
        Bootstrapping,
        MainMenu,
        PreparingLevel,
        Playing,
        Paused,
        Victory,
        Defeat
    }
}
