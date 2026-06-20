#if UNITY_EDITOR
using FlowBlast.Data;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    [CustomEditor(typeof(LevelMapLayout))]
    public sealed class LevelMapLayoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelMapLayout layout = (LevelMapLayout)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Main Waypoints", layout.MainWaypointLocalPositions.Count.ToString());
            EditorGUILayout.LabelField("Queue Paths", layout.QueuePaths.Count.ToString());
            EditorGUILayout.LabelField("Queue Entry Local", layout.BoxQueueLocalPosition.ToString("F2"));
            EditorGUILayout.LabelField("Collection Point Local", layout.CollectionPointLocalPosition.ToString("F2"));

            EditorGUILayout.HelpBox(
                "Edit layout in FlowBlast → Map Editor. Use Scene Sync → Apply To Scene when ready.",
                MessageType.Info);
        }
    }
}
#endif
