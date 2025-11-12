using System;
using System.Collections.Generic;
using UnityEngine;
using TD.Data;

namespace TD.Managers
{
    /// <summary>
    /// Handles level loading, progression, and map configuration.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public event Action<LevelData> LevelLoaded;
        public event Action<LevelData> LevelStarted;
        public event Action<LevelData> LevelCompleted;
        public event Action<LevelData> LevelFailed;

        [SerializeField] private List<LevelData> availableLevels = new();
        [SerializeField] private WaveManager waveManager;

        public int CurrentLevelIndex { get; private set; } = -1;
        public LevelData CurrentLevel { get; private set; }
        public bool LevelIsRunning { get; private set; }

        public IReadOnlyList<LevelData> AvailableLevels => availableLevels;

        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= availableLevels.Count)
            {
                Debug.LogError($"Level index {levelIndex} is out of bounds.");
                return;
            }

            CurrentLevelIndex = levelIndex;
            CurrentLevel = availableLevels[levelIndex];
            LevelIsRunning = false;

            if (waveManager != null)
            {
                waveManager.ConfigureForLevel(CurrentLevel);
                waveManager.AllWavesCompleted -= HandleAllWavesCompleted;
                waveManager.AllWavesCompleted += HandleAllWavesCompleted;
            }

            LevelLoaded?.Invoke(CurrentLevel);
        }

        public void LoadLevel(LevelData levelData)
        {
            int index = availableLevels.IndexOf(levelData);
            if (index >= 0)
            {
                LoadLevel(index);
                return;
            }

            availableLevels.Add(levelData);
            LoadLevel(availableLevels.Count - 1);
        }

        public void StartLevel()
        {
            if (CurrentLevel == null)
            {
                Debug.LogWarning("Cannot start level: no level loaded.");
                return;
            }

            if (LevelIsRunning)
            {
                Debug.LogWarning("Level is already running.");
                return;
            }

            LevelIsRunning = true;
            LevelStarted?.Invoke(CurrentLevel);

            if (waveManager != null)
            {
                waveManager.StartWaves();
            }
        }

        public void RestartLevel()
        {
            if (CurrentLevelIndex >= 0)
            {
                LoadLevel(CurrentLevelIndex);
                StartLevel();
            }
        }

        public void CompleteLevel()
        {
            if (!LevelIsRunning)
            {
                return;
            }

            LevelIsRunning = false;
            LevelCompleted?.Invoke(CurrentLevel);
        }

        public void FailLevel()
        {
            if (!LevelIsRunning)
            {
                return;
            }

            LevelIsRunning = false;

            if (waveManager != null)
            {
                waveManager.StopWaves();
            }

            LevelFailed?.Invoke(CurrentLevel);
        }

        private void HandleAllWavesCompleted(LevelData level)
        {
            if (level != CurrentLevel || !LevelIsRunning)
            {
                return;
            }

            CompleteLevel();
        }
    }
}
