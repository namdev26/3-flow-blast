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
        private BeltFollowerRegistry followerRegistry;

        public void Initialize(IGameEventBus gameEventBus, BeltFollowerRegistry registry)
        {
            eventBus = gameEventBus;
            followerRegistry = registry;

            eventBus.Subscribe<BoxSentToBeltEvent>(OnBoxSentToBelt);
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
            followerRegistry.Register(view);
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

            followerRegistry.Unregister(view);
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
