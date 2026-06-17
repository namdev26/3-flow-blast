using System.Collections.Generic;
using FlowBlast.Domain;

namespace FlowBlast.Services.Box
{
    public sealed class BoxRegistryService
    {
        private readonly List<BoxModel> allBoxes = new List<BoxModel>();

        public void Register(BoxModel box)
        {
            if (!allBoxes.Contains(box))
            {
                allBoxes.Add(box);
            }
        }

        public IReadOnlyList<BoxModel> GetAllBoxes()
        {
            return allBoxes;
        }

        public void Clear()
        {
            allBoxes.Clear();
        }
    }
}
