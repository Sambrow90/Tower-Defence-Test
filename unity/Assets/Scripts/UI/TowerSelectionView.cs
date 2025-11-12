using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TD.Gameplay.Towers;
using TD.Managers;

namespace TD.UI
{
    /// <summary>
    /// Presents tower build buttons and routes touch events to the placement controller.
    /// </summary>
    public class TowerSelectionView : MonoBehaviour
    {
        [Serializable]
        private class TowerButton
        {
            public string towerId;
            public Button button;
            public TMP_Text label;
            public TMP_Text costLabel;
        }

        [SerializeField] private TowerPlacementController placementController;
        [SerializeField] private Button cancelButton;
        [SerializeField] private List<TowerButton> towerButtons = new();

        private TowerManager towerManager;

        public void Initialize(TowerManager manager)
        {
            towerManager = manager;

            if (towerManager != null)
            {
                towerManager.CurrencyChanged += HandleCurrencyChanged;
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }

            foreach (var entry in towerButtons)
            {
                if (entry == null || entry.button == null)
                {
                    continue;
                }

                string towerId = entry.towerId;
                entry.button.onClick.RemoveAllListeners();
                entry.button.onClick.AddListener(() => HandleTowerSelected(towerId));

                UpdateLabels(entry);
            }

            HandleCurrencyChanged(towerManager != null ? towerManager.CurrentCurrency : 0);
        }

        private void HandleTowerSelected(string towerId)
        {
            if (placementController == null)
            {
                Debug.LogWarning("TowerSelectionView requires a TowerPlacementController reference.");
                return;
            }

            placementController.SelectTower(towerId);
        }

        private void HandleCancelClicked()
        {
            placementController?.CancelSelection();
        }

        private void HandleCurrencyChanged(int currency)
        {
            foreach (var entry in towerButtons)
            {
                if (entry == null || entry.button == null)
                {
                    continue;
                }

                int cost = GetTowerCost(entry.towerId);
                entry.button.interactable = currency >= cost && cost >= 0;

                if (entry.costLabel != null && cost >= 0)
                {
                    entry.costLabel.text = $"{cost}";
                }
            }
        }

        private void UpdateLabels(TowerButton entry)
        {
            if (entry.label != null)
            {
                entry.label.text = string.IsNullOrEmpty(entry.towerId) ? "Tower" : entry.towerId;
            }

            if (entry.costLabel != null)
            {
                int cost = GetTowerCost(entry.towerId);
                entry.costLabel.text = cost >= 0 ? cost.ToString() : "--";
            }
        }

        private int GetTowerCost(string towerId)
        {
            if (towerManager == null || string.IsNullOrEmpty(towerId))
            {
                return -1;
            }

            var definition = towerManager.GetTowerDefinition(towerId);
            return definition != null ? definition.BuildCost : -1;
        }

        private void OnDestroy()
        {
            if (towerManager != null)
            {
                towerManager.CurrencyChanged -= HandleCurrencyChanged;
            }
        }
    }
}
