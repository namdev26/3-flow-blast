using System.Collections.Generic;
using UnityEngine;

namespace FlowBlast.Data
{
    [System.Serializable]
    public sealed class QueuePathLayout
    {
        [SerializeField] private string queueId = "Queue_01";
        [SerializeField] private string displayName = "Queue 01";
        [SerializeField] private bool isClosedLoop;
        [SerializeField] private List<Vector3> waypointLocalPositions = new List<Vector3>();
        [SerializeField] private float curveStrength = 1f;

        public string QueueId => queueId;
        public string DisplayName => displayName;
        public bool IsClosedLoop => isClosedLoop;
        public IReadOnlyList<Vector3> WaypointLocalPositions => waypointLocalPositions;
        public float CurveStrength => curveStrength;

        public void SetIdentity(string id, string name)
        {
            queueId = string.IsNullOrWhiteSpace(id) ? "Queue" : id;
            displayName = string.IsNullOrWhiteSpace(name) ? queueId : name;
        }

        public void SetPathData(
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            float strength)
        {
            waypointLocalPositions.Clear();

            if (localPositions != null)
            {
                for (int i = 0; i < localPositions.Count; i++)
                {
                    waypointLocalPositions.Add(localPositions[i]);
                }
            }

            bool canUseClosedLoop = waypointLocalPositions.Count >= 3;
            isClosedLoop = canUseClosedLoop && closedLoop;
            curveStrength = Mathf.Clamp01(strength);
        }
    }
}
