using System;
using System.Collections.Generic;
using UnityEngine;

namespace TD.Systems.Grid
{
    /// <summary>
    /// Central authority for translating between grid coordinates and world positions,
    /// and for tracking tile availability/occupancy.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 originPosition = Vector2.zero;
        [SerializeField] private bool initialiseBuildableTiles = true;
        [SerializeField] private List<Vector2Int> blockedTiles = new();
        [SerializeField] private Color debugBuildableColor = new(0f, 1f, 0f, 0.25f);
        [SerializeField] private Color debugBlockedColor = new(1f, 0f, 0f, 0.25f);
        [SerializeField] private bool drawDebugGizmos = true;

        private GridTile[,] tiles;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            InitialiseTiles();
        }

        private void InitialiseTiles()
        {
            tiles = new GridTile[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    tiles[x, y] = new GridTile
                    {
                        IsBuildable = initialiseBuildableTiles,
                        IsOccupied = false
                    };
                }
            }

            foreach (var tile in blockedTiles)
            {
                if (IsInBounds(tile))
                {
                    tiles[tile.x, tile.y].IsBuildable = false;
                }
            }
        }

        /// <summary>
        /// Converts a grid coordinate into the centre position in world space.
        /// </summary>
        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            var offset = new Vector2((gridPosition.x + 0.5f) * cellSize, (gridPosition.y + 0.5f) * cellSize);
            return new Vector3(originPosition.x + offset.x, originPosition.y + offset.y, 0f);
        }

        /// <summary>
        /// Converts world position into grid coordinates if within bounds.
        /// </summary>
        public bool TryGetGridPosition(Vector3 worldPosition, out Vector2Int gridPosition)
        {
            var relative = new Vector2(worldPosition.x - originPosition.x, worldPosition.y - originPosition.y);
            var x = Mathf.FloorToInt(relative.x / cellSize);
            var y = Mathf.FloorToInt(relative.y / cellSize);

            gridPosition = new Vector2Int(x, y);
            return IsInBounds(gridPosition);
        }

        public bool IsInBounds(Vector2Int gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.y >= 0 && gridPosition.x < width && gridPosition.y < height;
        }

        public void SetBuildable(Vector2Int gridPosition, bool buildable)
        {
            if (!IsInBounds(gridPosition))
            {
                throw new ArgumentOutOfRangeException(nameof(gridPosition), "Grid position is outside bounds.");
            }

            tiles[gridPosition.x, gridPosition.y].IsBuildable = buildable;
        }

        public bool IsBuildable(Vector2Int gridPosition)
        {
            if (!IsInBounds(gridPosition))
            {
                return false;
            }

            return tiles[gridPosition.x, gridPosition.y].IsBuildable;
        }

        public bool IsOccupied(Vector2Int gridPosition)
        {
            if (!IsInBounds(gridPosition))
            {
                return true;
            }

            return tiles[gridPosition.x, gridPosition.y].IsOccupied;
        }

        public bool CanPlaceStructure(Vector2Int gridPosition)
        {
            return IsBuildable(gridPosition) && !IsOccupied(gridPosition);
        }

        public bool TryReserveTile(Vector2Int gridPosition)
        {
            if (!CanPlaceStructure(gridPosition))
            {
                return false;
            }

            tiles[gridPosition.x, gridPosition.y].IsOccupied = true;
            return true;
        }

        public void ReleaseTile(Vector2Int gridPosition)
        {
            if (!IsInBounds(gridPosition))
            {
                return;
            }

            tiles[gridPosition.x, gridPosition.y].IsOccupied = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Gizmos.matrix = Matrix4x4.identity;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var gridPos = new Vector2Int(x, y);
                    var worldPos = GetWorldPosition(gridPos);
                    var halfSize = new Vector3(cellSize, cellSize, 0f) * 0.5f;
                    Gizmos.color = IsBuildable(gridPos) ? debugBuildableColor : debugBlockedColor;
                    Gizmos.DrawCube(worldPos, halfSize * 2f);
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(worldPos, halfSize * 2f);
                }
            }
        }

        [Serializable]
        private class GridTile
        {
            public bool IsBuildable;
            public bool IsOccupied;
        }
    }
}
