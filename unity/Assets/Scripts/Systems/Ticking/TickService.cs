using System.Collections.Generic;
using UnityEngine;

namespace TD.Systems.Ticking
{
    /// <summary>
    /// Centralized tick dispatcher to reduce the number of individual Update() methods.
    /// </summary>
    public static class TickService
    {
        private static TickRunner runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            EnsureRunnerExists();
        }

        public static void Register(ITickable tickable)
        {
            EnsureRunnerExists().Register(tickable);
        }

        public static void Unregister(ITickable tickable)
        {
            if (runner == null)
            {
                return;
            }

            runner.Unregister(tickable);
        }

        private static TickRunner EnsureRunnerExists()
        {
            if (runner != null)
            {
                return runner;
            }

            var host = new GameObject("[TickService]");
            Object.DontDestroyOnLoad(host);
            runner = host.AddComponent<TickRunner>();
            return runner;
        }

        private sealed class TickRunner : MonoBehaviour
        {
            private readonly List<ITickable> tickables = new(256);
            private readonly HashSet<ITickable> registered = new();
            private readonly List<ITickable> pendingAdd = new();
            private readonly List<ITickable> pendingRemove = new();
            private bool iterating;

            internal void Register(ITickable tickable)
            {
                if (tickable == null)
                {
                    return;
                }

                if (registered.Contains(tickable))
                {
                    pendingRemove.Remove(tickable);
                    return;
                }

                if (iterating)
                {
                    if (!pendingAdd.Contains(tickable))
                    {
                        pendingAdd.Add(tickable);
                    }
                }
                else
                {
                    AddTickable(tickable);
                }
            }

            internal void Unregister(ITickable tickable)
            {
                if (tickable == null)
                {
                    return;
                }

                if (!registered.Contains(tickable))
                {
                    pendingAdd.Remove(tickable);
                    return;
                }

                if (iterating)
                {
                    if (!pendingRemove.Contains(tickable))
                    {
                        pendingRemove.Add(tickable);
                    }
                }
                else
                {
                    RemoveTickable(tickable);
                }
            }

            private void Update()
            {
                iterating = true;

                var deltaTime = Time.deltaTime;
                for (var i = 0; i < tickables.Count; i++)
                {
                    tickables[i].Tick(deltaTime);
                }

                iterating = false;

                if (pendingRemove.Count > 0)
                {
                    for (var i = pendingRemove.Count - 1; i >= 0; i--)
                    {
                        RemoveTickable(pendingRemove[i]);
                    }

                    pendingRemove.Clear();
                }

                if (pendingAdd.Count > 0)
                {
                    for (var i = 0; i < pendingAdd.Count; i++)
                    {
                        AddTickable(pendingAdd[i]);
                    }

                    pendingAdd.Clear();
                }
            }

            private void AddTickable(ITickable tickable)
            {
                if (tickable == null || registered.Contains(tickable))
                {
                    return;
                }

                registered.Add(tickable);
                tickables.Add(tickable);
            }

            private void RemoveTickable(ITickable tickable)
            {
                if (tickable == null || !registered.Remove(tickable))
                {
                    return;
                }

                var index = tickables.IndexOf(tickable);
                if (index >= 0)
                {
                    var lastIndex = tickables.Count - 1;
                    tickables[index] = tickables[lastIndex];
                    tickables.RemoveAt(lastIndex);
                }
            }
        }
    }
}
