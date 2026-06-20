using System.Collections.Generic;
using FlowBlast.Data;

namespace FlowBlast.Bootstrap
{
    public static class GameplayProgressionState
    {
        private static readonly List<LevelData> LevelSequence = new List<LevelData>();

        public static LevelData ActiveLevelData { get; set; }

        public static void SetLevelSequence(IReadOnlyList<LevelData> levels)
        {
            LevelSequence.Clear();

            if (levels == null)
            {
                return;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null)
                {
                    LevelSequence.Add(levels[i]);
                }
            }
        }

        public static LevelData GetNextLevel(LevelData currentLevel)
        {
            if (LevelSequence.Count == 0)
            {
                return currentLevel;
            }

            if (currentLevel == null)
            {
                return LevelSequence[0];
            }

            int currentIndex = LevelSequence.IndexOf(currentLevel);

            if (currentIndex < 0)
            {
                return LevelSequence[0];
            }

            int nextIndex = currentIndex + 1 < LevelSequence.Count
                ? currentIndex + 1
                : currentIndex;
            return LevelSequence[nextIndex];
        }

        public static void Clear()
        {
            ActiveLevelData = null;
            LevelSequence.Clear();
        }
    }
}
