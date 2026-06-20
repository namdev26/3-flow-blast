#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Editor
{
    public sealed class MapLayoutEditState
    {
        private readonly List<Vector3> waypointLocalPositions = new List<Vector3>();
        private readonly CatmullRomPathSampler pathSampler = new CatmullRomPathSampler();

        public IReadOnlyList<Vector3> WaypointLocalPositions => waypointLocalPositions;
        public bool IsClosedLoop { get; set; } = true;
        public Vector3 BoxQueueLocalPosition { get; set; } = new Vector3(-2f, 0f, -3f);
        public Vector3 CollectionPointLocalPosition { get; set; } = new Vector3(0f, 0f, 3f);
        public float CurveStrength { get; set; } = 1f;
        public int SelectedWaypointIndex { get; set; } = -1;
        public string QueueId { get; private set; }
        public string DisplayName { get; private set; }

        public float PathLength
        {
            get
            {
                RebuildSampler();
                return pathSampler.TotalLength;
            }
        }

        public void Clear()
        {
            waypointLocalPositions.Clear();
            IsClosedLoop = true;
            BoxQueueLocalPosition = new Vector3(-2f, 0f, -3f);
            CollectionPointLocalPosition = new Vector3(0f, 0f, 3f);
            CurveStrength = 1f;
            SelectedWaypointIndex = -1;
            QueueId = null;
            DisplayName = null;
            pathSampler.Rebuild(waypointLocalPositions, IsClosedLoop, CurveStrength);
        }

        public void InitializeQueueIdentity(string queueId, string displayName)
        {
            QueueId = queueId;
            DisplayName = displayName;
        }

        public void LoadMainPath(LevelMapLayout layout)
        {
            Clear();

            if (layout == null)
            {
                return;
            }

            IsClosedLoop = layout.IsMainPathClosedLoop;
            CurveStrength = layout.MainCurveStrength;
            BoxQueueLocalPosition = layout.BoxQueueLocalPosition;
            CollectionPointLocalPosition = layout.CollectionPointLocalPosition;

            for (int i = 0; i < layout.MainWaypointLocalPositions.Count; i++)
            {
                waypointLocalPositions.Add(layout.MainWaypointLocalPositions[i]);
            }

            RebuildSampler();
        }

        public void LoadQueuePath(LevelMapLayout layout, string queueId, string displayName)
        {
            Clear();
            InitializeQueueIdentity(queueId, displayName);

            if (layout == null)
            {
                return;
            }

            BoxQueueLocalPosition = layout.BoxQueueLocalPosition;
            CollectionPointLocalPosition = layout.CollectionPointLocalPosition;

            if (!layout.TryGetQueuePath(queueId, out QueuePathLayout queueLayout))
            {
                return;
            }

            IsClosedLoop = queueLayout.IsClosedLoop;
            CurveStrength = queueLayout.CurveStrength;

            for (int i = 0; i < queueLayout.WaypointLocalPositions.Count; i++)
            {
                waypointLocalPositions.Add(queueLayout.WaypointLocalPositions[i]);
            }

            RebuildSampler();
        }

        public void WriteMainPathTo(LevelMapLayout layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.SetMainPathData(waypointLocalPositions, IsClosedLoop, CurveStrength);
            layout.SetBoxQueueLocalPosition(BoxQueueLocalPosition);
            layout.SetCollectionPointLocalPosition(CollectionPointLocalPosition);
        }

        public void WriteQueuePathTo(LevelMapLayout layout)
        {
            if (layout == null || string.IsNullOrWhiteSpace(QueueId))
            {
                return;
            }

            layout.SetQueuePathData(QueueId, DisplayName, waypointLocalPositions, IsClosedLoop, CurveStrength);
            layout.SetBoxQueueLocalPosition(BoxQueueLocalPosition);
            layout.SetCollectionPointLocalPosition(CollectionPointLocalPosition);
        }

        public void LoadFromScene(BeltPath beltPath, Transform boxQueueParent, Transform collectionPointMarker)
        {
            waypointLocalPositions.Clear();
            SelectedWaypointIndex = -1;

            if (beltPath == null)
            {
                return;
            }

            beltPath.CaptureLocalWaypointPositions(waypointLocalPositions);
            IsClosedLoop = beltPath.IsClosedLoop;
            CurveStrength = beltPath.CurveStrength;
            BoxQueueLocalPosition = boxQueueParent != null
                ? boxQueueParent.localPosition
                : Vector3.zero;
            CollectionPointLocalPosition = collectionPointMarker != null
                ? collectionPointMarker.localPosition
                : Vector3.zero;
            QueueId = beltPath.PathRole == BeltPathRole.Queue ? beltPath.PathId : null;
            DisplayName = beltPath.PathRole == BeltPathRole.Queue ? beltPath.name : null;

            RebuildSampler();
        }

        public void ApplyPreset(Vector3[] localPositions, bool closedLoop)
        {
            waypointLocalPositions.Clear();

            if (localPositions != null)
            {
                for (int i = 0; i < localPositions.Length; i++)
                {
                    waypointLocalPositions.Add(localPositions[i]);
                }
            }

            IsClosedLoop = closedLoop;
            SelectedWaypointIndex = -1;
            RebuildSampler();
        }

        public void AddWaypoint(Vector3 localPosition)
        {
            waypointLocalPositions.Add(localPosition);
            SelectedWaypointIndex = waypointLocalPositions.Count - 1;
            RebuildSampler();
        }

        public void RemoveWaypoint(int index)
        {
            if (index < 0 || index >= waypointLocalPositions.Count)
            {
                return;
            }

            waypointLocalPositions.RemoveAt(index);

            if (SelectedWaypointIndex >= waypointLocalPositions.Count)
            {
                SelectedWaypointIndex = waypointLocalPositions.Count - 1;
            }

            RebuildSampler();
        }

        public void SetWaypointPosition(int index, Vector3 localPosition)
        {
            if (index < 0 || index >= waypointLocalPositions.Count)
            {
                return;
            }

            waypointLocalPositions[index] = localPosition;
            RebuildSampler();
        }

        public void SetBoxQueuePosition(Vector3 localPosition)
        {
            BoxQueueLocalPosition = localPosition;
        }

        public void SetCollectionPointPosition(Vector3 localPosition)
        {
            CollectionPointLocalPosition = localPosition;
        }

        public void SwapWaypoints(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= waypointLocalPositions.Count)
            {
                return;
            }

            if (targetIndex < 0 || targetIndex >= waypointLocalPositions.Count)
            {
                return;
            }

            Vector3 temp = waypointLocalPositions[sourceIndex];
            waypointLocalPositions[sourceIndex] = waypointLocalPositions[targetIndex];
            waypointLocalPositions[targetIndex] = temp;
            SelectedWaypointIndex = targetIndex;
            RebuildSampler();
        }

        public int GetBlockCapacity(MapEditorSettings settings)
        {
            if (settings == null)
            {
                return 0;
            }

            RebuildSampler();
            return BeltLaneLayout.GetTotalBlockCapacity(
                pathSampler.TotalLength,
                settings.LaneSpacing,
                settings.LaneCount);
        }

        public IReadOnlyList<Vector3> GetSampledPath()
        {
            RebuildSampler();
            return pathSampler.GetSampledPositions();
        }

        private void RebuildSampler()
        {
            pathSampler.Rebuild(waypointLocalPositions, IsClosedLoop, CurveStrength);
        }
    }
}
#endif
