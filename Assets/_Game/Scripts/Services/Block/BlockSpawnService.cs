using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Factory;
using FlowBlast.Presentation.Block;
using FlowBlast.Services.Belt;
using FlowBlast.Services.Box;
using UnityEngine;

namespace FlowBlast.Services.Block
{
    public sealed class BlockSpawnService
    {
        private readonly BlockFactory blockFactory;
        private readonly IBeltPath beltPath;
        private readonly BeltFollowerRegistry followerRegistry;
        private readonly BoxCollectionService boxCollectionService;
        private readonly BoxBlastService boxBlastService;
        private readonly BeltSlotService beltSlotService;

        private readonly List<BlockRuntimeEntry> activeBlocks = new List<BlockRuntimeEntry>();
        private readonly List<BlockColor> blockSequence = new List<BlockColor>();

        private int sequenceIndex;
        private int laneCount = GameConstants.BeltLaneCount;
        private float rowSpacing = GameConstants.FallbackBlockSpacing;
        private float laneSpacing = GameConstants.FallbackBlockSpacing;

        public BlockSpawnService(
            BlockFactory blockFactory,
            IBeltPath beltPath,
            BeltFollowerRegistry followerRegistry,
            BoxCollectionService boxCollectionService,
            BoxBlastService boxBlastService,
            BeltSlotService beltSlotService)
        {
            this.blockFactory = blockFactory;
            this.beltPath = beltPath;
            this.followerRegistry = followerRegistry;
            this.boxCollectionService = boxCollectionService;
            this.boxBlastService = boxBlastService;
            this.beltSlotService = beltSlotService;
        }

        public void ConfigureBlockSpacing(float spacing)
        {
            rowSpacing = Mathf.Max(0.01f, spacing);
            laneSpacing = rowSpacing;
        }

        public void ConfigureBeltLanes(int totalLanes)
        {
            laneCount = Mathf.Max(1, totalLanes);
        }

        public int BacklogCount
        {
            get
            {
                int beltCapacity = BeltLaneLayout.GetTotalBlockCapacity(
                    beltPath.TotalLength,
                    rowSpacing,
                    laneCount);

                if (beltCapacity <= 0)
                {
                    return activeBlocks.Count;
                }

                return Mathf.Max(0, activeBlocks.Count - beltCapacity);
            }
        }

        public void LoadSequence(IReadOnlyList<BlockColor> sequence)
        {
            ClearActiveBlocks();
            blockSequence.Clear();
            sequenceIndex = 0;

            if (beltPath is BeltPath beltPathComponent)
            {
                beltPathComponent.EnsureInitialized();
            }

            if (beltPath.TotalLength <= Mathf.Epsilon)
            {
                Debug.LogWarning("[FlowBlast] Belt path length is 0. Assign at least 2 waypoints on BeltPath.");
            }

            for (int i = 0; i < sequence.Count; i++)
            {
                blockSequence.Add(sequence[i]);
            }

            PrewarmBelt();
        }

        public void Tick(float deltaTime)
        {
            if (!IsSpawnPointClear())
            {
                return;
            }

            if (!TryGetNextColor(out BlockColor nextColor))
            {
                return;
            }

            SpawnRow(nextColor, 0f);
        }

        public void TickCollection()
        {
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                BlockRuntimeEntry entry = activeBlocks[i];

                if (!entry.View.IsActiveOnBelt)
                {
                    continue;
                }

                if (!boxCollectionService.TryCollect(entry.Model, entry.View.BeltDistance))
                {
                    continue;
                }

                ReleaseBlockAt(i);
                TryBlastFullBoxes();
            }
        }

        public bool HasActiveBlocks()
        {
            return activeBlocks.Count > 0;
        }

        private void PrewarmBelt()
        {
            if (beltPath.TotalLength <= Mathf.Epsilon || blockSequence.Count == 0)
            {
                return;
            }

            float rowDistance = 0f;

            while (rowDistance < beltPath.TotalLength)
            {
                if (!TryGetNextColor(out BlockColor color))
                {
                    break;
                }

                SpawnRow(color, rowDistance);
                rowDistance += rowSpacing;
            }
        }

        private bool IsSpawnPointClear()
        {
            if (beltPath.TotalLength <= Mathf.Epsilon)
            {
                return false;
            }

            float normalizedRowSpacing = rowSpacing / beltPath.TotalLength;

            for (int i = 0; i < activeBlocks.Count; i++)
            {
                float normalized = beltPath.NormalizeDistance(activeBlocks[i].View.BeltDistance);
                bool isNearSpawn = normalized < normalizedRowSpacing
                    || normalized > 1f - normalizedRowSpacing;

                if (isNearSpawn)
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnRow(BlockColor color, float rowDistance)
        {
            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                BlockModel model = blockFactory.CreateModel(color);
                BlockView view = blockFactory.CreateView(model);
                view.ActivateOnBelt(rowDistance, laneIndex, laneCount, laneSpacing);
                followerRegistry.Register(view);
                activeBlocks.Add(new BlockRuntimeEntry(model, view));
            }
        }

        private void ReleaseBlockAt(int index)
        {
            BlockRuntimeEntry entry = activeBlocks[index];
            followerRegistry.Unregister(entry.View);
            blockFactory.ReleaseView(entry.View);
            activeBlocks.RemoveAt(index);
        }

        private void ClearActiveBlocks()
        {
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                ReleaseBlockAt(i);
            }
        }

        private void TryBlastFullBoxes()
        {
            List<BoxModel> activeBoxes = new List<BoxModel>(beltSlotService.GetActiveBoxes());

            for (int i = 0; i < activeBoxes.Count; i++)
            {
                boxBlastService.TryBlast(activeBoxes[i]);
            }
        }

        private bool TryGetNextColor(out BlockColor color)
        {
            color = BlockColor.None;

            if (blockSequence.Count == 0)
            {
                return false;
            }

            color = blockSequence[sequenceIndex];
            sequenceIndex++;

            if (sequenceIndex >= blockSequence.Count)
            {
                sequenceIndex = 0;
            }

            return color != BlockColor.None;
        }

        private readonly struct BlockRuntimeEntry
        {
            public BlockModel Model { get; }
            public BlockView View { get; }

            public BlockRuntimeEntry(BlockModel model, BlockView view)
            {
                Model = model;
                View = view;
            }
        }
    }
}
