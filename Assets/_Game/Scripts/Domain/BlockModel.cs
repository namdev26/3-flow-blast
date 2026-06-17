using FlowBlast.Core.Enums;

namespace FlowBlast.Domain
{
    public sealed class BlockModel
    {
        public int Id { get; }
        public BlockColor Color { get; }

        public BlockModel(int id, BlockColor color)
        {
            Id = id;
            Color = color;
        }
    }
}
