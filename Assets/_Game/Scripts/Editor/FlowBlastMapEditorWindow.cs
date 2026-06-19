#if UNITY_EDITOR
using FlowBlast.Bootstrap;
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
        private const float BottomPanelHeight = 220f;
        private const float SectionSpacing = 8f;

        private static FlowBlastMapEditorWindow instance;

        private readonly MapLayoutEditState editState = new MapLayoutEditState();

        private BeltPath beltPath;
        private Transform boxQueueParent;
        private MapEditorSettings settings;
        private LevelMapLayout layoutAsset;
        private LevelMapLayout trackedLayoutAsset;

        private float presetHalfWidth = 1.6f;
        private float presetHalfDepth = 4f;
        private float canvasZoom = 24f;
        private Vector2 canvasPan = Vector2.zero;
        private Vector2 waypointScrollPosition;
        private Vector2 sidebarScrollPosition;
        private bool isBoxQueueSelected;
        private bool showSceneSync;

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
            window.minSize = new Vector2(720f, 640f);
            window.InitializeTargets(targetPath);
            window.ReloadEditStateFromAsset();
        }

        private void OnEnable()
        {
            instance = this;
            settings = BeltPathEditorUtility.LoadSettings();
            InitializeTargets(null);
            TryLoadLayoutFromBinder();
            ReloadEditStateFromAsset();
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void InitializeTargets(BeltPath targetPath)
        {
            if (targetPath != null)
            {
                beltPath = targetPath;
                ResolveBoxQueueParent();
                return;
            }

            beltPath = FindFirstObjectByType<BeltPath>();
            ResolveBoxQueueParent();
        }

        private void ResolveBoxQueueParent()
        {
            if (beltPath == null)
            {
                boxQueueParent = null;
                return;
            }

            GameplayInstaller installer = beltPath.GetComponentInParent<GameplayInstaller>();

            if (installer == null)
            {
                installer = FindFirstObjectByType<GameplayInstaller>();
            }

            if (installer == null)
            {
                return;
            }

            SerializedObject serializedInstaller = new SerializedObject(installer);
            boxQueueParent = serializedInstaller.FindProperty("boxQueueParent").objectReferenceValue as Transform;
        }

        private void OnGUI()
        {
            settings = (MapEditorSettings)EditorGUILayout.ObjectField("Editor Settings", settings, typeof(MapEditorSettings), false);
            DrawLayoutAssetField();
            DrawToolbar();

            EditorGUILayout.Space(SectionSpacing);
            DrawCanvas();
            EditorGUILayout.Space(SectionSpacing);
            DrawBottomPanel();
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

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Len {editState.PathLength:0.0}", EditorStyles.miniLabel, GUILayout.Width(64f));
                EditorGUILayout.LabelField($"WP {editState.WaypointLocalPositions.Count}", EditorStyles.miniLabel, GUILayout.Width(48f));
            }
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
                    ReloadEditStateFromAsset();
                    ShowNotification(new GUIContent("Layout reloaded"));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                ReloadEditStateFromAsset();
            }
        }

        private void DrawCanvas()
        {
            float availableHeight = position.height
                - EditorGUIUtility.singleLineHeight * 2f
                - BottomPanelHeight
                - SectionSpacing * 4f;
            float canvasHeight = Mathf.Max(CanvasMinHeight, availableHeight);
            Rect canvasRect = GUILayoutUtility.GetRect(
                10f,
                canvasHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(canvasHeight));

            MapLayoutCanvasView.Draw(canvasRect, editState, settings, ref canvasZoom, ref canvasPan, ref isBoxQueueSelected);
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

            EditorGUI.BeginChangeCheck();
            bool closedLoop = EditorGUILayout.Toggle("Closed Loop", editState.IsClosedLoop);

            if (EditorGUI.EndChangeCheck())
            {
                editState.IsClosedLoop = closedLoop;
            }

            if (isBoxQueueSelected)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 queuePosition = EditorGUILayout.Vector3Field("Box Queue", editState.BoxQueueLocalPosition);

                if (EditorGUI.EndChangeCheck())
                {
                    editState.SetBoxQueuePosition(queuePosition);
                }
            }
            else if (editState.SelectedWaypointIndex >= 0
                && editState.SelectedWaypointIndex < editState.WaypointLocalPositions.Count)
            {
                int index = editState.SelectedWaypointIndex;
                EditorGUI.BeginChangeCheck();
                Vector3 waypointPosition = EditorGUILayout.Vector3Field(
                    $"Waypoint {index}",
                    editState.WaypointLocalPositions[index]);

                if (EditorGUI.EndChangeCheck())
                {
                    if (settings != null && settings.SnapToGrid)
                    {
                        waypointPosition = BeltPathEditorUtility.SnapLocalPosition(
                            waypointPosition,
                            settings.GridCellSize);
                    }

                    editState.SetWaypointPosition(index, waypointPosition);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Up") && index > 0)
                    {
                        editState.SwapWaypoints(index, index - 1);
                    }

                    if (GUILayout.Button("Down") && index < editState.WaypointLocalPositions.Count - 1)
                    {
                        editState.SwapWaypoints(index, index + 1);
                    }

                    if (GUILayout.Button("Delete"))
                    {
                        RemoveSelectedWaypoint();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a waypoint or Box Queue on the canvas.", MessageType.None);
            }

            if (settings != null)
            {
                EditorGUILayout.LabelField("Block Capacity", editState.GetBlockCapacity(settings).ToString());
            }
        }

        private void DrawPresetPanel()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            presetHalfWidth = EditorGUILayout.FloatField("Half Width", presetHalfWidth);
            presetHalfDepth = EditorGUILayout.FloatField("Half Depth", presetHalfDepth);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rectangle"))
                {
                    ApplyPreset(MapPathPresets.CreateRectangleLoop(presetHalfWidth, presetHalfDepth), true);
                }

                if (GUILayout.Button("U Shape"))
                {
                    ApplyPreset(MapPathPresets.CreateUShape(presetHalfWidth, presetHalfDepth), false);
                }

                if (GUILayout.Button("Line"))
                {
                    ApplyPreset(MapPathPresets.CreateStraightLine(presetHalfDepth * 2f, 5), false);
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

            beltPath = (BeltPath)EditorGUILayout.ObjectField("Belt Path", beltPath, typeof(BeltPath), true);
            boxQueueParent = (Transform)EditorGUILayout.ObjectField("Box Queue", boxQueueParent, typeof(Transform), true);

            using (new EditorGUI.DisabledScope(beltPath == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Apply To Scene"))
                    {
                        ApplyEditStateToScene();
                    }

                    if (GUILayout.Button("Import From Scene"))
                    {
                        editState.LoadFromScene(beltPath, boxQueueParent);
                        ShowNotification(new GUIContent("Imported from scene"));
                    }
                }
            }
        }

        private void AddWaypointAfterSelection()
        {
            float step = settings != null ? settings.GridCellSize : 0.4f;
            Vector3 localPosition = Vector3.zero;

            if (editState.SelectedWaypointIndex >= 0
                && editState.SelectedWaypointIndex < editState.WaypointLocalPositions.Count)
            {
                localPosition = editState.WaypointLocalPositions[editState.SelectedWaypointIndex] + Vector3.forward * step;
            }
            else if (editState.WaypointLocalPositions.Count > 0)
            {
                localPosition = editState.WaypointLocalPositions[^1] + Vector3.forward * step;
            }

            if (settings != null && settings.SnapToGrid)
            {
                localPosition = BeltPathEditorUtility.SnapLocalPosition(localPosition, settings.GridCellSize);
            }

            editState.AddWaypoint(localPosition);
        }

        private void RemoveSelectedWaypoint()
        {
            if (editState.SelectedWaypointIndex < 0)
            {
                return;
            }

            editState.RemoveWaypoint(editState.SelectedWaypointIndex);
        }

        private void ApplyPreset(Vector3[] localPositions, bool isClosedLoop)
        {
            editState.ApplyPreset(localPositions, isClosedLoop);
        }

        private void ReloadEditStateFromAsset()
        {
            if (layoutAsset == null)
            {
                if (trackedLayoutAsset != null)
                {
                    editState.Clear();
                }

                trackedLayoutAsset = null;
                isBoxQueueSelected = false;
                Repaint();
                return;
            }

            editState.LoadFrom(layoutAsset);
            trackedLayoutAsset = layoutAsset;
            isBoxQueueSelected = false;
            Repaint();
        }

        private void SaveLayoutToAsset()
        {
            if (layoutAsset == null)
            {
                CreateLayoutAssetQuick();
                return;
            }

            editState.WriteTo(layoutAsset);
            EditorUtility.SetDirty(layoutAsset);
            AssetDatabase.SaveAssets();
            SyncMapLayoutBinder(layoutAsset);
            ShowNotification(new GUIContent($"Saved: {layoutAsset.name}"));
        }

        private void ApplyEditStateToScene()
        {
            if (beltPath == null)
            {
                return;
            }

            Undo.RecordObject(beltPath, "Apply Map Layout To Scene");
            BeltWaypointMarker waypointPrefab = LoadWaypointPrefab();
            beltPath.ApplyLocalWaypoints(editState.WaypointLocalPositions, editState.IsClosedLoop, waypointPrefab);

            if (boxQueueParent != null)
            {
                Undo.RecordObject(boxQueueParent, "Apply Box Queue Position");
                boxQueueParent.localPosition = editState.BoxQueueLocalPosition;
                EditorUtility.SetDirty(boxQueueParent);
            }

            if (layoutAsset != null)
            {
                editState.WriteTo(layoutAsset);
                EditorUtility.SetDirty(layoutAsset);
            }

            EditorUtility.SetDirty(beltPath);
            MarkSceneDirty();
            ShowNotification(new GUIContent("Applied to scene"));
        }

        private void CreateLayoutAssetQuick()
        {
            EnsureFolder(DefaultLayoutFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{DefaultLayoutFolder}/MapLayout.asset");
            LevelMapLayout layout = ScriptableObject.CreateInstance<LevelMapLayout>();
            editState.WriteTo(layout);

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
            if (layoutAsset != null || beltPath == null)
            {
                return;
            }

            MapLayoutBinder binder = beltPath.GetComponentInParent<MapLayoutBinder>();

            if (binder == null)
            {
                return;
            }

            SerializedObject serializedBinder = new SerializedObject(binder);
            layoutAsset = serializedBinder.FindProperty("mapLayout").objectReferenceValue as LevelMapLayout;
        }

        private void SyncMapLayoutBinder(LevelMapLayout layout)
        {
            if (beltPath == null || layout == null)
            {
                return;
            }

            MapLayoutBinder binder = beltPath.GetComponentInParent<MapLayoutBinder>();

            if (binder == null)
            {
                return;
            }

            SerializedObject serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("mapLayout").objectReferenceValue = layout;
            serializedBinder.ApplyModifiedProperties();
            EditorUtility.SetDirty(binder);
        }

        private void MarkSceneDirty()
        {
            if (beltPath == null || Application.isPlaying)
            {
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(beltPath.gameObject.scene);
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
