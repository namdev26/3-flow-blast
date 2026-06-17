using FlowBlast.Data;

namespace FlowBlast.Services.Level
{
    public interface ILevelRepository
    {
        LevelData GetLevel(string levelId);
    }
}
