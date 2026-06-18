#if UNITY_EDITOR
using FlowBlast.Bootstrap;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowBlast.Editor
{
    public static class GameplayZoneReorganizer
    {
        private static readonly string[] MainConveyorChildNames =
        {
            "Belt",
            GameplayZoneNames.ConveyorRoot,
            GameplayZoneNames.BlockPoolParent
        };

        private static readonly string[] BoxConveyorChildNames =
        {
            GameplayZoneNames.BoxQueueParent,
            GameplayZoneNames.BoxBeltParent,
            "Convenyor_Box",
            "BoxConveyor"
        };

        private static readonly string[] BoardChildNames =
        {
            GameplayZoneNames.BoardRoot,
            "Board and wall root"
        };

        [MenuItem("FlowBlast/Setup Gameplay Zones")]
        public static void SetupGameplayZones()
        {
            GameplayInstaller installer = Object.FindFirstObjectByType<GameplayInstaller>();

            if (installer == null)
            {
                Debug.LogError("[FlowBlast] No GameplayInstaller found in scene.");
                return;
            }

            Transform gameplayRoot = installer.transform;
            Undo.SetCurrentGroupName("Setup Gameplay Zones");
            int undoGroup = Undo.GetCurrentGroup();

            Transform mainZone = EnsureZone(gameplayRoot, GameplayZoneNames.MainConveyor);
            Transform boxZone = EnsureZone(gameplayRoot, GameplayZoneNames.BoxConveyor);
            Transform boardZone = EnsureZone(gameplayRoot, GameplayZoneNames.Board);

            MoveNamedChildrenToZone(gameplayRoot, mainZone, MainConveyorChildNames);
            MoveNamedChildrenToZone(gameplayRoot, boxZone, BoxConveyorChildNames);
            MoveNamedChildrenToZone(gameplayRoot, boardZone, BoardChildNames);

            RenameIfExists(mainZone, "Belt", GameplayZoneNames.ConveyorRoot);
            EnsureZoneChild(mainZone, GameplayZoneNames.BlockPoolParent);
            EnsureZoneChild(boxZone, GameplayZoneNames.BoxQueueParent);
            EnsureZoneChild(boxZone, GameplayZoneNames.BoxBeltParent);
            EnsureZoneChild(boardZone, GameplayZoneNames.BoardRoot);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FlowBlast] Gameplay zones setup complete.");
        }

        public static void ApplyToGameplayRoot(Transform gameplayRoot)
        {
            if (gameplayRoot == null)
            {
                return;
            }

            Transform mainZone = EnsureZone(gameplayRoot, GameplayZoneNames.MainConveyor);
            Transform boxZone = EnsureZone(gameplayRoot, GameplayZoneNames.BoxConveyor);
            Transform boardZone = EnsureZone(gameplayRoot, GameplayZoneNames.Board);

            MoveNamedChildrenToZone(gameplayRoot, mainZone, MainConveyorChildNames);
            MoveNamedChildrenToZone(gameplayRoot, boxZone, BoxConveyorChildNames);
            MoveNamedChildrenToZone(gameplayRoot, boardZone, BoardChildNames);

            RenameIfExists(mainZone, "Belt", GameplayZoneNames.ConveyorRoot);
            EnsureZoneChild(mainZone, GameplayZoneNames.BlockPoolParent);
            EnsureZoneChild(boxZone, GameplayZoneNames.BoxQueueParent);
            EnsureZoneChild(boxZone, GameplayZoneNames.BoxBeltParent);
            EnsureZoneChild(boardZone, GameplayZoneNames.BoardRoot);
        }

        private static Transform EnsureZone(Transform gameplayRoot, string zoneName)
        {
            Transform zone = TransformHierarchyUtility.FindChildRecursive(gameplayRoot, zoneName);

            if (zone != null)
            {
                return zone;
            }

            GameObject zoneObject = new GameObject(zoneName);
            Undo.RegisterCreatedObjectUndo(zoneObject, "Create Gameplay Zone");
            zoneObject.transform.SetParent(gameplayRoot, false);
            return zoneObject.transform;
        }

        private static void EnsureZoneChild(Transform zone, string childName)
        {
            if (zone == null)
            {
                return;
            }

            Transform existing = TransformHierarchyUtility.FindChildRecursive(zone, childName);

            if (existing != null)
            {
                return;
            }

            GameObject childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, "Create Zone Child");
            childObject.transform.SetParent(zone, false);
        }

        private static void MoveNamedChildrenToZone(Transform gameplayRoot, Transform zone, string[] childNames)
        {
            if (zone == null)
            {
                return;
            }

            for (int i = 0; i < childNames.Length; i++)
            {
                Transform child = TransformHierarchyUtility.FindChildRecursive(gameplayRoot, childNames[i]);

                if (child == null || child == zone || IsDescendantOf(child, zone))
                {
                    continue;
                }

                MoveToZone(child, zone);
            }
        }

        private static void MoveToZone(Transform child, Transform zone)
        {
            Undo.SetTransformParent(child, zone, "Move To Gameplay Zone");
            child.SetParent(zone, true);
        }

        private static void RenameIfExists(Transform searchRoot, string currentName, string newName)
        {
            Transform target = TransformHierarchyUtility.FindChildRecursive(searchRoot, currentName);

            if (target == null || target.name == newName)
            {
                return;
            }

            Undo.RecordObject(target.gameObject, "Rename Conveyor Root");
            target.name = newName;
        }

        private static bool IsDescendantOf(Transform child, Transform potentialAncestor)
        {
            Transform current = child;

            while (current != null)
            {
                if (current == potentialAncestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
#endif
