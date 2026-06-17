using FlowBlast.Domain;

namespace FlowBlast.Patterns.Strategy
{
    public interface IFrozenUnlockStrategy
    {
        bool CanSendToBelt(BoxModel box);
        void OnBoxBlasted(BoxModel blastedBox, BoxModel targetBox);
    }
}
