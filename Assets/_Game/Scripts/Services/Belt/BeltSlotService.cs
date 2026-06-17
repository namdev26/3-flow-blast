using System.Collections.Generic;
using FlowBlast.Domain;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltSlotService
    {
        private readonly int maxSlots;
        private readonly List<BoxModel> activeBoxes = new List<BoxModel>();

        public BeltSlotService(int maxSlots)
        {
            this.maxSlots = maxSlots;
        }

        public bool HasAvailableSlot()
        {
            return activeBoxes.Count < maxSlots;
        }

        public void OccupySlot(BoxModel box)
        {
            if (!HasAvailableSlot())
            {
                return;
            }

            activeBoxes.Add(box);
        }

        public void ReleaseSlot(BoxModel box)
        {
            activeBoxes.Remove(box);
        }

        public IReadOnlyList<BoxModel> GetActiveBoxes()
        {
            return activeBoxes;
        }
    }
}
