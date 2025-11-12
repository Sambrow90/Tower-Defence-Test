using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Data
{
    /// <summary>
    /// ScriptableObject describing a single enemy wave.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Levels/Wave Data", fileName = "WaveData")]
    public class WaveData : ScriptableObject
    {
        [SerializeField] private string waveId;
        [SerializeField] private float startDelay;
        [SerializeField] private List<SpawnGroup> spawnGroups = new();

        public string WaveId => waveId;
        public float StartDelay => Mathf.Max(0f, startDelay);
        public IReadOnlyList<SpawnGroup> SpawnGroups => spawnGroups;

        [Serializable]
        public class SpawnGroup
        {
            [SerializeField] private string enemyId;
            [SerializeField] private int count = 1;
            [SerializeField] private float spawnInterval = 1f;

            public string EnemyId => enemyId;
            public int Count => Mathf.Max(0, count);
            public float SpawnInterval => Mathf.Max(0f, spawnInterval);
        }
    }
}
