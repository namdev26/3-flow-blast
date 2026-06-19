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
        public float CurveStrength { get; set; } = 1f;
        public int SelectedWaypointIndex { get; set; } = -1;

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
            CurveStrength = 1f;
            SelectedWaypointIndex = -1;
            pathSampler.Rebuild(waypointLocalPositions, IsClosedLoop, CurveStrength);
        }

        public void LoadFrom(LevelMapLayout layout)
        {
            Clear();

            if (layout == null)
            {
                return;
            }

            IsClosedLoop = layout.IsClosedLoop;
            BoxQueueLocalPosition = layout.BoxQueueLocalPosition;
            CurveStrength = layout.CurveStrength;

            for (int i = 0; i < layout.WaypointLocalPositions.Count; i++)
            {
                waypointLocalPositions.Add(layout.WaypointLocalPositions[i]);
            }

            RebuildSampler();
        }

        public void WriteTo(LevelMapLayout layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.SetLayoutData(waypointLocalPositions, IsClosedLoop, BoxQueueLocalPosition, CurveStrength);
        }

        public void LoadFromScene(BeltPath beltPath, Transform boxQueueParent)
        {
            Clear();

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
