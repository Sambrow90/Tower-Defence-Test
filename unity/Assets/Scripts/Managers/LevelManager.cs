using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Managers
{
    /// <summary>
    /// Handles level loading, progression, and map configuration.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public event Action<int> LevelLoaded;
        public event Action<int> LevelCompleted;
        public event Action<int> LevelFailed;

        [SerializeField] private List<LevelDefinition> availableLevels = new();

        public int CurrentLevelIndex { get; private set; } = -1;
        public LevelDefinition CurrentLevel { get; private set; }

        public void Initialize()
        {
            // TODO: Load level metadata from resources or persistence.
        }

        public void LoadLevel(int levelIndex)
        {
            // TODO: Load scene/level data and configure environment.
        }

        public void RestartLevel()
        {
            // TODO: Reset current level state and reinitialize systems.
        }

        public void CompleteLevel()
        {
            // TODO: Handle victory logic and trigger events.
        }

        public void FailLevel()
        {
            // TODO: Handle defeat logic and trigger events.
        }
    }

    [Serializable]
    public class LevelDefinition
    {
        [field: SerializeField] public string LevelName { get; private set; }
        [field: SerializeField] public string SceneName { get; private set; }
        [field: SerializeField] public int StartingCurrency { get; private set; }
        [field: SerializeField] public int StartingLives { get; private set; }
        [field: SerializeField] public List<WaveDefinition> Waves { get; private set; }
    }

    [Serializable]
    public class WaveDefinition
    {
        [field: SerializeField] public string WaveId { get; private set; }
        [field: SerializeField] public float DelayBeforeStart { get; private set; }
        [field: SerializeField] public List<EnemySpawnDefinition> Enemies { get; private set; }
    }

    [Serializable]
    public class EnemySpawnDefinition
    {
        [field: SerializeField] public string EnemyId { get; private set; }
        [field: SerializeField] public int Count { get; private set; }
        [field: SerializeField] public float SpawnInterval { get; private set; }
    }
}
