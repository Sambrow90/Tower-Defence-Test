using System.Collections.Generic;
using UnityEngine;

namespace TD.Data
{
    /// <summary>
    /// ScriptableObject that stores level configuration such as start resources and wave layout.
    /// </summary>
    [CreateAssetMenu(menuName = "TD/Levels/Level Data", fileName = "LevelData")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private string levelId;
        [SerializeField] private string displayName;
        [SerializeField] private string sceneName;
        [SerializeField] private int startingCurrency = 100;
        [SerializeField] private int startingLives = 20;
        [SerializeField] private List<WaveData> waves = new();

        public string LevelId => levelId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public int StartingCurrency => startingCurrency;
        public int StartingLives => startingLives;
        public IReadOnlyList<WaveData> Waves => waves;
    }
}
