using System;
using UnityEngine;
using TD.Gameplay.Enemies;
using TD.Gameplay.Towers;

namespace TD.Gameplay.Data
{
    /// <summary>
    /// Immutable data configuration for a tower.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Data/Tower", fileName = "TowerData")]
    public class TowerData : ScriptableObject
    {
        [SerializeField] private string towerId = Guid.NewGuid().ToString();
        [SerializeField] private float range = 5f;
        [SerializeField, Min(0.1f)] private float fireRate = 1f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [SerializeField] private TargetPriority targetPriority = TargetPriority.First;
        [SerializeField] private ProjectileBehaviour projectilePrefab;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
        [SerializeField, Min(1)] private int projectilePoolSize = 8;

        public string TowerId => towerId;
        public float Range => range;
        public float FireRate => fireRate;
        public int Damage => damage;
        public DamageType DamageType => damageType;
        public TargetPriority TargetPriority => targetPriority;
        public ProjectileBehaviour ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
        public int ProjectilePoolSize => projectilePoolSize;
    }

    public enum TargetPriority
    {
        First,
        Last,
        Strongest
    }
}
