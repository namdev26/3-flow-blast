using FlowBlast.Data;
using UnityEngine;

namespace FlowBlast.Services.Level
{
    public sealed class LevelRepository : ILevelRepository
    {
        private readonly LevelData defaultLevel;

        public LevelRepository(LevelData defaultLevel)
        {
            this.defaultLevel = defaultLevel;
        }

        public LevelData GetLevel(string levelId)
        {
            if (defaultLevel != null && defaultLevel.LevelId == levelId)
            {
                return defaultLevel;
            }

            return defaultLevel;
        }
    }
}
