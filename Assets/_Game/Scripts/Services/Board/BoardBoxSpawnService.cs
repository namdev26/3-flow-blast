using System.Collections.Generic;
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

        public void SpawnLevelBoxes(
            IReadOnlyList<LevelBoxPlacement> boxPlacements,
            BoxRegistryService boxRegistryService,
            BlockColorPalette colorPalette,
            IBeltPath beltPath,
            Transform beltParent)
        {
            if (boardRoot == null || boxPlacements == null || boxPlacements.Count == 0)
            {
                return;
            }

            for (int i = 0; i < boxPlacements.Count; i++)
            {
                LevelBoxPlacement boxPlacement = boxPlacements[i];
                BoxModel model = boxFactory.CreateModel(boxPlacement.CreateDefinition());
                BoxView view = boxFactory.CreateView(model, boardRoot, boxPlacement.LocalPosition);
                view.Configure(beltPath, beltParent, colorPalette);
                view.RefreshPresentation();
                boxRegistryService.Register(model);
            }
        }
    }
}
