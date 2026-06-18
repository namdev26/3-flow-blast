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
            EditorGUILayout.LabelField("Waypoint Count", layout.WaypointLocalPositions.Count.ToString());
            EditorGUILayout.LabelField("Box Queue Local", layout.BoxQueueLocalPosition.ToString("F2"));

            EditorGUILayout.HelpBox(
                "Edit layout in FlowBlast → Map Editor. Use Scene Sync → Apply To Scene when ready.",
                MessageType.Info);
        }
    }
}
#endif
