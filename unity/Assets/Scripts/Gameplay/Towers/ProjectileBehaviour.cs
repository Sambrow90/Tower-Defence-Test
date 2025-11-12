using UnityEngine;
using TD.Gameplay.Enemies;
using TD.Systems;
using TD.Systems.Ticking;

namespace TD.Gameplay.Towers
{
    /// <summary>
    /// Handles projectile travel and applying damage on impact before returning to the pool.
    /// </summary>
    public class ProjectileBehaviour : MonoBehaviour, ITickable
    {
        [SerializeField, Min(0.1f)] private float lifeTime = 3f;

        private EnemyBehaviour target;
        private int damage;
        private float speed;
        private DamageType damageType;
        private ObjectPool<ProjectileBehaviour> pool;
        private float timeRemaining;

        public void Launch(EnemyBehaviour target, int damage, float speed, DamageType damageType, ObjectPool<ProjectileBehaviour> pool)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            this.damageType = damageType;
            this.pool = pool;
            timeRemaining = lifeTime;
        }

        private void OnEnable()
        {
            TickService.Register(this);
        }

        private void OnDisable()
        {
            TickService.Unregister(this);
            target = null;
            pool = null;
            timeRemaining = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (target == null || !target.IsAlive)
            {
                Release();
                return;
            }

            timeRemaining -= deltaTime;
            if (timeRemaining <= 0f)
            {
                Release();
                return;
            }

            var targetPosition = target.AimPosition;
            var toTarget = targetPosition - transform.position;
            var distanceThisFrame = speed * deltaTime;

            if (toTarget.sqrMagnitude <= distanceThisFrame * distanceThisFrame)
            {
                Impact();
            }
            else
            {
                var direction = toTarget.normalized;
                transform.position += direction * distanceThisFrame;
                if (direction.sqrMagnitude > 0f)
                {
                    transform.forward = direction;
                }
            }
        }

        private void Impact()
        {
            if (target != null)
            {
                target.ReceiveDamage(damage, damageType);
            }

            Release();
        }

        private void Release()
        {
            target = null;
            pool?.Release(this);
        }
    }
}
