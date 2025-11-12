using System;
using System.Collections.Generic;
using UnityEngine;
using TD.Gameplay.Towers;
using TD.Systems.Grid;

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
        [SerializeField] private GridManager gridManager;
        [SerializeField] private int startingCurrency = 200;
        [SerializeField] private List<TowerDefinition> towerDefinitions = new();

        private readonly List<TowerBehaviour> activeTowers = new();
        private readonly Dictionary<TowerBehaviour, Vector2Int> towerPositions = new();

        public IReadOnlyList<TowerBehaviour> ActiveTowers => activeTowers;
        public int CurrentCurrency { get; private set; }

        public void Initialize()
        {
            CurrentCurrency = startingCurrency;
        }

        public bool CanPlaceTower(string towerId, Vector3 position)
        {
            if (gridManager == null)
            {
                return false;
            }

            return gridManager.TryGetGridPosition(position, out var gridPosition) &&
                   CanPlaceTower(towerId, gridPosition);
        }

        public bool CanPlaceTower(string towerId, Vector2Int gridPosition)
        {
            var definition = GetTowerDefinition(towerId);
            if (definition == null)
            {
                return false;
            }

            if (CurrentCurrency < definition.BuildCost)
            {
                return false;
            }

            return gridManager != null && gridManager.CanPlaceStructure(gridPosition);
        }

        public TowerBehaviour PlaceTower(string towerId, Vector3 position)
        {
            if (gridManager == null)
            {
                Debug.LogWarning("No GridManager assigned to TowerManager.");
                return null;
            }

            return gridManager.TryGetGridPosition(position, out var gridPosition) &&
                   TryPlaceTower(towerId, gridPosition, out var tower)
                ? tower
                : null;
        }

        public bool TryPlaceTower(string towerId, Vector2Int gridPosition, out TowerBehaviour tower)
        {
            tower = null;

            var definition = GetTowerDefinition(towerId);
            if (definition == null)
            {
                Debug.LogWarning($"No tower definition found for id '{towerId}'. Configure TowerManager.towerDefinitions.");
                return false;
            }

            if (gridManager == null)
            {
                Debug.LogWarning("No GridManager assigned to TowerManager.");
                return false;
            }

            if (CurrentCurrency < definition.BuildCost)
            {
                return false;
            }

            if (!gridManager.TryReserveTile(gridPosition))
            {
                return false;
            }

            var spawnPosition = gridManager.GetWorldPosition(gridPosition);
            var parent = towerRoot != null ? towerRoot : transform;
            var instance = Instantiate(definition.Prefab, spawnPosition, Quaternion.identity, parent);

            tower = instance.GetComponent<TowerBehaviour>();
            if (tower == null)
            {
                Debug.LogError($"Tower prefab '{definition.Prefab.name}' is missing a TowerBehaviour component.");
                gridManager.ReleaseTile(gridPosition);
                Destroy(instance);
                return false;
            }

            tower.Initialize(definition, this);
            activeTowers.Add(tower);
            towerPositions[tower] = gridPosition;

            CurrentCurrency -= definition.BuildCost;
            TowerPlaced?.Invoke(tower);
            return true;
        }

        public void UpgradeTower(TowerBehaviour tower)
        {
            // TODO: Apply upgrade data and update stats/visuals.
        }

        public void RemoveTower(TowerBehaviour tower)
        {
            if (tower == null)
            {
                return;
            }

            if (towerPositions.TryGetValue(tower, out var gridPosition))
            {
                gridManager?.ReleaseTile(gridPosition);
                towerPositions.Remove(tower);
            }

            if (activeTowers.Remove(tower))
            {
                TowerRemoved?.Invoke(tower);
            }

            Destroy(tower.gameObject);
        }

        public TowerDefinition GetTowerDefinition(string towerId)
        {
            return towerDefinitions.Find(definition => definition.TowerId == towerId);
        }

        public Vector2Int? GetTowerGridPosition(TowerBehaviour tower)
        {
            return towerPositions.TryGetValue(tower, out var position) ? position : null;
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
