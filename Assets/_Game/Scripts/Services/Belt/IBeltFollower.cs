namespace FlowBlast.Services.Belt
{
    public interface IBeltFollower
    {
        float BeltDistance { get; }
        void SetBeltDistance(float distance);
        bool IsActiveOnBelt { get; }
    }
}
