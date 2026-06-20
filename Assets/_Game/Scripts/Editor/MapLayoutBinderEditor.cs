#if UNITY_EDITOR
using FlowBlast.Bootstrap;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    [CustomEditor(typeof(MapLayoutBinder))]
    public sealed class MapLayoutBinderEditor : UnityEditor.Editor
    {
        private const string WaypointPrefabPath = "Assets/_Game/Prefabs/BeltWaypointMarker.prefab";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapLayoutBinder binder = (MapLayoutBinder)target;
            LevelMapLayout layout = binder.MapLayout;
            BeltPath beltPath = binder.BeltPath;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);

            if (layout != null)
            {
                EditorGUILayout.LabelField("Layout Id", layout.LayoutId);
                EditorGUILayout.LabelField("Main Waypoints", layout.MainWaypointLocalPositions.Count.ToString());
                EditorGUILayout.LabelField("Queue Paths", layout.QueuePaths.Count.ToString());
                EditorGUILayout.LabelField("Main Closed Loop", layout.IsMainPathClosedLoop ? "Yes" : "No");
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Level Map Layout asset to preview and apply belt paths.", MessageType.Info);
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(layout == null || beltPath == null))
            {
                if (GUILayout.Button("Apply Layout To Scene"))
                {
                    Undo.RecordObject(binder, "Apply Map Layout");
                    ResolveWaypointPrefab(binder);
                    binder.ApplyLayout();
                    MarkSceneDirty(binder);
                }

                if (GUILayout.Button("Capture Scene To Layout Asset"))
                {
                    Undo.RecordObject(layout, "Capture Map Layout");
                    binder.CaptureLayout();
                    EditorUtility.SetDirty(layout);
                    AssetDatabase.SaveAssets();
                }
            }

            if (GUILayout.Button("Create New Layout Asset From Scene"))
            {
                CreateLayoutAssetFromScene(binder);
            }
        }

        private static void ResolveWaypointPrefab(MapLayoutBinder binder)
        {
            SerializedObject serializedBinder = new SerializedObject(binder);
            SerializedProperty prefabProperty = serializedBinder.FindProperty("waypointPrefab");

            if (prefabProperty.objectReferenceValue != null)
            {
                return;
            }

            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(WaypointPrefabPath);

            if (prefabObject == null)
            {
                return;
            }

            BeltWaypointMarker marker = prefabObject.GetComponent<BeltWaypointMarker>();
            prefabProperty.objectReferenceValue = marker;
            serializedBinder.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLayoutAssetFromScene(MapLayoutBinder binder)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Level Map Layout",
                "LevelMapLayout",
                "asset",
                "Choose save location");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ResolveWaypointPrefab(binder);

            LevelMapLayout layout = ScriptableObject.CreateInstance<LevelMapLayout>();
            Transform boxQueueParent = binder.transform.Find("BoxQueueParent");
            Transform collectionPointMarker = binder.transform.Find("CollectionPointMarker");
            MapLayoutApplicator.Capture(layout, binder.BeltPath, binder.QueueBeltPaths, boxQueueParent, collectionPointMarker);

            AssetDatabase.CreateAsset(layout, path);
            AssetDatabase.SaveAssets();

            SerializedObject serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("mapLayout").objectReferenceValue = layout;
            serializedBinder.ApplyModifiedProperties();
            EditorGUIUtility.PingObject(layout);
        }

        private static void MarkSceneDirty(MapLayoutBinder binder)
        {
            EditorUtility.SetDirty(binder);

            if (binder.BeltPath != null)
            {
                EditorUtility.SetDirty(binder.BeltPath);
            }

            for (int i = 0; i < binder.QueueBeltPaths.Count; i++)
            {
                if (binder.QueueBeltPaths[i] != null)
                {
                    EditorUtility.SetDirty(binder.QueueBeltPaths[i]);
                }
            }

            Transform boxQueueParent = binder.transform.Find("BoxQueueParent");
            Transform collectionPointMarker = binder.transform.Find("CollectionPointMarker");

            if (boxQueueParent != null)
            {
                EditorUtility.SetDirty(boxQueueParent);
            }

            if (collectionPointMarker != null)
            {
                EditorUtility.SetDirty(collectionPointMarker);
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(binder.gameObject.scene);
            }

            SceneView.RepaintAll();
        }
    }
}
#endif
