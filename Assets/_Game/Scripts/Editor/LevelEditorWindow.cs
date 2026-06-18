#if UNITY_EDITOR
using FlowBlast.Bootstrap;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const float DefaultCellSpacing = 1.5f;
        private const float GridCellButtonSize = 34f;
        private const float GridColorPreviewSize = 18f;
        private const int DefaultGridWidth = 5;
        private const int DefaultGridHeight = 5;

        private static LevelEditorWindow instance;

        private LevelData levelData;
        private GameplayInstaller gameplayInstaller;
        private SerializedObject serializedLevelData;
        private SerializedProperty boxPlacementsProperty;
        private SerializedProperty autoBuildBlockSequenceFromBoxesProperty;
        private SerializedProperty blockSequenceProperty;
        private SerializedProperty beltLaneCountProperty;
        private Vector2 boxListScrollPosition;
        private int selectedPlacementIndex = -1;
        private int gridWidth = DefaultGridWidth;
        private int gridHeight = DefaultGridHeight;
        private float gridCellSpacing = DefaultCellSpacing;
        private int brushCapacity = GameConstants.DefaultBoxCapacity;
        private bool brushHidden;
        private int brushFrozenClearsRequired;
        private BlockColor brushColor = BlockColor.Green;
        private bool eraseMode;
        private bool showSettings = true;
        private bool showSequence = true;
        private bool showGridAuthoring = true;
        private bool showBoxList = true;

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
            SyncGridSizeFromData();
        }

        private void RefreshSerializedData()
        {
            if (levelData == null)
            {
                serializedLevelData = null;
                boxPlacementsProperty = null;
                autoBuildBlockSequenceFromBoxesProperty = null;
                blockSequenceProperty = null;
                beltLaneCountProperty = null;
                return;
            }

            serializedLevelData = new SerializedObject(levelData);
            boxPlacementsProperty = serializedLevelData.FindProperty("boxPlacements");
            autoBuildBlockSequenceFromBoxesProperty = serializedLevelData.FindProperty("autoBuildBlockSequenceFromBoxes");
            blockSequenceProperty = serializedLevelData.FindProperty("blockSequence");
            beltLaneCountProperty = serializedLevelData.FindProperty("beltLaneCount");
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

            DrawSceneBindings();
            EditorGUILayout.Space(6f);
            DrawSettingsSection();
            EditorGUILayout.Space(6f);
            DrawGridAuthoringSection();
            EditorGUILayout.Space(6f);
            DrawBoxListSection();
            EditorGUILayout.Space(6f);
            DrawSelectedBoxPanel();
            EditorGUILayout.Space(6f);
            DrawSequenceSection();
            EditorGUILayout.Space(6f);
            DrawSummarySection();

            serializedLevelData.ApplyModifiedProperties();
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
            DrawProperty("beltSpeed");
            DrawProperty("maxBeltSlots");
            DrawProperty("maxBacklogBlocks");
            DrawProperty("beltLaneCount");
            DrawProperty("autoBuildBlockSequenceFromBoxes");
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
            DrawGridCanvas();
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                gridWidth = Mathf.Max(1, EditorGUILayout.IntField("Columns", gridWidth));
                gridHeight = Mathf.Max(1, EditorGUILayout.IntField("Rows", gridHeight));
            }

            gridCellSpacing = Mathf.Max(0.25f, EditorGUILayout.FloatField("Cell Spacing", gridCellSpacing));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit From Boxes"))
                {
                    SyncGridSizeFromData();
                }

                if (GUILayout.Button("Center Layout"))
                {
                    CenterAllPlacements();
                }
            }
        }

        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                brushColor = (BlockColor)EditorGUILayout.EnumPopup("Color", brushColor);
                DrawInlineColorPreview(ResolveBoxColor(brushColor));
            }

            brushCapacity = Mathf.Max(1, EditorGUILayout.IntField("Capacity", brushCapacity));
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

            if (GUI.Button(cellRect, GUIContent.none, GUIStyle.none))
            {
                HandleGridCellClick(column, row, placementIndex);
            }
        }

        private void DrawBoxListSection()
        {
            showBoxList = EditorGUILayout.Foldout(showBoxList, $"Box List ({GetPlacementCount()})", true);

            if (!showBoxList)
            {
                return;
            }

            if (GetPlacementCount() == 0)
            {
                EditorGUILayout.HelpBox("No boxes placed yet. Click grid cells above to place boxes.", MessageType.Info);
                return;
            }

            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(boxListScrollPosition, GUILayout.MaxHeight(220f)))
            {
                boxListScrollPosition = scrollView.scrollPosition;

                for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
                {
                    DrawPlacementRow(i, boxPlacementsProperty.GetArrayElementAtIndex(i));
                }
            }
        }

        private void DrawPlacementRow(int index, SerializedProperty placementProperty)
        {
            SerializedProperty colorProperty = placementProperty.FindPropertyRelative("color");
            SerializedProperty capacityProperty = placementProperty.FindPropertyRelative("capacity");
            SerializedProperty localPositionProperty = placementProperty.FindPropertyRelative("localPosition");
            bool isSelected = selectedPlacementIndex == index;
            Color boxColor = ResolveBoxColor((BlockColor)colorProperty.enumValueIndex);
            Vector3 localPosition = localPositionProperty.vector3Value;
            Vector2Int cell = GetGridCellFromLocalPosition(localPosition);
            GUIStyle rowStyle = new GUIStyle(EditorStyles.helpBox);

            if (isSelected)
            {
                rowStyle.normal.background = Texture2D.grayTexture;
            }

            using (new EditorGUILayout.HorizontalScope(rowStyle))
            {
                DrawColorPreview(boxColor);

                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button($"Box #{index + 1}", EditorStyles.miniButtonLeft, GUILayout.Width(72f)))
                        {
                            SelectPlacement(index);
                        }

                        EditorGUILayout.PropertyField(colorProperty, GUIContent.none, GUILayout.Width(96f));
                        GUILayout.Label($"Cap {capacityProperty.intValue}", EditorStyles.miniLabel, GUILayout.Width(54f));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"Grid ({cell.x + 1},{gridHeight - cell.y})", EditorStyles.miniLabel, GUILayout.Width(88f));
                        GUILayout.Label($"X {localPosition.x:0.00}", EditorStyles.miniLabel, GUILayout.Width(58f));
                        GUILayout.Label($"Z {localPosition.z:0.00}", EditorStyles.miniLabel, GUILayout.Width(58f));
                    }
                }

                if (GUILayout.Button("Select", GUILayout.Width(48f)))
                {
                    SelectPlacement(index);
                }

                if (GUILayout.Button("S", GUILayout.Width(24f)))
                {
                    SelectPlacement(index);
                    FrameSelectedPlacementInScene();
                }
            }
        }

        private void DrawSelectedBoxPanel()
        {
            EditorGUILayout.LabelField("Selected Box", EditorStyles.boldLabel);

            if (!HasSelectedPlacement())
            {
                EditorGUILayout.HelpBox("Select a box from the grid or list to edit its data and move it in the Scene view.", MessageType.None);
                return;
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(selectedPlacementIndex);
            SerializedProperty localPositionProperty = placementProperty.FindPropertyRelative("localPosition");
            Vector3 previousLocalPosition = localPositionProperty.vector3Value;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(localPositionProperty);

            if (EditorGUI.EndChangeCheck())
            {
                localPositionProperty.vector3Value = GetUniqueSnappedPosition(
                    SnapLocalPosition(localPositionProperty.vector3Value),
                    selectedPlacementIndex,
                    previousLocalPosition);
            }

            EditorGUILayout.PropertyField(placementProperty.FindPropertyRelative("color"));
            EditorGUILayout.PropertyField(placementProperty.FindPropertyRelative("capacity"));
            EditorGUILayout.PropertyField(placementProperty.FindPropertyRelative("isHidden"));
            EditorGUILayout.PropertyField(placementProperty.FindPropertyRelative("frozenClearsRequired"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Brush"))
                {
                    ApplyBrushToPlacement(selectedPlacementIndex);
                }

                if (GUILayout.Button("Delete"))
                {
                    RemoveSelectedPlacement();
                }

                if (GUILayout.Button("Snap"))
                {
                    Vector3 localPosition = localPositionProperty.vector3Value;
                    localPositionProperty.vector3Value = GetUniqueSnappedPosition(
                        SnapLocalPosition(localPosition),
                        selectedPlacementIndex,
                        previousLocalPosition);
                }
            }
        }

        private void DrawSequenceSection()
        {
            showSequence = EditorGUILayout.Foldout(showSequence, "Block Sequence", true);

            if (!showSequence)
            {
                return;
            }

            if (autoBuildBlockSequenceFromBoxesProperty.boolValue)
            {
                EditorGUILayout.HelpBox("Sequence is generated automatically from box colors and capacities.", MessageType.None);
                return;
            }

            EditorGUILayout.PropertyField(blockSequenceProperty, true);
        }

        private void DrawSummarySection()
        {
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Box Count", levelData.BoxPlacements.Count.ToString());
            EditorGUILayout.LabelField("Total Blocks", levelData.TotalBlockCount.ToString());
            EditorGUILayout.LabelField("Sequence Rows", levelData.BlockSequence.Count.ToString());
            EditorGUILayout.LabelField("Lane Count", beltLaneCountProperty.intValue.ToString());
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
                SelectPlacement(placementIndex);
                ApplyBrushToPlacement(placementIndex);
                MovePlacementToGridCell(placementIndex, column, row);
                return;
            }

            AddPlacementAtGridCell(column, row);
        }

        private void AddPlacementAtGridCell(int column, int row)
        {
            serializedLevelData.Update();
            int insertIndex = boxPlacementsProperty.arraySize;
            boxPlacementsProperty.InsertArrayElementAtIndex(insertIndex);
            selectedPlacementIndex = insertIndex;
            ApplyBrushToPlacement(insertIndex);
            MovePlacementToGridCell(insertIndex, column, row);
            serializedLevelData.ApplyModifiedProperties();
            EditorUtility.SetDirty(levelData);
            Repaint();
        }

        private void ApplyBrushToPlacement(int placementIndex)
        {
            if (placementIndex < 0 || placementIndex >= boxPlacementsProperty.arraySize)
            {
                return;
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
            placementProperty.FindPropertyRelative("color").enumValueIndex = (int)brushColor;
            placementProperty.FindPropertyRelative("capacity").intValue = brushCapacity;
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
            boxPlacementsProperty.DeleteArrayElementAtIndex(selectedPlacementIndex);
            selectedPlacementIndex = Mathf.Clamp(selectedPlacementIndex - 1, -1, boxPlacementsProperty.arraySize - 1);
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

        private Color ResolvePlacementColor(int placementIndex)
        {
            if (placementIndex < 0 || placementIndex >= boxPlacementsProperty.arraySize)
            {
                return new Color(0.16f, 0.16f, 0.16f, 1f);
            }

            SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(placementIndex);
            BlockColor blockColor = (BlockColor)placementProperty.FindPropertyRelative("color").enumValueIndex;
            return ResolveBoxColor(blockColor);
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
            float offsetX = (gridWidth - 1) * gridCellSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * gridCellSpacing * 0.5f;
            float x = column * gridCellSpacing - offsetX;
            float z = offsetZ - row * gridCellSpacing;
            return SnapLocalPosition(new Vector3(x, 0f, z));
        }

        private Vector2Int GetGridCellFromLocalPosition(Vector3 localPosition)
        {
            float offsetX = (gridWidth - 1) * gridCellSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * gridCellSpacing * 0.5f;
            int column = Mathf.RoundToInt((localPosition.x + offsetX) / gridCellSpacing);
            int row = Mathf.RoundToInt((offsetZ - localPosition.z) / gridCellSpacing);
            return new Vector2Int(
                Mathf.Clamp(column, 0, Mathf.Max(0, gridWidth - 1)),
                Mathf.Clamp(row, 0, Mathf.Max(0, gridHeight - 1)));
        }

        private void SyncGridSizeFromData()
        {
            if (boxPlacementsProperty == null || boxPlacementsProperty.arraySize == 0)
            {
                gridWidth = Mathf.Max(1, gridWidth);
                gridHeight = Mathf.Max(1, gridHeight);
                return;
            }

            int maxColumn = 0;
            int maxRow = 0;

            for (int i = 0; i < boxPlacementsProperty.arraySize; i++)
            {
                SerializedProperty placementProperty = boxPlacementsProperty.GetArrayElementAtIndex(i);
                Vector2Int cell = GetGridCellFromLocalPosition(placementProperty.FindPropertyRelative("localPosition").vector3Value);
                maxColumn = Mathf.Max(maxColumn, cell.x);
                maxRow = Mathf.Max(maxRow, cell.y);
            }

            gridWidth = Mathf.Max(DefaultGridWidth, maxColumn + 1);
            gridHeight = Mathf.Max(DefaultGridHeight, maxRow + 1);
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
