using System.Collections.Generic;
using FlowBlast.Data;
using UnityEngine;

namespace FlowBlast.Services.Level
{
    public static class LevelBlockSequenceBuilder
    {
        public static List<LevelBlockSequenceItem> BuildItemsFromPlacements(
            IReadOnlyList<LevelBoxPlacement> boxPlacements,
            int laneCount,
            int boxCapacity)
        {
            List<LevelBlockSequenceItem> items = new List<LevelBlockSequenceItem>();

            if (boxPlacements == null || boxPlacements.Count == 0)
            {
                return items;
            }

            int safeLaneCount = Mathf.Max(1, laneCount);
            int safeBoxCapacity = Mathf.Max(1, boxCapacity);
            int itemCountPerPlacement = safeBoxCapacity;

            for (int placementIndex = 0; placementIndex < boxPlacements.Count; placementIndex++)
            {
                LevelBoxPlacement placement = boxPlacements[placementIndex];

                for (int itemIndex = 0; itemIndex < itemCountPerPlacement; itemIndex++)
                {
                    LevelBlockSequenceItem item = new LevelBlockSequenceItem
                    {
                        VisualProfile = placement.VisualProfile
                    };
                    items.Add(item);
                }
            }

            return items;
        }

        public static List<LevelBlockSpawnRow> BuildRowsFromItems(
            IReadOnlyList<LevelBlockSequenceItem> sequenceItems,
            int laneCount)
        {
            List<LevelBlockSpawnRow> rows = new List<LevelBlockSpawnRow>();

            if (sequenceItems == null || sequenceItems.Count == 0)
            {
                return rows;
            }

            int safeLaneCount = Mathf.Max(1, laneCount);
            LevelBlockSpawnRow currentRow = null;

            for (int itemIndex = 0; itemIndex < sequenceItems.Count; itemIndex++)
            {
                int laneIndex = itemIndex % safeLaneCount;

                if (laneIndex == 0)
                {
                    currentRow = new LevelBlockSpawnRow();
                    currentRow.EnsureLaneCount(safeLaneCount);
                    rows.Add(currentRow);
                }

                LevelBlockSequenceItem item = sequenceItems[itemIndex];
                currentRow.SetLaneProfile(laneIndex, item != null ? item.VisualProfile : null);
            }

            return rows;
        }
    }
}
