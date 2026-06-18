using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Factory;
using FlowBlast.Presentation.Box;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Box;
using UnityEngine;

namespace FlowBlast.Services.Board
{
    public sealed class BoardBoxSpawnService
    {
        private readonly BoxFactory boxFactory;
        private readonly Transform boardRoot;

        public BoardBoxSpawnService(BoxFactory boxFactory, Transform boardRoot)
        {
            this.boxFactory = boxFactory;
            this.boardRoot = boardRoot;
        }

        public void SpawnTestBoxes(
            int count,
            BoxRegistryService boxRegistryService,
            BlockColorPalette colorPalette,
            IBeltPath beltPath,
            Transform beltParent,
            float cellSpacing)
        {
            if (boardRoot == null || count <= 0)
            {
                return;
            }

            float safeSpacing = Mathf.Max(0.01f, cellSpacing);
            int spawnCount = Mathf.Min(count, BoardBoxLayout.TestBoxCount);

            for (int i = 0; i < spawnCount; i++)
            {
                BoxDefinition definition = CreateTestDefinition(i);
                BoxModel model = boxFactory.CreateModel(definition);
                Vector3 localPosition = BoardBoxLayout.GetLocalSpawnPosition(
                    i,
                    BoardBoxLayout.TestBoxColumns,
                    safeSpacing);

                BoxView view = boxFactory.CreateView(model, boardRoot, localPosition);
                view.Configure(beltPath, beltParent, colorPalette);
                view.RefreshPresentation();

                boxRegistryService.Register(model);
            }
        }

        private static BoxDefinition CreateTestDefinition(int index)
        {
            return new BoxDefinition
            {
                Color = BoardBoxLayout.GetTestBoxColor(index),
                Capacity = BoardBoxLayout.TestBoxCapacity,
                IsHidden = false,
                FrozenClearsRequired = 0
            };
        }
    }
}
