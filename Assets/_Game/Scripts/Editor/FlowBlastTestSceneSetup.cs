#if UNITY_EDITOR
using FlowBlast.Bootstrap;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Presentation;
using FlowBlast.Presentation.Block;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowBlast.Editor
{
    public static class FlowBlastTestSceneSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/GameplayTest.unity";
        private const string PrefabFolder = "Assets/_Game/Prefabs";
        private const string BlockPrefabPath = PrefabFolder + "/PixelBlock.prefab";
        private const string BoxPrefabPath = PrefabFolder + "/Box.prefab";

        [MenuItem("FlowBlast/Setup Test Scene (Auto)")]
        public static void SetupTestScene()
        {
            FlowBlastSampleDataCreator.CreateSampleAssets();

            BlockColorPalette palette = AssetDatabase.LoadAssetAtPath<BlockColorPalette>("Assets/_Game/Data/BlockColorPalette.asset");
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/_Game/Data/Level_01.asset");

            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder(PrefabFolder);

            BlockView blockPrefab = CreateOrLoadBlockPrefab(palette);
            BoxView boxPrefab = CreateOrLoadBoxPrefab(palette);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetupCamera();
            GameplayInstaller installer = CreateGameplayHierarchy(blockPrefab, boxPrefab, palette, levelData);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            Selection.activeGameObject = installer.gameObject;
            EditorUtility.SetDirty(installer);

            Debug.Log($"[FlowBlast] Test scene ready: {ScenePath}. Press Play, then click to send boxes to the belt.");
        }

        private static void SetupCamera()
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(0f, 8f, -6f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.17f, 0.2f);
        }

        private static GameplayInstaller CreateGameplayHierarchy(
            BlockView blockPrefab,
            BoxView boxPrefab,
            BlockColorPalette palette,
            LevelData levelData)
        {
            GameObject root = new GameObject("Gameplay");
            GameplayInstaller installer = root.AddComponent<GameplayInstaller>();
            GameplayLoop gameplayLoop = root.AddComponent<GameplayLoop>();
            TapInputController tapInputController = root.AddComponent<TapInputController>();
            BoxPresentationCoordinator presentationCoordinator = root.AddComponent<BoxPresentationCoordinator>();

            Transform mainZone = CreateChild(root.transform, GameplayZoneNames.MainConveyor);
            Transform boxZone = CreateChild(root.transform, GameplayZoneNames.BoxConveyor);
            Transform boardZone = CreateChild(root.transform, GameplayZoneNames.Board);

            GameObject beltRoot = new GameObject(GameplayZoneNames.ConveyorRoot);
            beltRoot.transform.SetParent(mainZone, false);
            BeltPath beltPath = beltRoot.AddComponent<BeltPath>();

            Transform blockPoolParent = CreateChild(mainZone, GameplayZoneNames.BlockPoolParent);
            Transform boxQueueParent = CreateChild(boxZone, GameplayZoneNames.BoxQueueParent);
            Transform boxBeltParent = CreateChild(boxZone, GameplayZoneNames.BoxBeltParent);
            CreateChild(boardZone, GameplayZoneNames.BoardRoot);

            SerializedObject installerSerialized = new SerializedObject(installer);
            installerSerialized.FindProperty("levelData").objectReferenceValue = levelData;
            installerSerialized.FindProperty("colorPalette").objectReferenceValue = palette;
            installerSerialized.FindProperty("blockPrefab").objectReferenceValue = blockPrefab;
            installerSerialized.FindProperty("boxPrefab").objectReferenceValue = boxPrefab;
            installerSerialized.FindProperty("beltPath").objectReferenceValue = beltPath;
            installerSerialized.FindProperty("blockPoolParent").objectReferenceValue = blockPoolParent;
            installerSerialized.FindProperty("boxQueueParent").objectReferenceValue = boxQueueParent;
            installerSerialized.FindProperty("boxBeltParent").objectReferenceValue = boxBeltParent;
            installerSerialized.FindProperty("presentationCoordinator").objectReferenceValue = presentationCoordinator;
            installerSerialized.FindProperty("gameplayLoop").objectReferenceValue = gameplayLoop;
            installerSerialized.FindProperty("tapInputController").objectReferenceValue = tapInputController;
            installerSerialized.ApplyModifiedPropertiesWithoutUndo();

            return installer;
        }

        private static BlockView CreateOrLoadBlockPrefab(BlockColorPalette palette)
        {
            BlockView existing = AssetDatabase.LoadAssetAtPath<BlockView>(BlockPrefabPath);

            if (existing != null)
            {
                return existing;
            }

            GameObject blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockObject.name = "PixelBlock";
            blockObject.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

            Object.DestroyImmediate(blockObject.GetComponent<BoxCollider>());

            BlockView blockView = blockObject.AddComponent<BlockView>();
            SerializedObject blockSerialized = new SerializedObject(blockView);
            blockSerialized.FindProperty("meshRenderer").objectReferenceValue = blockObject.GetComponent<MeshRenderer>();
            blockSerialized.ApplyModifiedPropertiesWithoutUndo();

            BlockView prefab = PrefabUtility.SaveAsPrefabAsset(blockObject, BlockPrefabPath).GetComponent<BlockView>();
            Object.DestroyImmediate(blockObject);
            return prefab;
        }

        private static BoxView CreateOrLoadBoxPrefab(BlockColorPalette palette)
        {
            BoxView existing = AssetDatabase.LoadAssetAtPath<BoxView>(BoxPrefabPath);

            if (existing != null)
            {
                return existing;
            }

            GameObject boxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObject.name = "Box";
            boxObject.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

            Object.DestroyImmediate(boxObject.GetComponent<BoxCollider>());

            GameObject fillIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fillIndicator.name = "FillIndicator";
            fillIndicator.transform.SetParent(boxObject.transform, false);
            fillIndicator.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            fillIndicator.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            Object.DestroyImmediate(fillIndicator.GetComponent<BoxCollider>());

            BoxView boxView = boxObject.AddComponent<BoxView>();
            SerializedObject boxSerialized = new SerializedObject(boxView);
            boxSerialized.FindProperty("meshRenderer").objectReferenceValue = boxObject.GetComponent<MeshRenderer>();
            boxSerialized.FindProperty("fillIndicator").objectReferenceValue = fillIndicator.transform;
            boxSerialized.FindProperty("colorPalette").objectReferenceValue = palette;
            boxSerialized.ApplyModifiedPropertiesWithoutUndo();

            BoxView prefab = PrefabUtility.SaveAsPrefabAsset(boxObject, BoxPrefabPath).GetComponent<BoxView>();
            Object.DestroyImmediate(boxObject);
            return prefab;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
