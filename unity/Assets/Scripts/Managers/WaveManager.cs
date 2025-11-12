using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Managers
{
    /// <summary>
    /// Manages spawning waves of enemies and timing.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action AllWavesCompleted;

        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private LevelManager levelManager;

        private readonly Queue<WaveRuntimeData> pendingWaves = new();
        public bool IsRunning { get; private set; }
        public int CurrentWaveIndex { get; private set; } = -1;

        public void Initialize()
        {
            // TODO: Prepare wave queue based on current level definition.
        }

        public void StartWaves()
        {
            // TODO: Begin processing waves.
        }

        public void StopWaves()
        {
            // TODO: Stop wave progression and clear queue.
        }

        public void StartNextWave()
        {
            // TODO: Dequeue next wave and schedule spawns.
        }

        public void OnEnemyEliminated(string enemyId)
        {
            // TODO: Track remaining enemies and close wave when complete.
        }
    }

    public class WaveRuntimeData
    {
        public LevelDefinition LevelDefinition { get; init; }
        public WaveDefinition WaveDefinition { get; init; }
        public int WaveIndex { get; init; }
        public float TimeRemainingUntilStart { get; set; }
        public int ActiveEnemies { get; set; }
    }
}
