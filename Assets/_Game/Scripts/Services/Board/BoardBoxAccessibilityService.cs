using System.Collections.Generic;
using FlowBlast.Domain;
using UnityEngine;

namespace FlowBlast.Services.Board
{
    public sealed class BoardBoxAccessibilityService
    {
        private const float MinimumCellSpacing = 0.01f;

        private readonly Dictionary<int, Vector2Int> boxCellsById = new Dictionary<int, Vector2Int>();
        private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        private readonly float cellSpacing;

        private int minColumn = int.MaxValue;
        private int maxColumn = int.MinValue;
        private int minRow = int.MaxValue;

        public BoardBoxAccessibilityService(float cellSpacing)
        {
            this.cellSpacing = Mathf.Max(MinimumCellSpacing, Mathf.Abs(cellSpacing));
        }

        public void Register(BoxModel box, Vector3 localPosition)
        {
            if (box == null)
            {
                return;
            }

            Vector2Int cell = ToCell(localPosition);
            UpdateBounds(cell);

            if (boxCellsById.TryGetValue(box.Id, out Vector2Int previousCell))
            {
                occupiedCells.Remove(previousCell);
            }

            boxCellsById[box.Id] = cell;
            occupiedCells.Add(cell);
        }

        public bool CanSelect(BoxModel box)
        {
            if (box == null)
            {
                return false;
            }

            if (!boxCellsById.TryGetValue(box.Id, out Vector2Int cell))
            {
                return true;
            }

            return HasOpenNeighbor(cell);
        }

        public void Remove(BoxModel box)
        {
            if (box == null)
            {
                return;
            }

            if (!boxCellsById.TryGetValue(box.Id, out Vector2Int cell))
            {
                return;
            }

            boxCellsById.Remove(box.Id);
            occupiedCells.Remove(cell);
        }

        public void Clear()
        {
            boxCellsById.Clear();
            occupiedCells.Clear();
            minColumn = int.MaxValue;
            maxColumn = int.MinValue;
            minRow = int.MaxValue;
        }

        private bool HasOpenNeighbor(Vector2Int cell)
        {
            if (!occupiedCells.Contains(cell))
            {
                return false;
            }

            return IsOpenTop(cell)
                || IsOpenBottom(cell)
                || IsOpenLeft(cell)
                || IsOpenRight(cell);
        }

        private bool IsOpenTop(Vector2Int cell)
        {
            return !occupiedCells.Contains(cell + Vector2Int.up);
        }

        private bool IsOpenBottom(Vector2Int cell)
        {
            if (cell.y <= minRow)
            {
                return false;
            }

            return !occupiedCells.Contains(cell + Vector2Int.down);
        }

        private bool IsOpenLeft(Vector2Int cell)
        {
            if (cell.x <= minColumn)
            {
                return false;
            }

            return !occupiedCells.Contains(cell + Vector2Int.left);
        }

        private bool IsOpenRight(Vector2Int cell)
        {
            if (cell.x >= maxColumn)
            {
                return false;
            }

            return !occupiedCells.Contains(cell + Vector2Int.right);
        }

        private void UpdateBounds(Vector2Int cell)
        {
            minColumn = Mathf.Min(minColumn, cell.x);
            maxColumn = Mathf.Max(maxColumn, cell.x);
            minRow = Mathf.Min(minRow, cell.y);
        }

        private Vector2Int ToCell(Vector3 localPosition)
        {
            int x = Mathf.RoundToInt(localPosition.x / cellSpacing);
            int y = Mathf.RoundToInt(localPosition.z / cellSpacing);
            return new Vector2Int(x, y);
        }
    }
}
