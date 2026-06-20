using System.Collections.Generic;
using UnityEngine;

namespace FlowBlast.Services.Block
{
    public static class QueuePathRowDistributionUtility
    {
        public static void BuildBalancedRowAllocations(
            IReadOnlyList<int> rowCapacities,
            int totalRows,
            int rowsPerChunk,
            List<int> allocations)
        {
            if (allocations == null)
            {
                return;
            }

            allocations.Clear();
            int pathCount = rowCapacities != null ? rowCapacities.Count : 0;

            for (int i = 0; i < pathCount; i++)
            {
                allocations.Add(0);
            }

            if (pathCount == 0 || totalRows <= 0)
            {
                return;
            }

            int safeRowsPerChunk = Mathf.Max(1, rowsPerChunk);
            int remainingRows = totalRows;

            while (remainingRows > 0)
            {
                bool assignedAnyRow = false;

                for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
                {
                    int remainingCapacity = Mathf.Max(0, rowCapacities[pathIndex] - allocations[pathIndex]);

                    if (remainingCapacity <= 0)
                    {
                        continue;
                    }

                    int rowsToAssign = Mathf.Min(safeRowsPerChunk, Mathf.Min(remainingCapacity, remainingRows));

                    if (rowsToAssign <= 0)
                    {
                        continue;
                    }

                    allocations[pathIndex] += rowsToAssign;
                    remainingRows -= rowsToAssign;
                    assignedAnyRow = true;

                    if (remainingRows <= 0)
                    {
                        break;
                    }
                }

                if (!assignedAnyRow)
                {
                    break;
                }
            }
        }
    }
}
