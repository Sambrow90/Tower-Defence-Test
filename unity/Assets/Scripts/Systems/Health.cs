using System;
using UnityEngine;

namespace TD.Systems
{
    /// <summary>
    /// Reusable health component emitting events on state changes.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public event Action<Health, int, int> HealthChanged;
        public event Action<Health> Died;

        [SerializeField, Min(1)] private int maxHealth = 1;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        private void Awake()
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            if (CurrentHealth == 0)
            {
                CurrentHealth = maxHealth;
            }
        }

        public void Initialize(int max)
        {
            maxHealth = Mathf.Max(1, max);
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            var newValue = Mathf.Max(0, CurrentHealth - amount);
            if (newValue == CurrentHealth)
            {
                return;
            }

            CurrentHealth = newValue;
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                Died?.Invoke(this);
            }
        }

        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = 0;
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
            Died?.Invoke(this);
        }
    }
}
