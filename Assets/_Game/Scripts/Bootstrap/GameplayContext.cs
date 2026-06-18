using FlowBlast.Core.Events;
using FlowBlast.Patterns.Factory;
using FlowBlast.Presentation;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Box;
using FlowBlast.Services.Level;

namespace FlowBlast.Bootstrap
{
    public sealed class GameplayContext
    {
        public IGameEventBus EventBus { get; }
        public LevelController LevelController { get; }
        public BeltFollowerRegistry FollowerRegistry { get; }
        public BoxPresentationCoordinator PresentationCoordinator { get; }
        public BoxFactory BoxFactory { get; }
        public BoxRegistryService BoxRegistryService { get; }

        public GameplayContext(
            IGameEventBus eventBus,
            LevelController levelController,
            BeltFollowerRegistry followerRegistry,
            BoxPresentationCoordinator presentationCoordinator,
            BoxFactory boxFactory,
            BoxRegistryService boxRegistryService)
        {
            EventBus = eventBus;
            LevelController = levelController;
            FollowerRegistry = followerRegistry;
            PresentationCoordinator = presentationCoordinator;
            BoxFactory = boxFactory;
            BoxRegistryService = boxRegistryService;
        }
    }
}
