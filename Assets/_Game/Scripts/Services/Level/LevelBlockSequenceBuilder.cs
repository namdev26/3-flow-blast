using System.Collections.Generic;
using FlowBlast.Data;
using UnityEngine;

namespace FlowBlast.Services.Level
{
    public static class LevelBlockSequenceBuilder
    {
        public static List<BoxVisualProfile> BuildFromPlacements(IReadOnlyList<LevelBoxPlacement> boxPlacements, int laneCount)
        {
            List<BoxVisualProfile> sequence = new List<BoxVisualProfile>();

            if (boxPlacements == null || boxPlacements.Count == 0)
            {
                return sequence;
            }

            int safeLaneCount = Mathf.Max(1, laneCount);

            for (int i = 0; i < boxPlacements.Count; i++)
            {
                LevelBoxPlacement placement = boxPlacements[i];
                int rowCount = Mathf.CeilToInt((float)Mathf.Max(1, placement.Capacity) / safeLaneCount);

                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    sequence.Add(placement.VisualProfile);
                }
            }

            return sequence;
        }
    }
}
