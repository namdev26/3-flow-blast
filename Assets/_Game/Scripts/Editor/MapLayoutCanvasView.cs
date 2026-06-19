#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public static class MapLayoutCanvasView
    {
        private const float MinZoom = 8f;
        private const float MaxZoom = 80f;
        private const float WaypointHitRadius = 12f;
        private const float QueueHitRadius = 14f;
        private const float VisibleAreaMinX = -4f;
        private const float VisibleAreaMaxX = 4f;
        private const float VisibleAreaMinZ = 0f;
        private const float VisibleAreaMaxZ = 4f;
        private static readonly Color VisibleAreaFillColor = new Color(0.2f, 0.7f, 1f, 0.08f);
        private static readonly Color VisibleAreaOutlineColor = new Color(0.2f, 0.7f, 1f, 0.85f);

        private static readonly int CanvasControlId = "MapLayoutCanvas".GetHashCode();

        public static void Draw(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            ref float zoom,
            ref Vector2 pan,
            ref bool isBoxQueueSelected)
        {
            if (editState == null)
            {
                return;
            }

            MapEditorSettings activeSettings = settings ?? BeltPathEditorUtility.LoadSettings();
            DrawBackground(rect);
            HandleInput(rect, editState, activeSettings, ref zoom, ref pan, ref isBoxQueueSelected);
            DrawContent(rect, editState, activeSettings, zoom, pan, isBoxQueueSelected);

            if (Event.current.type == EventType.Repaint)
            {
                DrawHelpOverlay(rect);
            }
        }

        private static void DrawBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.13f, 0.15f));

            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = new Color(1f, 1f, 1f, 0.08f);
                Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin));
                Handles.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax, rect.yMax));
                Handles.EndGUI();
            }
        }

        private static void DrawContent(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan,
            bool isBoxQueueSelected)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Handles.BeginGUI();
            DrawGrid(rect, settings, zoom, pan);
            DrawVisibleArea(rect, zoom, pan);

            IReadOnlyList<Vector3> samples = editState.GetSampledPath();

            if (samples.Count >= 2)
            {
                DrawPath(rect, samples, settings.PathColor, zoom, pan);

                if (settings.ShowLanePreview)
                {
                    DrawLanePreview(rect, editState, settings, zoom, pan);
                }
            }

            DrawBoxQueue(rect, editState.BoxQueueLocalPosition, settings, zoom, pan, isBoxQueueSelected);
            DrawWaypoints(rect, editState, settings, zoom, pan);
            Handles.EndGUI();
        }

        private static void DrawGrid(Rect rect, MapEditorSettings settings, float zoom, Vector2 pan)
        {
            if (!settings.ShowGrid)
            {
                return;
            }

            float cellSize = settings.GridCellSize;
            int extent = settings.GridExtentCells;
            Handles.color = settings.GridColor;

            float worldCenterX = -pan.x;
            float worldCenterZ = -pan.y;

            int minX = Mathf.Max(Mathf.FloorToInt(worldCenterX - rect.width * 0.5f / zoom / cellSize) - 1, -extent);
            int maxX = Mathf.Min(Mathf.CeilToInt(worldCenterX + rect.width * 0.5f / zoom / cellSize) + 1, extent);
            int minZ = Mathf.Max(Mathf.FloorToInt(worldCenterZ - rect.height * 0.5f / zoom / cellSize) - 1, -extent);
            int maxZ = Mathf.Min(Mathf.CeilToInt(worldCenterZ + rect.height * 0.5f / zoom / cellSize) + 1, extent);

            for (int x = minX; x <= maxX; x++)
            {
                Vector2 start = WorldToCanvas(new Vector3(x * cellSize, 0f, minZ * cellSize), rect, zoom, pan);
                Vector2 end = WorldToCanvas(new Vector3(x * cellSize, 0f, maxZ * cellSize), rect, zoom, pan);
                Handles.DrawLine(start, end);
            }

            for (int z = minZ; z <= maxZ; z++)
            {
                Vector2 start = WorldToCanvas(new Vector3(minX * cellSize, 0f, z * cellSize), rect, zoom, pan);
                Vector2 end = WorldToCanvas(new Vector3(maxX * cellSize, 0f, z * cellSize), rect, zoom, pan);
                Handles.DrawLine(start, end);
            }

            Vector2 origin = WorldToCanvas(Vector3.zero, rect, zoom, pan);
            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Handles.DrawLine(new Vector3(rect.xMin, origin.y), new Vector3(rect.xMax, origin.y));
            Handles.DrawLine(new Vector3(origin.x, rect.yMin), new Vector3(origin.x, rect.yMax));
        }

        private static void DrawVisibleArea(Rect rect, float zoom, Vector2 pan)
        {
            Vector3 bottomLeft = new Vector3(VisibleAreaMinX, 0f, VisibleAreaMinZ);
            Vector3 topLeft = new Vector3(VisibleAreaMinX, 0f, VisibleAreaMaxZ);
            Vector3 topRight = new Vector3(VisibleAreaMaxX, 0f, VisibleAreaMaxZ);
            Vector3 bottomRight = new Vector3(VisibleAreaMaxX, 0f, VisibleAreaMinZ);

            Vector3[] vertices =
            {
                WorldToCanvas(bottomLeft, rect, zoom, pan),
                WorldToCanvas(topLeft, rect, zoom, pan),
                WorldToCanvas(topRight, rect, zoom, pan),
                WorldToCanvas(bottomRight, rect, zoom, pan)
            };

            Handles.DrawSolidRectangleWithOutline(vertices, VisibleAreaFillColor, VisibleAreaOutlineColor);
            Handles.Label((Vector2)vertices[2] + new Vector2(8f, -18f), "Visible Area");
        }

        private static void DrawPath(Rect rect, IReadOnlyList<Vector3> samples, Color color, float zoom, Vector2 pan)
        {
            Handles.color = color;

            for (int i = 1; i < samples.Count; i++)
            {
                Vector2 start = WorldToCanvas(samples[i - 1], rect, zoom, pan);
                Vector2 end = WorldToCanvas(samples[i], rect, zoom, pan);
                Handles.DrawAAPolyLine(3f, start, end);
            }
        }

        private static void DrawLanePreview(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan)
        {
            CatmullRomPathSampler sampler = new CatmullRomPathSampler();
            List<Vector3> points = new List<Vector3>(editState.WaypointLocalPositions);
            sampler.Rebuild(points, editState.IsClosedLoop);

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
                Vector2 previousPoint = Vector2.zero;
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

                    Vector2 canvasPoint = WorldToCanvas(lanePoint, rect, zoom, pan);

                    if (hasPrevious)
                    {
                        Handles.DrawLine(previousPoint, canvasPoint);
                    }

                    previousPoint = canvasPoint;
                    hasPrevious = true;
                }
            }
        }

        private static void DrawWaypoints(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan)
        {
            IReadOnlyList<Vector3> waypoints = editState.WaypointLocalPositions;

            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector2 canvasPoint = WorldToCanvas(waypoints[i], rect, zoom, pan);
                bool isSelected = editState.SelectedWaypointIndex == i;
                float radius = isSelected ? 9f : 7f;

                Handles.color = isSelected ? Color.white : settings.WaypointColor;
                Handles.DrawSolidDisc(canvasPoint, Vector3.forward, radius);
                Handles.Label(canvasPoint + new Vector2(10f, -8f), $"WP{i}");
            }
        }

        private static void DrawBoxQueue(
            Rect rect,
            Vector3 localPosition,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan,
            bool isSelected)
        {
            Vector2 canvasPoint = WorldToCanvas(localPosition, rect, zoom, pan);
            float radius = isSelected ? 11f : 9f;

            Handles.color = isSelected
                ? Color.white
                : new Color(settings.BoxQueueColor.r, settings.BoxQueueColor.g, settings.BoxQueueColor.b, 0.95f);
            Handles.DrawSolidDisc(canvasPoint, Vector3.forward, radius);
            Handles.Label(canvasPoint + new Vector2(12f, -8f), "Queue");
        }

        private static void DrawHelpOverlay(Rect rect)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 0.85f) }
            };

            GUI.Label(
                new Rect(rect.x + 8f, rect.yMax - 36f, rect.width - 16f, 32f),
                "Drag: move  |  Click empty: add WP  |  Scroll: zoom  |  Alt+Drag: pan  |  Del: remove",
                style);
        }

        private static void HandleInput(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            ref float zoom,
            ref Vector2 pan,
            ref bool isBoxQueueSelected)
        {
            Event currentEvent = Event.current;

            if (!rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            int controlId = GUIUtility.GetControlID(CanvasControlId, FocusType.Passive);
            float cellSize = settings.GridCellSize;

            switch (currentEvent.type)
            {
                case EventType.ScrollWheel:
                    zoom = Mathf.Clamp(zoom - currentEvent.delta.y * 2f, MinZoom, MaxZoom);
                    currentEvent.Use();
                    GUI.changed = true;
                    break;

                case EventType.MouseDown:
                    if (currentEvent.button == 2 || (currentEvent.button == 0 && currentEvent.alt))
                    {
                        GUIUtility.hotControl = controlId;
                        currentEvent.Use();
                    }
                    else if (currentEvent.button == 0)
                    {
                        HandleLeftMouseDown(rect, editState, settings, zoom, pan, ref isBoxQueueSelected, cellSize, controlId);
                        currentEvent.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        pan += currentEvent.delta / zoom;
                        currentEvent.Use();
                        GUI.changed = true;
                    }
                    else if (GUIUtility.hotControl == controlId + 1)
                    {
                        DragSelected(rect, editState, settings, zoom, pan, isBoxQueueSelected, cellSize);
                        currentEvent.Use();
                        GUI.changed = true;
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId || GUIUtility.hotControl == controlId + 1)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }

                    break;

                case EventType.KeyDown:
                    if (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace)
                    {
                        if (editState.SelectedWaypointIndex >= 0)
                        {
                            editState.RemoveWaypoint(editState.SelectedWaypointIndex);
                            isBoxQueueSelected = false;
                            currentEvent.Use();
                            GUI.changed = true;
                        }
                    }

                    break;
            }
        }

        private static void HandleLeftMouseDown(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan,
            ref bool isBoxQueueSelected,
            float cellSize,
            int controlId)
        {
            Vector2 mouse = Event.current.mousePosition;
            int waypointHit = HitTestWaypoint(rect, editState, zoom, pan, mouse);

            if (waypointHit >= 0)
            {
                editState.SelectedWaypointIndex = waypointHit;
                isBoxQueueSelected = false;
                GUIUtility.hotControl = controlId + 1;
                return;
            }

            if (HitTestBoxQueue(rect, editState.BoxQueueLocalPosition, zoom, pan, mouse))
            {
                editState.SelectedWaypointIndex = -1;
                isBoxQueueSelected = true;
                GUIUtility.hotControl = controlId + 1;
                return;
            }

            Vector3 localPosition = CanvasToWorld(mouse, rect, zoom, pan);

            if (settings.SnapToGrid)
            {
                localPosition = BeltPathEditorUtility.SnapLocalPosition(localPosition, cellSize);
            }

            editState.AddWaypoint(localPosition);
            isBoxQueueSelected = false;
            GUI.changed = true;
        }

        private static void DragSelected(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan,
            bool isBoxQueueSelected,
            float cellSize)
        {
            Vector3 localPosition = CanvasToWorld(Event.current.mousePosition, rect, zoom, pan);

            if (settings.SnapToGrid)
            {
                localPosition = BeltPathEditorUtility.SnapLocalPosition(localPosition, cellSize);
            }

            if (isBoxQueueSelected)
            {
                editState.SetBoxQueuePosition(localPosition);
                return;
            }

            if (editState.SelectedWaypointIndex >= 0)
            {
                editState.SetWaypointPosition(editState.SelectedWaypointIndex, localPosition);
            }
        }

        private static int HitTestWaypoint(Rect rect, MapLayoutEditState editState, float zoom, Vector2 pan, Vector2 mouse)
        {
            IReadOnlyList<Vector3> waypoints = editState.WaypointLocalPositions;

            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                Vector2 canvasPoint = WorldToCanvas(waypoints[i], rect, zoom, pan);

                if (Vector2.Distance(mouse, canvasPoint) <= WaypointHitRadius)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool HitTestBoxQueue(Rect rect, Vector3 localPosition, float zoom, Vector2 pan, Vector2 mouse)
        {
            Vector2 canvasPoint = WorldToCanvas(localPosition, rect, zoom, pan);
            return Vector2.Distance(mouse, canvasPoint) <= QueueHitRadius;
        }

        private static Vector2 WorldToCanvas(Vector3 localPosition, Rect rect, float zoom, Vector2 pan)
        {
            float x = rect.center.x + (localPosition.x + pan.x) * zoom;
            float y = rect.center.y - (localPosition.z + pan.y) * zoom;
            return new Vector2(x, y);
        }

        private static Vector3 CanvasToWorld(Vector2 canvasPoint, Rect rect, float zoom, Vector2 pan)
        {
            float x = (canvasPoint.x - rect.center.x) / zoom - pan.x;
            float z = -(canvasPoint.y - rect.center.y) / zoom - pan.y;
            return new Vector3(x, 0f, z);
        }
    }
}
#endif
