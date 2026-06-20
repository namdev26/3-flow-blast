using System;
using FlowBlast.Core.Enums;
using FlowBlast.Core.Events;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Command;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Block;
using FlowBlast.Services.Box;
using FlowBlast.Services.Belt;
using UnityEngine;

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

        public event Action<int> OnReviveCountdownChanged;
        public event Action<int> OnRemainingTimeChanged;
        public event Action OnReviveExpired;

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
        public float RemainingTimeSeconds => loseConditionEvaluator.RemainingTimeSeconds;
        public float ReviveCountdownRemainingTimeSeconds => loseConditionEvaluator.ReviveCountdownRemainingSeconds;

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
            blockSpawnService.LoadSequence(levelData.BlockSpawnRows);
            loseConditionEvaluator.StartLevel();
            NotifyRemainingTimeChanged();
        }

        public void Tick(float deltaTime)
        {
            beltMovementService.Tick(deltaTime);
            boxConveyorMovementService.Tick(deltaTime);

            if (currentPhase == GamePhase.Lost && loseConditionEvaluator.IsReviveOfferPending)
            {
                TickReviveCountdown(deltaTime);
                return;
            }

            if (currentPhase != GamePhase.Playing)
            {
                return;
            }

            loseConditionEvaluator.Tick(deltaTime);
            NotifyRemainingTimeChanged();
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

        public bool TryRevive()
        {
            if (currentPhase != GamePhase.Lost)
            {
                return false;
            }

            if (!loseConditionEvaluator.TryRevive())
            {
                return false;
            }

            currentPhase = GamePhase.Playing;
            OnReviveCountdownChanged?.Invoke(0);
            NotifyRemainingTimeChanged();
            return true;
        }

        public void DeclineRevive()
        {
            if (currentPhase != GamePhase.Lost)
            {
                return;
            }

            loseConditionEvaluator.DeclineRevive();
            OnReviveCountdownChanged?.Invoke(0);
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

                if (loseConditionEvaluator.TryConsumeReviveOffer(out int countdownSeconds))
                {
                    OnReviveCountdownChanged?.Invoke(countdownSeconds);
                    eventBus.Publish(new LevelLostEvent(currentLevel.LevelId, loseReason, true, countdownSeconds));
                    return;
                }

                PublishFinalLose();
                return;
            }

            if (winConditionEvaluator.IsWin(completedBoxCount, requiredBoxCount))
            {
                currentPhase = GamePhase.Won;
                eventBus.Publish(new LevelWonEvent(currentLevel.LevelId));
            }
        }

        private void PublishFinalLose()
        {
            OnReviveCountdownChanged?.Invoke(0);
            string loseReason = currentLevel != null ? "Don't give up! Try again!" : string.Empty;
            string levelId = currentLevel != null ? currentLevel.LevelId : string.Empty;
            eventBus.Publish(new LevelLostEvent(levelId, loseReason));
        }

        private void TickReviveCountdown(float deltaTime)
        {
            loseConditionEvaluator.Tick(deltaTime);

            if (loseConditionEvaluator.HasReviveCountdownExpired())
            {
                loseConditionEvaluator.DeclineRevive();
                OnReviveExpired?.Invoke();
                PublishFinalLose();
                return;
            }

            int remainingSeconds = Mathf.Max(1, Mathf.CeilToInt(loseConditionEvaluator.ReviveCountdownRemainingSeconds));
            OnReviveCountdownChanged?.Invoke(remainingSeconds);
        }

        private void NotifyRemainingTimeChanged()
        {
            int remainingSeconds = Mathf.CeilToInt(loseConditionEvaluator.RemainingTimeSeconds);
            OnRemainingTimeChanged?.Invoke(remainingSeconds);
        }
    }
}
