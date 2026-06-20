using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Events;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Command;
using FlowBlast.Patterns.Factory;
using FlowBlast.Patterns.Pool;
using FlowBlast.Patterns.Strategy;
using FlowBlast.Presentation;
using FlowBlast.Presentation.Block;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Block;
using FlowBlast.Services.Board;
using FlowBlast.Services.Box;
using FlowBlast.Services.Level;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlowBlast.Bootstrap
{
    public sealed class GameplayInstaller : MonoBehaviour
    {
        private const string DefaultLevelPath = "Assets/_Game/Data/Level_01.asset";
        private const string DefaultPalettePath = "Assets/_Game/Data/BlockColorPalette.asset";
        private const string DefaultBlockPrefabPath = "Assets/_Game/Prefabs/PixelBlock.prefab";
        private const string DefaultBoxPrefabPath = "Assets/_Game/Prefabs/Box.prefab";
        private const int ExpandedBoardColumnThreshold = 5;
        private const float ExpandedBoardRootScale = 1.4f;
        private const float DenseBoardRootScale = 1f;

        [Header("Data")]
        [SerializeField] private LevelData levelData;
        [SerializeField] private BlockColorPalette colorPalette;

        [Header("Prefabs")]
        [SerializeField] private BlockView blockPrefab;
        [SerializeField] private BoxView boxPrefab;

        [Header("Scene References")]
        [SerializeField] private MapLayoutBinder mapLayoutBinder;
        [SerializeField] private BeltPath beltPath;
        [SerializeField] private BeltPath boxConveyorPath;
        [SerializeField] private Transform collectionPointMarker;
        [SerializeField] private Transform blockPoolParent;
        [SerializeField] private Transform boxBeltParent;
        [Header("Board Level Preview")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private bool spawnBoardLevelBoxesOnPlay = true;
        [SerializeField] private BoxPresentationCoordinator presentationCoordinator;
        [SerializeField] private GameplayLoop gameplayLoop;
        [SerializeField] private TapInputController tapInputController;

        private GameplayContext context;
        private Vector3 initialBoardRootScale = Vector3.one;

        public GameplayContext Context => context;

        private void Awake()
        {
            ResolveMissingReferences();
            ApplyMapLayoutAtRuntime();

            if (!ValidateReferences())
            {
                return;
            }

            context = BuildContext();
            gameplayLoop.Initialize(context);
            tapInputController.Initialize(context);
        }

        private void Start()
        {
            if (context == null || levelData == null)
            {
                return;
            }

            SpawnBoardLevelBoxes();
            RegisterCreatedBoxViews();
            context.LevelController.StartLevel(levelData);
        }

        private void EnsureBoxConveyorPathReady()
        {
            if (boxConveyorPath == null || boxConveyorPath.Waypoints.Length >= 2)
            {
                return;
            }

            Transform[] boxWaypoints = new Transform[BoxConveyorLayout.DefaultOvalWaypointLocalPositions.Length];
            BeltWaypointMarker waypointPrefab = null;

#if UNITY_EDITOR
            GameObject waypointPrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/BeltWaypointMarker.prefab");
            waypointPrefab = waypointPrefabObject != null ? waypointPrefabObject.GetComponent<BeltWaypointMarker>() : null;
#endif

            for (int i = 0; i < BoxConveyorLayout.DefaultOvalWaypointLocalPositions.Length; i++)
            {
                GameObject waypointObject;

                if (waypointPrefab != null)
                {
                    waypointObject = Instantiate(waypointPrefab.gameObject, boxConveyorPath.transform);
                }
                else
                {
                    waypointObject = new GameObject($"BoxConveyorWaypoint_{i}");
                    waypointObject.transform.SetParent(boxConveyorPath.transform, false);
                    waypointObject.AddComponent<BeltWaypointMarker>();
                }

                waypointObject.transform.localPosition = BoxConveyorLayout.DefaultOvalWaypointLocalPositions[i];

                BeltWaypointMarker marker = waypointObject.GetComponent<BeltWaypointMarker>();

                if (marker != null)
                {
                    marker.SetWaypointIndex(i);
                    marker.SetPathRole(boxConveyorPath.PathRole);
                }

                boxWaypoints[i] = waypointObject.transform;
            }

            SerializedObject serializedPath = new SerializedObject(boxConveyorPath);
            SerializedProperty waypointProperty = serializedPath.FindProperty("waypoints");
            waypointProperty.arraySize = boxWaypoints.Length;

            for (int i = 0; i < boxWaypoints.Length; i++)
            {
                waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue = boxWaypoints[i];
            }

            serializedPath.ApplyModifiedPropertiesWithoutUndo();
            boxConveyorPath.EnsureInitialized();
        }

        private void ApplyMapLayoutAtRuntime()
        {
            if (mapLayoutBinder == null)
            {
                return;
            }

            if (levelData != null && levelData.MapLayout != null)
            {
                mapLayoutBinder.SetMapLayout(levelData.MapLayout);
            }
            else
            {
                mapLayoutBinder.ApplyLayout();
            }

            EnsureBoxConveyorPathReady();
        }

        private void ResolveMissingReferences()
        {
#if UNITY_EDITOR
            if (levelData == null)
            {
                levelData = AssetDatabase.LoadAssetAtPath<LevelData>(DefaultLevelPath);
            }

            if (colorPalette == null)
            {
                colorPalette = AssetDatabase.LoadAssetAtPath<BlockColorPalette>(DefaultPalettePath);
            }

            if (blockPrefab == null)
            {
                blockPrefab = AssetDatabase.LoadAssetAtPath<BlockView>(DefaultBlockPrefabPath);
            }

            if (boxPrefab == null)
            {
                boxPrefab = AssetDatabase.LoadAssetAtPath<BoxView>(DefaultBoxPrefabPath);
            }
#endif

            if (mapLayoutBinder == null)
            {
                mapLayoutBinder = GetComponent<MapLayoutBinder>();
            }

            if (beltPath == null)
            {
                Transform mainConveyorRoot = TransformHierarchyUtility.FindChildRecursive(
                    transform,
                    GameplayZoneNames.ConveyorRoot);

                if (mainConveyorRoot != null)
                {
                    beltPath = mainConveyorRoot.GetComponent<BeltPath>();
                }
            }

            if (boxConveyorPath == null)
            {
                Transform boxConveyorPathRoot = TransformHierarchyUtility.FindChildRecursive(
                    transform,
                    GameplayZoneNames.BoxConveyorPath);

                if (boxConveyorPathRoot != null)
                {
                    boxConveyorPath = boxConveyorPathRoot.GetComponent<BeltPath>();
                }
            }

            collectionPointMarker = ResolveSceneParent(collectionPointMarker, GameplayZoneNames.CollectionPointMarker);
            blockPoolParent = ResolveSceneParent(blockPoolParent, GameplayZoneNames.BlockPoolParent);
            boxBeltParent = ResolveSceneParent(boxBeltParent, GameplayZoneNames.BoxBeltParent);

            if (boardRoot != null)
            {
                initialBoardRootScale = boardRoot.localScale;
            }

            if (presentationCoordinator == null)
            {
                presentationCoordinator = GetComponent<BoxPresentationCoordinator>();
            }

            if (gameplayLoop == null)
            {
                gameplayLoop = GetComponent<GameplayLoop>();
            }

            if (tapInputController == null)
            {
                tapInputController = GetComponent<TapInputController>();
            }
        }

        private Transform ResolveSceneParent(Transform current, string childName)
        {
            if (current != null)
            {
                return current;
            }

            return TransformHierarchyUtility.FindChildRecursive(transform, childName);
        }

        private bool TryResolveCollectionPointWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (levelData?.MapLayout == null)
            {
                return false;
            }

            worldPosition = beltPath.transform.TransformPoint(levelData.MapLayout.CollectionPointLocalPosition);
            return true;
        }

        private bool ValidateReferences()
        {
            if (levelData == null)
            {
                Debug.LogError("[FlowBlast] Missing LevelData. Run FlowBlast/Create Sample Level Assets or assign Level_01.");
                return false;
            }

            if (colorPalette == null)
            {
                Debug.LogError("[FlowBlast] Missing BlockColorPalette.");
                return false;
            }

            if (blockPrefab == null)
            {
                Debug.LogError("[FlowBlast] Missing PixelBlock prefab.");
                return false;
            }

            if (boxPrefab == null)
            {
                Debug.LogError("[FlowBlast] Missing Box prefab.");
                return false;
            }

            if (beltPath == null)
            {
                Debug.LogError("[FlowBlast] Missing main BeltPath on ConveyorRoot.");
                return false;
            }

            if (boxConveyorPath == null)
            {
                Debug.LogError("[FlowBlast] Missing BoxConveyorPath in Zone_BoxConveyor.");
                return false;
            }

            if (blockPoolParent == null || boxBeltParent == null)
            {
                Debug.LogError("[FlowBlast] Missing scene parents (BlockPoolParent / BoxBeltParent).");
                return false;
            }

            if (boardRoot == null)
            {
                Debug.LogError("[FlowBlast] Missing BoardRoot reference. Assign the board root Transform in GameplayInstaller.");
                return false;
            }

            if (presentationCoordinator == null || gameplayLoop == null || tapInputController == null)
            {
                Debug.LogError("[FlowBlast] Missing runtime components on Gameplay object.");
                return false;
            }

            beltPath.EnsureInitialized();
            boxConveyorPath.EnsureInitialized();
            return true;
        }

        private GameplayContext BuildContext()
        {
            IGameEventBus eventBus = new GameEventBus();
            BeltFollowerRegistry followerRegistry = new BeltFollowerRegistry();
            BeltFollowerRegistry boxConveyorFollowerRegistry = new BeltFollowerRegistry();

            BlockViewPool blockPool = new BlockViewPool(blockPrefab, blockPoolParent);
            blockPool.Prewarm(ResolveInitialBlockPoolSize());

            BlockFactory blockFactory = new BlockFactory(blockPool, colorPalette, beltPath);
            BoxFactory boxFactory = new BoxFactory(boxPrefab, boxBeltParent);

            int maxSlots = levelData != null ? levelData.MaxBeltSlots : GameConstants.DefaultMaxBeltSlots;
            int maxBacklog = levelData != null ? levelData.MaxBacklogBlocks : GameConstants.DefaultMaxBacklogBlocks;
            float beltSpeed = levelData != null ? levelData.BeltSpeed : GameConstants.DefaultBeltSpeed;

            BeltSlotService beltSlotService = new BeltSlotService(maxSlots);
            BoxConveyorSlotService boxConveyorSlotService = new BoxConveyorSlotService(
                GameConstants.DefaultMaxBoxConveyorSlots);
            BoardBoxAccessibilityService boardBoxAccessibilityService = new BoardBoxAccessibilityService(
                levelData != null ? levelData.EditorGridCellSpacing : BoardBoxLayout.DefaultTestBoxCellSpacing);
            BoxRegistryService boxRegistryService = new BoxRegistryService(boardBoxAccessibilityService);
            bool hasCollectionPointOverride = TryResolveCollectionPointWorldPosition(out Vector3 collectionPointWorldPosition);
            BeltMovementService beltMovementService = new BeltMovementService(
                beltPath,
                followerRegistry,
                collectionPointWorldPosition,
                hasCollectionPointOverride);
            beltMovementService.SetSpeed(beltSpeed);
            BoxConveyorMovementService boxConveyorMovementService = new BoxConveyorMovementService(
                boxConveyorPath,
                boxConveyorFollowerRegistry,
                GameConstants.DefaultMaxBoxConveyorSlots);
            boxConveyorMovementService.SetSpeed(beltSpeed);

            FrozenUnlockStrategy frozenUnlockStrategy = new FrozenUnlockStrategy(eventBus);
            BoxCollectionService boxCollectionService = new BoxCollectionService(
                beltSlotService,
                boxConveyorSlotService,
                beltMovementService,
                boxConveyorMovementService,
                eventBus);

            BlockCollectPresentationService blockCollectPresentationService =
                new BlockCollectPresentationService(presentationCoordinator);

            BoxBlastService boxBlastService = new BoxBlastService(
                beltSlotService,
                boxConveyorSlotService,
                boxRegistryService,
                frozenUnlockStrategy,
                eventBus);

            BlockSpawnService blockSpawnService = new BlockSpawnService(
                blockFactory,
                beltPath,
                followerRegistry,
                boxCollectionService,
                blockCollectPresentationService,
                boxBlastService);
            blockSpawnService.ConfigureBlockSpacing(BlockBeltLayout.CalculateSpacing(blockPrefab));
            blockSpawnService.ConfigureBeltLanes(
                levelData != null ? levelData.BeltLaneCount : GameConstants.BeltLaneCount);
            blockSpawnService.ConfigureQueueMergeDistances(ResolveQueueMergeDistances());

            QueueBlockDisplayService queueBlockDisplayService = BuildQueueBlockDisplayService(blockFactory, blockPrefab, levelData);
            blockSpawnService.SetQueueDisplayService(queueBlockDisplayService);

            WinConditionEvaluator winEvaluator = new WinConditionEvaluator(
                blockSpawnService,
                beltSlotService,
                boxConveyorSlotService);
            LoseConditionEvaluator loseEvaluator = new LoseConditionEvaluator(blockSpawnService, maxBacklog);
            LevelRepository levelRepository = new LevelRepository(levelData);

            SendBoardBoxToConveyorCommand sendBoardBoxCommand = new SendBoardBoxToConveyorCommand(
                boxConveyorSlotService,
                boxRegistryService,
                eventBus);

            LevelController levelController = new LevelController(
                eventBus,
                levelRepository,
                boxRegistryService,
                beltMovementService,
                boxConveyorMovementService,
                blockSpawnService,
                winEvaluator,
                loseEvaluator,
                sendBoardBoxCommand);

            presentationCoordinator.Initialize(
                eventBus,
                followerRegistry,
                boxConveyorFollowerRegistry,
                boxConveyorPath,
                boxConveyorMovementService);

            return new GameplayContext(
                eventBus,
                levelController,
                followerRegistry,
                presentationCoordinator,
                boxFactory,
                boxRegistryService);
        }

        private QueueBlockDisplayService BuildQueueBlockDisplayService(
            BlockFactory blockFactory,
            BlockView blockPrefab,
            LevelData levelData)
        {
            List<IBeltPath> queuePaths = ResolveQueueBeltPaths();

            if (queuePaths.Count == 0)
            {
                return null;
            }

            float spacing = BlockBeltLayout.CalculateSpacing(blockPrefab);
            int lanes = levelData != null ? levelData.BeltLaneCount : GameConstants.BeltLaneCount;

            QueueBlockDisplayService service = new QueueBlockDisplayService(queuePaths, blockFactory);
            service.ConfigureLayout(spacing, lanes, spacing);
            return service;
        }

        private int ResolveInitialBlockPoolSize()
        {
            if (levelData == null || blockPrefab == null || beltPath == null)
            {
                return GameConstants.DefaultPoolPrewarmCount;
            }

            float spacing = BlockBeltLayout.CalculateSpacing(blockPrefab);
            int laneCount = Mathf.Max(1, levelData.BeltLaneCount);
            int sequenceBlockCount = levelData.TotalBlockCount;
            int mainCapacity = BeltLaneLayout.GetTotalBlockCapacity(beltPath.TotalLength, spacing, laneCount);
            int queueCapacity = GetQueueDisplayCapacity(spacing, laneCount);
            int recommendedPoolSize = Mathf.Max(sequenceBlockCount, mainCapacity + queueCapacity);
            return Mathf.Max(GameConstants.DefaultPoolPrewarmCount, recommendedPoolSize);
        }

        private int GetQueueDisplayCapacity(float rowSpacing, int laneCount)
        {
            if (rowSpacing <= Mathf.Epsilon || laneCount <= 0)
            {
                return 0;
            }

            List<IBeltPath> queuePaths = ResolveQueueBeltPaths();
            int totalCapacity = 0;

            for (int i = 0; i < queuePaths.Count; i++)
            {
                IBeltPath queuePath = queuePaths[i];

                if (queuePath == null)
                {
                    continue;
                }

                totalCapacity += BeltLaneLayout.GetTotalBlockCapacity(queuePath.TotalLength, rowSpacing, laneCount);
            }

            return totalCapacity;
        }

        private List<IBeltPath> ResolveQueueBeltPaths()
        {
            if (mapLayoutBinder != null && mapLayoutBinder.QueueBeltPaths.Count > 0)
            {
                List<IBeltPath> fromBinder = new List<IBeltPath>();

                for (int i = 0; i < mapLayoutBinder.QueueBeltPaths.Count; i++)
                {
                    if (mapLayoutBinder.QueueBeltPaths[i] != null)
                    {
                        fromBinder.Add(mapLayoutBinder.QueueBeltPaths[i]);
                    }
                }

                if (fromBinder.Count > 0)
                {
                    return fromBinder;
                }
            }

            List<IBeltPath> scenePaths = new List<IBeltPath>();
            BeltPath[] allPaths = GetComponentsInChildren<BeltPath>(true);

            for (int i = 0; i < allPaths.Length; i++)
            {
                BeltPath candidate = allPaths[i];

                if (candidate == null || candidate.PathRole != BeltPathRole.Queue)
                {
                    continue;
                }

                if (candidate == beltPath || candidate == boxConveyorPath)
                {
                    continue;
                }

                scenePaths.Add(candidate);
            }

            if (scenePaths.Count > 0)
            {
                return scenePaths;
            }

            return ResolveVirtualQueuePaths();
        }

        private List<float> ResolveQueueMergeDistances()
        {
            List<float> mergeDistances = new List<float>();

            if (levelData?.MapLayout == null || beltPath == null)
            {
                return mergeDistances;
            }

            IReadOnlyList<QueuePathLayout> queueLayouts = levelData.MapLayout.QueuePaths;

            for (int i = 0; i < queueLayouts.Count; i++)
            {
                QueuePathLayout queueLayout = queueLayouts[i];

                if (queueLayout == null)
                {
                    continue;
                }

                Vector3 worldEntryPosition = beltPath.transform.TransformPoint(queueLayout.QueueEntryLocalPosition);
                float mergeDistance = beltPath.GetClosestDistance(worldEntryPosition);
                mergeDistances.Add(mergeDistance);
            }

            return mergeDistances;
        }

        private List<IBeltPath> ResolveVirtualQueuePaths()
        {
            List<IBeltPath> virtualPaths = new List<IBeltPath>();

            if (levelData?.MapLayout == null || beltPath == null)
            {
                return virtualPaths;
            }

            IReadOnlyList<QueuePathLayout> queueLayouts = levelData.MapLayout.QueuePaths;

            for (int i = 0; i < queueLayouts.Count; i++)
            {
                QueuePathLayout queueLayout = queueLayouts[i];

                if (queueLayout == null || queueLayout.WaypointLocalPositions.Count < 2)
                {
                    continue;
                }

                VirtualBeltPath virtualPath = new VirtualBeltPath(
                    queueLayout.WaypointLocalPositions,
                    queueLayout.IsClosedLoop,
                    queueLayout.CurveStrength,
                    beltPath.transform);

                if (virtualPath.TotalLength > Mathf.Epsilon)
                {
                    virtualPaths.Add(virtualPath);
                }
            }

            return virtualPaths;
        }

        private void SpawnBoardLevelBoxes()
        {
            if (!spawnBoardLevelBoxesOnPlay || boardRoot == null || context == null || levelData == null)
            {
                return;
            }

            ApplyBoardRootScale();

            BoardBoxSpawnService boardBoxSpawnService = new BoardBoxSpawnService(
                context.BoxFactory,
                boardRoot,
                context.BoxRegistryService.BoardBoxAccessibilityService);
            boardBoxSpawnService.SpawnLevelBoxes(
                levelData.BoxPlacements,
                levelData.BoxCapacity,
                context.BoxRegistryService,
                colorPalette,
                beltPath,
                boxBeltParent);
        }

        private void RegisterCreatedBoxViews()
        {
            RegisterBoxViewsUnder(boardRoot);
        }

        private void ApplyBoardRootScale()
        {
            if (boardRoot == null)
            {
                return;
            }

            float targetScale = ResolveBoardRootScale(levelData != null ? levelData.BoxPlacements : null);
            boardRoot.localScale = initialBoardRootScale * targetScale;
        }

        private float ResolveBoardRootScale(IReadOnlyList<LevelBoxPlacement> boxPlacements)
        {
            int horizontalBoxCount = GetHorizontalBoxCount(boxPlacements);
            return horizontalBoxCount <= ExpandedBoardColumnThreshold
                ? ExpandedBoardRootScale
                : DenseBoardRootScale;
        }

        private static int GetHorizontalBoxCount(IReadOnlyList<LevelBoxPlacement> boxPlacements)
        {
            if (boxPlacements == null || boxPlacements.Count == 0)
            {
                return 0;
            }

            HashSet<int> occupiedColumns = new HashSet<int>();

            for (int i = 0; i < boxPlacements.Count; i++)
            {
                int columnKey = Mathf.RoundToInt(boxPlacements[i].LocalPosition.x * 1000f);
                occupiedColumns.Add(columnKey);
            }

            return occupiedColumns.Count;
        }

        private void RegisterBoxViewsUnder(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            BoxView[] views = parent.GetComponentsInChildren<BoxView>(true);

            for (int i = 0; i < views.Length; i++)
            {
                views[i].Configure(beltPath, boxBeltParent, colorPalette, context.BoxRegistryService);
                views[i].ConfigureBoxConveyor(boxConveyorPath, boxBeltParent);

                if (views[i].Model != null)
                {
                    presentationCoordinator.RegisterView(views[i]);
                }
            }
        }

    }
}
