using System;
using UnityEngine;
using TD.Gameplay.Data;
using TD.Gameplay.Enemies;
using TD.Systems;

namespace TD.Gameplay.Towers
{
    /// <summary>
    /// Controls targeting and shooting for a tower instance using TowerData configuration.
    /// </summary>
    public class TowerBehaviour : MonoBehaviour
    {
        public event Action<TowerBehaviour, EnemyBehaviour> TargetAcquired;
        public event Action<TowerBehaviour, EnemyBehaviour> TargetLost;
        public event Action<TowerBehaviour> ShotFired;

        [SerializeField] private TowerData towerData;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask enemyLayerMask = ~0;
        [SerializeField, Min(0.02f)] private float retargetInterval = 0.1f;

        private static readonly Collider[] TargetBuffer = new Collider[32];

        private ObjectPool<ProjectileBehaviour> projectilePool;
        private EnemyBehaviour currentTarget;
        private float fireCooldown;
        private float targetTimer;

        public TowerData Data => towerData;
        public EnemyBehaviour CurrentTarget => currentTarget;

        private void Awake()
        {
            if (towerData == null)
            {
                Debug.LogError($"{nameof(TowerBehaviour)} on {name} has no TowerData assigned.");
                enabled = false;
                return;
            }

            if (towerData.ProjectilePrefab == null)
            {
                Debug.LogError($"{nameof(TowerBehaviour)} on {name} is missing a projectile prefab in the assigned TowerData.");
                enabled = false;
                return;
            }

            projectilePool = new ObjectPool<ProjectileBehaviour>(towerData.ProjectilePrefab, towerData.ProjectilePoolSize, transform);
        }

        private void OnEnable()
        {
            fireCooldown = 0f;
            targetTimer = 0f;
        }

        /// <summary>
        /// Compatibility hook for existing managers that expect to initialize towers after instantiation.
        /// </summary>
        public void Initialize(TD.Managers.TowerDefinition definition, TD.Managers.TowerManager manager)
        {
            if (towerData == null)
            {
                Debug.LogWarning($"Tower '{name}' was initialized without TowerData. Assign a TowerData asset in the inspector.");
            }

            fireCooldown = 0f;
            targetTimer = 0f;
        }

        private void Update()
        {
            if (towerData == null)
            {
                return;
            }

            targetTimer -= Time.deltaTime;
            if (targetTimer <= 0f)
            {
                targetTimer = retargetInterval;
                RefreshTarget();
            }

            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                ReleaseTarget();
                return;
            }

            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f)
            {
                Shoot();
            }
        }

        private void RefreshTarget()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, towerData.Range, TargetBuffer, enemyLayerMask);
            EnemyBehaviour bestCandidate = IsTargetValid(currentTarget) ? currentTarget : null;

            for (var i = 0; i < count; i++)
            {
                if (TargetBuffer[i] == null)
                {
                    continue;
                }

                if (!TargetBuffer[i].TryGetComponent(out EnemyBehaviour enemy))
                {
                    continue;
                }

                if (!IsTargetValid(enemy))
                {
                    continue;
                }

                bestCandidate = SelectTarget(bestCandidate, enemy);
            }

            if (bestCandidate != null)
            {
                AcquireTarget(bestCandidate);
            }
            else
            {
                ReleaseTarget();
            }
        }

        private bool IsTargetValid(EnemyBehaviour enemy)
        {
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive)
            {
                return false;
            }

            var sqrRange = towerData.Range * towerData.Range;
            return (enemy.transform.position - transform.position).sqrMagnitude <= sqrRange;
        }

        private EnemyBehaviour SelectTarget(EnemyBehaviour current, EnemyBehaviour candidate)
        {
            if (current == null)
            {
                return candidate;
            }

            switch (towerData.TargetPriority)
            {
                case TargetPriority.First:
                    return candidate.PathProgress > current.PathProgress ? candidate : current;
                case TargetPriority.Last:
                    return candidate.PathProgress < current.PathProgress ? candidate : current;
                case TargetPriority.Strongest:
                    return candidate.CurrentHealth > current.CurrentHealth ? candidate : current;
                default:
                    return current;
            }
        }

        private void AcquireTarget(EnemyBehaviour target)
        {
            if (currentTarget == target)
            {
                return;
            }

            ReleaseTarget();
            currentTarget = target;
            TargetAcquired?.Invoke(this, currentTarget);
        }

        private void ReleaseTarget()
        {
            if (currentTarget == null)
            {
                return;
            }

            TargetLost?.Invoke(this, currentTarget);
            currentTarget = null;
        }

        private void Shoot()
        {
            if (currentTarget == null)
            {
                return;
            }

            var projectile = projectilePool.Get();
            projectile.transform.position = firePoint != null ? firePoint.position : transform.position;
            projectile.transform.rotation = firePoint != null ? firePoint.rotation : transform.rotation;
            projectile.Launch(currentTarget, towerData.Damage, towerData.ProjectileSpeed, towerData.DamageType, projectilePool);

            fireCooldown = towerData.FireRate > 0f ? 1f / towerData.FireRate : 0f;
            ShotFired?.Invoke(this);
        }
    }
}
