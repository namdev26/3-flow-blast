using System.Collections.Generic;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "LevelMapLayout", menuName = "FlowBlast/Level Map Layout")]
    public sealed class LevelMapLayout : ScriptableObject
    {
        [SerializeField] private string layoutId = "MapLayout_01";
        [SerializeField] private bool isClosedLoop = true;
        [SerializeField] private List<Vector3> waypointLocalPositions = new List<Vector3>();
        [SerializeField] private Vector3 boxQueueLocalPosition = new Vector3(-2f, 0f, -3f);

        public string LayoutId => layoutId;
        public bool IsClosedLoop => isClosedLoop;
        public IReadOnlyList<Vector3> WaypointLocalPositions => waypointLocalPositions;
        public Vector3 BoxQueueLocalPosition => boxQueueLocalPosition;

        public void SetLayoutData(
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            Vector3 queueLocalPosition)
        {
            waypointLocalPositions.Clear();

            for (int i = 0; i < localPositions.Count; i++)
            {
                waypointLocalPositions.Add(localPositions[i]);
            }

            isClosedLoop = closedLoop;
            boxQueueLocalPosition = queueLocalPosition;
        }

        public void ApplyTo(BeltPath beltPath, Transform boxQueueParent, BeltWaypointMarker waypointPrefab)
        {
            MapLayoutApplicator.Apply(this, beltPath, boxQueueParent, waypointPrefab);
        }

        public void CaptureFrom(BeltPath beltPath, Transform boxQueueParent)
        {
            MapLayoutApplicator.Capture(this, beltPath, boxQueueParent);
        }
    }
}
