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

        [Header("Data")]
        [SerializeField] private LevelData levelData;
        [SerializeField] private BlockColorPalette colorPalette;

        [Header("Prefabs")]
        [SerializeField] private BlockView blockPrefab;
        [SerializeField] private BoxView boxPrefab;

        [Header("Scene References")]
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

        public GameplayContext Context => context;

        private void Awake()
        {
            ResolveMissingReferences();

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
            boardRoot = ResolveSceneParent(boardRoot, GameplayZoneNames.BoardRoot);

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
            blockPool.Prewarm(GameConstants.DefaultPoolPrewarmCount);

            BlockFactory blockFactory = new BlockFactory(blockPool, colorPalette, beltPath);
            BoxFactory boxFactory = new BoxFactory(boxPrefab, boxBeltParent);

            int maxSlots = levelData != null ? levelData.MaxBeltSlots : GameConstants.DefaultMaxBeltSlots;
            int maxBacklog = levelData != null ? levelData.MaxBacklogBlocks : GameConstants.DefaultMaxBacklogBlocks;
            float beltSpeed = levelData != null ? levelData.BeltSpeed : GameConstants.DefaultBeltSpeed;

            BeltSlotService beltSlotService = new BeltSlotService(maxSlots);
            BoxConveyorSlotService boxConveyorSlotService = new BoxConveyorSlotService(
                GameConstants.DefaultMaxBoxConveyorSlots);
            BoxRegistryService boxRegistryService = new BoxRegistryService();
            BeltMovementService beltMovementService = new BeltMovementService(
                beltPath,
                followerRegistry,
                collectionPointMarker);
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

            WinConditionEvaluator winEvaluator = new WinConditionEvaluator(
                blockSpawnService,
                beltSlotService,
                boxConveyorSlotService);
            LoseConditionEvaluator loseEvaluator = new LoseConditionEvaluator(blockSpawnService, maxBacklog);
            LevelRepository levelRepository = new LevelRepository(levelData);

            SendBoardBoxToConveyorCommand sendBoardBoxCommand = new SendBoardBoxToConveyorCommand(
                boxConveyorSlotService,
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

        private void SpawnBoardLevelBoxes()
        {
            if (!spawnBoardLevelBoxesOnPlay || boardRoot == null || context == null || levelData == null)
            {
                return;
            }

            BoardBoxSpawnService boardBoxSpawnService = new BoardBoxSpawnService(context.BoxFactory, boardRoot);
            boardBoxSpawnService.SpawnLevelBoxes(
                levelData.BoxPlacements,
                context.BoxRegistryService,
                colorPalette,
                beltPath,
                boxBeltParent);
        }

        private void RegisterCreatedBoxViews()
        {
            RegisterBoxViewsUnder(boardRoot);
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
                views[i].Configure(beltPath, boxBeltParent, colorPalette);
                views[i].ConfigureBoxConveyor(boxConveyorPath, boxBeltParent);

                if (views[i].Model != null)
                {
                    presentationCoordinator.RegisterView(views[i]);
                }
            }
        }

    }
}
