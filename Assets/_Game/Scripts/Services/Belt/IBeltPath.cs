using FlowBlast.Domain;

namespace FlowBlast.Services.Belt
{
    public interface IBeltPath
    {
        float TotalLength { get; }
        UnityEngine.Vector3 GetPositionAtDistance(float distance);
        UnityEngine.Quaternion GetRotationAtDistance(float distance);
        float NormalizeDistance(float distance);
        float GetClosestDistance(UnityEngine.Vector3 worldPosition);
    }
}
