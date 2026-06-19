#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Bootstrap;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using FlowBlast.Presentation.Block;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string DefaultLevelFolder = "Assets/_Game/Data/Levels";
        private const string BoxVisualProfileFolder = "Assets/_Game/Data/BoxVisualProfiles";
        private const string DefaultLevelName = "LevelData";
        private const float GridCellButtonSize = 34f;
        private const float GridColorPreviewSize = 18f;
        private const float VisualPaletteButtonSize = 26f;
        private const float VisualPalettePanelWidth = 152f;
        private const int DefaultGridWidth = 5;
        private const int DefaultGridHeight = 5;

        private static LevelEditorWindow instance;

        private LevelData levelData;
        private GameplayInstaller gameplayInstaller;
        private SerializedObject serializedLevelData;
        private SerializedProperty boxPlacementsProperty;
        private SerializedProperty beltLaneCountProperty;
        private SerializedProperty editorGridColumnsProperty;
        private SerializedProperty editorGridRowsProperty;
        private SerializedProperty mapLayoutProperty;
        private Vector2 windowScrollPosition;
        private int selectedPlacementIndex = -1;
        private int gridWidth = DefaultGridWidth;
        private int gridHeight = DefaultGridHeight;
        private const float GridCellSpacing = 1f;
        private bool brushHidden;
        private int brushFrozenClearsRequired;
        private BoxVisualProfile brushVisualProfile;
        private bool eraseMode;
        private bool showSettings = true;
        private bool showGridAuthoring = true;
        private bool showBlockPreview = true;
        private bool isSaveRequested;
        private List<BoxVisualProfile> cachedVisualProfiles;

        public static bool IsOpen => instance != null;

        [MenuItem("FlowBlast/Level Editor")]
        public static void ShowWindow()
        {
            ShowWindow(null);
        }

        public static void ShowWindow(LevelData targetLevelData)
        {
            LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(520f, 680f);
            window.Initialize(targetLevelData);
            window.Focus();
        }

        private void OnEnable()
        {
            instance = this;
            SceneView.duringSceneGui += OnSceneGui;
            Initialize(levelData);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;

            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is LevelData selectedLevelData)
            {
                Initialize(selectedLevelData);
                Repaint();
                return;
            }

            if (Selection.activeGameObject == null)
            {
                return;
            }

            GameplayInstaller selectedInstaller = Selection.activeGameObject.GetComponentInParent<GameplayInstaller>();

            if (selectedInstaller == null)
            {
                return;
            }

            gameplayInstaller = selectedInstaller;
            Repaint();
        }

        private void Initialize(LevelData targetLevelData)
        {
            levelData = targetLevelData != null
                ? targetLevelData
                : Selection.activeObject as LevelData;
            gameplayInstaller = FindFirstObjectByType<GameplayInstaller>();
            RefreshSerializedData();
            selectedPlacementIndex = Mathf.Clamp(selectedPlacementIndex, -1, GetPlacementCount() - 1);
            SyncGridSettingsFromSerializedData();
        }

        private void RefreshSerializedData()
        {
            if (levelData == null)
            {
                serializedLevelData = null;
                boxPlacementsProperty = null;
                beltLaneCountProperty = null;
                editorGridColumnsProperty = null;
                editorGridRowsProperty = null;
                return;
            }

            serializedLevelData = new SerializedObject(levelData);
            boxPlacementsProperty = serializedLevelData.FindProperty("boxPlacements");
            beltLaneCountProperty = serializedLevelData.FindProperty("beltLaneCount");
            editorGridColumnsProperty = serializedLevelData.FindProperty("editorGridColumns");
            editorGridRowsProperty = serializedLevelData.FindProperty("editorGridRows");
            mapLayoutProperty = serializedLevelData.FindProperty("mapLayout");
            SyncGridSettingsFromSerializedData();
        }

        private void OnGUI()
        {
            DrawHeader();

            if (levelData == null)
            {
                EditorGUILayout.HelpBox("Select a LevelData asset or open this window from a LevelData inspector.", MessageType.Info);
                return;
            }

            RefreshSerializedDataIfNeeded();
            serializedLevelData.Update();
            SyncGridSettingsFromSerializedData();

            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(windowScrollPosition))
            {
                windowScrollPosition = scrollView.scrollPosition;
                DrawSceneBindings();
                EditorGUILayout.Space(6f);
                DrawSettingsSection();
                EditorGUILayout.Space(6f);
                DrawGridAuthoringSection();
                EditorGUILayout.Space(6f);
                DrawSummarySection();
            }

            SyncSerializedGridSettings();

            if (serializedLevelData.hasModifiedProperties)
            {
                serializedLevelData.ApplyModifiedProperties();
            }

            if (isSaveRequested)
            {
                isSaveRequested = false;
                SaveLevel();
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                LevelData newLevelData = (LevelData)EditorGUILayout.ObjectField(levelData, typeof(LevelData), false, GUILayout.MinWidth(220f));

                if (newLevelData != levelData)
                {
                    Initialize(newLevelData);
                }

                GUILayout.Space(6f);

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                {
                    CreateLevelAsset();
                }

                if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                {
                    LoadSelectedLevel();
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                {
                    isSaveRequested = true;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Use Selection", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                {
                    Initialize(Selection.activeObject as LevelData);
                }

                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(44f)) && levelData != null)
                {
                    EditorGUIUtility.PingObject(levelData);
                }
            }
        }

        private void DrawSceneBindings()
        {
            EditorGUILayout.LabelField("Scene Bindings", EditorStyles.boldLabel);
            gameplayInstaller = (GameplayInstaller)EditorGUILayout.ObjectField("Gameplay Installer", gameplayInstaller, typeof(GameplayInstaller), true);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Board Root", ResolveBoardRoot(), typeof(Transform), true);
            }

            Transform collectionPointMarker = ResolveCollectionPointMarker();
            EditorGUI.BeginChangeCheck();
            collectionPointMarker = (Transform)EditorGUILayout.ObjectField("Collection Marker", collectionPointMarker, typeof(Transform), true);

            if (EditorGUI.EndChangeCheck())
            {
                AssignCollectionPointMarker(collectionPointMarker);
            }

            EditorGUILayout.HelpBox("Grid authoring uses local positions relative to Board Root. Click a grid cell to place, update, or remove a box.", MessageType.None);
        }

        private void DrawSettingsSection()
        {
            showSettings = EditorGUILayout.Foldout(showSettings, "Level Settings", true);

            if (!showSettings)
            {
                return;
            }

            DrawProperty("levelId");
            DrawMapLayoutPicker();
            DrawProperty("beltSpeed");
            DrawProperty("maxBeltSlots");
            DrawProperty("maxBacklogBlocks");
            DrawProperty("beltLaneCount");
            DrawProperty("boxCapacity");
            DrawProperty("autoBuildBlockSequenceFromBoxes");
        }

        private void DrawMapLayoutPicker()
        {
            if (mapLayoutProperty == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            LevelMapLayout newLayout = (LevelMapLayout)EditorGUILayout.ObjectField(
                "Map Layout",
                mapLayoutProperty.objectReferenceValue as LevelMapLayout,
                typeof(LevelMapLayout),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                mapLayoutProperty.objectReferenceValue = newLayout;
                SceneView.RepaintAll();
            }
        }

        private void DrawGridAuthoringSection()
        {
            showGridAuthoring = EditorGUILayout.Foldout(showGridAuthoring, "Grid Authoring", true);

            if (!showGridAuthoring)
            {
                return;
            }

            DrawGridSettings();
            EditorGUILayout.Space(4f);
            DrawBrushSettings();
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawGridCanvas();
                }

                GUILayout.Space(8f);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(VisualPalettePanelWidth)))
                {
                    DrawBrushVisualPalette();
                }
            }
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);

            if (editorGridColumnsProperty != null && editorGridRowsProperty != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    editorGridColumnsProperty.intValue = Mathf.Max(1, EditorGUILayout.IntField("Columns", editorGridColumnsProperty.intValue));
                    editorGridRowsProperty.intValue = Mathf.Max(1, EditorGUILayout.IntField("Rows", editorGridRowsProperty.intValue));
                }

                gridWidth = editorGridColumnsProperty.intValue;
                gridHeight = editorGridRowsProperty.intValue;
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    gridWidth = Mathf.Max(1, EditorGUILayout.IntField("Columns", gridWidth));
                    gridHeight = Mathf.Max(1, EditorGUILayout.IntField("Rows", gridHeight));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Center Layout"))
                {
                    CenterAllPlacements();
                }
            }
        }

        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            DrawInlineColorPreview(ResolveBrushPreviewColor());
            brushHidden = EditorGUILayout.Toggle("Is Hidden", brushHidden);
            brushFrozenClearsRequired = Mathf.Max(0, EditorGUILayout.IntField("Frozen Clears", brushFrozenClearsRequired));
            eraseMode = EditorGUILayout.Toggle("Erase Mode", eraseMode);
        }

        private void DrawGridCanvas()
        {
            EditorGUILayout.LabelField($"Grid Preview ({gridWidth} x {gridHeight})", EditorStyles.boldLabel);

            for (int row = 0; row < gridHeight; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label((gridHeight - row).ToString(), EditorStyles.miniLabel, GUILayout.Width(18f));

                    for (int column = 0; column < gridWidth; column++)
                    {
                        DrawGridCell(column, row);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(18f);

                for (int column = 0; column < gridWidth; column++)
                {
                    GUILayout.Label((column + 1).ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(GridCellButtonSize));
                }
            }
        }

        private void DrawGridCell(int column, int row)
        {
            int placementIndex = FindPlacementIndexAtGridCell(column, row);
            bool hasPlacement = placementIndex >= 0;
            bool isSelected = placementIndex == selectedPlacementIndex;
            Color fillColor = hasPlacement
                ? ResolvePlacementColor(placementIndex)
                : new Color(0.16f, 0.16f, 0.16f, 1f);
            Rect cellRect = GUILayoutUtility.GetRect(GridCellButtonSize, GridCellButtonSize, GUILayout.Width(GridCellButtonSize), GUILayout.Height(GridCellButtonSize));

            EditorGUI.DrawRect(cellRect, fillColor);
            DrawCellOutline(cellRect, isSelected ? Color.white : Color.black);

            if (hasPlacement)
            {
                DrawCellLabel(cellRect, placementIndex + 1, GetReadableTextColor(fillColor));
            }

            if (HandleGridCellRightClick(cellRect, placementIndex))
            {
                return;
            }

            if (GUI.Button(cellRect, GUIContent.none, GUIStyle.none))
            {
                HandleGridCellClick(column, row, placementIndex);
            }
        }

        private bool HandleGridCellRightClick(Rect cellRect, int placementIndex)
        {
            Event currentEvent = Event.current;

            if (currentEvent == null)
            {
                return false;
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 1)
            {
                return false;
            }

            if (!cellRect.Contains(currentEvent.mousePosition) || placementIndex < 0)
            {
                return false;
            }

            selectedPlacementIndex = placementIndex;
            RemoveSelectedPlacement();
            currentEvent.Use();
            return true;
        }

        private void DrawSummarySection()
        {
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Box Count", GetPlacementCount().ToString());
            EditorGUILayout.LabelField("Total Blocks", GetTotalBlockCountFromSerializedData().ToString());
            EditorGUILayout.LabelField("Sequence Rows", levelData.BlockSequence.Count.ToString());
            EditorGUILayout.LabelField("Lane Count", beltLaneCountProperty.intValue.ToString());

            LevelMapLayout layout = mapLayoutProperty?.objectReferenceValue as LevelMapLayout;
            EditorGUILayout.LabelField("Map Layout", layout != null ? layout.name : "— None —");

            EditorGUI.BeginChangeCheck();
            showBlockPreview = EditorGUILayout.Toggle("Preview Blocks On Paths", showBlockPreview);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSceneGui(SceneView sceneView)
        {
            if (!IsOpen || levelData == null)
            {
                return;
            }

            RefreshSerializedDataIfNeeded();

            if (serializedLevelData == null || boxPlacementsProperty == null)
            {
                return;
            }

            serializedLevelData.Update();

            if (showBlockPreview)
            {
                DrawBlockPreviewOnPaths();
            }

            DrawScenePlacements();
            DrawSelectedPlacementHandle();
            serializedLevelData.ApplyModifiedProperties();
        }

        private void DrawScenePlacements()
        {
            Transform boardRoot = ResolveBoardRoot();
            Vector3 boardPosition = boardRoot != null ? boardRoot.position : Vector3.zero;
            Quaternion boardRotation = boardRoot != null ? boardRoot.rotation : Quaternion.identity;

            for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
            {
                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(i);
                Vector3 localPosition = placementProperty.FindPropertyRelative("localPosition").vector3Value;
                BlockColor color = (BlockColor)placementProperty.FindPropertyRelative("color").enumValueIndex;
                Vector3 worldPosition = boardPosition + boardRotation * localPosition;
                float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.14f;
                Handles.color = i == selectedPlacementIndex ? Color.white : ResolveBoxColor(color);

                if (Handles.Button(worldPosition, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                {
                    SelectPlacement(i);
                }

                Handles.Label(worldPosition + Vector3.up * handleSize * 1.6f, $"B{i + 1}");
            }
        }

        private void DrawSelectedPlacementHandle()
        {
            if (!HasSelectedPlacement())
            {
                return;
            }

            Transform boardRoot = ResolveBoardRoot();
            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(selectedPlacementIndex);
            SerializedProperty localPositionProperty = placementProperty.FindPropertyRelative("localPosition");
            Vector3 localPosition = localPositionProperty.vector3Value;
            Vector3 worldPosition = LocalToWorld(boardRoot, localPosition);

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(levelData, "Move Level Box");
            Vector3 previousLocalPosition = localPositionProperty.vector3Value;
            Vector3 newLocalPosition = WorldToLocal(boardRoot, newWorldPosition);
            localPositionProperty.vector3Value = GetUniqueSnappedPosition(
                SnapLocalPosition(newLocalPosition),
                selectedPlacementIndex,
                previousLocalPosition);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void HandleGridCellClick(int column, int row, int placementIndex)
        {
            if (eraseMode)
            {
                if (placementIndex >= 0)
                {
                    selectedPlacementIndex = placementIndex;
                    RemoveSelectedPlacement();
                }

                return;
            }

            if (placementIndex >= 0)
            {
                UpdatePlacementAtGridCell(placementIndex, column, row);
                return;
            }

            AddPlacementAtGridCell(column, row);
        }

        private void AddPlacementAtGridCell(int column, int row)
        {
            serializedLevelData.Update();
            int insertIndex = AppendPlacement();
            selectedPlacementIndex = insertIndex;
            ApplyBrushToPlacement(insertIndex);
            MovePlacementToGridCell(insertIndex, column, row);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void UpdatePlacementAtGridCell(int placementIndex, int column, int row)
        {
            if (placementIndex < 0 || placementIndex >= boxPlacementsProperty.arraySize)
            {
                return;
            }

            serializedLevelData.Update();
            selectedPlacementIndex = placementIndex;
            ApplyBrushToPlacement(placementIndex);
            MovePlacementToGridCell(placementIndex, column, row);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
            SceneView.RepaintAll();
        }

        private void ApplyBrushToPlacement(int placementIndex)
        {
            if (placementIndex < 0 || placementIndex >= boxPlacementsProperty.arraySize)
            {
                return;
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
            placementProperty.FindPropertyRelative("visualProfile").objectReferenceValue = brushVisualProfile;
            SyncPlacementColorFromVisualProfile(placementProperty);
            placementProperty.FindPropertyRelative("isHidden").boolValue = brushHidden;
            placementProperty.FindPropertyRelative("frozenClearsRequired").intValue = brushFrozenClearsRequired;
        }

        private void MovePlacementToGridCell(int placementIndex, int column, int row)
        {
            if (placementIndex < 0 || placementIndex >= boxPlacementsProperty.arraySize)
            {
                return;
            }

            int existingPlacementIndex = FindPlacementIndexAtGridCell(column, row);

            if (existingPlacementIndex >= 0 && existingPlacementIndex != placementIndex)
            {
                SelectPlacement(existingPlacementIndex);
                return;
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
            placementProperty.FindPropertyRelative("localPosition").vector3Value = GetLocalPositionFromGridCell(column, row);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
            SceneView.RepaintAll();
        }

        private void CenterAllPlacements()
        {
            if (GetPlacementCount() == 0)
            {
                return;
            }

            serializedLevelData.Update();

            for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
            {
                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(i);
                Vector2Int cell = GetGridCellFromLocalPosition(placementProperty.FindPropertyRelative("localPosition").vector3Value);
                placementProperty.FindPropertyRelative("localPosition").vector3Value = GetLocalPositionFromGridCell(cell.x, cell.y);
            }

            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
            SceneView.RepaintAll();
        }

        private void DrawColorPreview(Color color)
        {
            Rect previewRect = GUILayoutUtility.GetRect(GridColorPreviewSize, GridColorPreviewSize, GUILayout.Width(GridColorPreviewSize), GUILayout.Height(36f));
            EditorGUI.DrawRect(previewRect, color);
            DrawCellOutline(previewRect, Color.black);
        }

        private void DrawInlineColorPreview(Color color)
        {
            Rect previewRect = GUILayoutUtility.GetRect(20f, 20f, GUILayout.Width(20f), GUILayout.Height(18f));
            EditorGUI.DrawRect(previewRect, color);
            DrawCellOutline(previewRect, Color.black);
        }

        private void DrawBrushVisualPalette()
        {
            EditorGUILayout.LabelField("Visual Palette", EditorStyles.boldLabel);
            DrawVisualProfilePalette(
                brushVisualProfile,
                profile => brushVisualProfile = profile,
                false,
                "Choose brush color");
        }

        private void DrawSelectedPlacementVisualPalette(SerializedProperty placementProperty, SerializedProperty visualProfileProperty)
        {
            if (placementProperty == null || visualProfileProperty == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Visual Profile", EditorStyles.boldLabel);
            DrawVisualProfilePalette(
                visualProfileProperty.objectReferenceValue as BoxVisualProfile,
                profile =>
                {
                    visualProfileProperty.objectReferenceValue = profile;
                    SyncPlacementColorFromVisualProfile(placementProperty);
                },
                true,
                "Choose box color");
        }

        private void DrawVisualProfilePalette(
            BoxVisualProfile selectedProfile,
            System.Action<BoxVisualProfile> onSelected,
            bool allowNone,
            string tooltip)
        {
            List<BoxVisualProfile> visualProfiles = GetVisualProfiles();
            int buttonCount = allowNone ? visualProfiles.Count + 1 : visualProfiles.Count;

            if (buttonCount == 0)
            {
                EditorGUILayout.HelpBox("No BoxVisualProfile assets found.", MessageType.Warning);
                return;
            }

            int columns = 4;
            int index = 0;

            while (index < buttonCount)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        if (index >= buttonCount)
                        {
                            GUILayout.Space(VisualPaletteButtonSize + 4f);
                            continue;
                        }

                        if (allowNone && index == 0)
                        {
                            DrawVisualProfileButton(selectedProfile == null, Color.gray, "None", () => onSelected?.Invoke(null), tooltip);
                            index++;
                            continue;
                        }

                        int profileIndex = allowNone ? index - 1 : index;
                        BoxVisualProfile profile = visualProfiles[profileIndex];
                        bool isSelected = selectedProfile == profile;
                        DrawVisualProfileButton(isSelected, profile.TintColor, profile.name, () => onSelected?.Invoke(profile), tooltip);
                        index++;
                    }
                }
            }
        }

        private void DrawVisualProfileButton(bool isSelected, Color color, string label, System.Action onClick, string tooltip)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect buttonRect = GUILayoutUtility.GetRect(
                VisualPaletteButtonSize,
                VisualPaletteButtonSize,
                GUILayout.Width(VisualPaletteButtonSize),
                GUILayout.Height(VisualPaletteButtonSize));

            if (GUI.Button(buttonRect, new GUIContent(string.Empty, $"{label}\n{tooltip}"), buttonStyle))
            {
                onClick?.Invoke();
            }

            EditorGUI.DrawRect(new Rect(buttonRect.x + 3f, buttonRect.y + 3f, buttonRect.width - 6f, buttonRect.height - 6f), color);
            DrawCellOutline(buttonRect, isSelected ? Color.white : Color.black);
        }

        private List<BoxVisualProfile> GetVisualProfiles()
        {
            if (cachedVisualProfiles != null && cachedVisualProfiles.Count > 0)
            {
                return cachedVisualProfiles;
            }

            string[] profileGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { BoxVisualProfileFolder });
            cachedVisualProfiles = new List<BoxVisualProfile>(profileGuids.Length);

            for (int i = 0; i < profileGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(profileGuids[i]);
                BoxVisualProfile profile = AssetDatabase.LoadAssetAtPath<BoxVisualProfile>(assetPath);

                if (profile != null)
                {
                    cachedVisualProfiles.Add(profile);
                }
            }

            cachedVisualProfiles.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return cachedVisualProfiles;
        }

        private void DrawCellOutline(Rect rect, Color outlineColor)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), outlineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), outlineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), outlineColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), outlineColor);
        }

        private void DrawCellLabel(Rect rect, int label, Color textColor)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor }
            };
            GUI.Label(rect, label.ToString(), labelStyle);
        }

        private void SelectPlacement(int index)
        {
            selectedPlacementIndex = index;
            Repaint();
            SceneView.RepaintAll();
        }

        private void RemoveSelectedPlacement()
        {
            if (!HasSelectedPlacement())
            {
                return;
            }

            serializedLevelData.Update();
            RemovePlacementAtIndex(selectedPlacementIndex);
            selectedPlacementIndex = Mathf.Clamp(selectedPlacementIndex - 1, -1, boxPlacementsProperty.arraySize - 1);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
            SceneView.RepaintAll();
        }

        private void ClearAllPlacements()
        {
            if (GetPlacementCount() == 0)
            {
                return;
            }

            serializedLevelData.Update();
            boxPlacementsProperty.ClearArray();
            selectedPlacementIndex = -1;
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
            SceneView.RepaintAll();
        }

        private void FrameSelectedPlacementInScene()
        {
            if (!HasSelectedPlacement())
            {
                return;
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(selectedPlacementIndex);
            Vector3 localPosition = placementProperty.FindPropertyRelative("localPosition").vector3Value;
            Vector3 worldPosition = LocalToWorld(ResolveBoardRoot(), localPosition);
            SceneView.lastActiveSceneView?.LookAt(worldPosition);
            SceneView.RepaintAll();
        }

        private bool HasSelectedPlacement()
        {
            return selectedPlacementIndex >= 0 && selectedPlacementIndex < GetPlacementCount();
        }

        private int GetPlacementCount()
        {
            return boxPlacementsProperty != null ? boxPlacementsProperty.arraySize : 0;
        }

        private int GetTotalBlockCountFromSerializedData()
        {
            if (boxPlacementsProperty == null)
            {
                return 0;
            }

            SerializedProperty boxCapacityProperty = serializedLevelData.FindProperty("boxCapacity");
            int safeBoxCapacity = Mathf.Max(1, boxCapacityProperty != null ? boxCapacityProperty.intValue : GameConstants.DefaultBoxCapacity);
            return boxPlacementsProperty.arraySize * safeBoxCapacity;
        }

        private int AppendPlacement()
        {
            int insertIndex = boxPlacementsProperty.arraySize;
            boxPlacementsProperty.arraySize++;
            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(insertIndex);
            placementProperty.FindPropertyRelative("localPosition").vector3Value = Vector3.zero;
            placementProperty.FindPropertyRelative("color").enumValueIndex = (int)BlockColor.Green;
            placementProperty.FindPropertyRelative("isHidden").boolValue = false;
            placementProperty.FindPropertyRelative("frozenClearsRequired").intValue = 0;
            return insertIndex;
        }

        private void RemovePlacementAtIndex(int placementIndex)
        {
            boxPlacementsProperty.DeleteArrayElementAtIndex(placementIndex);
        }

        private void RefreshSerializedDataIfNeeded()
        {
            if (levelData == null)
            {
                return;
            }

            if (serializedLevelData == null || serializedLevelData.targetObject != levelData)
            {
                RefreshSerializedData();
            }
        }

        private void DrawProperty(string propertyName)
        {
            SerializedProperty property = serializedLevelData.FindProperty(propertyName);

            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void CreateLevelAsset()
        {
            EnsureFolder(DefaultLevelFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Level Data",
                DefaultLevelName,
                "asset",
                "Choose save location for the new level data asset.",
                DefaultLevelFolder);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            LevelData newLevelData = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(newLevelData, path);
            AssetDatabase.SaveAssets();
            Initialize(newLevelData);
            ApplyDefaultLevelMetadata(path);
            EditorGUIUtility.PingObject(newLevelData);
            Selection.activeObject = newLevelData;
            ShowNotification(new GUIContent($"Created: {newLevelData.name}"));
        }

        private void LoadSelectedLevel()
        {
            if (Selection.activeObject is not LevelData selectedLevelData)
            {
                ShowNotification(new GUIContent("Select a LevelData asset first"));
                return;
            }

            Initialize(selectedLevelData);
            EditorGUIUtility.PingObject(selectedLevelData);
            ShowNotification(new GUIContent($"Loaded: {selectedLevelData.name}"));
        }

        private void SaveLevel()
        {
            if (levelData == null)
            {
                ShowNotification(new GUIContent("No level selected"));
                return;
            }

            PersistLevelDataChanges();
            RefreshSerializedData();
            Repaint();
            ShowNotification(new GUIContent($"Saved: {levelData.name}"));
        }

        private void ApplyDefaultLevelMetadata(string assetPath)
        {
            if (levelData == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(levelData);
            SerializedProperty levelIdProperty = serializedObject.FindProperty("levelId");

            if (levelIdProperty == null)
            {
                return;
            }

            serializedObject.Update();
            levelIdProperty.stringValue = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();
            RefreshSerializedData();
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

        private Transform ResolveBoardRoot()
        {
            if (gameplayInstaller == null)
            {
                return null;
            }

            SerializedObject serializedInstaller = new SerializedObject(gameplayInstaller);
            SerializedProperty boardRootProperty = serializedInstaller.FindProperty("boardRoot");
            return boardRootProperty != null
                ? boardRootProperty.objectReferenceValue as Transform
                : null;
        }

        private Transform ResolveCollectionPointMarker()
        {
            if (gameplayInstaller == null)
            {
                return null;
            }

            SerializedObject serializedInstaller = new SerializedObject(gameplayInstaller);
            SerializedProperty collectionPointMarkerProperty = serializedInstaller.FindProperty("collectionPointMarker");
            return collectionPointMarkerProperty != null
                ? collectionPointMarkerProperty.objectReferenceValue as Transform
                : null;
        }

        private void AssignCollectionPointMarker(Transform collectionPointMarker)
        {
            if (gameplayInstaller == null)
            {
                return;
            }

            SerializedObject serializedInstaller = new SerializedObject(gameplayInstaller);
            SerializedProperty collectionPointMarkerProperty = serializedInstaller.FindProperty("collectionPointMarker");

            if (collectionPointMarkerProperty == null)
            {
                return;
            }

            serializedInstaller.Update();
            collectionPointMarkerProperty.objectReferenceValue = collectionPointMarker;
            serializedInstaller.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameplayInstaller);
        }

        private void DrawBlockPreviewOnPaths()
        {
            IReadOnlyList<BoxVisualProfile> sequence = levelData.BlockSequence;

            if (sequence == null || sequence.Count == 0)
            {
                return;
            }

            BeltPath mainPath = ResolveMainBeltPath();

            if (mainPath == null || mainPath.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            float rowSpacing = ResolveBlockRowSpacing();
            int laneCount = levelData.BeltLaneCount;
            float laneSpacing = rowSpacing;
            int sequenceCursor = 0;

            LevelMapLayout layout = mapLayoutProperty?.objectReferenceValue as LevelMapLayout;

            if (layout != null && layout.MainWaypointLocalPositions.Count >= 2)
            {
                DrawPreviewFromLayout(layout, mainPath.transform, sequence, ref sequenceCursor, rowSpacing, laneCount, laneSpacing);
                return;
            }

            DrawBlockRowsOnPath(mainPath, sequence, ref sequenceCursor, rowSpacing, laneCount, laneSpacing, 0.9f);

            List<BeltPath> queuePaths = ResolveSceneQueuePaths();

            for (int i = 0; i < queuePaths.Count; i++)
            {
                if (sequenceCursor >= sequence.Count)
                {
                    break;
                }

                BeltPath queuePath = queuePaths[i];

                if (queuePath == null || queuePath.TotalLength <= Mathf.Epsilon)
                {
                    continue;
                }

                DrawBlockRowsOnPath(queuePath, sequence, ref sequenceCursor, rowSpacing, laneCount, laneSpacing, 0.65f);
            }
        }

        private void DrawPreviewFromLayout(
            LevelMapLayout layout,
            Transform pathRootTransform,
            IReadOnlyList<BoxVisualProfile> sequence,
            ref int cursor,
            float rowSpacing,
            int laneCount,
            float laneSpacing)
        {
            CatmullRomPathSampler mainSampler = BuildSamplerFromLocalWaypoints(
                layout.MainWaypointLocalPositions,
                layout.IsMainPathClosedLoop,
                layout.MainCurveStrength,
                pathRootTransform);

            DrawBlockRowsOnSampler(mainSampler, sequence, ref cursor, rowSpacing, laneCount, laneSpacing, 0.9f);

            for (int i = 0; i < layout.QueuePaths.Count; i++)
            {
                if (cursor >= sequence.Count)
                {
                    break;
                }

                QueuePathLayout queueLayout = layout.QueuePaths[i];

                if (queueLayout == null || queueLayout.WaypointLocalPositions.Count < 2)
                {
                    continue;
                }

                CatmullRomPathSampler queueSampler = BuildSamplerFromLocalWaypoints(
                    queueLayout.WaypointLocalPositions,
                    queueLayout.IsClosedLoop,
                    queueLayout.CurveStrength,
                    pathRootTransform);

                DrawBlockRowsOnSampler(queueSampler, sequence, ref cursor, rowSpacing, laneCount, laneSpacing, 0.65f);
            }
        }

        private void DrawBlockRowsOnSampler(
            CatmullRomPathSampler sampler,
            IReadOnlyList<BoxVisualProfile> sequence,
            ref int cursor,
            float rowSpacing,
            int laneCount,
            float laneSpacing,
            float alpha)
        {
            if (sampler == null || sampler.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            int rowCapacity = Mathf.FloorToInt(sampler.TotalLength / rowSpacing);
            int rowsToShow = Mathf.Min(rowCapacity, sequence.Count - cursor);

            for (int rowIndex = 0; rowIndex < rowsToShow; rowIndex++)
            {
                BoxVisualProfile profile = sequence[cursor + rowIndex];
                Color color = profile != null ? profile.TintColor : Color.gray;
                color.a = alpha;
                Handles.color = color;

                float rowDistance = rowIndex * rowSpacing;
                Vector3 center = sampler.GetPositionAtDistance(rowDistance);
                Quaternion rotation = sampler.GetRotationAtDistance(rowDistance);
                float handleSize = HandleUtility.GetHandleSize(center) * 0.055f;

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    Vector3 lanePosition = BeltLaneLayout.GetLanePosition(center, rotation, laneIndex, laneCount, laneSpacing);
                    Handles.DrawSolidDisc(lanePosition, rotation * Vector3.up, handleSize);
                }
            }

            cursor += rowsToShow;
        }

        private void DrawBlockRowsOnPath(
            BeltPath path,
            IReadOnlyList<BoxVisualProfile> sequence,
            ref int cursor,
            float rowSpacing,
            int laneCount,
            float laneSpacing,
            float alpha)
        {
            int rowCapacity = Mathf.FloorToInt(path.TotalLength / rowSpacing);
            int rowsToShow = Mathf.Min(rowCapacity, sequence.Count - cursor);

            for (int rowIndex = 0; rowIndex < rowsToShow; rowIndex++)
            {
                BoxVisualProfile profile = sequence[cursor + rowIndex];
                Color color = profile != null ? profile.TintColor : Color.gray;
                color.a = alpha;
                Handles.color = color;

                float rowDistance = rowIndex * rowSpacing;
                Vector3 center = path.GetPositionAtDistance(rowDistance);
                Quaternion rotation = path.GetRotationAtDistance(rowDistance);
                float handleSize = HandleUtility.GetHandleSize(center) * 0.055f;

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    Vector3 lanePosition = BeltLaneLayout.GetLanePosition(center, rotation, laneIndex, laneCount, laneSpacing);
                    Handles.DrawSolidDisc(lanePosition, rotation * Vector3.up, handleSize);
                }
            }

            cursor += rowsToShow;
        }

        private static CatmullRomPathSampler BuildSamplerFromLocalWaypoints(
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            float curveStrength,
            Transform parentTransform)
        {
            List<Vector3> worldPositions = new List<Vector3>(localPositions.Count);

            for (int i = 0; i < localPositions.Count; i++)
            {
                Vector3 worldPos = parentTransform != null
                    ? parentTransform.TransformPoint(localPositions[i])
                    : localPositions[i];
                worldPositions.Add(worldPos);
            }

            CatmullRomPathSampler sampler = new CatmullRomPathSampler();
            sampler.Rebuild(worldPositions, closedLoop, curveStrength);
            return sampler;
        }

        private BeltPath ResolveMainBeltPath()
        {
            if (gameplayInstaller == null)
            {
                return null;
            }

            Transform conveyorRoot = TransformHierarchyUtility.FindChildRecursive(
                gameplayInstaller.transform,
                GameplayZoneNames.ConveyorRoot);

            return conveyorRoot != null ? conveyorRoot.GetComponent<BeltPath>() : null;
        }

        private List<BeltPath> ResolveSceneQueuePaths()
        {
            List<BeltPath> result = new List<BeltPath>();

            if (gameplayInstaller == null)
            {
                return result;
            }

            BeltPath mainPath = ResolveMainBeltPath();
            BeltPath[] allPaths = gameplayInstaller.GetComponentsInChildren<BeltPath>(true);

            for (int i = 0; i < allPaths.Length; i++)
            {
                BeltPath candidate = allPaths[i];

                if (candidate == null || candidate == mainPath || candidate.PathRole != BeltPathRole.Queue)
                {
                    continue;
                }

                result.Add(candidate);
            }

            result.Sort((left, right) => string.CompareOrdinal(left.PathId, right.PathId));
            return result;
        }

        private float ResolveBlockRowSpacing()
        {
            if (gameplayInstaller == null)
            {
                return GameConstants.FallbackBlockSpacing;
            }

            SerializedObject installerSO = new SerializedObject(gameplayInstaller);
            BlockView blockPrefab = installerSO.FindProperty("blockPrefab")?.objectReferenceValue as BlockView;
            return blockPrefab != null
                ? BlockBeltLayout.CalculateSpacing(blockPrefab)
                : GameConstants.FallbackBlockSpacing;
        }

        private void OnProjectChange()
        {
            cachedVisualProfiles = null;
            Repaint();
        }

        private Color ResolvePlacementColor(int placementIndex)
        {
            if (placementIndex < 0 || placementIndex >= boxPlacementsProperty.arraySize)
            {
                return new Color(0.16f, 0.16f, 0.16f, 1f);
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
            SerializedProperty visualProfileProperty = placementProperty.FindPropertyRelative("visualProfile");
            return ResolvePlacementPreviewColor(visualProfileProperty);
        }

        private Color ResolvePlacementPreviewColor(SerializedProperty visualProfileProperty)
        {
            if (visualProfileProperty?.objectReferenceValue is BoxVisualProfile visualProfile)
            {
                return visualProfile.TintColor;
            }

            return Color.gray;
        }

        private Color ResolveBrushPreviewColor()
        {
            if (brushVisualProfile != null)
            {
                return brushVisualProfile.TintColor;
            }

            return Color.gray;
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

        private Color ResolveBoxColor(BlockColor blockColor)
        {
            BlockColorPalette colorPalette = ResolveColorPalette();

            if (colorPalette != null)
            {
                return colorPalette.GetUnityColor(blockColor);
            }

            return GetFallbackColor(blockColor);
        }

        private BlockColorPalette ResolveColorPalette()
        {
            if (gameplayInstaller == null)
            {
                return null;
            }

            SerializedObject serializedInstaller = new SerializedObject(gameplayInstaller);
            SerializedProperty colorPaletteProperty = serializedInstaller.FindProperty("colorPalette");
            return colorPaletteProperty != null
                ? colorPaletteProperty.objectReferenceValue as BlockColorPalette
                : null;
        }

        private int FindPlacementIndexAtGridCell(int column, int row)
        {
            for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
            {
                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(i);
                Vector2Int cell = GetGridCellFromLocalPosition(placementProperty.FindPropertyRelative("localPosition").vector3Value);

                if (cell.x == column && cell.y == row)
                {
                    return i;
                }
            }

            return -1;
        }

        private Vector3 GetUniqueSnappedPosition(Vector3 targetPosition, int movingPlacementIndex, Vector3 fallbackPosition)
        {
            for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
            {
                if (i == movingPlacementIndex)
                {
                    continue;
                }

                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(i);
                Vector3 existingPosition = placementProperty.FindPropertyRelative("localPosition").vector3Value;

                if ((existingPosition - targetPosition).sqrMagnitude <= 0.0001f)
                {
                    return fallbackPosition;
                }
            }

            return targetPosition;
        }

        private Vector3 GetLocalPositionFromGridCell(int column, int row)
        {
            float offsetX = (gridWidth - 1) * GridCellSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * GridCellSpacing * 0.5f;
            float x = column * GridCellSpacing - offsetX;
            float z = offsetZ - row * GridCellSpacing;
            return SnapLocalPosition(new Vector3(x, 0f, z));
        }

        private Vector2Int GetGridCellFromLocalPosition(Vector3 localPosition)
        {
            Vector2Int rawCell = GetRawGridCellFromLocalPosition(localPosition);
            return new Vector2Int(
                Mathf.Clamp(rawCell.x, 0, Mathf.Max(0, gridWidth - 1)),
                Mathf.Clamp(rawCell.y, 0, Mathf.Max(0, gridHeight - 1)));
        }

        private Vector2Int GetRawGridCellFromLocalPosition(Vector3 localPosition)
        {
            float offsetX = (gridWidth - 1) * GridCellSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * GridCellSpacing * 0.5f;
            int column = Mathf.RoundToInt((localPosition.x + offsetX) / GridCellSpacing);
            int row = Mathf.RoundToInt((offsetZ - localPosition.z) / GridCellSpacing);
            return new Vector2Int(column, row);
        }

        private void SyncGridSettingsFromSerializedData()
        {
            if (editorGridColumnsProperty != null)
            {
                gridWidth = Mathf.Max(1, editorGridColumnsProperty.intValue);
            }

            if (editorGridRowsProperty != null)
            {
                gridHeight = Mathf.Max(1, editorGridRowsProperty.intValue);
            }

        }

        private void SyncSerializedGridSettings()
        {
            if (editorGridColumnsProperty != null)
            {
                editorGridColumnsProperty.intValue = Mathf.Max(1, gridWidth);
            }

            if (editorGridRowsProperty != null)
            {
                editorGridRowsProperty.intValue = Mathf.Max(1, gridHeight);
            }

        }

        private void PersistLevelDataChanges()
        {
            if (levelData == null || serializedLevelData == null)
            {
                return;
            }

            SyncSerializedGridSettings();
            serializedLevelData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssetIfDirty(levelData);
            serializedLevelData.UpdateIfRequiredOrScript();
        }

        private static Vector3 LocalToWorld(Transform boardRoot, Vector3 localPosition)
        {
            return boardRoot != null
                ? boardRoot.TransformPoint(localPosition)
                : localPosition;
        }

        private static Vector3 WorldToLocal(Transform boardRoot, Vector3 worldPosition)
        {
            return boardRoot != null
                ? boardRoot.InverseTransformPoint(worldPosition)
                : worldPosition;
        }

        private static Vector3 SnapLocalPosition(Vector3 localPosition)
        {
            return new Vector3(
                Mathf.Round(localPosition.x * 2f) * 0.5f,
                Mathf.Round(localPosition.y * 2f) * 0.5f,
                Mathf.Round(localPosition.z * 2f) * 0.5f);
        }

        private static Vector3Int GetCellKey(Vector3 localPosition)
        {
            return new Vector3Int(
                Mathf.RoundToInt(localPosition.x * 2f),
                Mathf.RoundToInt(localPosition.y * 2f),
                Mathf.RoundToInt(localPosition.z * 2f));
        }

        private static Color GetReadableTextColor(Color backgroundColor)
        {
            float luminance = backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f;
            return luminance > 0.6f ? Color.black : Color.white;
        }

        private static Color GetFallbackColor(BlockColor blockColor)
        {
            return blockColor switch
            {
                BlockColor.Red => new Color(0.9f, 0.25f, 0.25f),
                BlockColor.Green => new Color(0.25f, 0.85f, 0.35f),
                BlockColor.Blue => new Color(0.25f, 0.5f, 0.95f),
                BlockColor.Yellow => new Color(0.95f, 0.8f, 0.2f),
                BlockColor.Purple => new Color(0.7f, 0.35f, 0.9f),
                BlockColor.Cyan => new Color(0.2f, 0.85f, 0.9f),
                _ => Color.gray
            };
        }
    }
}
#endif
