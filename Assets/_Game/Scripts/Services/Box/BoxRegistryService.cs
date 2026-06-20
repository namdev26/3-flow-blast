using System.Collections.Generic;
using FlowBlast.Domain;
using FlowBlast.Services.Board;

namespace FlowBlast.Services.Box
{
    public sealed class BoxRegistryService
    {
        private readonly List<BoxModel> allBoxes = new List<BoxModel>();
        private readonly BoardBoxAccessibilityService boardBoxAccessibilityService;

        public BoxRegistryService(BoardBoxAccessibilityService boardBoxAccessibilityService)
        {
            this.boardBoxAccessibilityService = boardBoxAccessibilityService;
        }

        public void Register(BoxModel box)
        {
            if (!allBoxes.Contains(box))
            {
                allBoxes.Add(box);
            }
        }

        public BoardBoxAccessibilityService BoardBoxAccessibilityService => boardBoxAccessibilityService;

        public IReadOnlyList<BoxModel> GetAllBoxes()
        {
            return allBoxes;
        }

        public bool CanSelect(BoxModel box)
        {
            return boardBoxAccessibilityService == null || boardBoxAccessibilityService.CanSelect(box);
        }

        public void MarkSentToConveyor(BoxModel box)
        {
            boardBoxAccessibilityService?.Remove(box);
        }

        public void Clear()
        {
            allBoxes.Clear();
            boardBoxAccessibilityService?.Clear();
        }
    }
}
