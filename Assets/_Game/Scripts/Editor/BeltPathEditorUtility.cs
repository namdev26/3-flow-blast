#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public static class MapPathPresets
    {
        public static Vector3[] CreateRectangleLoop(float halfWidth, float halfDepth)
        {
            return new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, 0f),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, 0f),
                new Vector3(halfWidth, 0f, -halfDepth)
            };
        }

        public static Vector3[] CreateUShape(float halfWidth, float halfDepth)
        {
            return new[]
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(0f, 0f, halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth)
            };
        }

        public static Vector3[] CreateStraightLine(float length, int pointCount)
        {
            int safePointCount = Mathf.Max(2, pointCount);
            Vector3[] points = new Vector3[safePointCount];
            float step = length / (safePointCount - 1);

            for (int i = 0; i < safePointCount; i++)
            {
                points[i] = new Vector3(0f, 0f, -length * 0.5f + step * i);
            }

            return points;
        }
    }

    public static class BeltPathEditorUtility
    {
        private const string WaypointMarkerPrefabPath = "Assets/_Game/Prefabs/BeltWaypointMarker.prefab";
        private const string DefaultSettingsPath = "Assets/_Game/Data/MapEditorSettings.asset";

        public static MapEditorSettings LoadSettings()
        {
            MapEditorSettings settings = AssetDatabase.LoadAssetAtPath<MapEditorSettings>(DefaultSettingsPath);

            if (settings != null)
            {
                return settings;
            }

            return ScriptableObject.CreateInstance<MapEditorSettings>();
        }

        public static void ApplyPreset(BeltPath beltPath, Vector3[] localPositions, bool isClosedLoop)
        {
            if (beltPath == null || localPositions == null || localPositions.Length == 0)
            {
                return;
            }

            Undo.RecordObject(beltPath, "Apply Belt Path Preset");

            ClearWaypoints(beltPath);
            SerializedObject serializedPath = new SerializedObject(beltPath);
            SerializedProperty waypointProperty = serializedPath.FindProperty("waypoints");
            SerializedProperty closedLoopProperty = serializedPath.FindProperty("isClosedLoop");
            waypointProperty.arraySize = localPositions.Length;
            closedLoopProperty.boolValue = isClosedLoop;

            for (int i = 0; i < localPositions.Length; i++)
            {
                Transform waypoint = CreateWaypointTransform(beltPath.transform, localPositions[i], i);
                waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoint;
            }

            serializedPath.ApplyModifiedProperties();
            EditorUtility.SetDirty(beltPath);
            SceneView.RepaintAll();
        }

        public static Transform AddWaypoint(BeltPath beltPath, Vector3 localPosition, int insertIndex = -1)
        {
            if (beltPath == null)
            {
                return null;
            }

            Undo.RecordObject(beltPath, "Add Belt Waypoint");

            SerializedObject serializedPath = new SerializedObject(beltPath);
            SerializedProperty waypointProperty = serializedPath.FindProperty("waypoints");
            int targetIndex = insertIndex < 0 ? waypointProperty.arraySize : Mathf.Clamp(insertIndex, 0, waypointProperty.arraySize);
            waypointProperty.InsertArrayElementAtIndex(targetIndex);
            Transform waypoint = CreateWaypointTransform(beltPath.transform, localPosition, targetIndex);
            waypointProperty.GetArrayElementAtIndex(targetIndex).objectReferenceValue = waypoint;
            serializedPath.ApplyModifiedProperties();
            RefreshWaypointIndices(beltPath);
            EditorUtility.SetDirty(beltPath);
            return waypoint;
        }

        public static void RemoveWaypoint(BeltPath beltPath, int index)
        {
            if (beltPath == null)
            {
                return;
            }

            SerializedObject serializedPath = new SerializedObject(beltPath);
            SerializedProperty waypointProperty = serializedPath.FindProperty("waypoints");

            if (index < 0 || index >= waypointProperty.arraySize)
            {
                return;
            }

            Undo.RecordObject(beltPath, "Remove Belt Waypoint");

            Object waypointReference = waypointProperty.GetArrayElementAtIndex(index).objectReferenceValue;

            if (waypointReference is Transform waypointTransform)
            {
                Undo.DestroyObjectImmediate(waypointTransform.gameObject);
            }

            waypointProperty.DeleteArrayElementAtIndex(index);
            serializedPath.ApplyModifiedProperties();
            RefreshWaypointIndices(beltPath);
            EditorUtility.SetDirty(beltPath);
        }

        public static void ClearWaypoints(BeltPath beltPath)
        {
            if (beltPath == null)
            {
                return;
            }

            SerializedObject serializedPath = new SerializedObject(beltPath);
            SerializedProperty waypointProperty = serializedPath.FindProperty("waypoints");

            for (int i = waypointProperty.arraySize - 1; i >= 0; i--)
            {
                Object waypointReference = waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue;

                if (waypointReference is Transform waypointTransform)
                {
                    Undo.DestroyObjectImmediate(waypointTransform.gameObject);
                }
            }

            waypointProperty.arraySize = 0;
            serializedPath.ApplyModifiedProperties();
            EditorUtility.SetDirty(beltPath);
        }

        public static void RefreshWaypointIndices(BeltPath beltPath)
        {
            if (beltPath == null)
            {
                return;
            }

            Transform[] waypoints = beltPath.Waypoints;

            if (waypoints == null)
            {
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                BeltWaypointMarker marker = waypoints[i].GetComponent<BeltWaypointMarker>();

                if (marker == null)
                {
                    marker = waypoints[i].gameObject.AddComponent<BeltWaypointMarker>();
                }

                marker.SetWaypointIndex(i);
                marker.SetPathRole(beltPath.PathRole);
            }
        }

        public static void SnapAllWaypointsToGrid(BeltPath beltPath, MapEditorSettings settings)
        {
            if (beltPath == null || settings == null)
            {
                return;
            }

            Transform[] waypoints = beltPath.Waypoints;

            if (waypoints == null)
            {
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Undo.RecordObject(waypoints[i], "Snap Waypoint To Grid");
                Vector3 localPosition = waypoints[i].localPosition;
                waypoints[i].localPosition = SnapLocalPosition(localPosition, settings.GridCellSize);
            }

            EditorUtility.SetDirty(beltPath);
        }

        public static Vector3 SnapLocalPosition(Vector3 localPosition, float cellSize)
        {
            if (cellSize <= Mathf.Epsilon)
            {
                return localPosition;
            }

            return new Vector3(
                Mathf.Round(localPosition.x / cellSize) * cellSize,
                localPosition.y,
                Mathf.Round(localPosition.z / cellSize) * cellSize);
        }

        public static Vector3 SnapWorldPosition(Vector3 worldPosition, float cellSize, float planeY)
        {
            if (cellSize <= Mathf.Epsilon)
            {
                return worldPosition;
            }

            return new Vector3(
                Mathf.Round(worldPosition.x / cellSize) * cellSize,
                planeY,
                Mathf.Round(worldPosition.z / cellSize) * cellSize);
        }

        public static void CaptureLayout(
            BeltPath beltPath,
            IReadOnlyList<BeltPath> queueBeltPaths,
            Transform boxQueueParent,
            LevelMapLayout layout)
        {
            if (layout == null)
            {
                return;
            }

            Undo.RecordObject(layout, "Capture Map Layout");
            layout.CaptureFrom(beltPath, queueBeltPaths, boxQueueParent);
            EditorUtility.SetDirty(layout);
        }

        public static void ApplyLayout(
            BeltPath beltPath,
            IReadOnlyList<BeltPath> queueBeltPaths,
            Transform boxQueueParent,
            LevelMapLayout layout)
        {
            if (layout == null)
            {
                return;
            }

            if (beltPath != null)
            {
                Undo.RecordObject(beltPath, "Apply Main Map Layout");
            }

            if (queueBeltPaths != null)
            {
                for (int i = 0; i < queueBeltPaths.Count; i++)
                {
                    if (queueBeltPaths[i] != null)
                    {
                        Undo.RecordObject(queueBeltPaths[i], "Apply Queue Map Layout");
                    }
                }
            }

            BeltWaypointMarker waypointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaypointMarkerPrefabPath)
                ?.GetComponent<BeltWaypointMarker>();

            layout.ApplyTo(beltPath, queueBeltPaths, boxQueueParent, waypointPrefab);

            if (boxQueueParent != null)
            {
                Undo.RecordObject(boxQueueParent, "Apply Box Queue Position");
                EditorUtility.SetDirty(boxQueueParent);
            }

            if (beltPath != null)
            {
                EditorUtility.SetDirty(beltPath);
            }

            if (queueBeltPaths != null)
            {
                for (int i = 0; i < queueBeltPaths.Count; i++)
                {
                    if (queueBeltPaths[i] != null)
                    {
                        EditorUtility.SetDirty(queueBeltPaths[i]);
                    }
                }
            }

            SceneView.RepaintAll();
        }

        public static CatmullRomPathSampler BuildPreviewSampler(BeltPath beltPath)
        {
            CatmullRomPathSampler sampler = new CatmullRomPathSampler();
            List<Vector3> points = new List<Vector3>();
            Transform[] waypoints = beltPath.Waypoints;

            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null)
                    {
                        continue;
                    }

                    points.Add(waypoints[i].position);
                }
            }

            sampler.Rebuild(points, beltPath.IsClosedLoop, beltPath.CurveStrength);
            return sampler;
        }

        public static int GetBlockCapacity(BeltPath beltPath, MapEditorSettings settings)
        {
            if (beltPath == null || settings == null)
            {
                return 0;
            }

            CatmullRomPathSampler sampler = BuildPreviewSampler(beltPath);
            return BeltLaneLayout.GetTotalBlockCapacity(
                sampler.TotalLength,
                settings.LaneSpacing,
                settings.LaneCount);
        }

        private static Transform CreateWaypointTransform(Transform parent, Vector3 localPosition, int index)
        {
            GameObject waypointObject = LoadOrCreateWaypointObject(parent, localPosition, index);
            Undo.RegisterCreatedObjectUndo(waypointObject, "Create Belt Waypoint");
            return waypointObject.transform;
        }

        private static GameObject LoadOrCreateWaypointObject(Transform parent, Vector3 localPosition, int index)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaypointMarkerPrefabPath);

            GameObject waypointObject;

            if (prefab != null)
            {
                waypointObject = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            }
            else
            {
                waypointObject = new GameObject($"Waypoint_{index}");
                waypointObject.transform.SetParent(parent, false);
                waypointObject.AddComponent<BeltWaypointMarker>();
            }

            waypointObject.transform.localPosition = localPosition;

            BeltWaypointMarker marker = waypointObject.GetComponent<BeltWaypointMarker>();

            if (marker == null)
            {
                marker = waypointObject.AddComponent<BeltWaypointMarker>();
            }

            marker.SetWaypointIndex(index);
            return waypointObject;
        }
    }
}
#endif
