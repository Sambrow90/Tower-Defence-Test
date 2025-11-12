using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using TD.Gameplay.Data;
using TD.Gameplay.Enemies;
using TD.Systems;

namespace TD.Managers
{
    /// <summary>
    /// Controls enemy pooling, spawning, and despawning.
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        public event Action<EnemyBehaviour> EnemySpawned;
        public event Action<EnemyBehaviour> EnemyDespawned;

        [SerializeField] private Transform enemyRoot;
        [SerializeField] private List<EnemyDefinition> enemyDefinitions = new();

        private readonly List<EnemyBehaviour> activeEnemies = new();
        private readonly Dictionary<string, EnemyDefinition> definitionLookup = new();
        private readonly Dictionary<string, ObjectPool<EnemyBehaviour>> pools = new();

        public IReadOnlyList<EnemyBehaviour> ActiveEnemies => activeEnemies;

        public void Initialize()
        {
            for (var i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = activeEnemies[i];
                if (enemy != null)
                {
                    enemy.ForceKill();
                }
            }

            activeEnemies.Clear();
            definitionLookup.Clear();
            pools.Clear();

            foreach (var definition in enemyDefinitions)
            {
                if (definition == null || definition.Data == null || definition.Prefab == null)
                {
                    continue;
                }

                var enemyId = definition.Data.EnemyId;
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    continue;
                }

                if (definitionLookup.ContainsKey(enemyId))
                {
                    Debug.LogWarning($"Duplicate enemy definition for id '{enemyId}'. Only the first entry will be used.");
                    continue;
                }

                definitionLookup.Add(enemyId, definition);
                pools.Add(enemyId, new ObjectPool<EnemyBehaviour>(definition.Prefab, definition.InitialPoolSize, enemyRoot));
            }
        }

        public EnemyBehaviour SpawnEnemy(string enemyId, Vector3 spawnPosition, WaypointPath overridePath = null)
        {
            if (!definitionLookup.TryGetValue(enemyId, out var definition))
            {
                Debug.LogWarning($"No enemy definition configured for id '{enemyId}'.");
                return null;
            }

            if (!pools.TryGetValue(enemyId, out var pool))
            {
                pool = new ObjectPool<EnemyBehaviour>(definition.Prefab, definition.InitialPoolSize, enemyRoot);
                pools.Add(enemyId, pool);
            }

            var enemy = pool.Get(false);
            if (enemy == null)
            {
                return null;
            }

            using (SpawnMarker.Auto())
            {
                enemy.transform.SetParent(enemyRoot != null ? enemyRoot : transform, false);
                enemy.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                enemy.Activate(definition.Data, overridePath != null ? overridePath : definition.DefaultPath, pool);
                enemy.gameObject.SetActive(true);
            }

            RegisterEnemy(enemy);
            return enemy;
        }

        public void DespawnEnemy(EnemyBehaviour enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (activeEnemies.Remove(enemy))
            {
                enemy.EnemyDied -= HandleEnemyDied;
                enemy.EnemyReachedGoal -= HandleEnemyReachedGoal;
                EnemyDespawned?.Invoke(enemy);
            }
        }

        public EnemyDefinition GetEnemyDefinition(string enemyId)
        {
            return definitionLookup.TryGetValue(enemyId, out var definition) ? definition : null;
        }

        private void OnEnable()
        {
            foreach (var enemy in activeEnemies)
            {
                AttachEnemyCallbacks(enemy);
            }
        }

        private void OnDisable()
        {
            foreach (var enemy in activeEnemies)
            {
                DetachEnemyCallbacks(enemy);
            }
        }

        private void RegisterEnemy(EnemyBehaviour enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (activeEnemies.Contains(enemy))
            {
                return;
            }

            activeEnemies.Add(enemy);
            AttachEnemyCallbacks(enemy);
            EnemySpawned?.Invoke(enemy);
        }

        private void AttachEnemyCallbacks(EnemyBehaviour enemy)
        {
            enemy.EnemyDied += HandleEnemyDied;
            enemy.EnemyReachedGoal += HandleEnemyReachedGoal;
        }

        private void DetachEnemyCallbacks(EnemyBehaviour enemy)
        {
            enemy.EnemyDied -= HandleEnemyDied;
            enemy.EnemyReachedGoal -= HandleEnemyReachedGoal;
        }

        private void HandleEnemyDied(EnemyBehaviour enemy)
        {
            DespawnEnemy(enemy);
        }

        private void HandleEnemyReachedGoal(EnemyBehaviour enemy)
        {
            DespawnEnemy(enemy);
        }

        private static readonly ProfilerMarker SpawnMarker = new("EnemyManager.SpawnEnemy");
    }

    [Serializable]
    public class EnemyDefinition
    {
        [field: SerializeField] public EnemyData Data { get; private set; }
        [field: SerializeField] public EnemyBehaviour Prefab { get; private set; }
        [field: SerializeField, Min(1)] public int InitialPoolSize { get; private set; } = 8;
        [field: SerializeField] public WaypointPath DefaultPath { get; private set; }

        public string EnemyId => Data != null ? Data.EnemyId : string.Empty;
    }
}
