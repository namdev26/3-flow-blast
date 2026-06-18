#if UNITY_EDITOR
using FlowBlast.Core.Constants;
using FlowBlast.Core.Utilities;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowBlast.Editor
{
    public static class BoxConveyorPathSetupEditor
    {
        [MenuItem("FlowBlast/Setup Box Conveyor Oval Path")]
        public static void SetupBoxConveyorOvalPath()
        {
            BeltPath boxConveyorPath = FindBoxConveyorPath();

            if (boxConveyorPath == null)
            {
                Debug.LogError("[FlowBlast] BoxConveyorPath not found under Zone_BoxConveyor.");
                return;
            }

            BeltPathEditorUtility.ApplyPreset(
                boxConveyorPath,
                BoxConveyorLayout.DefaultOvalWaypointLocalPositions,
                isClosedLoop: true);

            EditorUtility.SetDirty(boxConveyorPath);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = boxConveyorPath.gameObject;
            Debug.Log("[FlowBlast] Box conveyor oval path applied. Select BoxConveyorPath and drag waypoints to fine-tune.");
        }

        private static BeltPath FindBoxConveyorPath()
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform pathTransform = TransformHierarchyUtility.FindChildRecursive(
                    roots[i].transform,
                    GameplayZoneNames.BoxConveyorPath);

                if (pathTransform != null && pathTransform.TryGetComponent(out BeltPath beltPath))
                {
                    return beltPath;
                }
            }

            return null;
        }
    }
}
#endif
