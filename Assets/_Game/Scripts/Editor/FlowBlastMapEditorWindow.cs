#if UNITY_EDITOR
using System.Collections.Generic;
using FlowBlast.Bootstrap;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    public sealed class FlowBlastMapEditorWindow : EditorWindow
    {
        private const string DefaultLayoutFolder = "Assets/_Game/Data/MapLayouts";
        private const string WaypointPrefabPath = "Assets/_Game/Prefabs/BeltWaypointMarker.prefab";
        private const float CanvasMinHeight = 420f;
        private const float BottomPanelHeight = 240f;
        private const float SectionSpacing = 8f;
        private const float MinCurveStrength = 0f;
        private const float MaxCurveStrength = 1f;
        private const string MainPathTabLabel = "Main Path";

        private static FlowBlastMapEditorWindow instance;

        private readonly MapLayoutEditState mainEditState = new MapLayoutEditState();
        private readonly List<MapLayoutEditState> queueEditStates = new List<MapLayoutEditState>();

        private BeltPath beltPath;
        private readonly List<BeltPath> queueBeltPaths = new List<BeltPath>();
        private Transform boxQueueParent;
        private Transform collectionPointMarker;
        private MapEditorSettings settings;
        [SerializeField] private LevelMapLayout layoutAsset;
        [SerializeField] private LevelMapLayout trackedLayoutAsset;

        private float canvasZoom = 24f;
        private Vector2 canvasPan = Vector2.zero;
        private Vector2 sidebarScrollPosition;
        private bool isBoxQueueSelected;
        private bool isCollectionPointSelected;
        private bool showSceneSync;
        private int activeQueueIndex = -1;

        private const float DefaultPresetHalfWidth = 1.6f;
        private const float DefaultPresetHalfDepth = 4f;

        public static bool IsOpen => instance != null;

        [MenuItem("FlowBlast/Map Editor")]
        public static void ShowWindow()
        {
            ShowWindow(Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<BeltPath>()
                : null);
        }

        public static void ShowWindow(BeltPath targetPath)
        {
            FlowBlastMapEditorWindow window = GetWindow<FlowBlastMapEditorWindow>("FlowBlast Map");
            window.minSize = new Vector2(760f, 680f);
            window.InitializeTargets(targetPath);
            window.ReloadEditStates();
        }

        private void OnEnable()
        {
            instance = this;
            settings = BeltPathEditorUtility.LoadSettings();
            InitializeTargets(null);

            if (layoutAsset == null)
            {
                layoutAsset = trackedLayoutAsset;
            }

            TryLoadLayoutFromBinder();
            ReloadEditStates();
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private BeltPathRole ActivePathRole => activeQueueIndex >= 0 ? BeltPathRole.Queue : BeltPathRole.Main;

        private MapLayoutEditState ActiveEditState
        {
            get
            {
                if (activeQueueIndex < 0 || activeQueueIndex >= queueEditStates.Count)
                {
                    return mainEditState;
                }

                return queueEditStates[activeQueueIndex];
            }
        }

        private string ActivePathDisplayName
        {
            get
            {
                if (activeQueueIndex < 0 || activeQueueIndex >= queueEditStates.Count)
                {
                    return MainPathTabLabel;
                }

                string displayName = queueEditStates[activeQueueIndex].DisplayName;
                return string.IsNullOrWhiteSpace(displayName) ? $"Queue {activeQueueIndex + 1:00}" : displayName;
            }
        }

        private BeltPath GetActiveBeltPath()
        {
            if (activeQueueIndex < 0 || activeQueueIndex >= queueBeltPaths.Count)
            {
                return beltPath;
            }

            return queueBeltPaths[activeQueueIndex];
        }

        private void InitializeTargets(BeltPath targetPath)
        {
            if (targetPath != null)
            {
                AssignPathTarget(targetPath);
                ResolveQueueBeltPaths();
                ResolveBoxQueueParent();
                ResolveCollectionPointMarker();
                return;
            }

            beltPath = ResolveMainConveyorBeltPath();
            ResolveQueueBeltPaths();
            ResolveBoxQueueParent();
            ResolveCollectionPointMarker();
        }

        private void AssignPathTarget(BeltPath targetPath)
        {
            if (targetPath == null)
            {
                return;
            }

            if (targetPath.PathRole == BeltPathRole.Queue)
            {
                queueBeltPaths.Clear();
                queueBeltPaths.Add(targetPath);
                beltPath = ResolveMainConveyorBeltPath();
                activeQueueIndex = 0;
                return;
            }

            beltPath = targetPath;
            activeQueueIndex = -1;
        }

        private BeltPath ResolveMainConveyorBeltPath()
        {
            MapLayoutBinder binder = FindFirstObjectByType<MapLayoutBinder>();

            if (binder != null && binder.BeltPath != null)
            {
                return binder.BeltPath;
            }

            GameplayInstaller installer = FindFirstObjectByType<GameplayInstaller>();

            if (installer != null)
            {
                Transform conveyorRoot = TransformHierarchyUtility.FindChildRecursive(
                    installer.transform,
                    GameplayZoneNames.ConveyorRoot);

                if (conveyorRoot != null)
                {
                    return conveyorRoot.GetComponent<BeltPath>();
                }
            }

            return null;
        }

        private void ResolveQueueBeltPaths()
        {
            queueBeltPaths.Clear();
            GameplayInstaller installer = ResolveInstaller();

            if (installer == null)
            {
                return;
            }

            MapLayoutBinder binder = installer.GetComponent<MapLayoutBinder>();

            if (binder != null && binder.QueueBeltPaths.Count > 0)
            {
                for (int i = 0; i < binder.QueueBeltPaths.Count; i++)
                {
                    if (binder.QueueBeltPaths[i] != null)
                    {
                        queueBeltPaths.Add(binder.QueueBeltPaths[i]);
                    }
                }
            }

            if (queueBeltPaths.Count > 0)
            {
                SortQueueBeltPaths();
                ClampActiveQueueIndex();
                return;
            }

            BeltPath[] beltPaths = installer.GetComponentsInChildren<BeltPath>(true);

            for (int i = 0; i < beltPaths.Length; i++)
            {
                if (beltPaths[i] != null && beltPaths[i].PathRole == BeltPathRole.Queue)
                {
                    queueBeltPaths.Add(beltPaths[i]);
                }
            }

            SortQueueBeltPaths();
            ClampActiveQueueIndex();
        }

        private void SortQueueBeltPaths()
        {
            queueBeltPaths.Sort((left, right) =>
            {
                string leftId = left != null ? left.PathId : string.Empty;
                string rightId = right != null ? right.PathId : string.Empty;
                return string.CompareOrdinal(leftId, rightId);
            });
        }

        private void ClampActiveQueueIndex()
        {
            if (queueBeltPaths.Count == 0)
            {
                activeQueueIndex = -1;
                return;
            }

            if (activeQueueIndex >= queueBeltPaths.Count)
            {
                activeQueueIndex = queueBeltPaths.Count - 1;
            }
        }

        private GameplayInstaller ResolveInstaller()
        {
            if (beltPath != null)
            {
                GameplayInstaller installer = beltPath.GetComponentInParent<GameplayInstaller>();

                if (installer != null)
                {
                    return installer;
                }
            }

            for (int i = 0; i < queueBeltPaths.Count; i++)
            {
                if (queueBeltPaths[i] == null)
                {
                    continue;
                }

                GameplayInstaller installer = queueBeltPaths[i].GetComponentInParent<GameplayInstaller>();

                if (installer != null)
                {
                    return installer;
                }
            }

            return FindFirstObjectByType<GameplayInstaller>();
        }

        private void ResolveBoxQueueParent()
        {
            GameplayInstaller installer = ResolveInstaller();

            if (installer == null)
            {
                boxQueueParent = null;
                return;
            }

            SerializedObject serializedInstaller = new SerializedObject(installer);
            SerializedProperty boxQueueParentProperty = serializedInstaller.FindProperty("boxQueueParent");
            boxQueueParent = boxQueueParentProperty != null
                ? boxQueueParentProperty.objectReferenceValue as Transform
                : null;
        }

        private void ResolveCollectionPointMarker()
        {
            GameplayInstaller installer = ResolveInstaller();

            if (installer == null)
            {
                collectionPointMarker = null;
                return;
            }

            SerializedObject serializedInstaller = new SerializedObject(installer);
            SerializedProperty collectionPointMarkerProperty = serializedInstaller.FindProperty("collectionPointMarker");
            collectionPointMarker = collectionPointMarkerProperty != null
                ? collectionPointMarkerProperty.objectReferenceValue as Transform
                : null;
        }

        private void OnGUI()
        {
            settings = (MapEditorSettings)EditorGUILayout.ObjectField("Editor Settings", settings, typeof(MapEditorSettings), false);
            DrawPathTabs();
            DrawLayoutAssetField();
            DrawToolbar();

            EditorGUILayout.Space(SectionSpacing);
            DrawCanvas();
            EditorGUILayout.Space(SectionSpacing);
            DrawBottomPanel();
        }

        private void DrawPathTabs()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool isMainSelected = activeQueueIndex < 0;

                if (GUILayout.Toggle(isMainSelected, MainPathTabLabel, EditorStyles.toolbarButton) && !isMainSelected)
                {
                    SwitchToMainPath();
                }

                for (int i = 0; i < queueEditStates.Count; i++)
                {
                    bool isSelected = activeQueueIndex == i;
                    string label = GetQueueTabLabel(i);

                    if (GUILayout.Toggle(isSelected, label, EditorStyles.toolbarButton) && !isSelected)
                    {
                        SwitchToQueuePath(i);
                    }
                }
            }
        }

        private string GetQueueTabLabel(int index)
        {
            if (index < 0 || index >= queueEditStates.Count)
            {
                return $"Queue {index + 1:00}";
            }

            string displayName = queueEditStates[index].DisplayName;
            return string.IsNullOrWhiteSpace(displayName) ? $"Queue {index + 1:00}" : displayName;
        }

        private void SwitchToMainPath()
        {
            activeQueueIndex = -1;
            isBoxQueueSelected = false;
            isCollectionPointSelected = false;
            mainEditState.SelectedWaypointIndex = -1;
        }

        private void SwitchToQueuePath(int index)
        {
            activeQueueIndex = Mathf.Clamp(index, 0, queueEditStates.Count - 1);
            isBoxQueueSelected = false;
            isCollectionPointSelected = false;
            ActiveEditState.SelectedWaypointIndex = -1;
        }

        private void DrawLayoutAssetField()
        {
            EditorGUI.BeginChangeCheck();
            layoutAsset = (LevelMapLayout)EditorGUILayout.ObjectField("Layout Asset", layoutAsset, typeof(LevelMapLayout), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("New", GUILayout.Width(48f)))
                {
                    CreateLayoutAssetQuick();
                }

                if (GUILayout.Button("Save", GUILayout.Width(48f)))
                {
                    SaveLayoutToAsset();
                }

                if (GUILayout.Button("Load", GUILayout.Width(48f)))
                {
                    ReloadEditStates();
                    ShowNotification(new GUIContent("Layout reloaded"));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                trackedLayoutAsset = layoutAsset;
                ReloadEditStates();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Add WP", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                {
                    AddWaypointAfterSelection();
                }

                if (GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                {
                    RemoveSelectedWaypoint();
                }

                GUILayout.Space(8f);

                if (GUILayout.Button("Add Queue", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    AddQueuePathTab();
                }

                using (new EditorGUI.DisabledScope(activeQueueIndex < 0))
                {
                    if (GUILayout.Button("Remove Queue", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                    {
                        RemoveActiveQueuePathTab();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(ActivePathDisplayName, EditorStyles.miniLabel, GUILayout.Width(90f));
                EditorGUILayout.LabelField($"Len {ActiveEditState.PathLength:0.0}", EditorStyles.miniLabel, GUILayout.Width(64f));
                EditorGUILayout.LabelField($"WP {ActiveEditState.WaypointLocalPositions.Count}", EditorStyles.miniLabel, GUILayout.Width(48f));
            }
        }

        private void DrawCanvas()
        {
            float availableHeight = position.height
                - EditorGUIUtility.singleLineHeight * 3f
                - BottomPanelHeight
                - SectionSpacing * 4f;
            float canvasHeight = Mathf.Max(CanvasMinHeight, availableHeight);
            Rect canvasRect = GUILayoutUtility.GetRect(
                10f,
                canvasHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(canvasHeight));

            MapLayoutCanvasView.Draw(
                canvasRect,
                ActiveEditState,
                GetVisibleEditStates(),
                settings,
                ActivePathRole,
                ref canvasZoom,
                ref canvasPan,
                ref isBoxQueueSelected,
                ref isCollectionPointSelected);
        }

        private IReadOnlyList<MapLayoutEditState> GetVisibleEditStates()
        {
            List<MapLayoutEditState> visibleStates = new List<MapLayoutEditState>(1 + queueEditStates.Count)
            {
                mainEditState
            };
            visibleStates.AddRange(queueEditStates);
            return visibleStates;
        }

        private void DrawBottomPanel()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(sidebarScrollPosition, GUILayout.Height(BottomPanelHeight)))
            {
                sidebarScrollPosition = scroll.scrollPosition;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.48f)))
                    {
                        DrawSelectionPanel();
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        DrawPresetPanel();
                        DrawSceneSyncPanel();
                    }
                }
            }
        }

        private void DrawSelectionPanel()
        {
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Editing", ActivePathDisplayName);

            EditorGUI.BeginChangeCheck();
            bool closedLoop = EditorGUILayout.Toggle("Closed Loop", ActiveEditState.IsClosedLoop);

            if (EditorGUI.EndChangeCheck())
            {
                ActiveEditState.IsClosedLoop = closedLoop;
            }

            EditorGUI.BeginChangeCheck();
            float curveStrength = EditorGUILayout.Slider("Curve Strength", ActiveEditState.CurveStrength, MinCurveStrength, MaxCurveStrength);

            if (EditorGUI.EndChangeCheck())
            {
                ActiveEditState.CurveStrength = curveStrength;
            }

            if (isBoxQueueSelected)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 queuePosition = EditorGUILayout.Vector3Field("Queue Entry", ActiveEditState.BoxQueueLocalPosition);

                if (EditorGUI.EndChangeCheck())
                {
                    SetSharedQueuePosition(queuePosition);
                }
            }
            else if (isCollectionPointSelected)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 collectionPointPosition = EditorGUILayout.Vector3Field("Collection Point", ActiveEditState.CollectionPointLocalPosition);

                if (EditorGUI.EndChangeCheck())
                {
                    SetSharedCollectionPointPosition(collectionPointPosition);
                }
            }
            else if (ActiveEditState.SelectedWaypointIndex >= 0
                && ActiveEditState.SelectedWaypointIndex < ActiveEditState.WaypointLocalPositions.Count)
            {
                int index = ActiveEditState.SelectedWaypointIndex;
                EditorGUI.BeginChangeCheck();
                Vector3 waypointPosition = EditorGUILayout.Vector3Field(
                    $"Waypoint {index}",
                    ActiveEditState.WaypointLocalPositions[index]);

                if (EditorGUI.EndChangeCheck())
                {
                    if (settings != null && settings.SnapToGrid)
                    {
                        waypointPosition = BeltPathEditorUtility.SnapLocalPosition(
                            waypointPosition,
                            settings.GridCellSize);
                    }

                    ActiveEditState.SetWaypointPosition(index, waypointPosition);
                }

                EditorGUILayout.LabelField("Curve Preview", $"{ActiveEditState.CurveStrength:0.00}");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Up") && index > 0)
                    {
                        ActiveEditState.SwapWaypoints(index, index - 1);
                    }

                    if (GUILayout.Button("Down") && index < ActiveEditState.WaypointLocalPositions.Count - 1)
                    {
                        ActiveEditState.SwapWaypoints(index, index + 1);
                    }

                    if (GUILayout.Button("Delete"))
                    {
                        RemoveSelectedWaypoint();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a waypoint, queue entry, or collection point on the canvas.", MessageType.None);
            }

            if (settings != null)
            {
                EditorGUILayout.LabelField("Block Capacity", ActiveEditState.GetBlockCapacity(settings).ToString());
            }
        }

        private void DrawPresetPanel()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rectangle"))
                {
                    ApplyPreset(MapPathPresets.CreateRectangleLoop(DefaultPresetHalfWidth, DefaultPresetHalfDepth), true);
                }

                if (GUILayout.Button("U Shape"))
                {
                    ApplyPreset(MapPathPresets.CreateUShape(DefaultPresetHalfWidth, DefaultPresetHalfDepth), false);
                }

                if (GUILayout.Button("Line"))
                {
                    ApplyPreset(MapPathPresets.CreateStraightLine(DefaultPresetHalfDepth * 2f, 5), false);
                }
            }
        }

        private void DrawSceneSyncPanel()
        {
            showSceneSync = EditorGUILayout.Foldout(showSceneSync, "Scene Sync (optional)", true);

            if (!showSceneSync)
            {
                return;
            }

            beltPath = (BeltPath)EditorGUILayout.ObjectField("Main Belt Path", beltPath, typeof(BeltPath), true);
            boxQueueParent = (Transform)EditorGUILayout.ObjectField("Queue Entry Anchor", boxQueueParent, typeof(Transform), true);
            collectionPointMarker = (Transform)EditorGUILayout.ObjectField("Collection Point Anchor", collectionPointMarker, typeof(Transform), true);

            EditorGUILayout.LabelField("Queue Belt Paths", EditorStyles.miniBoldLabel);

            for (int i = 0; i < queueBeltPaths.Count; i++)
            {
                queueBeltPaths[i] = (BeltPath)EditorGUILayout.ObjectField($"Queue {i + 1:00}", queueBeltPaths[i], typeof(BeltPath), true);
            }

            using (new EditorGUI.DisabledScope(GetActiveBeltPath() == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Apply To Scene"))
                    {
                        ApplyEditStateToScene();
                    }

                    if (GUILayout.Button("Import From Scene"))
                    {
                        ImportActivePathFromScene();
                        ShowNotification(new GUIContent("Imported from scene"));
                    }
                }
            }
        }

        private void AddQueuePathTab()
        {
            int queueNumber = queueEditStates.Count + 1;
            string queueId = $"Queue_{queueNumber:00}";
            string displayName = $"Queue {queueNumber:00}";
            MapLayoutEditState editState = new MapLayoutEditState();
            editState.InitializeQueueIdentity(queueId, displayName);
            editState.SetBoxQueuePosition(mainEditState.BoxQueueLocalPosition);
            editState.SetCollectionPointPosition(mainEditState.CollectionPointLocalPosition);
            queueEditStates.Add(editState);
            activeQueueIndex = queueEditStates.Count - 1;
            isBoxQueueSelected = false;
        }

        private void RemoveActiveQueuePathTab()
        {
            if (activeQueueIndex < 0 || activeQueueIndex >= queueEditStates.Count)
            {
                return;
            }

            queueEditStates.RemoveAt(activeQueueIndex);

            if (activeQueueIndex >= queueEditStates.Count)
            {
                activeQueueIndex = queueEditStates.Count - 1;
            }

            if (queueEditStates.Count == 0)
            {
                activeQueueIndex = -1;
            }

            trackedLayoutAsset = layoutAsset;
        }

        private void AddWaypointAfterSelection()
        {
            float step = settings != null ? settings.GridCellSize : 0.4f;
            Vector3 localPosition = Vector3.zero;

            if (ActiveEditState.SelectedWaypointIndex >= 0
                && ActiveEditState.SelectedWaypointIndex < ActiveEditState.WaypointLocalPositions.Count)
            {
                localPosition = ActiveEditState.WaypointLocalPositions[ActiveEditState.SelectedWaypointIndex] + Vector3.forward * step;
            }
            else if (ActiveEditState.WaypointLocalPositions.Count > 0)
            {
                localPosition = ActiveEditState.WaypointLocalPositions[^1] + Vector3.forward * step;
            }

            if (settings != null && settings.SnapToGrid)
            {
                localPosition = BeltPathEditorUtility.SnapLocalPosition(localPosition, settings.GridCellSize);
            }

            ActiveEditState.AddWaypoint(localPosition);
        }

        private void RemoveSelectedWaypoint()
        {
            if (ActiveEditState.SelectedWaypointIndex < 0)
            {
                return;
            }

            ActiveEditState.RemoveWaypoint(ActiveEditState.SelectedWaypointIndex);
            trackedLayoutAsset = layoutAsset;
        }

        private void ApplyPreset(Vector3[] localPositions, bool isClosedLoop)
        {
            ActiveEditState.ApplyPreset(localPositions, isClosedLoop);
        }

        private void ReloadEditStates()
        {
            RestoreLayoutAssetReference();

            if (layoutAsset == null)
            {
                ImportAllFromScene();
                isBoxQueueSelected = false;
                isCollectionPointSelected = false;
                Repaint();
                return;
            }

            mainEditState.LoadMainPath(layoutAsset);
            queueEditStates.Clear();

            for (int i = 0; i < layoutAsset.QueuePaths.Count; i++)
            {
                QueuePathLayout queuePath = layoutAsset.QueuePaths[i];

                if (queuePath == null)
                {
                    continue;
                }

                MapLayoutEditState queueState = new MapLayoutEditState();
                queueState.LoadQueuePath(layoutAsset, queuePath.QueueId, queuePath.DisplayName);
                queueEditStates.Add(queueState);
            }

            trackedLayoutAsset = layoutAsset;
            isBoxQueueSelected = false;
            isCollectionPointSelected = false;
            SyncSharedQueuePosition();
            SyncSharedCollectionPointPosition();
            ClampActiveQueueIndex();
            Repaint();
        }

        private void SaveLayoutToAsset()
        {
            if (layoutAsset == null)
            {
                CreateLayoutAssetQuick();
                return;
            }

            mainEditState.WriteMainPathTo(layoutAsset);

            for (int i = 0; i < queueEditStates.Count; i++)
            {
                queueEditStates[i].WriteQueuePathTo(layoutAsset);
            }

            layoutAsset.SetBoxQueueLocalPosition(mainEditState.BoxQueueLocalPosition);
            layoutAsset.SetCollectionPointLocalPosition(mainEditState.CollectionPointLocalPosition);

            HashSet<string> validQueueIds = new HashSet<string>();

            for (int i = 0; i < queueEditStates.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(queueEditStates[i].QueueId))
                {
                    validQueueIds.Add(queueEditStates[i].QueueId);
                }
            }

            layoutAsset.RemoveMissingQueuePaths(validQueueIds);
            trackedLayoutAsset = layoutAsset;
            EditorUtility.SetDirty(layoutAsset);
            AssetDatabase.SaveAssets();
            SyncMapLayoutBinder(layoutAsset);
            ShowNotification(new GUIContent($"Saved: {layoutAsset.name}"));
        }

        private void ApplyEditStateToScene()
        {
            BeltPath activePath = GetActiveBeltPath();

            if (activePath == null)
            {
                return;
            }

            Undo.RecordObject(activePath, "Apply Map Layout To Scene");
            BeltWaypointMarker waypointPrefab = LoadWaypointPrefab();
            activePath.ApplyLocalWaypoints(
                ActiveEditState.WaypointLocalPositions,
                ActiveEditState.IsClosedLoop,
                ActiveEditState.CurveStrength,
                waypointPrefab);

            if (boxQueueParent != null && (activeQueueIndex < 0 || activeQueueIndex >= queueEditStates.Count))
            {
                Undo.RecordObject(boxQueueParent, "Apply Queue Entry Position");
                boxQueueParent.localPosition = ActiveEditState.BoxQueueLocalPosition;
                EditorUtility.SetDirty(boxQueueParent);
            }

            if (collectionPointMarker != null)
            {
                Undo.RecordObject(collectionPointMarker, "Apply Collection Point Position");
                collectionPointMarker.localPosition = ActiveEditState.CollectionPointLocalPosition;
                EditorUtility.SetDirty(collectionPointMarker);
            }

            if (layoutAsset != null)
            {
                SaveLayoutToAsset();
            }

            EditorUtility.SetDirty(activePath);
            MarkSceneDirty(activePath);
            ShowNotification(new GUIContent($"Applied {ActivePathDisplayName}"));
        }

        private void ImportActivePathFromScene()
        {
            BeltPath activePath = GetActiveBeltPath();

            if (activePath == null)
            {
                return;
            }

            ActiveEditState.LoadFromScene(activePath, boxQueueParent, collectionPointMarker);
            SyncSharedQueuePosition();
            SyncSharedCollectionPointPosition();
        }

        private void ImportAllFromScene()
        {
            if (beltPath != null)
            {
                mainEditState.LoadFromScene(beltPath, boxQueueParent, collectionPointMarker);
            }
            else
            {
                mainEditState.Clear();
            }

            queueEditStates.Clear();

            for (int i = 0; i < queueBeltPaths.Count; i++)
            {
                if (queueBeltPaths[i] == null)
                {
                    continue;
                }

                MapLayoutEditState queueState = new MapLayoutEditState();
                queueState.LoadFromScene(queueBeltPaths[i], boxQueueParent, collectionPointMarker);
                queueEditStates.Add(queueState);
            }

            SyncSharedQueuePosition();
            SyncSharedCollectionPointPosition();
            ClampActiveQueueIndex();
        }

        private void SyncSharedQueuePosition()
        {
            Vector3 queuePosition = mainEditState.BoxQueueLocalPosition;

            if (activeQueueIndex >= 0 && activeQueueIndex < queueEditStates.Count)
            {
                queuePosition = queueEditStates[activeQueueIndex].BoxQueueLocalPosition;
            }

            SetSharedQueuePosition(queuePosition);
        }

        private void SyncSharedCollectionPointPosition()
        {
            Vector3 collectionPointPosition = mainEditState.CollectionPointLocalPosition;

            if (activeQueueIndex >= 0 && activeQueueIndex < queueEditStates.Count)
            {
                collectionPointPosition = queueEditStates[activeQueueIndex].CollectionPointLocalPosition;
            }

            SetSharedCollectionPointPosition(collectionPointPosition);
        }

        private void SetSharedQueuePosition(Vector3 queuePosition)
        {
            if (activeQueueIndex >= 0 && activeQueueIndex < queueEditStates.Count)
            {
                queueEditStates[activeQueueIndex].SetBoxQueuePosition(queuePosition);
                return;
            }

            mainEditState.SetBoxQueuePosition(queuePosition);
        }

        private void SetSharedCollectionPointPosition(Vector3 collectionPointPosition)
        {
            mainEditState.SetCollectionPointPosition(collectionPointPosition);

            for (int i = 0; i < queueEditStates.Count; i++)
            {
                queueEditStates[i].SetCollectionPointPosition(collectionPointPosition);
            }
        }

        private void CreateLayoutAssetQuick()
        {
            EnsureFolder(DefaultLayoutFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{DefaultLayoutFolder}/MapLayout.asset");
            LevelMapLayout layout = ScriptableObject.CreateInstance<LevelMapLayout>();
            mainEditState.WriteMainPathTo(layout);

            for (int i = 0; i < queueEditStates.Count; i++)
            {
                queueEditStates[i].WriteQueuePathTo(layout);
            }

            layout.SetBoxQueueLocalPosition(mainEditState.BoxQueueLocalPosition);
            layout.SetCollectionPointLocalPosition(mainEditState.CollectionPointLocalPosition);

            AssetDatabase.CreateAsset(layout, assetPath);
            AssetDatabase.SaveAssets();

            SerializedObject serializedLayout = new SerializedObject(layout);
            serializedLayout.FindProperty("layoutId").stringValue =
                System.IO.Path.GetFileNameWithoutExtension(assetPath);
            serializedLayout.ApplyModifiedPropertiesWithoutUndo();

            layoutAsset = layout;
            trackedLayoutAsset = layout;
            SyncMapLayoutBinder(layout);
            EditorGUIUtility.PingObject(layout);
            ShowNotification(new GUIContent($"Created: {layout.name}"));
        }

        private void TryLoadLayoutFromBinder()
        {
            if (layoutAsset != null)
            {
                return;
            }

            MapLayoutBinder binder = ResolveMapLayoutBinder();

            if (binder == null)
            {
                return;
            }

            SerializedObject serializedBinder = new SerializedObject(binder);
            layoutAsset = serializedBinder.FindProperty("mapLayout").objectReferenceValue as LevelMapLayout;

            if (layoutAsset != null)
            {
                trackedLayoutAsset = layoutAsset;
            }
        }

        private void SyncMapLayoutBinder(LevelMapLayout layout)
        {
            if (layout == null)
            {
                return;
            }

            MapLayoutBinder binder = ResolveMapLayoutBinder();

            if (binder == null)
            {
                return;
            }

            SerializedObject serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("mapLayout").objectReferenceValue = layout;
            serializedBinder.ApplyModifiedProperties();
            EditorUtility.SetDirty(binder);
        }

        private void MarkSceneDirty(BeltPath activePath)
        {
            if (activePath == null || Application.isPlaying)
            {
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activePath.gameObject.scene);
        }

        private void RestoreLayoutAssetReference()
        {
            if (layoutAsset != null)
            {
                trackedLayoutAsset = layoutAsset;
                return;
            }

            if (trackedLayoutAsset != null)
            {
                layoutAsset = trackedLayoutAsset;
                return;
            }

            TryLoadLayoutFromBinder();
        }

        private MapLayoutBinder ResolveMapLayoutBinder()
        {
            if (beltPath != null)
            {
                MapLayoutBinder binder = beltPath.GetComponentInParent<MapLayoutBinder>();

                if (binder != null)
                {
                    return binder;
                }
            }

            for (int i = 0; i < queueBeltPaths.Count; i++)
            {
                if (queueBeltPaths[i] == null)
                {
                    continue;
                }

                MapLayoutBinder binder = queueBeltPaths[i].GetComponentInParent<MapLayoutBinder>();

                if (binder != null)
                {
                    return binder;
                }
            }

            return FindFirstObjectByType<MapLayoutBinder>();
        }

        private static BeltWaypointMarker LoadWaypointPrefab()
        {
            GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(WaypointPrefabPath);
            return prefabObject != null ? prefabObject.GetComponent<BeltWaypointMarker>() : null;
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
