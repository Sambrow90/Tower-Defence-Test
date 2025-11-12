using System;
using UnityEngine;
using TD.Managers;

namespace TD.Gameplay.Enemies
{
    /// <summary>
    /// Base behaviour for enemy navigation and combat interactions.
    /// </summary>
    public abstract class EnemyBehaviour : MonoBehaviour
    {
        public event Action<EnemyBehaviour> EnemyKilled;
        public event Action<EnemyBehaviour> EnemyReachedGoal;
        public event Action<int> HealthChanged;

        [SerializeField] private string enemyId;
        [SerializeField] private int maxHealth;
        [SerializeField] private float moveSpeed;

        protected int CurrentHealth { get; private set; }
        protected EnemyDefinition Definition { get; private set; }
        protected WaveManager WaveManager { get; private set; }

        public string EnemyId => enemyId;
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;

        protected virtual void Awake()
        {
            // TODO: Cache components and prepare navigation/pathing.
        }

        public virtual void Initialize(EnemyDefinition definition, WaveManager waveManager)
        {
            // TODO: Apply definition data and prepare runtime state.
        }

        public virtual void Tick(float deltaTime)
        {
            // TODO: Perform movement along the path and handle interactions.
        }

        public virtual void ApplyDamage(int amount)
        {
            // TODO: Reduce health, broadcast changes, and handle death.
        }

        protected virtual void OnKilled()
        {
            // TODO: Trigger kill events and cleanup logic.
        }

        protected virtual void OnGoalReached()
        {
            // TODO: Notify managers about player damage and cleanup.
        }
    }
}
