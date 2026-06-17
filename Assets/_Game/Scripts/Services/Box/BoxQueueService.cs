using System.Collections.Generic;
using FlowBlast.Domain;

namespace FlowBlast.Services.Box
{
    public sealed class BoxQueueService
    {
        private readonly Queue<BoxModel> waitingBoxes = new Queue<BoxModel>();

        public int Count => waitingBoxes.Count;

        public void Enqueue(BoxModel box)
        {
            waitingBoxes.Enqueue(box);
        }

        public BoxModel PeekFrontBox()
        {
            if (waitingBoxes.Count == 0)
            {
                return null;
            }

            return waitingBoxes.Peek();
        }

        public BoxModel DequeueFrontBox()
        {
            if (waitingBoxes.Count == 0)
            {
                return null;
            }

            return waitingBoxes.Dequeue();
        }

        public bool HasWaitingBoxes()
        {
            return waitingBoxes.Count > 0;
        }
    }
}
