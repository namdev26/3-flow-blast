using System;
using FlowBlast.Presentation.Block;
using FlowBlast.Presentation.Box;

namespace FlowBlast.Presentation
{
    public sealed class BlockCollectPresentationService
    {
        private readonly BoxPresentationCoordinator boxPresentationCoordinator;

        public BlockCollectPresentationService(BoxPresentationCoordinator boxPresentationCoordinator)
        {
            this.boxPresentationCoordinator = boxPresentationCoordinator;
        }

        public void PlayCollect(BlockView blockView, int boxId, Action onComplete)
        {
            if (blockView == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!boxPresentationCoordinator.TryGetView(boxId, out BoxView boxView))
            {
                onComplete?.Invoke();
                return;
            }

            blockView.BeginCollectFly(() => boxView.transform.position, onComplete);
        }
    }
}
