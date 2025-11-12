using UnityEngine;
using TD.Managers;
using TD.Systems.Grid;

namespace TD.Gameplay.Towers
{
    /// <summary>
    /// Handles user input to place towers on valid grid tiles using touch or mouse input.
    /// </summary>
    public class TowerPlacementController : MonoBehaviour
    {
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TowerPlacementPreview placementPreviewPrefab;
        [SerializeField] private Transform previewParent;

        private TowerPlacementPreview previewInstance;
        private TowerDefinition selectedDefinition;
        private bool hasValidPosition;

        private void Awake()
        {
            EnsurePreviewInstance();
            HidePreview();
        }

        private void Update()
        {
            if (selectedDefinition == null)
            {
                HidePreview();
                return;
            }

            if (TryGetPointerWorldPosition(out var worldPosition, out var pointerReleased))
            {
                if (gridManager.TryGetGridPosition(worldPosition, out var gridPosition))
                {
                    var snapped = gridManager.GetWorldPosition(gridPosition);
                    hasValidPosition = towerManager.CanPlaceTower(selectedDefinition.TowerId, gridPosition);

                    previewInstance.SetVisibility(true);
                    previewInstance.SetPosition(snapped);
                    previewInstance.SetPlacementValidity(hasValidPosition);

                    if (pointerReleased && hasValidPosition)
                    {
                        if (towerManager.TryPlaceTower(selectedDefinition.TowerId, gridPosition, out _))
                        {
                            // Keep selection active to allow rapid placement until the player cancels.
                            hasValidPosition = false;
                        }
                    }
                }
                else
                {
                    ShowInvalidPreview(worldPosition);
                }
            }
            else
            {
                previewInstance.SetVisibility(false);
            }
        }

        /// <summary>
        /// Called by UI buttons to select the tower that should be placed next.
        /// </summary>
        public void SelectTower(string towerId)
        {
            selectedDefinition = towerManager.GetTowerDefinition(towerId);
            if (selectedDefinition == null)
            {
                Debug.LogWarning($"Cannot select tower '{towerId}' – definition not found.");
                HidePreview();
                return;
            }

            EnsurePreviewInstance();
            previewInstance.SetVisibility(true);
            previewInstance.SetPlacementValidity(false);
        }

        public void CancelSelection()
        {
            selectedDefinition = null;
            HidePreview();
        }

        private void ShowInvalidPreview(Vector3 worldPosition)
        {
            EnsurePreviewInstance();
            previewInstance.SetVisibility(true);
            previewInstance.SetPlacementValidity(false);
            previewInstance.SetPosition(worldPosition);
            hasValidPosition = false;
        }

        private void HidePreview()
        {
            if (previewInstance != null)
            {
                previewInstance.SetVisibility(false);
            }

            hasValidPosition = false;
        }

        private void EnsurePreviewInstance()
        {
            if (previewInstance != null || placementPreviewPrefab == null)
            {
                return;
            }

            var parent = previewParent != null ? previewParent : transform;
            previewInstance = Instantiate(placementPreviewPrefab, parent);
            previewInstance.SetVisibility(false);
        }

        private bool TryGetPointerWorldPosition(out Vector3 worldPosition, out bool pointerReleased)
        {
            pointerReleased = false;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Canceled)
                {
                    worldPosition = default;
                    return false;
                }

                pointerReleased = touch.phase == TouchPhase.Ended;
                worldPosition = ScreenToWorld(touch.position);
                return true;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                pointerReleased = Input.GetMouseButtonUp(0);
                worldPosition = ScreenToWorld(Input.mousePosition);
                return true;
            }

            worldPosition = default;
            return false;
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                Debug.LogError("TowerPlacementController requires a Camera reference.");
                return Vector3.zero;
            }

            var ray = worldCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.forward, Vector3.zero);

            if (plane.Raycast(ray, out var distance))
            {
                var worldPoint = ray.GetPoint(distance);
                worldPoint.z = 0f;
                return worldPoint;
            }

            // Fallback for orthographic cameras looking along the forward axis.
            var fallback = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y,
                Mathf.Abs(worldCamera.transform.position.z)));
            fallback.z = 0f;
            return fallback;
        }
    }
}
