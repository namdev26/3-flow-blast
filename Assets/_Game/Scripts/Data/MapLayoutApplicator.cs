using System.Collections.Generic;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Data
{
    public static class MapLayoutApplicator
    {
        public static void Apply(
            LevelMapLayout layout,
            BeltPath beltPath,
            Transform boxQueueParent,
            BeltWaypointMarker waypointPrefab)
        {
            if (layout == null || beltPath == null)
            {
                return;
            }

            beltPath.ApplyLocalWaypoints(
                layout.WaypointLocalPositions,
                layout.IsClosedLoop,
                layout.CurveStrength,
                waypointPrefab);

            if (boxQueueParent != null)
            {
                boxQueueParent.localPosition = layout.BoxQueueLocalPosition;
            }
        }

        public static void Capture(
            LevelMapLayout layout,
            BeltPath beltPath,
            Transform boxQueueParent)
        {
            if (layout == null || beltPath == null)
            {
                return;
            }

            List<Vector3> localPositions = new List<Vector3>();
            beltPath.CaptureLocalWaypointPositions(localPositions);

            Vector3 queueLocalPosition = boxQueueParent != null
                ? boxQueueParent.localPosition
                : Vector3.zero;

            layout.SetLayoutData(localPositions, beltPath.IsClosedLoop, queueLocalPosition, beltPath.CurveStrength);
        }
    }
}
