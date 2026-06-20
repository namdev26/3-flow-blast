#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Bootstrap;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using FlowBlast.Presentation.Block;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Block;
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
        private const float SequenceCanvasHeight = 360f;
        private const float SequenceCanvasMinZoom = 26f;
        private const float SequenceCanvasMaxZoom = 120f;
        private const float SequenceSlotHitRadius = 10f;
        private const int SequenceRegionBlockCount = 80;
        private const int DefaultGridWidth = 5;
        private const int DefaultGridHeight = 5;

        private static LevelEditorWindow instance;

        private LevelData levelData;
        private GameplayInstaller gameplayInstaller;
        private SerializedObject serializedLevelData;
        private SerializedProperty boxPlacementsProperty;
        private SerializedProperty blockSequenceItemsProperty;
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
        private bool showSpawnAuthoring = true;
        private bool showBlockPreview = true;
        private bool showPathPainting = true;
        private bool isSaveRequested;
        private bool shouldAutoFitSequenceCanvas = true;
        private float sequenceCanvasZoom = 42f;
        private Vector2 sequenceCanvasPan = new Vector2(0f, -2f);
        private int selectedSequenceRegionIndex;
        private List<BoxVisualProfile> cachedVisualProfiles;

        private readonly struct CanvasQueuePathPreview
        {
            public System.Func<float, Vector3> GetPositionAtDistance { get; }
            public System.Func<float, Quaternion> GetRotationAtDistance { get; }
            public float PathLength { get; }

            public CanvasQueuePathPreview(
                System.Func<float, Vector3> getPositionAtDistance,
                System.Func<float, Quaternion> getRotationAtDistance,
                float pathLength)
            {
                GetPositionAtDistance = getPositionAtDistance;
                GetRotationAtDistance = getRotationAtDistance;
                PathLength = pathLength;
            }
        }

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
            ClampSelectedSequenceRegion();
            SyncGridSettingsFromSerializedData();
        }

        private void RefreshSerializedData()
        {
            if (levelData == null)
            {
                serializedLevelData = null;
                boxPlacementsProperty = null;
                blockSequenceItemsProperty = null;
                beltLaneCountProperty = null;
                editorGridColumnsProperty = null;
                editorGridRowsProperty = null;
                return;
            }

            serializedLevelData = new SerializedObject(levelData);
            boxPlacementsProperty = serializedLevelData.FindProperty("boxPlacements");
            blockSequenceItemsProperty = serializedLevelData.FindProperty("blockSequenceItems");
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
                DrawSequenceAuthoringSection();
                EditorGUILayout.Space(6f);
                DrawSequenceCanvasSection();
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
                shouldAutoFitSequenceCanvas = true;
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

        private void DrawSequenceAuthoringSection()
        {
            showSpawnAuthoring = EditorGUILayout.Foldout(showSpawnAuthoring, "Block Sequence Authoring", true);

            if (!showSpawnAuthoring)
            {
                return;
            }

            bool isAutoBuild = serializedLevelData.FindProperty("autoBuildBlockSequenceFromBoxes")?.boolValue ?? true;

            if (isAutoBuild)
            {
                EditorGUILayout.HelpBox("Block sequence items are auto-built from box placements. Disable auto-build in Level Settings to author exact block-by-block spawn order manually.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Detailed block list has been hidden. Use the path canvas below to build and paint large regions only.", MessageType.None);
        }

        private void DrawSequenceCanvasSection()
        {
            EditorGUILayout.LabelField("Sequence Path Canvas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Số khu vực trên path sẽ bằng đúng số box. Mỗi khu vực đại diện cho 1 box color region, mặc định chưa có màu. Chỉ cần click khu vực rồi chọn màu để fill.", MessageType.None);

            int regionCount = GetSequenceRegionCount();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Regions From Boxes", GUILayout.Width(148f)))
                {
                    BuildSequenceRegionsFromBoxes();
                }

                if (GUILayout.Button("Clear All Region Colors", GUILayout.Width(156f)))
                {
                    ClearAllRegionColors();
                }

                if (GUILayout.Button("Reset View", GUILayout.Width(92f)))
                {
                    shouldAutoFitSequenceCanvas = true;
                    GUI.FocusControl(null);
                }

                GUILayout.Space(8f);
                EditorGUILayout.LabelField("Region", GUILayout.Width(44f));
                selectedSequenceRegionIndex = Mathf.Clamp(
                    EditorGUILayout.IntSlider(selectedSequenceRegionIndex + 1, 1, Mathf.Max(1, regionCount)) - 1,
                    0,
                    Mathf.Max(0, regionCount - 1));

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Zoom: {sequenceCanvasZoom:0}", GUILayout.Width(72f));
            }

            DrawSequenceRegionTabs(regionCount);
            Rect canvasRect = GUILayoutUtility.GetRect(10f, SequenceCanvasHeight, GUILayout.ExpandWidth(true), GUILayout.Height(SequenceCanvasHeight));
            DrawSequenceCanvas(canvasRect);

            DrawSelectedRegionFillControls();
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
            EditorGUILayout.LabelField("Total Blocks", levelData.TotalBlockCount.ToString());
            EditorGUILayout.LabelField("Sequence Items", levelData.BlockSequenceItems.Count.ToString());
            EditorGUILayout.LabelField("Spawn Rows", levelData.BlockSpawnRows.Count.ToString());
            EditorGUILayout.LabelField("Lane Count", beltLaneCountProperty.intValue.ToString());

            LevelMapLayout layout = mapLayoutProperty?.objectReferenceValue as LevelMapLayout;
            EditorGUILayout.LabelField("Map Layout", layout != null ? layout.name : "— None —");

            EditorGUI.BeginChangeCheck();
            showBlockPreview = EditorGUILayout.Toggle("Preview Blocks On Paths", showBlockPreview);
            showPathPainting = EditorGUILayout.Toggle("Paint Sequence On Path", showPathPainting);

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
                "Choose brush color",
                false);
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
                "Choose box color",
                false);
        }

        private void DrawVisualProfilePalette(
            BoxVisualProfile selectedProfile,
            System.Action<BoxVisualProfile> onSelected,
            bool allowNone,
            string tooltip,
            bool hideUsedInSequence)
        {
            List<BoxVisualProfile> visualProfiles = GetVisualProfiles(hideUsedInSequence, selectedProfile);
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

        private void DrawVisualProfileButton(bool isSelected, Color color, string label, System.Action onClick, string tooltip, bool isEnabled = true, bool showLockedX = false)
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

            using (new EditorGUI.DisabledScope(!isEnabled))
            {
                if (GUI.Button(buttonRect, new GUIContent(string.Empty, $"{label}\n{tooltip}"), buttonStyle))
                {
                    onClick?.Invoke();
                }
            }

            Color previewColor = isEnabled ? color : Color.Lerp(color, Color.black, 0.6f);
            EditorGUI.DrawRect(new Rect(buttonRect.x + 3f, buttonRect.y + 3f, buttonRect.width - 6f, buttonRect.height - 6f), previewColor);
            DrawCellOutline(buttonRect, isSelected ? Color.white : Color.black);

            if (showLockedX)
            {
                Handles.BeginGUI();
                Handles.color = Color.white;
                Handles.DrawAAPolyLine(3f, new Vector3(buttonRect.x + 5f, buttonRect.y + 5f), new Vector3(buttonRect.xMax - 5f, buttonRect.yMax - 5f));
                Handles.DrawAAPolyLine(3f, new Vector3(buttonRect.xMax - 5f, buttonRect.y + 5f), new Vector3(buttonRect.x + 5f, buttonRect.yMax - 5f));
                Handles.EndGUI();
            }
        }

        private void DrawBoxPlacementVisualPalette(
            BoxVisualProfile selectedProfile,
            System.Action<BoxVisualProfile> onSelected,
            bool allowNone,
            string tooltip)
        {
            List<BoxVisualProfile> boxProfiles = GetBoxPlacementVisualProfiles();
            Dictionary<BoxVisualProfile, int> boxProfileCapacities = GetBoxProfileCapacities();
            Dictionary<BoxVisualProfile, int> assignedRegionCounts = GetAssignedRegionCounts();
            int buttonCount = allowNone ? boxProfiles.Count + 1 : boxProfiles.Count;

            if (buttonCount == 0)
            {
                EditorGUILayout.HelpBox("No box colors found. Assign VisualProfile to boxes first.", MessageType.Info);
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
                            DrawVisualProfileButton(selectedProfile == null, Color.gray, "None", () => onSelected?.Invoke(null), tooltip, true);
                            index++;
                            continue;
                        }

                        int profileIndex = allowNone ? index - 1 : index;
                        BoxVisualProfile profile = boxProfiles[profileIndex];
                        bool isSelected = selectedProfile == profile;
                        int capacity = boxProfileCapacities.TryGetValue(profile, out int maxCount) ? maxCount : 0;
                        int assigned = assignedRegionCounts.TryGetValue(profile, out int currentCount) ? currentCount : 0;
                        bool canSelect = isSelected || assigned < capacity;
                        bool showLockedX = !canSelect;
                        DrawVisualProfileButton(
                            isSelected,
                            profile.TintColor,
                            $"{profile.name} ({assigned}/{capacity})",
                            () => onSelected?.Invoke(profile),
                            tooltip,
                            canSelect,
                            showLockedX);
                        index++;
                    }
                }
            }
        }

        private List<BoxVisualProfile> GetBoxPlacementVisualProfiles()
        {
            List<BoxVisualProfile> result = new List<BoxVisualProfile>();
            HashSet<BoxVisualProfile> uniqueProfiles = new HashSet<BoxVisualProfile>();

            if (boxPlacementsProperty == null)
            {
                return result;
            }

            for (int placementIndex = 0; placementIndex < boxPlacementsProperty.arraySize; placementIndex++)
            {
                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
                SerializedProperty visualProfileProperty = placementProperty.FindPropertyRelative("visualProfile");
                BoxVisualProfile profile = visualProfileProperty?.objectReferenceValue as BoxVisualProfile;

                if (profile == null || !uniqueProfiles.Add(profile))
                {
                    continue;
                }

                result.Add(profile);
            }

            result.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return result;
        }

        private Dictionary<BoxVisualProfile, int> GetBoxProfileCapacities()
        {
            Dictionary<BoxVisualProfile, int> result = new Dictionary<BoxVisualProfile, int>();

            if (boxPlacementsProperty == null)
            {
                return result;
            }

            for (int placementIndex = 0; placementIndex < boxPlacementsProperty.arraySize; placementIndex++)
            {
                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
                SerializedProperty visualProfileProperty = placementProperty.FindPropertyRelative("visualProfile");
                BoxVisualProfile profile = visualProfileProperty?.objectReferenceValue as BoxVisualProfile;

                if (profile == null)
                {
                    continue;
                }

                if (!result.ContainsKey(profile))
                {
                    result.Add(profile, 0);
                }

                result[profile]++;
            }

            return result;
        }

        private Dictionary<BoxVisualProfile, int> GetAssignedRegionCounts()
        {
            Dictionary<BoxVisualProfile, int> result = new Dictionary<BoxVisualProfile, int>();
            int regionCount = GetSequenceRegionCount();

            for (int regionIndex = 0; regionIndex < regionCount; regionIndex++)
            {
                BoxVisualProfile profile = GetSequenceRegionProfile(regionIndex);

                if (profile == null)
                {
                    continue;
                }

                if (!result.ContainsKey(profile))
                {
                    result.Add(profile, 0);
                }

                result[profile]++;
            }

            return result;
        }

        private List<BoxVisualProfile> GetVisualProfiles(bool hideUsedInSequence = false, BoxVisualProfile selectedProfile = null)
        {
            if (cachedVisualProfiles == null || cachedVisualProfiles.Count == 0)
            {
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
            }

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
            return levelData != null ? levelData.TotalBlockCount : 0;
        }

        private void AddSequenceItem()
        {
            serializedLevelData.Update();
            int itemIndex = blockSequenceItemsProperty.arraySize;
            blockSequenceItemsProperty.arraySize++;
            CopySequenceItemProfile(itemIndex - 1, blockSequenceItemsProperty.GetArrayElementAtIndex(itemIndex));
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void AddSequenceItemBatch(int count)
        {
            int safeCount = Mathf.Max(1, count);

            for (int index = 0; index < safeCount; index++)
            {
                AddSequenceItem();
            }
        }

        private void EnsureSequenceItemCount(int targetCount)
        {
            int safeTargetCount = Mathf.Max(0, targetCount);

            if (blockSequenceItemsProperty == null)
            {
                return;
            }

            serializedLevelData.Update();

            while (blockSequenceItemsProperty.arraySize < safeTargetCount)
            {
                int itemIndex = blockSequenceItemsProperty.arraySize;
                blockSequenceItemsProperty.arraySize++;
                CopySequenceItemProfile(itemIndex - 1, blockSequenceItemsProperty.GetArrayElementAtIndex(itemIndex));
            }

            while (blockSequenceItemsProperty.arraySize > safeTargetCount)
            {
                blockSequenceItemsProperty.DeleteArrayElementAtIndex(blockSequenceItemsProperty.arraySize - 1);
            }

            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void BuildSequenceRegionsFromBoxes()
        {
            int boxCount = Mathf.Max(0, GetPlacementCount());
            EnsureSequenceItemCount(boxCount * SequenceRegionBlockCount);
            ClampSelectedSequenceRegion();
        }

        private void InsertSequenceItem(int itemIndex)
        {
            serializedLevelData.Update();
            blockSequenceItemsProperty.InsertArrayElementAtIndex(itemIndex);
            CopySequenceItemProfile(itemIndex + 1, blockSequenceItemsProperty.GetArrayElementAtIndex(itemIndex));
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void RemoveSequenceItem(int itemIndex)
        {
            serializedLevelData.Update();
            blockSequenceItemsProperty.DeleteArrayElementAtIndex(itemIndex);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void ClearSequenceItems()
        {
            serializedLevelData.Update();
            blockSequenceItemsProperty.ClearArray();
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void CopySequenceItemProfile(int sourceItemIndex, SerializedProperty targetItemProperty)
        {
            if (targetItemProperty == null)
            {
                return;
            }

            SerializedProperty targetVisualProfileProperty = targetItemProperty.FindPropertyRelative("visualProfile");

            if (targetVisualProfileProperty == null)
            {
                return;
            }

            if (sourceItemIndex < 0 || sourceItemIndex >= blockSequenceItemsProperty.arraySize)
            {
                targetVisualProfileProperty.objectReferenceValue = null;
                return;
            }

            SerializedProperty sourceItemProperty = blockSequenceItemsProperty.GetArrayElementAtIndex(sourceItemIndex);
            SerializedProperty sourceVisualProfileProperty = sourceItemProperty.FindPropertyRelative("visualProfile");
            targetVisualProfileProperty.objectReferenceValue = sourceVisualProfileProperty?.objectReferenceValue;
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
            IReadOnlyList<LevelBlockSpawnRow> spawnRows = levelData.BlockSpawnRows;

            if (spawnRows == null || spawnRows.Count == 0)
            {
                return;
            }

            float rowSpacing = ResolveBlockRowSpacing();
            int laneCount = levelData.BeltLaneCount;
            float laneSpacing = rowSpacing;
            int sequenceCursor = 0;

            if (TryDrawPreviewFromMapLayout(spawnRows, rowSpacing, laneCount, laneSpacing, ref sequenceCursor))
            {
                return;
            }

            BeltPath mainPath = ResolveMainBeltPath();

            if (mainPath == null || mainPath.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            DrawBlockRowsOnPath(mainPath, spawnRows, ref sequenceCursor, rowSpacing, laneCount, laneSpacing, 0.9f);

            List<BeltPath> queuePaths = ResolveSceneQueuePaths();

            for (int i = 0; i < queuePaths.Count; i++)
            {
                if (sequenceCursor >= spawnRows.Count)
                {
                    break;
                }

                BeltPath queuePath = queuePaths[i];

                if (queuePath == null || queuePath.TotalLength <= Mathf.Epsilon)
                {
                    continue;
                }

                DrawBlockRowsOnPath(queuePath, spawnRows, ref sequenceCursor, rowSpacing, laneCount, laneSpacing, 0.65f);
            }
        }

        private bool TryDrawPreviewFromMapLayout(
            IReadOnlyList<LevelBlockSpawnRow> spawnRows,
            float rowSpacing,
            int laneCount,
            float laneSpacing,
            ref int sequenceCursor)
        {
            LevelMapLayout layout = mapLayoutProperty?.objectReferenceValue as LevelMapLayout;
            BeltPath mainPath = ResolveMainBeltPath();

            if (layout == null || mainPath == null || layout.MainWaypointLocalPositions.Count < 2)
            {
                return false;
            }

            DrawPreviewFromLayout(layout, mainPath.transform, spawnRows, ref sequenceCursor, rowSpacing, laneCount, laneSpacing);
            return true;
        }

        private void DrawPreviewFromLayout(
            LevelMapLayout layout,
            Transform pathRootTransform,
            IReadOnlyList<LevelBlockSpawnRow> spawnRows,
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

            DrawBlockRowsOnSampler(mainSampler, spawnRows, ref cursor, rowSpacing, laneCount, laneSpacing, 0.9f);
            List<BeltPath> sceneQueuePaths = ResolveSceneQueuePaths();

            for (int i = 0; i < layout.QueuePaths.Count; i++)
            {
                if (cursor >= spawnRows.Count)
                {
                    break;
                }

                QueuePathLayout queueLayout = layout.QueuePaths[i];

                if (queueLayout == null || queueLayout.WaypointLocalPositions.Count < 2)
                {
                    continue;
                }

                Transform queueTransform = ResolveQueueTransformForLayout(queueLayout, sceneQueuePaths, pathRootTransform);
                CatmullRomPathSampler queueSampler = BuildSamplerFromLocalWaypoints(
                    queueLayout.WaypointLocalPositions,
                    queueLayout.IsClosedLoop,
                    queueLayout.CurveStrength,
                    queueTransform);

                DrawBlockRowsOnSampler(queueSampler, spawnRows, ref cursor, rowSpacing, laneCount, laneSpacing, 0.65f);
            }
        }

        private void DrawBlockRowsOnSampler(
            CatmullRomPathSampler sampler,
            IReadOnlyList<LevelBlockSpawnRow> spawnRows,
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
            int rowsToShow = Mathf.Min(rowCapacity, spawnRows.Count - cursor);

            for (int rowIndex = 0; rowIndex < rowsToShow; rowIndex++)
            {
                LevelBlockSpawnRow spawnRow = spawnRows[cursor + rowIndex];
                float rowDistance = rowIndex * rowSpacing;
                Vector3 center = sampler.GetPositionAtDistance(rowDistance);
                Quaternion rotation = sampler.GetRotationAtDistance(rowDistance);
                float handleSize = HandleUtility.GetHandleSize(center) * 0.055f;

                DrawSpawnRowPreview(spawnRow, center, rotation, laneCount, laneSpacing, handleSize, alpha);
            }

            cursor += rowsToShow;
        }

        private void DrawBlockRowsOnPath(
            BeltPath path,
            IReadOnlyList<LevelBlockSpawnRow> spawnRows,
            ref int cursor,
            float rowSpacing,
            int laneCount,
            float laneSpacing,
            float alpha)
        {
            int rowCapacity = Mathf.FloorToInt(path.TotalLength / rowSpacing);
            int rowsToShow = Mathf.Min(rowCapacity, spawnRows.Count - cursor);

            for (int rowIndex = 0; rowIndex < rowsToShow; rowIndex++)
            {
                LevelBlockSpawnRow spawnRow = spawnRows[cursor + rowIndex];
                float rowDistance = rowIndex * rowSpacing;
                Vector3 center = path.GetPositionAtDistance(rowDistance);
                Quaternion rotation = path.GetRotationAtDistance(rowDistance);
                float handleSize = HandleUtility.GetHandleSize(center) * 0.055f;

                DrawSpawnRowPreview(spawnRow, center, rotation, laneCount, laneSpacing, handleSize, alpha);
            }

            cursor += rowsToShow;
        }

        private void DrawSpawnRowPreview(
            LevelBlockSpawnRow spawnRow,
            Vector3 center,
            Quaternion rotation,
            int laneCount,
            float laneSpacing,
            float handleSize,
            float alpha)
        {
            if (spawnRow == null)
            {
                return;
            }

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                BoxVisualProfile profile = spawnRow.GetLaneProfile(laneIndex);
                Color color = profile != null ? profile.TintColor : Color.gray;
                color.a = alpha;
                Handles.color = color;
                Vector3 lanePosition = BeltLaneLayout.GetLanePosition(center, rotation, laneIndex, laneCount, laneSpacing);
                Handles.DrawSolidDisc(lanePosition, rotation * Vector3.up, handleSize);
            }
        }

        private void DrawSequenceCanvas(Rect rect)
        {
            AutoFitSequenceCanvasIfNeeded(rect);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.13f, 0.15f));

            if (Event.current.type == EventType.Repaint)
            {
                DrawSequenceCanvasGrid(rect);
            }

            HandleSequenceCanvasInput(rect);
            DrawSequenceCanvasRegionOverlay(rect);
            DrawSequenceCanvasPaths(rect);
            DrawCellOutline(rect, new Color(1f, 1f, 1f, 0.08f));
        }

        private void DrawSequenceCanvasGrid(Rect rect)
        {
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.06f);
            float cellSize = GridCellSpacing;
            int extent = 30;

            for (int x = -extent; x <= extent; x++)
            {
                Vector2 start = SequenceWorldToCanvas(new Vector3(x * cellSize, 0f, -extent * cellSize), rect);
                Vector2 end = SequenceWorldToCanvas(new Vector3(x * cellSize, 0f, extent * cellSize), rect);
                Handles.DrawLine(start, end);
            }

            for (int z = -extent; z <= extent; z++)
            {
                Vector2 start = SequenceWorldToCanvas(new Vector3(-extent * cellSize, 0f, z * cellSize), rect);
                Vector2 end = SequenceWorldToCanvas(new Vector3(extent * cellSize, 0f, z * cellSize), rect);
                Handles.DrawLine(start, end);
            }

            Handles.EndGUI();
        }

        private void DrawSequenceCanvasRegionOverlay(Rect rect)
        {
            int regionStartIndex = GetSequenceRegionStartIndex(selectedSequenceRegionIndex);
            int regionEndExclusive = Mathf.Min(regionStartIndex + SequenceRegionBlockCount, blockSequenceItemsProperty.arraySize);
            BoxVisualProfile regionProfile = GetSequenceRegionProfile(selectedSequenceRegionIndex);
            string regionStateLabel = regionProfile != null ? regionProfile.name : "Empty";
            Rect labelRect = new Rect(rect.x + 12f, rect.y + 12f, 320f, 22f);
            EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.45f));
            EditorGUI.LabelField(labelRect, $"Region {selectedSequenceRegionIndex + 1}  |  Blocks {regionStartIndex + 1}-{Mathf.Max(regionStartIndex + 1, regionEndExclusive)}  |  {regionStateLabel}", EditorStyles.whiteMiniLabel);
        }

        private void DrawSequenceRegionTabs(int regionCount)
        {
            if (regionCount <= 0)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int regionIndex = 0; regionIndex < regionCount; regionIndex++)
                {
                    BoxVisualProfile regionProfile = GetSequenceRegionProfile(regionIndex);
                    GUI.backgroundColor = regionProfile != null ? regionProfile.TintColor : Color.gray;
                    bool isSelected = regionIndex == selectedSequenceRegionIndex;
                    GUIStyle style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

                    if (GUILayout.Button($"R{regionIndex + 1}", style, GUILayout.Height(26f)))
                    {
                        selectedSequenceRegionIndex = regionIndex;
                    }
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawSelectedRegionFillControls()
        {
            if (GetSequenceRegionCount() <= 0)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Selected Region {selectedSequenceRegionIndex + 1}", EditorStyles.boldLabel);
                BoxVisualProfile selectedRegionProfile = GetSequenceRegionProfile(selectedSequenceRegionIndex);
                DrawInlineColorPreview(selectedRegionProfile != null ? selectedRegionProfile.TintColor : Color.gray);
                EditorGUILayout.LabelField("Region color is sourced from current box colors.", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Locked when box count for that color is fully used.", EditorStyles.miniLabel);
                DrawBoxPlacementVisualPalette(selectedRegionProfile, FillSequenceRegion, true, "Fill selected region from box colors");
            }
        }

        private void HandleSequenceCanvasInput(Rect rect)
        {
            Event currentEvent = Event.current;

            if (!rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.ScrollWheel)
            {
                shouldAutoFitSequenceCanvas = false;
                float zoomDelta = -currentEvent.delta.y * 2f;
                sequenceCanvasZoom = Mathf.Clamp(sequenceCanvasZoom + zoomDelta, SequenceCanvasMinZoom, SequenceCanvasMaxZoom);
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 2)
            {
                shouldAutoFitSequenceCanvas = false;
                sequenceCanvasPan += currentEvent.delta / sequenceCanvasZoom;
                currentEvent.Use();
                Repaint();
            }
        }

        private void DrawSequenceCanvasPaths(Rect rect)
        {
            if (levelData == null || blockSequenceItemsProperty == null || blockSequenceItemsProperty.arraySize == 0)
            {
                return;
            }

            int laneCount = GetLaneCount();
            float rowSpacing = ResolveBlockRowSpacing();
            float laneSpacing = rowSpacing;
            int sequenceCursor = 0;
            int regionStartIndex = GetSequenceRegionStartIndex(selectedSequenceRegionIndex);
            int regionEndExclusive = Mathf.Min(regionStartIndex + SequenceRegionBlockCount, blockSequenceItemsProperty.arraySize);

            if (TryDrawCanvasFromMapLayout(rect, laneCount, rowSpacing, laneSpacing, ref sequenceCursor, regionStartIndex, regionEndExclusive))
            {
                return;
            }

            BeltPath mainPath = ResolveMainBeltPath();

            if (mainPath == null || mainPath.TotalLength <= Mathf.Epsilon)
            {
                return;
            }

            DrawCanvasPathSequence(
                rect,
                distance => mainPath.GetPositionAtDistance(distance),
                distance => mainPath.GetRotationAtDistance(distance),
                mainPath.TotalLength,
                laneCount,
                rowSpacing,
                laneSpacing,
                0.9f,
                ref sequenceCursor,
                regionStartIndex,
                regionEndExclusive);

            List<BeltPath> queuePaths = ResolveSceneQueuePaths();
            DrawBalancedQueueCanvasPaths(
                rect,
                queuePaths,
                laneCount,
                rowSpacing,
                laneSpacing,
                ref sequenceCursor,
                regionStartIndex,
                regionEndExclusive);
        }

        private bool TryDrawCanvasFromMapLayout(
            Rect rect,
            int laneCount,
            float rowSpacing,
            float laneSpacing,
            ref int sequenceCursor,
            int regionStartIndex,
            int regionEndExclusive)
        {
            LevelMapLayout layout = mapLayoutProperty?.objectReferenceValue as LevelMapLayout;
            BeltPath mainPath = ResolveMainBeltPath();

            if (layout == null || mainPath == null || layout.MainWaypointLocalPositions.Count < 2)
            {
                return false;
            }

            CatmullRomPathSampler mainSampler = BuildSamplerFromLocalWaypoints(
                layout.MainWaypointLocalPositions,
                layout.IsMainPathClosedLoop,
                layout.MainCurveStrength,
                mainPath.transform);
            DrawCanvasPathSequence(
                rect,
                distance => mainSampler.GetPositionAtDistance(distance),
                distance => mainSampler.GetRotationAtDistance(distance),
                mainSampler.TotalLength,
                laneCount,
                rowSpacing,
                laneSpacing,
                0.9f,
                ref sequenceCursor,
                regionStartIndex,
                regionEndExclusive);
            List<BeltPath> sceneQueuePaths = ResolveSceneQueuePaths();
            List<CanvasQueuePathPreview> queuePreviews = new List<CanvasQueuePathPreview>();

            for (int pathIndex = 0; pathIndex < layout.QueuePaths.Count; pathIndex++)
            {
                QueuePathLayout queueLayout = layout.QueuePaths[pathIndex];

                if (queueLayout == null || queueLayout.WaypointLocalPositions.Count < 2)
                {
                    continue;
                }

                Transform queueTransform = ResolveQueueTransformForLayout(queueLayout, sceneQueuePaths, mainPath.transform);
                CatmullRomPathSampler queueSampler = BuildSamplerFromLocalWaypoints(
                    queueLayout.WaypointLocalPositions,
                    queueLayout.IsClosedLoop,
                    queueLayout.CurveStrength,
                    queueTransform);

                if (queueSampler == null || queueSampler.TotalLength <= Mathf.Epsilon)
                {
                    continue;
                }

                queuePreviews.Add(new CanvasQueuePathPreview(
                    distance => queueSampler.GetPositionAtDistance(distance),
                    distance => queueSampler.GetRotationAtDistance(distance),
                    queueSampler.TotalLength));
            }

            DrawBalancedQueueCanvasPreviews(
                rect,
                queuePreviews,
                laneCount,
                rowSpacing,
                laneSpacing,
                ref sequenceCursor,
                regionStartIndex,
                regionEndExclusive);

            return true;
        }

        private void DrawCanvasPathSequence(
            Rect rect,
            System.Func<float, Vector3> getPositionAtDistance,
            System.Func<float, Quaternion> getRotationAtDistance,
            float pathLength,
            int laneCount,
            float rowSpacing,
            float laneSpacing,
            float alpha,
            ref int sequenceCursor,
            int regionStartIndex,
            int regionEndExclusive)
        {
            int rowCapacity = Mathf.FloorToInt(pathLength / rowSpacing);
            int remainingRows = Mathf.CeilToInt((levelData.BlockSequenceItems.Count - sequenceCursor) / (float)laneCount);
            int availableRows = Mathf.Min(rowCapacity, remainingRows);
            DrawCanvasPathSequenceWithRowCount(
                rect,
                getPositionAtDistance,
                getRotationAtDistance,
                pathLength,
                laneCount,
                rowSpacing,
                laneSpacing,
                alpha,
                ref sequenceCursor,
                regionStartIndex,
                regionEndExclusive,
                availableRows);
        }

        private void DrawCanvasPathSequenceWithRowCount(
            Rect rect,
            System.Func<float, Vector3> getPositionAtDistance,
            System.Func<float, Quaternion> getRotationAtDistance,
            float pathLength,
            int laneCount,
            float rowSpacing,
            float laneSpacing,
            float alpha,
            ref int sequenceCursor,
            int regionStartIndex,
            int regionEndExclusive,
            int availableRows)
        {
            if (pathLength <= Mathf.Epsilon)
            {
                return;
            }

            DrawCanvasPathLine(rect, getPositionAtDistance, pathLength, alpha);

            if (availableRows <= 0)
            {
                return;
            }

            int pathStartBlockIndex = sequenceCursor;
            int pathEndBlockExclusive = sequenceCursor + availableRows * laneCount;

            DrawCanvasRegionSegments(
                rect,
                getPositionAtDistance,
                getRotationAtDistance,
                availableRows,
                rowSpacing,
                laneSpacing,
                laneCount,
                alpha,
                pathStartBlockIndex,
                pathEndBlockExclusive,
                regionStartIndex,
                regionEndExclusive);

            sequenceCursor += availableRows * laneCount;
        }

        private void DrawBalancedQueueCanvasPaths(
            Rect rect,
            IReadOnlyList<BeltPath> queuePaths,
            int laneCount,
            float rowSpacing,
            float laneSpacing,
            ref int sequenceCursor,
            int regionStartIndex,
            int regionEndExclusive)
        {
            if (queuePaths == null || queuePaths.Count == 0)
            {
                return;
            }

            List<int> rowCapacities = new List<int>(queuePaths.Count);

            for (int pathIndex = 0; pathIndex < queuePaths.Count; pathIndex++)
            {
                BeltPath queuePath = queuePaths[pathIndex];
                int rowCapacity = queuePath != null && queuePath.TotalLength > Mathf.Epsilon
                    ? Mathf.FloorToInt(queuePath.TotalLength / rowSpacing)
                    : 0;
                rowCapacities.Add(rowCapacity);
            }

            DrawBalancedQueueCanvasPathsInternal(
                rect,
                queuePaths,
                rowCapacities,
                laneCount,
                rowSpacing,
                laneSpacing,
                ref sequenceCursor,
                regionStartIndex,
                regionEndExclusive);
        }

        private void DrawBalancedQueueCanvasPreviews(
            Rect rect,
            IReadOnlyList<CanvasQueuePathPreview> queuePreviews,
            int laneCount,
            float rowSpacing,
            float laneSpacing,
            ref int sequenceCursor,
            int regionStartIndex,
            int regionEndExclusive)
        {
            if (queuePreviews == null || queuePreviews.Count == 0)
            {
                return;
            }

            List<int> rowCapacities = new List<int>(queuePreviews.Count);

            for (int pathIndex = 0; pathIndex < queuePreviews.Count; pathIndex++)
            {
                CanvasQueuePathPreview preview = queuePreviews[pathIndex];
                int rowCapacity = preview.PathLength > Mathf.Epsilon
                    ? Mathf.FloorToInt(preview.PathLength / rowSpacing)
                    : 0;
                rowCapacities.Add(rowCapacity);
            }

            List<int> rowAllocations = BuildBalancedQueueRowAllocations(rowCapacities, sequenceCursor, laneCount);

            for (int pathIndex = 0; pathIndex < queuePreviews.Count; pathIndex++)
            {
                CanvasQueuePathPreview preview = queuePreviews[pathIndex];
                int availableRows = pathIndex < rowAllocations.Count ? rowAllocations[pathIndex] : 0;

                if (preview.PathLength <= Mathf.Epsilon)
                {
                    continue;
                }

                DrawCanvasPathSequenceWithRowCount(
                    rect,
                    preview.GetPositionAtDistance,
                    preview.GetRotationAtDistance,
                    preview.PathLength,
                    laneCount,
                    rowSpacing,
                    laneSpacing,
                    0.65f,
                    ref sequenceCursor,
                    regionStartIndex,
                    regionEndExclusive,
                    availableRows);
            }
        }

        private void DrawBalancedQueueCanvasPathsInternal(
            Rect rect,
            IReadOnlyList<BeltPath> queuePaths,
            IReadOnlyList<int> rowCapacities,
            int laneCount,
            float rowSpacing,
            float laneSpacing,
            ref int sequenceCursor,
            int regionStartIndex,
            int regionEndExclusive)
        {
            List<int> rowAllocations = BuildBalancedQueueRowAllocations(rowCapacities, sequenceCursor, laneCount);

            for (int pathIndex = 0; pathIndex < queuePaths.Count; pathIndex++)
            {
                BeltPath queuePath = queuePaths[pathIndex];
                int availableRows = pathIndex < rowAllocations.Count ? rowAllocations[pathIndex] : 0;

                if (queuePath == null || queuePath.TotalLength <= Mathf.Epsilon)
                {
                    continue;
                }

                DrawCanvasPathSequenceWithRowCount(
                    rect,
                    distance => queuePath.GetPositionAtDistance(distance),
                    distance => queuePath.GetRotationAtDistance(distance),
                    queuePath.TotalLength,
                    laneCount,
                    rowSpacing,
                    laneSpacing,
                    0.65f,
                    ref sequenceCursor,
                    regionStartIndex,
                    regionEndExclusive,
                    availableRows);
            }
        }

        private List<int> BuildBalancedQueueRowAllocations(IReadOnlyList<int> rowCapacities, int sequenceCursor, int laneCount)
        {
            int remainingRows = Mathf.CeilToInt((levelData.BlockSequenceItems.Count - sequenceCursor) / (float)laneCount);
            List<int> rowAllocations = new List<int>(rowCapacities != null ? rowCapacities.Count : 0);
            int rowsPerChunk = Mathf.Max(1, Mathf.CeilToInt(SequenceRegionBlockCount / (float)Mathf.Max(1, laneCount)));
            QueuePathRowDistributionUtility.BuildBalancedRowAllocations(rowCapacities, remainingRows, rowsPerChunk, rowAllocations);
            return rowAllocations;
        }

        private void DrawCanvasPathLine(
            Rect rect,
            System.Func<float, Vector3> getPositionAtDistance,
            float pathLength,
            float alpha)
        {
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, alpha * 0.2f);
            int previewSteps = Mathf.Max(24, Mathf.FloorToInt(pathLength / GridCellSpacing));
            Vector2 previousPoint = Vector2.zero;
            bool hasPrevious = false;

            for (int step = 0; step <= previewSteps; step++)
            {
                float distance = pathLength * step / previewSteps;
                Vector2 canvasPoint = SequenceWorldToCanvas(getPositionAtDistance(distance), rect);

                if (hasPrevious)
                {
                    Handles.DrawLine(previousPoint, canvasPoint);
                }

                previousPoint = canvasPoint;
                hasPrevious = true;
            }

            Handles.EndGUI();
        }

        private void DrawCanvasRegionSegments(
            Rect rect,
            System.Func<float, Vector3> getPositionAtDistance,
            System.Func<float, Quaternion> getRotationAtDistance,
            int availableRows,
            float rowSpacing,
            float laneSpacing,
            int laneCount,
            float alpha,
            int pathStartBlockIndex,
            int pathEndBlockExclusive,
            int selectedRegionStartIndex,
            int selectedRegionEndExclusive)
        {
            if (availableRows <= 0)
            {
                return;
            }

            int firstRegionIndex = GetSequenceRegionIndexFromBlockIndex(pathStartBlockIndex);
            int lastRegionIndex = GetSequenceRegionIndexFromBlockIndex(Mathf.Max(pathStartBlockIndex, pathEndBlockExclusive - 1));

            for (int regionIndex = firstRegionIndex; regionIndex <= lastRegionIndex; regionIndex++)
            {
                int regionBlockStart = GetSequenceRegionStartIndex(regionIndex);
                int regionBlockEnd = Mathf.Min(regionBlockStart + SequenceRegionBlockCount, blockSequenceItemsProperty.arraySize);
                int visibleBlockStart = Mathf.Max(regionBlockStart, pathStartBlockIndex);
                int visibleBlockEnd = Mathf.Min(regionBlockEnd, pathEndBlockExclusive);

                if (visibleBlockEnd <= visibleBlockStart)
                {
                    continue;
                }

                int startRowInPath = (visibleBlockStart - pathStartBlockIndex) / laneCount;
                int endRowExclusiveInPath = Mathf.Max(startRowInPath + 1, Mathf.CeilToInt((visibleBlockEnd - pathStartBlockIndex) / (float)laneCount));
                float startDistance = startRowInPath * rowSpacing;
                float endDistance = Mathf.Min(availableRows * rowSpacing, endRowExclusiveInPath * rowSpacing);
                float boundaryInset = Mathf.Min(rowSpacing * 0.12f, 0.12f);

                if (endDistance - startDistance > boundaryInset * 2f)
                {
                    startDistance += boundaryInset;
                    endDistance -= boundaryInset;
                }

                BoxVisualProfile regionProfile = GetSequenceRegionProfile(regionIndex);
                Color regionColor = regionProfile != null ? regionProfile.TintColor : new Color(0.3f, 0.3f, 0.3f, 0.9f);
                regionColor.a = alpha;
                bool isSelected = regionIndex == selectedSequenceRegionIndex;
                bool isPartial = visibleBlockStart > regionBlockStart || visibleBlockEnd < regionBlockEnd;

                DrawCanvasRegionSegment(
                    rect,
                    getPositionAtDistance,
                    getRotationAtDistance,
                    startDistance,
                    endDistance,
                    laneSpacing,
                    regionColor,
                    regionIndex,
                    isSelected,
                    isPartial,
                    visibleBlockStart,
                    selectedRegionStartIndex,
                    selectedRegionEndExclusive);
            }
        }

        private void DrawCanvasRegionSegment(
            Rect rect,
            System.Func<float, Vector3> getPositionAtDistance,
            System.Func<float, Quaternion> getRotationAtDistance,
            float startDistance,
            float endDistance,
            float laneSpacing,
            Color regionColor,
            int regionIndex,
            bool isSelected,
            bool isPartial,
            int visibleBlockStart,
            int selectedRegionStartIndex,
            int selectedRegionEndExclusive)
        {
            float safeEndDistance = Mathf.Max(startDistance, endDistance);
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt((safeEndDistance - startDistance) / Mathf.Max(0.25f, laneSpacing)) + 1);
            Vector2 labelPoint = Vector2.zero;
            bool labelPointAssigned = false;

            Handles.BeginGUI();
            Handles.color = regionColor;

            for (int sampleIndex = 0; sampleIndex < sampleCount - 1; sampleIndex++)
            {
                float fromDistance = Mathf.Lerp(startDistance, safeEndDistance, sampleIndex / (float)(sampleCount - 1));
                float toDistance = Mathf.Lerp(startDistance, safeEndDistance, (sampleIndex + 1) / (float)(sampleCount - 1));
                Vector3 fromCenter = getPositionAtDistance(fromDistance);
                Vector3 toCenter = getPositionAtDistance(toDistance);
                Quaternion fromRotation = getRotationAtDistance(fromDistance);
                Quaternion toRotation = getRotationAtDistance(toDistance);
                Vector3 fromLeft = BeltLaneLayout.GetLanePosition(fromCenter, fromRotation, 0, 2, laneSpacing * 2.2f);
                Vector3 fromRight = BeltLaneLayout.GetLanePosition(fromCenter, fromRotation, 1, 2, laneSpacing * 2.2f);
                Vector3 toLeft = BeltLaneLayout.GetLanePosition(toCenter, toRotation, 0, 2, laneSpacing * 2.2f);
                Vector3 toRight = BeltLaneLayout.GetLanePosition(toCenter, toRotation, 1, 2, laneSpacing * 2.2f);
                Vector3[] quad = new Vector3[]
                {
                    SequenceWorldToCanvas(fromLeft, rect),
                    SequenceWorldToCanvas(fromRight, rect),
                    SequenceWorldToCanvas(toRight, rect),
                    SequenceWorldToCanvas(toLeft, rect)
                };
                Handles.DrawAAConvexPolygon(quad);

                if (!labelPointAssigned && sampleIndex >= (sampleCount - 1) / 2)
                {
                    labelPoint = SequenceWorldToCanvas((fromCenter + toCenter) * 0.5f, rect);
                    labelPointAssigned = true;
                }
            }

            Handles.color = isSelected ? Color.white : new Color(0f, 0f, 0f, 0.8f);
            Handles.DrawAAPolyLine(isSelected ? 4f : 2f, BuildRegionPolyline(rect, getPositionAtDistance, startDistance, safeEndDistance, sampleCount));
            Handles.EndGUI();

            Rect hitRect = new Rect(labelPoint.x - 28f, labelPoint.y - 14f, 56f, 28f);

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && hitRect.Contains(Event.current.mousePosition))
            {
                selectedSequenceRegionIndex = Mathf.Clamp(regionIndex, 0, Mathf.Max(0, GetSequenceRegionCount() - 1));
                BoxVisualProfile currentRegionProfile = GetSequenceRegionProfile(selectedSequenceRegionIndex);

                if (currentRegionProfile != null)
                {
                    ClearSelectedRegionColor();
                }
                else if (brushVisualProfile != null)
                {
                    FillSequenceRegion(brushVisualProfile);
                }

                Event.current.Use();
                Repaint();
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            };
            string partialSuffix = isPartial ? "*" : string.Empty;
            GUI.Label(new Rect(labelPoint.x - 20f, labelPoint.y - 10f, 40f, 20f), $"R{regionIndex + 1}{partialSuffix}", labelStyle);
        }

        private void FillSequenceRegion(BoxVisualProfile visualProfile)
        {
            if (blockSequenceItemsProperty == null)
            {
                return;
            }

            int regionStartIndex = GetSequenceRegionStartIndex(selectedSequenceRegionIndex);
            int regionEndExclusive = Mathf.Min(regionStartIndex + SequenceRegionBlockCount, blockSequenceItemsProperty.arraySize);
            serializedLevelData.Update();

            for (int itemIndex = regionStartIndex; itemIndex < regionEndExclusive; itemIndex++)
            {
                SerializedProperty itemProperty = blockSequenceItemsProperty.GetArrayElementAtIndex(itemIndex);
                SerializedProperty visualProfileProperty = itemProperty.FindPropertyRelative("visualProfile");
                visualProfileProperty.objectReferenceValue = visualProfile;
            }

            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void ClearSelectedRegionColor()
        {
            if (blockSequenceItemsProperty == null)
            {
                return;
            }

            int regionStartIndex = GetSequenceRegionStartIndex(selectedSequenceRegionIndex);
            int regionEndExclusive = Mathf.Min(regionStartIndex + SequenceRegionBlockCount, blockSequenceItemsProperty.arraySize);
            serializedLevelData.Update();

            for (int itemIndex = regionStartIndex; itemIndex < regionEndExclusive; itemIndex++)
            {
                SerializedProperty itemProperty = blockSequenceItemsProperty.GetArrayElementAtIndex(itemIndex);
                SerializedProperty visualProfileProperty = itemProperty.FindPropertyRelative("visualProfile");
                visualProfileProperty.objectReferenceValue = null;
            }

            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void ClearAllRegionColors()
        {
            if (blockSequenceItemsProperty == null)
            {
                return;
            }

            serializedLevelData.Update();

            for (int itemIndex = 0; itemIndex < blockSequenceItemsProperty.arraySize; itemIndex++)
            {
                SerializedProperty itemProperty = blockSequenceItemsProperty.GetArrayElementAtIndex(itemIndex);
                SerializedProperty visualProfileProperty = itemProperty.FindPropertyRelative("visualProfile");
                visualProfileProperty.objectReferenceValue = null;
            }

            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private int GetSequenceRegionCount()
        {
            int regionCountFromSequence = blockSequenceItemsProperty != null ? Mathf.CeilToInt(blockSequenceItemsProperty.arraySize / (float)SequenceRegionBlockCount) : 0;
            return Mathf.Max(GetPlacementCount(), regionCountFromSequence, 1);
        }

        private int GetSequenceRegionStartIndex(int regionIndex)
        {
            return Mathf.Max(0, regionIndex) * SequenceRegionBlockCount;
        }

        private BoxVisualProfile GetSequenceRegionProfile(int regionIndex)
        {
            if (blockSequenceItemsProperty == null)
            {
                return null;
            }

            int regionStartIndex = GetSequenceRegionStartIndex(regionIndex);

            if (regionStartIndex < 0 || regionStartIndex >= blockSequenceItemsProperty.arraySize)
            {
                return null;
            }

            SerializedProperty itemProperty = blockSequenceItemsProperty.GetArrayElementAtIndex(regionStartIndex);
            SerializedProperty visualProfileProperty = itemProperty.FindPropertyRelative("visualProfile");
            return visualProfileProperty.objectReferenceValue as BoxVisualProfile;
        }

        private Vector3[] BuildRegionPolyline(
            Rect rect,
            System.Func<float, Vector3> getPositionAtDistance,
            float startDistance,
            float endDistance,
            int sampleCount)
        {
            int safeSampleCount = Mathf.Max(2, sampleCount);
            Vector3[] points = new Vector3[safeSampleCount];

            for (int sampleIndex = 0; sampleIndex < safeSampleCount; sampleIndex++)
            {
                float distance = Mathf.Lerp(startDistance, endDistance, sampleIndex / (float)(safeSampleCount - 1));
                Vector2 canvasPoint = SequenceWorldToCanvas(getPositionAtDistance(distance), rect);
                points[sampleIndex] = new Vector3(canvasPoint.x, canvasPoint.y, 0f);
            }

            return points;
        }

        private int GetSequenceRegionIndexFromBlockIndex(int blockIndex)
        {
            return blockIndex < 0 ? 0 : blockIndex / SequenceRegionBlockCount;
        }

        private int GetLaneCount()
        {
            return Mathf.Max(1, levelData != null ? levelData.BeltLaneCount : GameConstants.BeltLaneCount);
        }

        private void ClampSelectedSequenceRegion()
        {
            selectedSequenceRegionIndex = Mathf.Clamp(selectedSequenceRegionIndex, 0, Mathf.Max(0, GetSequenceRegionCount() - 1));
        }

        private Vector2 SequenceWorldToCanvas(Vector3 localPosition, Rect rect)
        {
            float x = rect.center.x + (localPosition.x + sequenceCanvasPan.x) * sequenceCanvasZoom;
            float y = rect.center.y - (localPosition.z + sequenceCanvasPan.y) * sequenceCanvasZoom;
            return new Vector2(x, y);
        }

        private Transform ResolveQueueTransformForLayout(
            QueuePathLayout queueLayout,
            IReadOnlyList<BeltPath> sceneQueuePaths,
            Transform fallbackTransform)
        {
            if (queueLayout == null || sceneQueuePaths == null)
            {
                return fallbackTransform;
            }

            string queueId = queueLayout.QueueId;

            for (int i = 0; i < sceneQueuePaths.Count; i++)
            {
                BeltPath queuePath = sceneQueuePaths[i];

                if (queuePath == null)
                {
                    continue;
                }

                if (queuePath.PathId == queueId)
                {
                    return queuePath.transform;
                }
            }

            return fallbackTransform;
        }

        private void AutoFitSequenceCanvasIfNeeded(Rect rect)
        {
            if (!shouldAutoFitSequenceCanvas || rect.width <= Mathf.Epsilon || rect.height <= Mathf.Epsilon)
            {
                return;
            }

            if (!TryGetSequenceCanvasBounds(out Vector3 minBounds, out Vector3 maxBounds))
            {
                shouldAutoFitSequenceCanvas = false;
                return;
            }

            float contentWidth = Mathf.Max(1f, maxBounds.x - minBounds.x);
            float contentHeight = Mathf.Max(1f, maxBounds.z - minBounds.z);
            float horizontalPadding = 48f;
            float verticalPadding = 48f;
            float zoomX = (rect.width - horizontalPadding) / contentWidth;
            float zoomY = (rect.height - verticalPadding) / contentHeight;
            float fittedZoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), SequenceCanvasMinZoom, SequenceCanvasMaxZoom);
            Vector3 contentCenter = (minBounds + maxBounds) * 0.5f;

            sequenceCanvasZoom = fittedZoom;
            sequenceCanvasPan = new Vector2(-contentCenter.x, -contentCenter.z);
            shouldAutoFitSequenceCanvas = false;
        }

        private bool TryGetSequenceCanvasBounds(out Vector3 minBounds, out Vector3 maxBounds)
        {
            minBounds = new Vector3(float.MaxValue, 0f, float.MaxValue);
            maxBounds = new Vector3(float.MinValue, 0f, float.MinValue);
            bool hasAnyPoint = false;
            LevelMapLayout layout = mapLayoutProperty?.objectReferenceValue as LevelMapLayout;

            if (layout != null)
            {
                hasAnyPoint |= AccumulateBounds(layout.MainWaypointLocalPositions, ref minBounds, ref maxBounds);

                for (int i = 0; i < layout.QueuePaths.Count; i++)
                {
                    QueuePathLayout queueLayout = layout.QueuePaths[i];

                    if (queueLayout == null)
                    {
                        continue;
                    }

                    hasAnyPoint |= AccumulateBounds(queueLayout.WaypointLocalPositions, ref minBounds, ref maxBounds);
                }
            }
            else
            {
                BeltPath mainPath = ResolveMainBeltPath();
                List<BeltPath> queuePaths = ResolveSceneQueuePaths();

                if (mainPath != null)
                {
                    hasAnyPoint |= AccumulateBounds(mainPath.Waypoints, ref minBounds, ref maxBounds);
                }

                for (int i = 0; i < queuePaths.Count; i++)
                {
                    if (queuePaths[i] == null)
                    {
                        continue;
                    }

                    hasAnyPoint |= AccumulateBounds(queuePaths[i].Waypoints, ref minBounds, ref maxBounds);
                }
            }

            return hasAnyPoint;
        }

        private static bool AccumulateBounds(
            IReadOnlyList<Vector3> positions,
            ref Vector3 minBounds,
            ref Vector3 maxBounds)
        {
            if (positions == null || positions.Count == 0)
            {
                return false;
            }

            bool hasAnyPoint = false;

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 point = positions[i];
                minBounds.x = Mathf.Min(minBounds.x, point.x);
                minBounds.z = Mathf.Min(minBounds.z, point.z);
                maxBounds.x = Mathf.Max(maxBounds.x, point.x);
                maxBounds.z = Mathf.Max(maxBounds.z, point.z);
                hasAnyPoint = true;
            }

            return hasAnyPoint;
        }

        private static bool AccumulateBounds(
            IReadOnlyList<Transform> waypoints,
            ref Vector3 minBounds,
            ref Vector3 maxBounds)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                return false;
            }

            bool hasAnyPoint = false;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Vector3 point = waypoints[i].position;
                minBounds.x = Mathf.Min(minBounds.x, point.x);
                minBounds.z = Mathf.Min(minBounds.z, point.z);
                maxBounds.x = Mathf.Max(maxBounds.x, point.x);
                maxBounds.z = Mathf.Max(maxBounds.z, point.z);
                hasAnyPoint = true;
            }

            return hasAnyPoint;
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
