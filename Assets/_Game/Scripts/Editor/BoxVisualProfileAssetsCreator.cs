#if UNITY_EDITOR
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public static class BoxVisualProfileAssetsCreator
    {
        private const string BoxVisualProfileFolder = "Assets/_Game/Data/BoxVisualProfiles";

        private static readonly ProfileSeed[] ProfileSeeds =
        {
            new ProfileSeed("Blue_color", BlockColor.Blue, new Color(0.16f, 0.53f, 0.95f)),
            new ProfileSeed("DarkBrown_Color", BlockColor.DarkBrown, new Color(0.46f, 0.29f, 0.13f)),
            new ProfileSeed("DarkGreen_Color", BlockColor.DarkGreen, new Color(0.05f, 0.65f, 0.05f)),
            new ProfileSeed("LightBlue_color", BlockColor.LightBlue, new Color(0.35f, 0.82f, 0.95f)),
            new ProfileSeed("Gray_Color", BlockColor.Gray, new Color(0.42f, 0.46f, 0.58f)),
            new ProfileSeed("Purple_Color", BlockColor.Purple, new Color(0.52f, 0.25f, 0.95f)),
            new ProfileSeed("LightGray_Color", BlockColor.LightGray, new Color(0.72f, 0.78f, 0.92f)),
            new ProfileSeed("White_Color", BlockColor.White, Color.white),
            new ProfileSeed("LightPurple_Color", BlockColor.LightPurple, new Color(0.82f, 0.57f, 0.95f)),
            new ProfileSeed("TealGreen_Color", BlockColor.TealGreen, new Color(0.14f, 0.68f, 0.57f)),
            new ProfileSeed("Yellow_Color", BlockColor.Yellow, new Color(1f, 0.84f, 0.1f)),
            new ProfileSeed("Orange_Color", BlockColor.Orange, new Color(1f, 0.55f, 0.15f)),
            new ProfileSeed("SkyBlue_Color", BlockColor.SkyBlue, new Color(0.43f, 0.7f, 0.97f)),
            new ProfileSeed("Pink_color", BlockColor.Pink, new Color(0.94f, 0.15f, 0.76f)),
            new ProfileSeed("Red_Color", BlockColor.Red, new Color(1f, 0.2f, 0.2f)),
            new ProfileSeed("Green_Color", BlockColor.Green, new Color(0.45f, 0.9f, 0.05f))
        };

        [MenuItem("FlowBlast/Create Box Visual Profiles")]
        public static void CreateBoxVisualProfiles()
        {
            EnsureFolder(BoxVisualProfileFolder);

            for (int i = 0; i < ProfileSeeds.Length; i++)
            {
                CreateProfile(ProfileSeeds[i]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Box Visual Profiles",
                "Created all box visual profiles with names matching your textures in Assets/_Game/Data/BoxVisualProfiles.",
                "OK");
        }

        private static void CreateProfile(ProfileSeed seed)
        {
            string assetPath = $"{BoxVisualProfileFolder}/{seed.AssetName}.asset";
            BoxVisualProfile profile = AssetDatabase.LoadAssetAtPath<BoxVisualProfile>(assetPath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BoxVisualProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            SerializedObject serializedProfile = new SerializedObject(profile);
            serializedProfile.Update();
            serializedProfile.FindProperty("blockColor").enumValueIndex = (int)seed.BlockColor;
            serializedProfile.FindProperty("tintColor").colorValue = seed.TintColor;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
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

        private readonly struct ProfileSeed
        {
            public ProfileSeed(string assetName, BlockColor blockColor, Color tintColor)
            {
                AssetName = assetName;
                BlockColor = blockColor;
                TintColor = tintColor;
            }

            public string AssetName { get; }
            public BlockColor BlockColor { get; }
            public Color TintColor { get; }
        }
    }
}
#endif
