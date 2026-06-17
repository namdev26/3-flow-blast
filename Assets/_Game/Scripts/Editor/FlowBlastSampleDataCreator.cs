#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public static class FlowBlastSampleDataCreator
    {
        private const string DataFolder = "Assets/_Game/Data";

        [MenuItem("FlowBlast/Create Sample Level Assets")]
        public static void CreateSampleAssets()
        {
            EnsureFolder("Assets/_Game");
            EnsureFolder(DataFolder);

            BlockColorPalette palette = CreatePalette();
            LevelData level = CreateLevel();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FlowBlast] Created sample assets at {DataFolder}. Palette: {palette.name}, Level: {level.name}");
        }

        private static BlockColorPalette CreatePalette()
        {
            string path = $"{DataFolder}/BlockColorPalette.asset";
            BlockColorPalette palette = AssetDatabase.LoadAssetAtPath<BlockColorPalette>(path);

            if (palette != null)
            {
                return palette;
            }

            palette = ScriptableObject.CreateInstance<BlockColorPalette>();
            AssetDatabase.CreateAsset(palette, path);

            SerializedObject serializedPalette = new SerializedObject(palette);
            SerializedProperty entries = serializedPalette.FindProperty("entries");
            entries.arraySize = 4;

            SetPaletteEntry(entries, 0, BlockColor.Red, Color.red);
            SetPaletteEntry(entries, 1, BlockColor.Green, Color.green);
            SetPaletteEntry(entries, 2, BlockColor.Blue, Color.blue);
            SetPaletteEntry(entries, 3, BlockColor.Yellow, Color.yellow);
            serializedPalette.ApplyModifiedPropertiesWithoutUndo();

            return palette;
        }

        private static void SetPaletteEntry(SerializedProperty entries, int index, BlockColor blockColor, Color unityColor)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("BlockColor").enumValueIndex = (int)blockColor;
            entry.FindPropertyRelative("UnityColor").colorValue = unityColor;
        }

        private static LevelData CreateLevel()
        {
            string path = $"{DataFolder}/Level_01.asset";
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

            if (level != null)
            {
                return level;
            }

            level = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(level, path);

            SerializedObject serializedLevel = new SerializedObject(level);
            serializedLevel.FindProperty("levelId").stringValue = "Level_01";
            serializedLevel.FindProperty("beltSpeed").floatValue = 2f;
            serializedLevel.FindProperty("maxBeltSlots").intValue = 2;
            serializedLevel.FindProperty("maxBacklogBlocks").intValue = 20;
            serializedLevel.FindProperty("beltLaneCount").intValue = GameConstants.BeltLaneCount;

            SerializedProperty blockSequence = serializedLevel.FindProperty("blockSequence");
            blockSequence.ClearArray();
            BlockColor[] colors =
            {
                BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green,
                BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red,
                BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue,
                BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow
            };

            for (int i = 0; i < colors.Length; i++)
            {
                blockSequence.InsertArrayElementAtIndex(i);
                blockSequence.GetArrayElementAtIndex(i).enumValueIndex = (int)colors[i];
            }

            SerializedProperty boxQueue = serializedLevel.FindProperty("boxQueue");
            boxQueue.ClearArray();
            boxQueue.InsertArrayElementAtIndex(0);
            SerializedProperty greenBox = boxQueue.GetArrayElementAtIndex(0);
            greenBox.FindPropertyRelative("Color").enumValueIndex = (int)BlockColor.Green;
            greenBox.FindPropertyRelative("Capacity").intValue = 8;
            greenBox.FindPropertyRelative("IsHidden").boolValue = false;
            greenBox.FindPropertyRelative("FrozenClearsRequired").intValue = 0;

            boxQueue.InsertArrayElementAtIndex(1);
            SerializedProperty redBox = boxQueue.GetArrayElementAtIndex(1);
            redBox.FindPropertyRelative("Color").enumValueIndex = (int)BlockColor.Red;
            redBox.FindPropertyRelative("Capacity").intValue = 8;
            redBox.FindPropertyRelative("IsHidden").boolValue = false;
            redBox.FindPropertyRelative("FrozenClearsRequired").intValue = 0;

            serializedLevel.ApplyModifiedPropertiesWithoutUndo();
            return level;
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
