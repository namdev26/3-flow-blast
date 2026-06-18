using System.Collections.Generic;
using FlowBlast.Core.Constants;
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
        private int maxBoxConveyorSlots;

        public void Initialize(
            IGameEventBus gameEventBus,
            BeltFollowerRegistry mainRegistry,
            BeltFollowerRegistry boxConveyorRegistry,
            IBeltPath conveyorPath,
            int maxConveyorSlots)
        {
            eventBus = gameEventBus;
            mainFollowerRegistry = mainRegistry;
            boxConveyorFollowerRegistry = boxConveyorRegistry;
            boxConveyorPath = conveyorPath;
            maxBoxConveyorSlots = maxConveyorSlots;

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

            view.MoveToBelt();
            mainFollowerRegistry.Register(view);
        }

        private void OnBoxSentToConveyor(BoxSentToConveyorEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            if (boxConveyorPath == null)
            {
                return;
            }

            float slotDistance = BoxConveyorLayout.GetSlotDistance(
                gameEvent.SlotIndex,
                boxConveyorPath.TotalLength,
                maxBoxConveyorSlots);

            Vector3 targetPosition = boxConveyorPath.GetPositionAtDistance(slotDistance);
            view.FlyToConveyorTarget(targetPosition, () => OnBoxArrivedAtConveyor(view, slotDistance));
        }

        private void OnBoxArrivedAtConveyor(BoxView view, float slotDistance)
        {
            if (view == null)
            {
                return;
            }

            view.ActivateOnBoxConveyor(slotDistance);
            boxConveyorFollowerRegistry.Register(view);
        }

        private void OnBlockCollected(BlockCollectedEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            view.RefreshPresentation();
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
        }

        private void OnBoxFrozenUnlocked(BoxFrozenUnlockedEvent gameEvent)
        {
            if (!viewsByBoxId.TryGetValue(gameEvent.BoxId, out BoxView view))
            {
                return;
            }

            view.RefreshPresentation();
        }
    }
}
