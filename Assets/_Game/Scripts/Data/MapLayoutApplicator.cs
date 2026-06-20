using System.Collections.Generic;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Data
{
    public static class MapLayoutApplicator
    {
        public static void Apply(
            LevelMapLayout layout,
            BeltPath mainBeltPath,
            IReadOnlyList<BeltPath> queueBeltPaths,
            Transform boxQueueParent,
            BeltWaypointMarker waypointPrefab,
            Transform collectionPointMarker = null)
        {
            if (layout == null)
            {
                return;
            }

            ApplyPath(
                mainBeltPath,
                layout.MainWaypointLocalPositions,
                layout.IsMainPathClosedLoop,
                layout.MainCurveStrength,
                waypointPrefab);

            if (queueBeltPaths != null)
            {
                for (int i = 0; i < queueBeltPaths.Count; i++)
                {
                    BeltPath queueBeltPath = queueBeltPaths[i];

                    if (queueBeltPath == null)
                    {
                        continue;
                    }

                    if (!layout.TryGetQueuePath(queueBeltPath.PathId, out QueuePathLayout queueLayout))
                    {
                        ApplyPath(queueBeltPath, null, false, 1f, waypointPrefab);
                        continue;
                    }

                    ApplyPath(
                        queueBeltPath,
                        queueLayout.WaypointLocalPositions,
                        queueLayout.IsClosedLoop,
                        queueLayout.CurveStrength,
                        waypointPrefab);
                }
            }

            if (boxQueueParent != null)
            {
                boxQueueParent.localPosition = layout.BoxQueueLocalPosition;
            }

            if (collectionPointMarker != null)
            {
                collectionPointMarker.localPosition = layout.CollectionPointLocalPosition;
            }
        }

        public static void Capture(
            LevelMapLayout layout,
            BeltPath mainBeltPath,
            IReadOnlyList<BeltPath> queueBeltPaths,
            Transform boxQueueParent,
            Transform collectionPointMarker = null)
        {
            if (layout == null)
            {
                return;
            }

            CaptureMainPath(mainBeltPath, layout);
            CaptureQueuePaths(queueBeltPaths, layout);
            layout.SetBoxQueueLocalPosition(boxQueueParent != null ? boxQueueParent.localPosition : Vector3.zero);
            layout.SetCollectionPointLocalPosition(collectionPointMarker != null ? collectionPointMarker.localPosition : Vector3.zero);
        }

        private static void CaptureMainPath(BeltPath mainBeltPath, LevelMapLayout layout)
        {
            List<Vector3> localPositions = new List<Vector3>();

            if (mainBeltPath != null)
            {
                mainBeltPath.CaptureLocalWaypointPositions(localPositions);
                layout.SetMainPathData(localPositions, mainBeltPath.IsClosedLoop, mainBeltPath.CurveStrength);
                return;
            }

            layout.SetMainPathData(localPositions, true, 1f);
        }

        private static void CaptureQueuePaths(IReadOnlyList<BeltPath> queueBeltPaths, LevelMapLayout layout)
        {
            HashSet<string> validQueueIds = new HashSet<string>();

            if (queueBeltPaths != null)
            {
                for (int i = 0; i < queueBeltPaths.Count; i++)
                {
                    BeltPath queueBeltPath = queueBeltPaths[i];

                    if (queueBeltPath == null)
                    {
                        continue;
                    }

                    List<Vector3> localPositions = new List<Vector3>();
                    queueBeltPath.CaptureLocalWaypointPositions(localPositions);
                    string queueId = queueBeltPath.PathId;
                    validQueueIds.Add(queueId);
                    layout.SetQueuePathData(
                        queueId,
                        queueBeltPath.name,
                        localPositions,
                        queueBeltPath.IsClosedLoop,
                        queueBeltPath.CurveStrength);
                }
            }

            layout.RemoveMissingQueuePaths(validQueueIds);
        }

        private static void ApplyPath(
            BeltPath beltPath,
            IReadOnlyList<Vector3> localPositions,
            bool isClosedLoop,
            float curveStrength,
            BeltWaypointMarker waypointPrefab)
        {
            if (beltPath == null)
            {
                return;
            }

            beltPath.ApplyLocalWaypoints(localPositions, isClosedLoop, curveStrength, waypointPrefab);
        }
    }
}
