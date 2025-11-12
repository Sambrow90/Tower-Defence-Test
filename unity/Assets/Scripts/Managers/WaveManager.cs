using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TD.Data;
using TD.Gameplay.Enemies;

namespace TD.Managers
{
    /// <summary>
    /// Manages spawning waves of enemies and timing.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public event Action<WaveData, int> WaveStarted;
        public event Action<WaveData, int> WaveCompleted;
        public event Action<LevelData> AllWavesCompleted;

        [SerializeField] private EnemyManager enemyManager;

        private readonly Queue<WaveData> pendingWaves = new();
        private Coroutine activeRoutine;
        private LevelData currentLevel;
        private WaveData currentWave;
        private int currentWaveIndex = -1;
        private int activeEnemyCount;
        private bool wavesConfigured;
        private bool wavesSpawned;
        private bool levelCompletionRaised;

        public bool IsRunning { get; private set; }
        public int CurrentWaveIndex => currentWaveIndex;
        public WaveData CurrentWave => currentWave;

        private void OnEnable()
        {
            if (enemyManager != null)
            {
                enemyManager.EnemySpawned += HandleEnemySpawned;
                enemyManager.EnemyDespawned += HandleEnemyDespawned;
            }
        }

        private void OnDisable()
        {
            if (enemyManager != null)
            {
                enemyManager.EnemySpawned -= HandleEnemySpawned;
                enemyManager.EnemyDespawned -= HandleEnemyDespawned;
            }
        }

        public void ConfigureForLevel(LevelData level)
        {
            StopActiveRoutine();

            currentLevel = level;
            pendingWaves.Clear();
            currentWave = null;
            currentWaveIndex = -1;
            activeEnemyCount = 0;
            wavesConfigured = level != null;
            wavesSpawned = false;
            levelCompletionRaised = false;

            if (level == null)
            {
                return;
            }

            foreach (var wave in level.Waves)
            {
                if (wave != null)
                {
                    pendingWaves.Enqueue(wave);
                }
            }
        }

        public void StartWaves()
        {
            if (!wavesConfigured)
            {
                Debug.LogWarning("WaveManager cannot start: no level configured.");
                return;
            }

            if (IsRunning)
            {
                Debug.LogWarning("WaveManager is already running.");
                return;
            }

            if (pendingWaves.Count == 0)
            {
                wavesSpawned = true;
                TryFinalizeLevel();
                return;
            }

            activeRoutine = StartCoroutine(RunWaves());
        }

        public void StopWaves()
        {
            StopActiveRoutine();

            IsRunning = false;
            currentWave = null;
            currentWaveIndex = -1;
            pendingWaves.Clear();
            activeEnemyCount = 0;
            wavesSpawned = false;
            levelCompletionRaised = false;
        }

        private void StopActiveRoutine()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
        }

        private IEnumerator RunWaves()
        {
            IsRunning = true;

            while (pendingWaves.Count > 0)
            {
                currentWave = pendingWaves.Dequeue();
                currentWaveIndex++;

                if (currentWave == null)
                {
                    continue;
                }

                float startDelay = Mathf.Max(0f, currentWave.StartDelay);
                if (startDelay > 0f)
                {
                    yield return new WaitForSeconds(startDelay);
                }

                WaveStarted?.Invoke(currentWave, currentWaveIndex);
                yield return StartCoroutine(SpawnWave(currentWave));
                WaveCompleted?.Invoke(currentWave, currentWaveIndex);
            }

            IsRunning = false;
            currentWave = null;
            wavesSpawned = true;
            TryFinalizeLevel();
        }

        private IEnumerator SpawnWave(WaveData wave)
        {
            foreach (var group in wave.SpawnGroups)
            {
                if (group == null)
                {
                    continue;
                }

                int spawnCount = Mathf.Max(0, group.Count);
                float interval = Mathf.Max(0f, group.SpawnInterval);

                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnEnemy(group.EnemyId);

                    bool isLastSpawn = i >= spawnCount - 1;
                    if (!isLastSpawn)
                    {
                        if (interval > 0f)
                        {
                            yield return new WaitForSeconds(interval);
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                }
            }

            yield return null;
        }

        private void SpawnEnemy(string enemyId)
        {
            if (enemyManager == null)
            {
                Debug.LogWarning($"Cannot spawn enemy '{enemyId}': EnemyManager not assigned.");
                return;
            }

            EnemyBehaviour enemy = enemyManager.SpawnEnemy(enemyId, Vector3.zero);
            if (enemy != null)
            {
                activeEnemyCount++;
            }
            else
            {
                Debug.LogWarning($"EnemyManager failed to spawn enemy '{enemyId}'.");
            }
        }

        private void HandleEnemySpawned(EnemyBehaviour enemy)
        {
            // Active enemy tracking is handled when SpawnEnemy succeeds, so nothing is required here.
        }

        private void HandleEnemyDespawned(EnemyBehaviour enemy)
        {
            if (activeEnemyCount > 0)
            {
                activeEnemyCount--;
            }

            TryFinalizeLevel();
        }

        private void TryFinalizeLevel()
        {
            if (!wavesSpawned || levelCompletionRaised)
            {
                return;
            }

            if (activeEnemyCount > 0)
            {
                return;
            }

            if (currentLevel == null)
            {
                return;
            }

            levelCompletionRaised = true;
            AllWavesCompleted?.Invoke(currentLevel);
        }
    }
}
