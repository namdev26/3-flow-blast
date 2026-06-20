using System.Collections.Generic;
using FlowBlast.Core.Events;
using FlowBlast.Domain;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Presentation
{
    public sealed class BoxPresentationCoordinator : MonoBehaviour
    {
        private readonly Dictionary<int, BoxView> viewsByBoxId = new Dictionary<int, BoxView>();

        private IGameEventBus eventBus;
        private BeltFollowerRegistry mainFollowerRegistry;
        private BeltFollowerRegistry boxConveyorFollowerRegistry;
        private IBeltPath boxConveyorPath;
        private BoxConveyorMovementService boxConveyorMovementService;

        public void Initialize(
            IGameEventBus gameEventBus,
            BeltFollowerRegistry mainRegistry,
            BeltFollowerRegistry boxConveyorRegistry,
            IBeltPath conveyorPath,
            BoxConveyorMovementService conveyorMovementService)
        {
            eventBus = gameEventBus;
            mainFollowerRegistry = mainRegistry;
            boxConveyorFollowerRegistry = boxConveyorRegistry;
            boxConveyorPath = conveyorPath;
            boxConveyorMovementService = conveyorMovementService;

            eventBus.Subscribe<BoxSentToBeltEvent>(OnBoxSentToBelt);
            eventBus.Subscribe<BoxSentToConveyorEvent>(OnBoxSentToConveyor);
            eventBus.Subscribe<BlockCollectedEvent>(OnBlockCollected);
            eventBus.Subscribe<BoxBlastedEvent>(OnBoxBlasted);
            eventBus.Subscribe<BoxFrozenUnlockedEvent>(OnBoxFrozenUnlocked);
        }

        public void RegisterView(BoxView view)
        {
            if (view.Model == null)
            {
                return;
            }

            viewsByBoxId[view.Model.Id] = view;
            RefreshBoardAvailabilityOutlines();
        }

        public bool TryGetView(int boxId, out BoxView view)
        {
            return viewsByBoxId.TryGetValue(boxId, out view);
        }

        private void OnDestroy()
        {
            if (eventBus == null)
            {
                return;
            }

            eventBus.Unsubscribe<BoxSentToBeltEvent>(OnBoxSentToBelt);
            eventBus.Unsubscribe<BoxSentToConveyorEvent>(OnBoxSentToConveyor);
            eventBus.Unsubscribe<BlockCollectedEvent>(OnBlockCollected);
            eventBus.Unsubscribe<BoxBlastedEvent>(OnBoxBlasted);
            eventBus.Unsubscribe<BoxFrozenUnlockedEvent>(OnBoxFrozenUnlocked);
        }

        private void OnBoxSentToBelt(BoxSentToBeltEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            boxConveyorFollowerRegistry.Unregister(view);
            view.MoveToBelt();
            mainFollowerRegistry.Register(view);
            RefreshBoardAvailabilityOutlines();
        }

        private void OnBoxSentToConveyor(BoxSentToConveyorEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            if (boxConveyorPath == null || boxConveyorMovementService == null)
            {
                return;
            }

            mainFollowerRegistry.Unregister(view);
            RefreshBoardAvailabilityOutlines();
            int slotIndex = gameEvent.SlotIndex;
            view.FlyToConveyorTarget(
                () => boxConveyorPath.GetPositionAtDistance(
                    boxConveyorMovementService.GetSlotDistance(slotIndex)),
                () => OnBoxArrivedAtConveyor(view, slotIndex));
        }

        private void OnBoxArrivedAtConveyor(BoxView view, int slotIndex)
        {
            if (view == null)
            {
                return;
            }

            float slotDistance = boxConveyorMovementService.GetSlotDistance(slotIndex);
            view.BoardOnBoxConveyor(slotIndex, slotDistance);
            view.Model?.MarkReadyForConveyorCollection();
            boxConveyorFollowerRegistry.Register(view);
        }

        private void OnBlockCollected(BlockCollectedEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            view.RefreshPresentation();
            RefreshBoardAvailabilityOutlines();
        }

        private void OnBoxBlasted(BoxBlastedEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            if (view.IsActiveOnBoxConveyor)
            {
                boxConveyorFollowerRegistry.Unregister(view);
            }
            else
            {
                mainFollowerRegistry.Unregister(view);
            }

            view.HideCompleted();
            RefreshBoardAvailabilityOutlines();
        }

        private void OnBoxFrozenUnlocked(BoxFrozenUnlockedEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            view.RefreshPresentation();
            RefreshBoardAvailabilityOutlines();
        }

        private void RefreshBoardAvailabilityOutlines()
        {
            foreach (KeyValuePair<int, BoxView> pair in viewsByBoxId)
            {
                BoxView view = pair.Value;

                if (view == null)
                {
                    continue;
                }

                view.UpdateBoardAvailabilityOutline(view.CanReceiveBoardClick);
            }
        }
    }
}
