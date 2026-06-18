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
                EditorGUILayout.LabelField("Waypoints", layout.WaypointLocalPositions.Count.ToString());
                EditorGUILayout.LabelField("Closed Loop", layout.IsClosedLoop ? "Yes" : "No");
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Level Map Layout asset to preview and apply the belt path.", MessageType.Info);
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
            MapLayoutApplicator.Capture(layout, binder.BeltPath, boxQueueParent);

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

            Transform boxQueueParent = binder.transform.Find("BoxQueueParent");

            if (boxQueueParent != null)
            {
                EditorUtility.SetDirty(boxQueueParent);
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
