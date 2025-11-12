using System;
using System.Collections.Generic;
using UnityEngine;
using TD.Gameplay.Towers;

namespace TD.Managers
{
    /// <summary>
    /// Coordinates placement, upgrades, and lifecycle of all towers.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        public event Action<TowerBehaviour> TowerPlaced;
        public event Action<TowerBehaviour> TowerUpgraded;
        public event Action<TowerBehaviour> TowerRemoved;

        [SerializeField] private Transform towerRoot;
        [SerializeField] private List<TowerDefinition> towerDefinitions = new();

        private readonly List<TowerBehaviour> activeTowers = new();

        public IReadOnlyList<TowerBehaviour> ActiveTowers => activeTowers;

        public void Initialize()
        {
            // TODO: Load tower definitions and prepare pooling if necessary.
        }

        public bool CanPlaceTower(string towerId, Vector3 position)
        {
            // TODO: Validate placement constraints (currency, path blocking, etc.).
            return false;
        }

        public TowerBehaviour PlaceTower(string towerId, Vector3 position)
        {
            // TODO: Instantiate tower prefab, initialize behaviour, and track it.
            return null;
        }

        public void UpgradeTower(TowerBehaviour tower)
        {
            // TODO: Apply upgrade data and update stats/visuals.
        }

        public void RemoveTower(TowerBehaviour tower)
        {
            // TODO: Remove tower from play and recycle resources.
        }

        public TowerDefinition GetTowerDefinition(string towerId)
        {
            // TODO: Retrieve definition for UI or validation purposes.
            return null;
        }
    }

    [Serializable]
    public class TowerDefinition
    {
        [field: SerializeField] public string TowerId { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public int BuildCost { get; private set; }
        [field: SerializeField] public List<TowerUpgradeDefinition> Upgrades { get; private set; }
    }

    [Serializable]
    public class TowerUpgradeDefinition
    {
        [field: SerializeField] public string UpgradeId { get; private set; }
        [field: SerializeField] public int UpgradeCost { get; private set; }
        [field: SerializeField] public float DamageModifier { get; private set; }
        [field: SerializeField] public float RangeModifier { get; private set; }
        [field: SerializeField] public float FireRateModifier { get; private set; }
    }
}
