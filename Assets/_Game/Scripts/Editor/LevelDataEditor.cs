#if UNITY_EDITOR
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    [CustomEditor(typeof(LevelData))]
    public sealed class LevelDataEditor : UnityEditor.Editor
    {
        private SerializedProperty levelIdProperty;
        private SerializedProperty beltSpeedProperty;
        private SerializedProperty maxBeltSlotsProperty;
        private SerializedProperty maxBacklogBlocksProperty;
        private SerializedProperty beltLaneCountProperty;
        private SerializedProperty autoBuildBlockSequenceFromBoxesProperty;
        private SerializedProperty boxPlacementsProperty;
        private SerializedProperty blockSequenceProperty;
        private int selectedPlacementIndex = -1;

        private void OnEnable()
        {
            levelIdProperty = serializedObject.FindProperty("levelId");
            beltSpeedProperty = serializedObject.FindProperty("beltSpeed");
            maxBeltSlotsProperty = serializedObject.FindProperty("maxBeltSlots");
            maxBacklogBlocksProperty = serializedObject.FindProperty("maxBacklogBlocks");
            beltLaneCountProperty = serializedObject.FindProperty("beltLaneCount");
            autoBuildBlockSequenceFromBoxesProperty = serializedObject.FindProperty("autoBuildBlockSequenceFromBoxes");
            boxPlacementsProperty = serializedObject.FindProperty("boxPlacements");
            blockSequenceProperty = serializedObject.FindProperty("blockSequence");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Level Editor"))
                {
                    LevelEditorWindow.ShowWindow((LevelData)target);
                }

                if (GUILayout.Button("Ping In Project"))
                {
                    EditorGUIUtility.PingObject(target);
                }
            }

            EditorGUILayout.Space(8f);
            DrawSettingsSection();
            EditorGUILayout.Space(8f);
            DrawBoxPlacementsSection();
            EditorGUILayout.Space(8f);
            DrawBlockSequenceSection();
            EditorGUILayout.Space(8f);
            DrawSummarySection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(levelIdProperty);
            EditorGUILayout.PropertyField(beltSpeedProperty);
            EditorGUILayout.PropertyField(maxBeltSlotsProperty);
            EditorGUILayout.PropertyField(maxBacklogBlocksProperty);
            EditorGUILayout.PropertyField(beltLaneCountProperty);
            EditorGUILayout.PropertyField(autoBuildBlockSequenceFromBoxesProperty);
        }

        private void DrawBoxPlacementsSection()
        {
            EditorGUILayout.LabelField("Box Placements", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Box"))
                {
                    AddPlacement();
                }

                if (GUILayout.Button("Add Row"))
                {
                    AddPlacementRow();
                }

                using (new EditorGUI.DisabledScope(selectedPlacementIndex < 0 || selectedPlacementIndex >= boxPlacementsProperty.arraySize))
                {
                    if (GUILayout.Button("Remove Selected"))
                    {
                        RemoveSelectedPlacement();
                    }
                }
            }

            if (boxPlacementsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add boxes here. Each box stores local position, color, and capacity. Press Play to test the assigned level immediately.", MessageType.Info);
                return;
            }

            for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
            {
                DrawPlacementItem(i, boxPlacementsProperty.GetArrayElementAtIndex(i));
            }
        }

        private void DrawPlacementItem(int index, SerializedProperty placementProperty)
        {
            SerializedProperty localPositionProperty = placementProperty.FindPropertyRelative("localPosition");
            SerializedProperty capacityProperty = placementProperty.FindPropertyRelative("capacity");
            SerializedProperty visualProfileProperty = placementProperty.FindPropertyRelative("visualProfile");
            SerializedProperty isHiddenProperty = placementProperty.FindPropertyRelative("isHidden");
            SerializedProperty frozenClearsRequiredProperty = placementProperty.FindPropertyRelative("frozenClearsRequired");
            bool isSelected = selectedPlacementIndex == index;
            GUIStyle itemStyle = isSelected ? EditorStyles.helpBox : EditorStyles.inspectorDefaultMargins;

            using (new EditorGUILayout.VerticalScope(itemStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(isSelected, $"Box {index + 1}", "Button"))
                    {
                        selectedPlacementIndex = index;
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(visualProfileProperty.objectReferenceValue != null ? visualProfileProperty.objectReferenceValue.name : "No Profile", EditorStyles.miniLabel);
                }

                EditorGUILayout.PropertyField(localPositionProperty);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(visualProfileProperty);

                if (EditorGUI.EndChangeCheck())
                {
                    SyncPlacementColorFromVisualProfile(placementProperty);
                }
                EditorGUILayout.PropertyField(capacityProperty);
                EditorGUILayout.PropertyField(isHiddenProperty);
                EditorGUILayout.PropertyField(frozenClearsRequiredProperty);
            }
        }

        private void DrawBlockSequenceSection()
        {
            bool isAutoBuild = autoBuildBlockSequenceFromBoxesProperty.boolValue;
            EditorGUILayout.LabelField("Block Sequence", EditorStyles.boldLabel);

            if (isAutoBuild)
            {
                EditorGUILayout.HelpBox("Block sequence is generated automatically from box capacities and colors.", MessageType.None);
                return;
            }

            EditorGUILayout.PropertyField(blockSequenceProperty, true);
        }

        private void DrawSummarySection()
        {
            LevelData levelData = (LevelData)target;
            EditorGUILayout.LabelField("Level Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Box Count", levelData.BoxPlacements.Count.ToString());
            EditorGUILayout.LabelField("Generated Block Count", levelData.TotalBlockCount.ToString());
            EditorGUILayout.LabelField("Preview Sequence Rows", levelData.BlockSequence.Count.ToString());
            EditorGUILayout.HelpBox("Assign this LevelData to GameplayInstaller and press Play to test that level in-scene immediately.", MessageType.Info);
        }

        private void AddPlacement()
        {
            int insertIndex = boxPlacementsProperty.arraySize;
            boxPlacementsProperty.arraySize++;
            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(insertIndex);
            placementProperty.FindPropertyRelative("localPosition").vector3Value = GetNextPlacementPosition();
            placementProperty.FindPropertyRelative("capacity").intValue = GameConstants.DefaultBoxCapacity;
            placementProperty.FindPropertyRelative("isHidden").boolValue = false;
            placementProperty.FindPropertyRelative("frozenClearsRequired").intValue = 0;
            selectedPlacementIndex = insertIndex;
        }

        private void AddPlacementRow()
        {
            for (int i = 0; i < 4; i++)
            {
                AddPlacement();
            }
        }

        private void RemoveSelectedPlacement()
        {
            boxPlacementsProperty.DeleteArrayElementAtIndex(selectedPlacementIndex);
            selectedPlacementIndex = Mathf.Clamp(selectedPlacementIndex - 1, -1, boxPlacementsProperty.arraySize - 1);
        }

        private void SyncPlacementColorFromVisualProfile(SerializedProperty placementProperty)
        {
            if (placementProperty == null)
            {
                return;
            }

            SerializedProperty visualProfileProperty = placementProperty.FindPropertyRelative("visualProfile");
            SerializedProperty colorProperty = placementProperty.FindPropertyRelative("color");

            if (visualProfileProperty?.objectReferenceValue is not BoxVisualProfile visualProfile || colorProperty == null)
            {
                return;
            }

            colorProperty.enumValueIndex = (int)visualProfile.BlockColor;
        }

        private Vector3 GetNextPlacementPosition()
        {
            if (boxPlacementsProperty.arraySize == 0)
            {
                return Vector3.zero;
            }

            SerializedProperty previousPlacementProperty = boxPlacementsProperty.GetArrayElementAtIndex(boxPlacementsProperty.arraySize - 1);
            Vector3 previousPosition = previousPlacementProperty.FindPropertyRelative("localPosition").vector3Value;
            return previousPosition + Vector3.right * 1.5f;
        }

    }
}
#endif
