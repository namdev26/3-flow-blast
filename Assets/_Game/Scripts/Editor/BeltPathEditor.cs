#if UNITY_EDITOR
using FlowBlast.Bootstrap;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    [CustomEditor(typeof(BeltPath))]
    public sealed class BeltPathEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BeltPath beltPath = (BeltPath)target;
            MapEditorSettings settings = BeltPathEditorUtility.LoadSettings();
            GameplayInstaller installer = beltPath.GetComponentInParent<GameplayInstaller>();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Map Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Edit the belt layout in FlowBlast → Map Editor, then use Apply To Scene when ready.",
                MessageType.Info);

            if (GUILayout.Button("Open Map Editor"))
            {
                FlowBlastMapEditorWindow.ShowWindow(beltPath);
            }

            if (installer != null)
            {
                SerializedObject serializedInstaller = new SerializedObject(installer);
                SerializedProperty collectionPointMarkerProperty = serializedInstaller.FindProperty("collectionPointMarker");
                EditorGUILayout.ObjectField(
                    "Collection Marker",
                    collectionPointMarkerProperty != null ? collectionPointMarkerProperty.objectReferenceValue : null,
                    typeof(Transform),
                    true);
            }

            CatmullRomPathSampler sampler = BeltPathEditorUtility.BuildPreviewSampler(beltPath);
            EditorGUILayout.LabelField("Path Length", sampler.TotalLength.ToString("0.00"));
            EditorGUILayout.LabelField("Block Capacity", BeltPathEditorUtility.GetBlockCapacity(beltPath, settings).ToString());
        }

        private void OnSceneGUI()
        {
            BeltPath beltPath = (BeltPath)target;
            GameplayInstaller installer = beltPath.GetComponentInParent<GameplayInstaller>();

            if (installer == null)
            {
                return;
            }

            SerializedObject serializedInstaller = new SerializedObject(installer);
            SerializedProperty collectionPointMarkerProperty = serializedInstaller.FindProperty("collectionPointMarker");
            Transform collectionPointMarker = collectionPointMarkerProperty != null
                ? collectionPointMarkerProperty.objectReferenceValue as Transform
                : null;

            if (collectionPointMarker == null)
            {
                return;
            }

            Handles.color = Color.red;
            float handleSize = HandleUtility.GetHandleSize(collectionPointMarker.position) * 0.18f;
            Handles.SphereHandleCap(0, collectionPointMarker.position, Quaternion.identity, handleSize, EventType.Repaint);
            Handles.Label(collectionPointMarker.position + Vector3.up * handleSize * 1.5f, "Collection Point");

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(collectionPointMarker.position, Quaternion.identity);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(collectionPointMarker, "Move Collection Point Marker");
            collectionPointMarker.position = newPosition;
            EditorUtility.SetDirty(collectionPointMarker);
        }
    }
}
#endif
