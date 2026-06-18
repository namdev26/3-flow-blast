#if UNITY_EDITOR
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public static class MapEditorAssetsCreator
    {
        private const string DataFolder = "Assets/_Game/Data";
        private const string PrefabFolder = "Assets/_Game/Prefabs";
        private const string SettingsPath = DataFolder + "/MapEditorSettings.asset";
        private const string WaypointPrefabPath = PrefabFolder + "/BeltWaypointMarker.prefab";

        [MenuItem("FlowBlast/Create Map Editor Assets")]
        public static void CreateMapEditorAssets()
        {
            EnsureFolder("Assets/_Game");
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);

            CreateSettingsAsset();
            CreateWaypointPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FlowBlast] Map editor assets created.");
        }

        private static void CreateSettingsAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<MapEditorSettings>(SettingsPath) != null)
            {
                return;
            }

            MapEditorSettings settings = ScriptableObject.CreateInstance<MapEditorSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        private static void CreateWaypointPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WaypointPrefabPath) != null)
            {
                return;
            }

            GameObject waypointObject = new GameObject("BeltWaypointMarker");
            waypointObject.AddComponent<BeltWaypointMarker>();
            PrefabUtility.SaveAsPrefabAsset(waypointObject, WaypointPrefabPath);
            Object.DestroyImmediate(waypointObject);
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
