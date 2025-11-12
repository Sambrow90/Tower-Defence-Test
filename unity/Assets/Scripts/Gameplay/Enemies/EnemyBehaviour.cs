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
        [SerializeField] private WaypointPath waypointPath;
        [SerializeField] private float waypointArrivalDistance = 0.05f;

        protected int CurrentHealth { get; private set; }
        protected EnemyDefinition Definition { get; private set; }
        protected WaveManager WaveManager { get; private set; }

        public string EnemyId => enemyId;
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;

        protected virtual void Awake()
        {
            waypointPath ??= GetComponentInParent<WaypointPath>();
            CurrentHealth = maxHealth;
            currentWaypointIndex = 0;
            hasReachedGoal = false;
        }

        public virtual void Initialize(EnemyDefinition definition, WaveManager waveManager)
        {
            Definition = definition;
            WaveManager = waveManager;

            if (definition != null)
            {
                enemyId = definition.EnemyId;
                maxHealth = definition.MaxHealth;
                moveSpeed = definition.MoveSpeed;
            }

            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth);
            ResetPath();
        }

        public virtual void Tick(float deltaTime)
        {
            if (hasReachedGoal || waypointPath == null)
            {
                return;
            }

            if (waypointPath.Waypoints.Count == 0)
            {
                return;
            }

            var targetPosition = waypointPath.GetWaypointPosition(currentWaypointIndex);
            var toTarget = targetPosition - transform.position;

            if (toTarget.sqrMagnitude <= waypointArrivalDistance * waypointArrivalDistance)
            {
                if (!waypointPath.Loop && currentWaypointIndex >= waypointPath.Waypoints.Count - 1)
                {
                    hasReachedGoal = true;
                    OnGoalReached();
                    return;
                }

                currentWaypointIndex = waypointPath.GetNextIndex(currentWaypointIndex);
                targetPosition = waypointPath.GetWaypointPosition(currentWaypointIndex);
                toTarget = targetPosition - transform.position;
            }

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                var movement = toTarget.normalized * moveSpeed * deltaTime;
                if (movement.sqrMagnitude > toTarget.sqrMagnitude)
                {
                    movement = toTarget;
                }

                transform.position += movement;
            }
        }

        public virtual void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth);

            if (CurrentHealth <= 0)
            {
                OnKilled();
            }
        }

        protected virtual void OnKilled()
        {
            EnemyKilled?.Invoke(this);
            Destroy(gameObject);
        }

        protected virtual void OnGoalReached()
        {
            EnemyReachedGoal?.Invoke(this);
            Destroy(gameObject);
        }

        protected virtual void Update()
        {
            Tick(Time.deltaTime);
        }

        private void ResetPath()
        {
            hasReachedGoal = false;
            currentWaypointIndex = 0;
        }

        private int currentWaypointIndex;
        private bool hasReachedGoal;
    }
}
