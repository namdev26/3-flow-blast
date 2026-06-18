using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Factory;
using FlowBlast.Presentation;
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
        private readonly BlockCollectPresentationService blockCollectPresentationService;
        private readonly BoxBlastService boxBlastService;

        private readonly List<BlockRuntimeEntry> activeBlocks = new List<BlockRuntimeEntry>();
        private readonly List<BlockColor> blockSequence = new List<BlockColor>();

        private int sequenceIndex;
        private int blocksInFlightCount;
        private int laneCount = GameConstants.BeltLaneCount;
        private float rowSpacing = GameConstants.FallbackBlockSpacing;
        private float laneSpacing = GameConstants.FallbackBlockSpacing;

        public BlockSpawnService(
            BlockFactory blockFactory,
            IBeltPath beltPath,
            BeltFollowerRegistry followerRegistry,
            BoxCollectionService boxCollectionService,
            BlockCollectPresentationService blockCollectPresentationService,
            BoxBlastService boxBlastService)
        {
            this.blockFactory = blockFactory;
            this.beltPath = beltPath;
            this.followerRegistry = followerRegistry;
            this.boxCollectionService = boxCollectionService;
            this.blockCollectPresentationService = blockCollectPresentationService;
            this.boxBlastService = boxBlastService;
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
            blockFactory.RecyclePool();
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

        public void TickCollection()
        {
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                BlockRuntimeEntry entry = activeBlocks[i];

                if (!entry.View.IsActiveOnBelt)
                {
                    continue;
                }

                if (!boxCollectionService.TryCollect(entry.Model, entry.View.BeltDistance, out BoxModel collectedBox))
                {
                    continue;
                }

                followerRegistry.Unregister(entry.View);
                activeBlocks.RemoveAt(i);
                blocksInFlightCount++;

                blockCollectPresentationService.PlayCollect(
                    entry.View,
                    collectedBox.Id,
                    () =>
                    {
                        blocksInFlightCount--;
                        blockFactory.ConsumeView(entry.View);
                    });

                if (collectedBox.IsFull())
                {
                    boxBlastService.TryBlast(collectedBox);
                }
            }
        }

        public bool HasActiveBlocks()
        {
            return activeBlocks.Count > 0 || blocksInFlightCount > 0;
        }

        public bool HasRemainingSequence()
        {
            return sequenceIndex < blockSequence.Count;
        }

        private void PrewarmBelt()
        {
            if (beltPath.TotalLength <= Mathf.Epsilon || blockSequence.Count == 0)
            {
                return;
            }

            float rowDistance = 0f;

            while (rowDistance < beltPath.TotalLength && HasRemainingSequence())
            {
                if (!TryConsumeNextColor(out BlockColor color))
                {
                    break;
                }

                if (!SpawnRow(color, rowDistance))
                {
                    break;
                }

                rowDistance += rowSpacing;
            }
        }

        private bool SpawnRow(BlockColor color, float rowDistance)
        {
            bool spawnedAny = false;

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                BlockModel model = blockFactory.CreateModel(color);

                if (!blockFactory.TryCreateView(model, out BlockView view))
                {
                    break;
                }

                view.ActivateOnBelt(rowDistance, laneIndex, laneCount, laneSpacing);
                followerRegistry.Register(view);
                activeBlocks.Add(new BlockRuntimeEntry(model, view));
                spawnedAny = true;
            }

            return spawnedAny;
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
            blocksInFlightCount = 0;

            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                ReleaseBlockAt(i);
            }
        }

        private bool TryConsumeNextColor(out BlockColor color)
        {
            color = BlockColor.None;

            if (sequenceIndex >= blockSequence.Count)
            {
                return false;
            }

            color = blockSequence[sequenceIndex];
            sequenceIndex++;

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
