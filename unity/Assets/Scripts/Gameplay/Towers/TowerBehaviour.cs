using System;
using UnityEngine;
using TD.Managers;
using TD.Gameplay.Enemies;

namespace TD.Gameplay.Towers
{
    /// <summary>
    /// Base behaviour for tower logic decoupled from presentation.
    /// </summary>
    public abstract class TowerBehaviour : MonoBehaviour
    {
        public event Action<TowerBehaviour> TargetAcquired;
        public event Action<TowerBehaviour> TargetLost;
        public event Action<TowerBehaviour> ShotFired;

        [SerializeField] private string towerId;
        [SerializeField] private float range;
        [SerializeField] private float fireRate;
        [SerializeField] private int baseDamage;

        protected EnemyBehaviour CurrentTarget { get; private set; }
        protected float CooldownTimer { get; set; }
        protected TowerDefinition Definition { get; private set; }

        public string TowerId => towerId;
        public float Range => range;
        public float FireRate => fireRate;
        public int BaseDamage => baseDamage;

        protected virtual void Awake()
        {
            CooldownTimer = 0f;
        }

        protected virtual void Update()
        {
            if (CurrentTarget == null)
            {
                return;
            }

            if (!CurrentTarget || Vector3.Distance(transform.position, CurrentTarget.transform.position) > range)
            {
                ReleaseTarget();
                return;
            }

            if (CooldownTimer > 0f)
            {
                CooldownTimer -= Time.deltaTime;
            }

            if (CooldownTimer <= 0f)
            {
                Fire();
            }
        }

        public virtual void Initialize(TowerDefinition definition, TowerManager manager)
        {
            Definition = definition;
        }

        public virtual void AcquireTarget(EnemyBehaviour enemy)
        {
            if (enemy == null)
            {
                return;
            }

            CurrentTarget = enemy;
            TargetAcquired?.Invoke(this);
        }

        public virtual void ReleaseTarget()
        {
            if (CurrentTarget == null)
            {
                return;
            }

            CurrentTarget = null;
            TargetLost?.Invoke(this);
        }

        public virtual void Fire()
        {
            if (CurrentTarget == null)
            {
                return;
            }

            CurrentTarget.ApplyDamage(baseDamage);
            ShotFired?.Invoke(this);
            CooldownTimer = FireRate > 0f ? 1f / FireRate : 0f;
        }

        public virtual void ApplyUpgrade(TowerUpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return;
            }

            baseDamage = Mathf.RoundToInt(baseDamage * upgrade.DamageModifier);
            range *= upgrade.RangeModifier;
            fireRate *= upgrade.FireRateModifier;
        }
    }
}
