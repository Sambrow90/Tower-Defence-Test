using System;
using System.Collections.Generic;
using UnityEngine;
using TD.Gameplay.Enemies;

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

        public IReadOnlyList<EnemyBehaviour> ActiveEnemies => activeEnemies;

        public void Initialize()
        {
            // TODO: Prepare object pools and lookup tables for enemy prefabs.
        }

        public EnemyBehaviour SpawnEnemy(string enemyId, Vector3 spawnPosition)
        {
            // TODO: Spawn or fetch enemy instance and initialize behaviour.
            return null;
        }

        public void DespawnEnemy(EnemyBehaviour enemy)
        {
            // TODO: Return enemy to pool and notify listeners.
        }

        public EnemyDefinition GetEnemyDefinition(string enemyId)
        {
            // TODO: Retrieve definition for spawn requests or UI.
            return null;
        }
    }

    [Serializable]
    public class EnemyDefinition
    {
        [field: SerializeField] public string EnemyId { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; }
        [field: SerializeField] public int Reward { get; private set; }
        [field: SerializeField] public int DamageToPlayer { get; private set; }
    }
}
