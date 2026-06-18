using FlowBlast.Core.Enums;
using FlowBlast.Data;

namespace FlowBlast.Domain
{
    public sealed class BlockModel
    {
        public int Id { get; }
        public BlockColor Color { get; }
        public BoxVisualProfile VisualProfile { get; }

        public BlockModel(int id, BlockColor color, BoxVisualProfile visualProfile)
        {
            Id = id;
            Color = color;
            VisualProfile = visualProfile;
        }
    }
}
