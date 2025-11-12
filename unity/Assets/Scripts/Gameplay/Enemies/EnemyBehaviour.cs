using System;
using UnityEngine;
using TD.Gameplay.Data;
using TD.Systems;
using TD.Systems.Ticking;

namespace TD.Gameplay.Enemies
{
    /// <summary>
    /// Handles movement and health for an enemy instance based on EnemyData.
    /// </summary>
    public class EnemyBehaviour : MonoBehaviour, ITickable
    {
        public event Action<EnemyBehaviour> EnemyDied;
        public event Action<EnemyBehaviour> EnemyReachedGoal;
        public event Action<EnemyBehaviour, int, int> EnemyHealthChanged;

        [SerializeField] private EnemyData enemyData;
        [SerializeField] private WaypointPath waypointPath;
        [SerializeField] private Transform hitPoint;
        [SerializeField, Min(0.01f)] private float waypointArrivalDistance = 0.1f;

        private Health health;
        private int previousWaypointIndex;
        private int currentWaypointIndex;
        private bool reachedGoal;
        private bool healthSubscribed;
        private ObjectPool<EnemyBehaviour> owningPool;

        public EnemyData Data => enemyData;
        public bool IsAlive => health != null && !health.IsDead;
        public Vector3 AimPosition => hitPoint != null ? hitPoint.position : transform.position;
        public float PathProgress { get; private set; }
        public int CurrentHealth => health != null ? health.CurrentHealth : 0;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }

            if (waypointPath == null)
            {
                waypointPath = GetComponentInParent<WaypointPath>();
            }
        }

        public void ApplyData(EnemyData data)
        {
            if (data == null)
            {
                Debug.LogError($"{nameof(EnemyBehaviour)} on {name} has no EnemyData assigned.");
                return;
            }

            enemyData = data;
            if (health == null)
            {
                return;
            }

            health.Initialize(enemyData.MaxHealth);
            reachedGoal = false;
            UpdateProgress();
        }

        public void Activate(EnemyData data, WaypointPath path, ObjectPool<EnemyBehaviour> pool)
        {
            owningPool = pool;
            if (path != null)
            {
                waypointPath = path;
            }

            SubscribeHealth();
            ApplyData(data);
            ResetPath();
        }

        public void Deactivate()
        {
            UnsubscribeHealth();
            owningPool = null;
            reachedGoal = false;
            currentWaypointIndex = 0;
            previousWaypointIndex = 0;
            PathProgress = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!isActiveAndEnabled || reachedGoal || enemyData == null || waypointPath == null)
            {
                return;
            }

            MoveAlongPath(deltaTime);
        }

        public void ReceiveDamage(int amount, DamageType damageType)
        {
            if (!IsAlive)
            {
                return;
            }

            var mitigated = CalculateMitigatedDamage(amount, damageType);
            health.TakeDamage(mitigated);
        }

        public void ForceKill()
        {
            if (health != null)
            {
                health.Kill();
            }
        }

        private void OnEnable()
        {
            TickService.Register(this);
            if (!healthSubscribed && enemyData != null)
            {
                SubscribeHealth();
                ApplyData(enemyData);
                ResetPath();
            }
        }

        private void OnDisable()
        {
            TickService.Unregister(this);
            Deactivate();
        }

        private void MoveAlongPath(float deltaTime)
        {
            var waypointCount = waypointPath?.Waypoints.Count ?? 0;
            if (waypointCount <= 0)
            {
                return;
            }

            var remainingDistance = enemyData.MoveSpeed * deltaTime;
            var safety = 32;
            while (remainingDistance > 0f && safety-- > 0 && !reachedGoal)
            {
                var targetPosition = waypointPath.GetWaypointPosition(currentWaypointIndex);
                var toTarget = targetPosition - transform.position;
                var distance = toTarget.magnitude;

                if (distance <= Mathf.Max(waypointArrivalDistance, 0.001f))
                {
                    AdvanceWaypoint();
                    continue;
                }

                var step = Mathf.Min(distance, remainingDistance);
                transform.position += toTarget.normalized * step;
                remainingDistance -= step;
                UpdateProgress();

                if (step < distance)
                {
                    break;
                }
            }
        }

        private void AdvanceWaypoint()
        {
            if (waypointPath == null)
            {
                return;
            }

            if (!waypointPath.Loop && currentWaypointIndex >= waypointPath.Waypoints.Count - 1)
            {
                reachedGoal = true;
                PathProgress = 1f;
                EnemyReachedGoal?.Invoke(this);
                ReleaseToPool();
                return;
            }

            previousWaypointIndex = currentWaypointIndex;
            currentWaypointIndex = waypointPath.GetNextIndex(currentWaypointIndex);
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (waypointPath == null || waypointPath.Waypoints.Count <= 1)
            {
                PathProgress = 0f;
                return;
            }

            var prevPos = waypointPath.GetWaypointPosition(previousWaypointIndex);
            var nextPos = waypointPath.GetWaypointPosition(currentWaypointIndex);
            var totalSegments = waypointPath.Waypoints.Count - 1f;
            var segmentLength = Vector3.Distance(prevPos, nextPos);
            var travelledOnSegment = segmentLength <= 0f ? 0f : Vector3.Distance(prevPos, transform.position) / segmentLength;
            travelledOnSegment = Mathf.Clamp01(travelledOnSegment);
            PathProgress = (previousWaypointIndex + travelledOnSegment) / totalSegments;
        }

        private int CalculateMitigatedDamage(int amount, DamageType damageType)
        {
            if (enemyData == null)
            {
                return amount;
            }

            if (damageType == DamageType.True)
            {
                return amount;
            }

            var multiplier = 1f - enemyData.Armor;
            foreach (var resistance in enemyData.Resistances)
            {
                if (resistance.DamageType == damageType)
                {
                    multiplier *= 1f - resistance.Reduction;
                }
            }

            multiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
            var mitigated = Mathf.RoundToInt(amount * multiplier);
            return Mathf.Max(1, mitigated);
        }

        private void ResetPath()
        {
            previousWaypointIndex = 0;
            if (waypointPath != null && waypointPath.Waypoints.Count > 1)
            {
                currentWaypointIndex = 1;
            }
            else
            {
                currentWaypointIndex = 0;
            }

            reachedGoal = false;
            UpdateProgress();
        }

        private void UnsubscribeHealth()
        {
            if (health == null || !healthSubscribed)
            {
                return;
            }

            health.HealthChanged -= OnHealthChanged;
            health.Died -= OnDied;
            healthSubscribed = false;
        }

        private void OnHealthChanged(Health sender, int current, int max)
        {
            EnemyHealthChanged?.Invoke(this, current, max);
        }

        private void OnDied(Health sender)
        {
            EnemyDied?.Invoke(this);
            ReleaseToPool();
        }

        private void SubscribeHealth()
        {
            if (health == null || healthSubscribed)
            {
                return;
            }

            health.HealthChanged += OnHealthChanged;
            health.Died += OnDied;
            healthSubscribed = true;
        }

        private void ReleaseToPool()
        {
            UnsubscribeHealth();
            if (owningPool != null)
            {
                owningPool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
