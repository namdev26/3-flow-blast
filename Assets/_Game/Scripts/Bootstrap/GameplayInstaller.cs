using FlowBlast.Core.Constants;
using FlowBlast.Core.Events;
using FlowBlast.Data;
using FlowBlast.Patterns.Command;
using FlowBlast.Patterns.Factory;
using FlowBlast.Patterns.Pool;
using FlowBlast.Patterns.Strategy;
using FlowBlast.Presentation;
using FlowBlast.Presentation.Block;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Block;
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
        [SerializeField] private Transform blockPoolParent;
        [SerializeField] private Transform boxQueueParent;
        [SerializeField] private Transform boxBeltParent;
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
            presentationCoordinator.Initialize(context.EventBus, context.FollowerRegistry);
        }

        private void Start()
        {
            if (context == null || levelData == null)
            {
                return;
            }

            context.LevelController.StartLevel(levelData);
            RegisterCreatedBoxViews();
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
                beltPath = GetComponentInChildren<BeltPath>();
            }

            if (blockPoolParent == null)
            {
                blockPoolParent = transform.Find("BlockPoolParent");
            }

            if (boxQueueParent == null)
            {
                boxQueueParent = transform.Find("BoxQueueParent");
            }

            if (boxBeltParent == null)
            {
                boxBeltParent = transform.Find("BoxBeltParent");
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
                Debug.LogError("[FlowBlast] Missing BeltPath in scene.");
                return false;
            }

            if (blockPoolParent == null || boxQueueParent == null || boxBeltParent == null)
            {
                Debug.LogError("[FlowBlast] Missing scene parents (BlockPoolParent / BoxQueueParent / BoxBeltParent).");
                return false;
            }

            if (presentationCoordinator == null || gameplayLoop == null || tapInputController == null)
            {
                Debug.LogError("[FlowBlast] Missing runtime components on Gameplay object.");
                return false;
            }

            beltPath.EnsureInitialized();
            return true;
        }

        private GameplayContext BuildContext()
        {
            IGameEventBus eventBus = new GameEventBus();
            BeltFollowerRegistry followerRegistry = new BeltFollowerRegistry();

            BlockViewPool blockPool = new BlockViewPool(blockPrefab, blockPoolParent);
            blockPool.Prewarm(GameConstants.DefaultPoolPrewarmCount);

            BlockFactory blockFactory = new BlockFactory(blockPool, colorPalette, beltPath);
            BoxFactory boxFactory = new BoxFactory(boxPrefab, boxQueueParent, boxBeltParent);

            int maxSlots = levelData != null ? levelData.MaxBeltSlots : GameConstants.DefaultMaxBeltSlots;
            int maxBacklog = levelData != null ? levelData.MaxBacklogBlocks : GameConstants.DefaultMaxBacklogBlocks;
            float beltSpeed = levelData != null ? levelData.BeltSpeed : GameConstants.DefaultBeltSpeed;

            BeltSlotService beltSlotService = new BeltSlotService(maxSlots);
            BoxQueueService boxQueueService = new BoxQueueService();
            BoxRegistryService boxRegistryService = new BoxRegistryService();
            BeltMovementService beltMovementService = new BeltMovementService(beltPath, followerRegistry);
            beltMovementService.SetSpeed(beltSpeed);

            FrozenUnlockStrategy frozenUnlockStrategy = new FrozenUnlockStrategy(eventBus);
            BoxCollectionService boxCollectionService = new BoxCollectionService(
                beltSlotService,
                beltMovementService,
                eventBus);

            BoxBlastService boxBlastService = new BoxBlastService(
                beltSlotService,
                boxRegistryService,
                frozenUnlockStrategy,
                eventBus);

            BlockSpawnService blockSpawnService = new BlockSpawnService(
                blockFactory,
                beltPath,
                followerRegistry,
                boxCollectionService,
                boxBlastService,
                beltSlotService);
            blockSpawnService.ConfigureBlockSpacing(BlockBeltLayout.CalculateSpacing(blockPrefab));
            blockSpawnService.ConfigureBeltLanes(
                levelData != null ? levelData.BeltLaneCount : GameConstants.BeltLaneCount);

            WinConditionEvaluator winEvaluator = new WinConditionEvaluator(
                boxQueueService,
                blockSpawnService,
                beltSlotService);
            LoseConditionEvaluator loseEvaluator = new LoseConditionEvaluator(blockSpawnService, maxBacklog);
            LevelRepository levelRepository = new LevelRepository(levelData);

            SendBoxToBeltCommand sendBoxCommand = new SendBoxToBeltCommand(
                boxQueueService,
                beltSlotService,
                eventBus);

            LevelController levelController = new LevelController(
                eventBus,
                levelRepository,
                boxFactory,
                boxQueueService,
                boxRegistryService,
                beltMovementService,
                blockSpawnService,
                winEvaluator,
                loseEvaluator,
                sendBoxCommand);

            return new GameplayContext(
                eventBus,
                levelController,
                sendBoxCommand,
                followerRegistry,
                presentationCoordinator);
        }

        private void RegisterCreatedBoxViews()
        {
            BoxView[] views = boxQueueParent.GetComponentsInChildren<BoxView>(true);

            for (int i = 0; i < views.Length; i++)
            {
                views[i].Configure(beltPath, boxBeltParent, colorPalette);
                presentationCoordinator.RegisterView(views[i]);
            }
        }
    }
}
