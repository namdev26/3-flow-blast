using FlowBlast.Domain;

namespace FlowBlast.Services.Belt
{
    public sealed class BoxConveyorSlotService
    {
        private readonly int maxSlots;
        private readonly BoxModel[] slots;

        public BoxConveyorSlotService(int maxSlots)
        {
            this.maxSlots = maxSlots;
            slots = new BoxModel[maxSlots];
        }

        public int MaxSlots => maxSlots;

        public bool HasAvailableSlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (slots[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAcquireSlot(BoxModel box, out int slotIndex)
        {
            slotIndex = -1;

            if (box == null)
            {
                return false;
            }

            for (int i = 0; i < maxSlots; i++)
            {
                if (slots[i] != null)
                {
                    continue;
                }

                slots[i] = box;
                slotIndex = i;
                return true;
            }

            return false;
        }

        public void ReleaseSlot(BoxModel box)
        {
            if (box == null)
            {
                return;
            }

            for (int i = 0; i < maxSlots; i++)
            {
                if (slots[i] != box)
                {
                    continue;
                }

                slots[i] = null;
                return;
            }
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < maxSlots; i++)
                {
                    if (slots[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public BoxModel GetBoxAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                return null;
            }

            return slots[slotIndex];
        }
    }
}
