using FlowBlast.Core.Events;
using FlowBlast.Patterns.Command;
using FlowBlast.Presentation;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Level;

namespace FlowBlast.Bootstrap
{
    public sealed class GameplayContext
    {
        public IGameEventBus EventBus { get; }
        public LevelController LevelController { get; }
        public SendBoxToBeltCommand SendBoxToBeltCommand { get; }
        public BeltFollowerRegistry FollowerRegistry { get; }
        public BoxPresentationCoordinator PresentationCoordinator { get; }

        public GameplayContext(
            IGameEventBus eventBus,
            LevelController levelController,
            SendBoxToBeltCommand sendBoxToBeltCommand,
            BeltFollowerRegistry followerRegistry,
            BoxPresentationCoordinator presentationCoordinator)
        {
            EventBus = eventBus;
            LevelController = levelController;
            SendBoxToBeltCommand = sendBoxToBeltCommand;
            FollowerRegistry = followerRegistry;
            PresentationCoordinator = presentationCoordinator;
        }
    }
}
