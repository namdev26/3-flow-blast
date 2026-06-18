#if UNITY_EDITOR
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

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Map Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Edit the belt layout in FlowBlast → Map Editor, then use Apply To Scene when ready.",
                MessageType.Info);

            if (GUILayout.Button("Open Map Editor"))
            {
                FlowBlastMapEditorWindow.ShowWindow(beltPath);
            }

            CatmullRomPathSampler sampler = BeltPathEditorUtility.BuildPreviewSampler(beltPath);
            EditorGUILayout.LabelField("Path Length", sampler.TotalLength.ToString("0.00"));
            EditorGUILayout.LabelField("Block Capacity", BeltPathEditorUtility.GetBlockCapacity(beltPath, settings).ToString());
        }
    }
}
#endif
