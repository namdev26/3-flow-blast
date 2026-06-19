#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public static class MapEditorSceneDrawer
    {
        public static void DrawGrid(MapEditorSettings settings, Transform origin)
        {
            if (settings == null || !settings.ShowGrid)
            {
                return;
            }

            float cellSize = settings.GridCellSize;
            int extent = settings.GridExtentCells;
            Vector3 center = origin != null ? origin.position : Vector3.zero;
            float planeY = center.y;
            Handles.color = settings.GridColor;

            for (int x = -extent; x <= extent; x++)
            {
                float offsetX = x * cellSize;
                Vector3 start = new Vector3(center.x + offsetX, planeY, center.z - extent * cellSize);
                Vector3 end = new Vector3(center.x + offsetX, planeY, center.z + extent * cellSize);
                Handles.DrawLine(start, end);
            }

            for (int z = -extent; z <= extent; z++)
            {
                float offsetZ = z * cellSize;
                Vector3 start = new Vector3(center.x - extent * cellSize, planeY, center.z + offsetZ);
                Vector3 end = new Vector3(center.x + extent * cellSize, planeY, center.z + offsetZ);
                Handles.DrawLine(start, end);
            }
        }

        public static void DrawBeltPath(
            BeltPath beltPath,
            MapEditorSettings settings,
            bool allowInteraction,
            ref int selectedWaypointIndex)
        {
            if (beltPath == null || settings == null)
            {
                return;
            }

            CatmullRomPathSampler sampler = BeltPathEditorUtility.BuildPreviewSampler(beltPath);
            IReadOnlyList<Vector3> samples = sampler.GetSampledPositions();

            Handles.color = beltPath.PathRole == BeltPathRole.Queue
                ? settings.QueuePathColor
                : settings.MainPathColor;

            for (int i = 1; i < samples.Count; i++)
            {
                Handles.DrawLine(samples[i - 1], samples[i], 3f);
            }

            if (beltPath.IsClosedLoop && samples.Count > 2)
            {
                Handles.DrawLine(samples[samples.Count - 1], samples[0], 3f);
            }

            if (settings.ShowLanePreview && samples.Count >= 2)
            {
                DrawLanePreview(sampler, settings);
            }

            DrawWaypoints(beltPath, settings, allowInteraction, ref selectedWaypointIndex);
        }

        public static void DrawBoxQueue(Transform boxQueueParent, MapEditorSettings settings)
        {
            if (boxQueueParent == null || settings == null)
            {
                return;
            }

            Vector3 center = boxQueueParent.position;
            float size = settings.GridCellSize * 2f;
            Handles.color = settings.BoxQueueColor;
            Handles.DrawSolidDisc(center, Vector3.up, size * 0.5f);
            Handles.color = new Color(settings.BoxQueueColor.r, settings.BoxQueueColor.g, settings.BoxQueueColor.b, 1f);
            Handles.DrawWireDisc(center, Vector3.up, size * 0.5f);
            Handles.Label(center + Vector3.up * 0.2f, "Box Queue");
        }

        public static bool TryGetGroundClickPosition(Event currentEvent, float planeY, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (currentEvent.button != 0 || currentEvent.alt)
            {
                return false;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

            if (!groundPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            return true;
        }

        private static void DrawLanePreview(CatmullRomPathSampler sampler, MapEditorSettings settings)
        {
            float pathLength = sampler.TotalLength;

            if (pathLength <= Mathf.Epsilon)
            {
                return;
            }

            int laneCount = settings.LaneCount;
            float laneSpacing = settings.LaneSpacing;
            int previewSteps = Mathf.Max(8, Mathf.FloorToInt(pathLength / settings.GridCellSize));
            Handles.color = settings.LanePreviewColor;

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                Vector3 previousPoint = Vector3.zero;
                bool hasPrevious = false;

                for (int step = 0; step <= previewSteps; step++)
                {
                    float distance = pathLength * step / previewSteps;
                    Vector3 center = sampler.GetPositionAtDistance(distance);
                    Quaternion rotation = sampler.GetRotationAtDistance(distance);
                    Vector3 lanePoint = BeltLaneLayout.GetLanePosition(
                        center,
                        rotation,
                        laneIndex,
                        laneCount,
                        laneSpacing);

                    if (hasPrevious)
                    {
                        Handles.DrawLine(previousPoint, lanePoint);
                    }

                    previousPoint = lanePoint;
                    hasPrevious = true;
                }
            }
        }

        private static void DrawWaypoints(
            BeltPath beltPath,
            MapEditorSettings settings,
            bool allowInteraction,
            ref int selectedWaypointIndex)
        {
            Transform[] waypoints = beltPath.Waypoints;

            if (waypoints == null)
            {
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform waypoint = waypoints[i];

                if (waypoint == null)
                {
                    continue;
                }

                Vector3 position = waypoint.position;
                float handleSize = HandleUtility.GetHandleSize(position) * settings.WaypointHandleSize;
                Handles.color = i == selectedWaypointIndex
                    ? Color.white
                    : beltPath.PathRole == BeltPathRole.Queue
                        ? settings.QueueWaypointColor
                        : settings.MainWaypointColor;
                Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
                Handles.Label(position + Vector3.up * handleSize * 1.5f, $"WP {i}");

                if (!allowInteraction)
                {
                    continue;
                }

                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(position, Quaternion.identity);

                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                Undo.RecordObject(waypoint, "Move Belt Waypoint");
                Vector3 localPosition = beltPath.transform.InverseTransformPoint(newPosition);

                if (settings.SnapToGrid)
                {
                    localPosition = BeltPathEditorUtility.SnapLocalPosition(localPosition, settings.GridCellSize);
                }

                waypoint.localPosition = localPosition;
                selectedWaypointIndex = i;
                EditorUtility.SetDirty(beltPath);
            }
        }
    }
}
#endif
