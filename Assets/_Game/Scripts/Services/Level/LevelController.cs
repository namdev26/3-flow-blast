using FlowBlast.Core.Enums;
using FlowBlast.Core.Events;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Command;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Block;
using FlowBlast.Services.Box;
using FlowBlast.Services.Belt;

namespace FlowBlast.Services.Level
{
    public sealed class LevelController
    {
        private readonly IGameEventBus eventBus;
        private readonly ILevelRepository levelRepository;
        private readonly BoxRegistryService boxRegistryService;
        private readonly BeltMovementService beltMovementService;
        private readonly BoxConveyorMovementService boxConveyorMovementService;
        private readonly BlockSpawnService blockSpawnService;
        private readonly WinConditionEvaluator winConditionEvaluator;
        private readonly LoseConditionEvaluator loseConditionEvaluator;
        private readonly SendBoardBoxToConveyorCommand sendBoardBoxToConveyorCommand;

        private LevelData currentLevel;
        private GamePhase currentPhase = GamePhase.Idle;
        private int requiredBoxCount;
        private int completedBoxCount;

        public LevelController(
            IGameEventBus eventBus,
            ILevelRepository levelRepository,
            BoxRegistryService boxRegistryService,
            BeltMovementService beltMovementService,
            BoxConveyorMovementService boxConveyorMovementService,
            BlockSpawnService blockSpawnService,
            WinConditionEvaluator winConditionEvaluator,
            LoseConditionEvaluator loseConditionEvaluator,
            SendBoardBoxToConveyorCommand sendBoardBoxToConveyorCommand)
        {
            this.eventBus = eventBus;
            this.levelRepository = levelRepository;
            this.boxRegistryService = boxRegistryService;
            this.beltMovementService = beltMovementService;
            this.boxConveyorMovementService = boxConveyorMovementService;
            this.blockSpawnService = blockSpawnService;
            this.winConditionEvaluator = winConditionEvaluator;
            this.loseConditionEvaluator = loseConditionEvaluator;
            this.sendBoardBoxToConveyorCommand = sendBoardBoxToConveyorCommand;

            eventBus.Subscribe<BoxBlastedEvent>(OnBoxBlasted);
        }

        public GamePhase CurrentPhase => currentPhase;

        public void StartLevel(string levelId)
        {
            LevelData levelData = levelRepository.GetLevel(levelId);

            if (levelData == null)
            {
                return;
            }

            StartLevel(levelData);
        }

        public void StartLevel(LevelData levelData)
        {
            currentLevel = levelData;
            currentPhase = GamePhase.Playing;
            completedBoxCount = 0;
            requiredBoxCount = boxRegistryService.GetAllBoxes().Count;

            beltMovementService.SetSpeed(levelData.BeltSpeed);
            boxConveyorMovementService.SetSpeed(levelData.BeltSpeed);
            boxConveyorMovementService.Reset();
            blockSpawnService.LoadSequence(levelData.BlockSequence);
        }

        public void Tick(float deltaTime)
        {
            beltMovementService.Tick(deltaTime);
            boxConveyorMovementService.Tick(deltaTime);

            if (currentPhase != GamePhase.Playing)
            {
                return;
            }

            blockSpawnService.TickCollection();
            EvaluateEndConditions();
        }

        public bool TrySendBoardBoxToConveyor(BoxModel box)
        {
            if (currentPhase != GamePhase.Playing)
            {
                return false;
            }

            return sendBoardBoxToConveyorCommand.Execute(box);
        }

        private void OnBoxBlasted(BoxBlastedEvent gameEvent)
        {
            completedBoxCount++;
        }

        private void EvaluateEndConditions()
        {
            if (loseConditionEvaluator.IsLose(out string loseReason))
            {
                currentPhase = GamePhase.Lost;
                eventBus.Publish(new LevelLostEvent(currentLevel.LevelId, loseReason));
                return;
            }

            if (winConditionEvaluator.IsWin(completedBoxCount, requiredBoxCount))
            {
                currentPhase = GamePhase.Won;
                eventBus.Publish(new LevelWonEvent(currentLevel.LevelId));
            }
        }
    }
}
