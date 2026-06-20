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
        private const float CollectionPointHitRadius = 14f;
        private const float VisibleAreaMinX = -4f;
        private const float VisibleAreaMaxX = 4f;
        private const float VisibleAreaMinZ = 0f;
        private const float VisibleAreaMaxZ = 4f;
        private static readonly Color VisibleAreaFillColor = new Color(0.2f, 0.7f, 1f, 0.08f);
        private static readonly Color VisibleAreaOutlineColor = new Color(0.2f, 0.7f, 1f, 0.85f);
        private static readonly Color CollectionPointColor = new Color(1f, 0.35f, 0.35f, 0.95f);

        private static readonly int CanvasControlId = "MapLayoutCanvas".GetHashCode();

        public static void Draw(
            Rect rect,
            MapLayoutEditState activeEditState,
            IReadOnlyList<MapLayoutEditState> visibleEditStates,
            MapEditorSettings settings,
            BeltPathRole activePathRole,
            ref float zoom,
            ref Vector2 pan,
            ref bool isBoxQueueSelected,
            ref bool isCollectionPointSelected)
        {
            if (activeEditState == null)
            {
                return;
            }

            MapEditorSettings activeSettings = settings ?? BeltPathEditorUtility.LoadSettings();
            DrawBackground(rect);
            HandleInput(
                rect,
                activeEditState,
                activeSettings,
                ref zoom,
                ref pan,
                ref isBoxQueueSelected,
                ref isCollectionPointSelected);
            DrawContent(
                rect,
                activeEditState,
                visibleEditStates,
                activeSettings,
                activePathRole,
                zoom,
                pan,
                isBoxQueueSelected,
                isCollectionPointSelected);

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
            MapLayoutEditState activeEditState,
            IReadOnlyList<MapLayoutEditState> visibleEditStates,
            MapEditorSettings settings,
            BeltPathRole activePathRole,
            float zoom,
            Vector2 pan,
            bool isBoxQueueSelected,
            bool isCollectionPointSelected)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Handles.BeginGUI();
            DrawGrid(rect, settings, zoom, pan);
            DrawVisibleArea(rect, zoom, pan);

            if (visibleEditStates != null)
            {
                for (int i = 0; i < visibleEditStates.Count; i++)
                {
                    MapLayoutEditState editState = visibleEditStates[i];

                    if (editState == null || ReferenceEquals(editState, activeEditState))
                    {
                        continue;
                    }

                    BeltPathRole pathRole = string.IsNullOrWhiteSpace(editState.QueueId)
                        ? BeltPathRole.Main
                        : BeltPathRole.Queue;
                    DrawPathSet(rect, editState, settings, pathRole, false, zoom, pan);

                    if (editState.HasQueuePathIdentity())
                    {
                        DrawBoxQueue(rect, editState.BoxQueueLocalPosition, editState.DisplayName, settings, zoom, pan, false);
                    }
                }
            }

            DrawPathSet(rect, activeEditState, settings, activePathRole, true, zoom, pan);
            DrawBoxQueue(rect, activeEditState.BoxQueueLocalPosition, activeEditState.DisplayName, settings, zoom, pan, isBoxQueueSelected);
            DrawCollectionPoint(rect, activeEditState.CollectionPointLocalPosition, zoom, pan, isCollectionPointSelected);
            Handles.EndGUI();
        }

        private static void DrawPathSet(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            BeltPathRole pathRole,
            bool isActivePath,
            float zoom,
            Vector2 pan)
        {
            if (editState == null)
            {
                return;
            }

            IReadOnlyList<Vector3> samples = editState.GetSampledPath();

            if (samples.Count >= 2)
            {
                Color pathColor = pathRole == BeltPathRole.Queue
                    ? settings.QueuePathColor
                    : settings.MainPathColor;
                DrawPath(rect, samples, pathColor, isActivePath ? 3f : 2f, zoom, pan);

                if (settings.ShowLanePreview && isActivePath)
                {
                    DrawLanePreview(rect, editState, settings, zoom, pan);
                }
            }

            DrawWaypoints(rect, editState, settings, pathRole, isActivePath, zoom, pan);
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

        private static void DrawPath(
            Rect rect,
            IReadOnlyList<Vector3> samples,
            Color color,
            float thickness,
            float zoom,
            Vector2 pan)
        {
            Handles.color = color;

            for (int i = 1; i < samples.Count; i++)
            {
                Vector2 start = WorldToCanvas(samples[i - 1], rect, zoom, pan);
                Vector2 end = WorldToCanvas(samples[i], rect, zoom, pan);
                Handles.DrawAAPolyLine(thickness, start, end);
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
            sampler.Rebuild(points, editState.IsClosedLoop, editState.CurveStrength);

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
            BeltPathRole pathRole,
            bool isActivePath,
            float zoom,
            Vector2 pan)
        {
            IReadOnlyList<Vector3> waypoints = editState.WaypointLocalPositions;

            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector2 canvasPoint = WorldToCanvas(waypoints[i], rect, zoom, pan);
                bool isSelected = isActivePath && editState.SelectedWaypointIndex == i;
                float radius = isSelected ? 9f : isActivePath ? 7f : 5.5f;

                Handles.color = isSelected
                    ? Color.white
                    : pathRole == BeltPathRole.Queue
                        ? settings.QueueWaypointColor
                        : settings.MainWaypointColor;
                Handles.DrawSolidDisc(canvasPoint, Vector3.forward, radius);
                Handles.Label(canvasPoint + new Vector2(10f, -8f), $"WP{i}");
            }
        }

        private static void DrawBoxQueue(
            Rect rect,
            Vector3 localPosition,
            string displayName,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan,
            bool isSelected)
        {
            Vector2 canvasPoint = WorldToCanvas(localPosition, rect, zoom, pan);
            float radius = isSelected ? 11f : 9f;
            string queueLabel = BuildQueueEntryLabel(displayName);

            Handles.color = isSelected
                ? Color.white
                : new Color(settings.BoxQueueColor.r, settings.BoxQueueColor.g, settings.BoxQueueColor.b, 0.95f);
            Handles.DrawSolidDisc(canvasPoint, Vector3.forward, radius);
            Handles.Label(canvasPoint + new Vector2(12f, -8f), queueLabel);
        }

        private static string BuildQueueEntryLabel(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "Queue In";
            }

            string compactName = displayName.Replace("Queue", "Q").Replace(" ", string.Empty);
            return $"{compactName} In";
        }

        private static void DrawCollectionPoint(
            Rect rect,
            Vector3 localPosition,
            float zoom,
            Vector2 pan,
            bool isSelected)
        {
            Vector2 canvasPoint = WorldToCanvas(localPosition, rect, zoom, pan);
            float radius = isSelected ? 11f : 9f;

            Handles.color = isSelected ? Color.white : CollectionPointColor;
            Handles.DrawSolidDisc(canvasPoint, Vector3.forward, radius);
            Handles.Label(canvasPoint + new Vector2(12f, -8f), "Collect");
        }

        private static void DrawHelpOverlay(Rect rect)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 0.85f) }
            };

            GUI.Label(
                new Rect(rect.x + 8f, rect.yMax - 36f, rect.width - 16f, 32f),
                "All queue paths are visible  |  Drag waypoints directly  |  Queue In + Collect are editable markers  |  Click empty: add WP  |  Scroll: zoom  |  Alt+Drag: pan",
                style);
        }

        private static void HandleInput(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            ref float zoom,
            ref Vector2 pan,
            ref bool isBoxQueueSelected,
            ref bool isCollectionPointSelected)
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
                        HandleLeftMouseDown(
                            rect,
                            editState,
                            settings,
                            zoom,
                            pan,
                            ref isBoxQueueSelected,
                            ref isCollectionPointSelected,
                            cellSize,
                            controlId);
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
                        DragSelected(
                            rect,
                            editState,
                            settings,
                            zoom,
                            pan,
                            isBoxQueueSelected,
                            isCollectionPointSelected,
                            cellSize);
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
                            isCollectionPointSelected = false;
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
            ref bool isCollectionPointSelected,
            float cellSize,
            int controlId)
        {
            Vector2 mouse = Event.current.mousePosition;
            int waypointHit = HitTestWaypoint(rect, editState, zoom, pan, mouse);

            if (waypointHit >= 0)
            {
                editState.SelectedWaypointIndex = waypointHit;
                isBoxQueueSelected = false;
                isCollectionPointSelected = false;
                GUIUtility.hotControl = controlId + 1;
                return;
            }

            if (HitTestBoxQueue(rect, editState.BoxQueueLocalPosition, zoom, pan, mouse))
            {
                editState.SelectedWaypointIndex = -1;
                isBoxQueueSelected = true;
                isCollectionPointSelected = false;
                GUIUtility.hotControl = controlId + 1;
                return;
            }

            if (HitTestCollectionPoint(rect, editState.CollectionPointLocalPosition, zoom, pan, mouse))
            {
                editState.SelectedWaypointIndex = -1;
                isBoxQueueSelected = false;
                isCollectionPointSelected = true;
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
            isCollectionPointSelected = false;
            GUI.changed = true;
        }

        private static void DragSelected(
            Rect rect,
            MapLayoutEditState editState,
            MapEditorSettings settings,
            float zoom,
            Vector2 pan,
            bool isBoxQueueSelected,
            bool isCollectionPointSelected,
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

            if (isCollectionPointSelected)
            {
                editState.SetCollectionPointPosition(localPosition);
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

        private static bool HitTestCollectionPoint(Rect rect, Vector3 localPosition, float zoom, Vector2 pan, Vector2 mouse)
        {
            Vector2 canvasPoint = WorldToCanvas(localPosition, rect, zoom, pan);
            return Vector2.Distance(mouse, canvasPoint) <= CollectionPointHitRadius;
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
