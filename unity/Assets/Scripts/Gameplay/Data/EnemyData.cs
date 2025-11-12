using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Gameplay.Data
{
    /// <summary>
    /// Immutable configuration for an enemy archetype.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Data/Enemy", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId = Guid.NewGuid().ToString();
        [SerializeField, Min(1)] private int maxHealth = 10;
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.5f;
        [SerializeField, Range(0f, 0.95f)] private float armor = 0f;
        [SerializeField] private List<DamageResistance> resistances = new();

        public string EnemyId => enemyId;
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float Armor => armor;
        public IReadOnlyList<DamageResistance> Resistances => resistances;
    }

    [Serializable]
    public struct DamageResistance
    {
        [SerializeField] private TD.Gameplay.Enemies.DamageType damageType;
        [SerializeField, Range(0f, 1f)] private float reduction;

        public TD.Gameplay.Enemies.DamageType DamageType => damageType;
        public float Reduction => reduction;
    }
}
