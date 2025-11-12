using System.Collections.Generic;
using UnityEngine;

namespace TD.Systems
{
    /// <summary>
    /// Lightweight generic object pool for Components.
    /// </summary>
    /// <typeparam name="T">Component type stored in the pool.</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> pool = new();
        private readonly T prefab;
        private readonly Transform parent;

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            if (prefab == null)
            {
                Debug.LogError("ObjectPool created without a prefab.");
                return;
            }

            for (var i = 0; i < initialSize; i++)
            {
                var instance = CreateInstance();
                ReturnToPool(instance);
            }
        }

        public T Get(bool activate = true)
        {
            if (pool.Count == 0)
            {
                return CreateInstance(activate);
            }

            var instance = pool.Pop();
            if (activate)
            {
                instance.gameObject.SetActive(true);
            }

            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null)
            {
                return;
            }

            ReturnToPool(instance);
        }

        private T CreateInstance(bool activate = false)
        {
            var instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(activate);
            return instance;
        }

        private void ReturnToPool(T instance)
        {
            instance.gameObject.SetActive(false);
            pool.Push(instance);
        }
    }
}
