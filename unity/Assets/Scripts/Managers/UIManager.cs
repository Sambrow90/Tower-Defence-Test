using System;
using System.Collections.Generic;
using UnityEngine;
using TD.UI;

namespace TD.Managers
{
    /// <summary>
    /// Handles UI screen transitions, HUD updates, and input prompts.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public event Action<string> ScreenShown;
        public event Action<string> ScreenHidden;

        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private RectTransform hudRoot;
        [SerializeField] private RectTransform menuRoot;
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject levelSelectScreen;
        [SerializeField] private GameObject settingsScreen;
        [SerializeField] private HudView hudView;
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private LevelSelectView levelSelectView;
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private TowerSelectionView towerSelectionView;

        private readonly Dictionary<string, GameObject> screenLookup = new();

        private GameManager gameManager;
        private LevelManager levelManager;
        private WaveManager waveManager;
        private TowerManager towerManager;
        private SaveManager saveManager;
        private int totalWaveCount;

        public Canvas MainCanvas => mainCanvas;
        public RectTransform HudRoot => hudRoot;
        public RectTransform MenuRoot => menuRoot;

        public void Initialize(GameManager gm, LevelManager lm, WaveManager wm, TowerManager tm, SaveManager sm)
        {
            gameManager = gm;
            levelManager = lm;
            waveManager = wm;
            towerManager = tm;
            saveManager = sm;

            screenLookup.Clear();

            if (mainMenuScreen != null)
            {
                screenLookup[UIScreenIds.MainMenu] = mainMenuScreen;
            }

            if (levelSelectScreen != null)
            {
                screenLookup[UIScreenIds.LevelSelect] = levelSelectScreen;
            }

            if (settingsScreen != null)
            {
                screenLookup[UIScreenIds.Settings] = settingsScreen;
            }

            if (hudView != null)
            {
                hudView.Initialize(gm);
                hudRoot.gameObject.SetActive(false);
            }

            if (mainMenuView != null)
            {
                mainMenuView.Initialize(this, gm);
                mainMenuView.PlayRequested += HandleMainMenuPlayRequested;
                mainMenuView.LevelSelectRequested += HandleMainMenuLevelSelectRequested;
                mainMenuView.SettingsRequested += HandleMainMenuSettingsRequested;
                mainMenuView.QuitRequested += HandleMainMenuQuitRequested;
            }

            if (levelSelectView != null)
            {
                levelSelectView.Initialize(this, lm, sm);
                levelSelectView.LevelRequested += HandleLevelRequested;
                levelSelectView.BackRequested += HandleLevelSelectBack;
            }

            if (settingsView != null)
            {
                settingsView.Initialize(this);
                settingsView.BackRequested += HandleSettingsBack;
            }

            if (towerSelectionView != null)
            {
                towerSelectionView.Initialize(towerManager);
            }

            SubscribeToEvents();
            ShowScreen(UIScreenIds.MainMenu);
        }

        public void ShowScreen(string screenId)
        {
            foreach (var kvp in screenLookup)
            {
                bool shouldShow = kvp.Key == screenId;
                if (kvp.Value != null)
                {
                    bool wasActive = kvp.Value.activeSelf;
                    kvp.Value.SetActive(shouldShow);
                    if (shouldShow)
                    {
                        ScreenShown?.Invoke(screenId);
                    }
                    else if (wasActive)
                    {
                        ScreenHidden?.Invoke(kvp.Key);
                    }
                }
            }

            bool showHud = screenId == UIScreenIds.Hud;
            if (hudRoot != null)
            {
                hudRoot.gameObject.SetActive(showHud);
            }
        }

        public void HideScreen(string screenId)
        {
            if (screenLookup.TryGetValue(screenId, out var screen) && screen != null)
            {
                if (screen.activeSelf)
                {
                    screen.SetActive(false);
                    ScreenHidden?.Invoke(screenId);
                }
            }
        }

        public void UpdateCurrencyDisplay(int amount)
        {
            hudView?.SetCurrency(amount);
        }

        public void UpdateLivesDisplay(int lives)
        {
            hudView?.SetLives(lives);
        }

        public void ShowWaveIncoming(int waveNumber)
        {
            hudView?.ShowWaveIncoming(waveNumber, totalWaveCount);
        }

        public void ShowResult(bool victory)
        {
            hudView?.ShowResult(victory);
        }

        public void NotifySpeedChanged(float speed)
        {
            hudView?.RefreshSpeedButtons(speed);
        }

        public void PrepareGameplayUI(int totalWaves)
        {
            totalWaveCount = Mathf.Max(1, totalWaves);
            hudView?.SetTotalWaves(totalWaveCount);
            ShowScreen(UIScreenIds.Hud);
        }

        private void SubscribeToEvents()
        {
            if (gameManager != null)
            {
                gameManager.PlayerCurrencyChanged += UpdateCurrencyDisplay;
                gameManager.PlayerLivesChanged += UpdateLivesDisplay;
                gameManager.GameStateChanged += HandleGameStateChanged;
                gameManager.GameSpeedChanged += NotifySpeedChanged;
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
            }
        }

        private void HandleLevelLoaded(TD.Data.LevelData level)
        {
            if (level == null)
            {
                return;
            }

            totalWaveCount = level.Waves != null ? level.Waves.Count : 0;
            hudView?.SetLevelName(level.DisplayName);
            hudView?.SetTotalWaves(totalWaveCount);
            UpdateCurrencyDisplay(gameManager.PlayerCurrency);
            UpdateLivesDisplay(gameManager.PlayerLives);
        }

        private void HandleLevelStarted(TD.Data.LevelData level)
        {
            PrepareGameplayUI(level.Waves != null ? level.Waves.Count : 0);
        }

        private void HandleLevelCompleted(TD.Data.LevelData level)
        {
            ShowResult(true);
        }

        private void HandleLevelFailed(TD.Data.LevelData level)
        {
            ShowResult(false);
        }

        private void HandleWaveStarted(TD.Data.WaveData wave, int index)
        {
            ShowWaveIncoming(index + 1);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                ShowScreen(UIScreenIds.MainMenu);
                hudView?.ShowPauseOverlay(false);
            }
            else if (state == GameState.Playing)
            {
                ShowScreen(UIScreenIds.Hud);
                hudView?.ShowPauseOverlay(false);
            }
            else if (state == GameState.Paused)
            {
                hudView?.ShowPauseOverlay(true);
            }
            else if (state == GameState.Victory || state == GameState.Defeat)
            {
                hudView?.ShowPauseOverlay(false);
            }
        }

        private void HandleMainMenuPlayRequested()
        {
            if (!TryStartLatestUnlockedLevel())
            {
                HandleMainMenuLevelSelectRequested();
            }
        }

        private void HandleMainMenuLevelSelectRequested()
        {
            ShowScreen(UIScreenIds.LevelSelect);
            levelSelectView?.Refresh();
        }

        private void HandleMainMenuSettingsRequested()
        {
            ShowScreen(UIScreenIds.Settings);
            settingsView?.SyncFromSettings();
        }

        private void HandleMainMenuQuitRequested()
        {
            // Handled in MainMenuView. Method kept for symmetry.
        }

        private void HandleLevelRequested(int levelIndex)
        {
            StartLevelByIndex(levelIndex);
        }

        private void HandleLevelSelectBack()
        {
            ShowScreen(UIScreenIds.MainMenu);
        }

        private void HandleSettingsBack()
        {
            ShowScreen(UIScreenIds.MainMenu);
        }

        private bool TryStartLatestUnlockedLevel()
        {
            if (levelManager == null || levelManager.AvailableLevels == null || levelManager.AvailableLevels.Count == 0)
            {
                return false;
            }

            int targetIndex = 0;

            if (saveManager != null && saveManager.CurrentSave != null)
            {
                targetIndex = Mathf.Clamp(saveManager.CurrentSave.HighestUnlockedLevel, 0, levelManager.AvailableLevels.Count - 1);
            }

            StartLevelByIndex(targetIndex);
            return true;
        }

        private void StartLevelByIndex(int levelIndex)
        {
            if (levelManager == null || gameManager == null)
            {
                return;
            }

            if (levelIndex < 0 || levelIndex >= levelManager.AvailableLevels.Count)
            {
                Debug.LogWarning($"UIManager attempted to start invalid level index {levelIndex}.");
                return;
            }

            levelManager.LoadLevel(levelIndex);
            ShowScreen(UIScreenIds.Hud);
            gameManager.StartGame();
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.PlayerCurrencyChanged -= UpdateCurrencyDisplay;
                gameManager.PlayerLivesChanged -= UpdateLivesDisplay;
                gameManager.GameStateChanged -= HandleGameStateChanged;
                gameManager.GameSpeedChanged -= NotifySpeedChanged;
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
            }

            if (mainMenuView != null)
            {
                mainMenuView.PlayRequested -= HandleMainMenuPlayRequested;
                mainMenuView.LevelSelectRequested -= HandleMainMenuLevelSelectRequested;
                mainMenuView.SettingsRequested -= HandleMainMenuSettingsRequested;
                mainMenuView.QuitRequested -= HandleMainMenuQuitRequested;
            }

            if (levelSelectView != null)
            {
                levelSelectView.LevelRequested -= HandleLevelRequested;
                levelSelectView.BackRequested -= HandleLevelSelectBack;
            }

            if (settingsView != null)
            {
                settingsView.BackRequested -= HandleSettingsBack;
            }
        }
    }
}
