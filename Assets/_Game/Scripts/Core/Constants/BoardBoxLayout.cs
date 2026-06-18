using FlowBlast.Core.Enums;
using UnityEngine;

namespace FlowBlast.Core.Constants
{
    public static class BoardBoxLayout
    {
        public const int TestBoxCount = 5;
        public const int TestBoxColumns = 5;
        public const float DefaultTestBoxCellSpacing = 1.2f;
        public const int TestBoxCapacity = GameConstants.DefaultBoxCapacity;

        private static readonly BlockColor[] TestBoxColors =
        {
            BlockColor.Red,
            BlockColor.Green,
            BlockColor.Blue,
            BlockColor.Yellow,
            BlockColor.Purple
        };

        public static BlockColor GetTestBoxColor(int index)
        {
            if (index < 0)
            {
                return BlockColor.Red;
            }

            return TestBoxColors[index % TestBoxColors.Length];
        }

        public static Vector3 GetLocalSpawnPosition(int index, int columns, float cellSpacing)
        {
            int safeColumns = Mathf.Max(1, columns);
            int row = index / safeColumns;
            int column = index % safeColumns;
            float totalWidth = (safeColumns - 1) * cellSpacing;
            float startX = -totalWidth * 0.5f;

            return new Vector3(startX + column * cellSpacing, 0f, -row * cellSpacing);
        }
    }
}
