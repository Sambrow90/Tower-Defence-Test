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
            // TODO: Initialize references and stats.
        }

        protected virtual void Update()
        {
            // TODO: Handle targeting and firing cadence.
        }

        public virtual void Initialize(TowerDefinition definition, TowerManager manager)
        {
            // TODO: Apply definition data and register with manager if needed.
        }

        public virtual void AcquireTarget(EnemyBehaviour enemy)
        {
            // TODO: Set current target and notify listeners.
        }

        public virtual void ReleaseTarget()
        {
            // TODO: Clear target reference and notify listeners.
        }

        public virtual void Fire()
        {
            // TODO: Execute attack logic and damage application.
        }

        public virtual void ApplyUpgrade(TowerUpgradeDefinition upgrade)
        {
            // TODO: Modify stats based on upgrade data.
        }
    }
}
