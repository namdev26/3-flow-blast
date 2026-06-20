using System.Collections.Generic;
using FlowBlast.Core.Constants;
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
        private readonly List<LevelBlockSpawnRow> blockSpawnRows = new List<LevelBlockSpawnRow>();
        private readonly List<float> queueMergeDistances = new List<float>();
        private readonly HashSet<int> occupiedRowSlots = new HashSet<int>();

        private bool isInitialMainPathPrefill = true;
        private int sequenceIndex;
        private int blocksInFlightCount;
        private int laneCount = GameConstants.BeltLaneCount;
        private float rowSpacing = GameConstants.FallbackBlockSpacing;
        private float laneSpacing = GameConstants.FallbackBlockSpacing;
        private QueueBlockDisplayService queueDisplayService;

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

        public void SetQueueDisplayService(QueueBlockDisplayService service)
        {
            queueDisplayService = service;
            queueDisplayService?.ConfigureLayout(rowSpacing, laneCount, laneSpacing);
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

        public void ConfigureQueueMergeDistances(IReadOnlyList<float> mergeDistances)
        {
            queueMergeDistances.Clear();

            if (mergeDistances == null)
            {
                return;
            }

            for (int i = 0; i < mergeDistances.Count; i++)
            {
                queueMergeDistances.Add(mergeDistances[i]);
            }
        }

        public int BacklogCount
        {
            get
            {
                int beltCapacity = GetBeltCapacity();

                if (beltCapacity <= 0)
                {
                    return activeBlocks.Count;
                }

                return Mathf.Max(0, activeBlocks.Count - beltCapacity);
            }
        }

        public void LoadSequence(IReadOnlyList<LevelBlockSpawnRow> spawnRows)
        {
            queueDisplayService?.Clear();
            ClearActiveBlocks();
            blockFactory.RecyclePool();
            blockSpawnRows.Clear();
            sequenceIndex = 0;
            blocksInFlightCount = 0;
            isInitialMainPathPrefill = true;

            if (beltPath is BeltPath beltPathComponent)
            {
                beltPathComponent.EnsureInitialized();
            }

            if (beltPath.TotalLength <= Mathf.Epsilon)
            {
                Debug.LogWarning("[FlowBlast] Belt path length is 0. Assign at least 2 waypoints on BeltPath.");
            }

            if (spawnRows != null)
            {
                for (int i = 0; i < spawnRows.Count; i++)
                {
                    blockSpawnRows.Add(spawnRows[i]);
                }
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

            MaintainBeltCoverage();
        }

        public bool HasActiveBlocks()
        {
            return activeBlocks.Count > 0 || blocksInFlightCount > 0;
        }

        public bool HasRemainingSequence()
        {
            return sequenceIndex < blockSpawnRows.Count;
        }

        private void PrewarmBelt()
        {
            MaintainBeltCoverage();
            isInitialMainPathPrefill = false;
        }

        private void MaintainBeltCoverage()
        {
            int missingRows = GetMissingRowCount();

            while (missingRows > 0 && HasRemainingSequence())
            {
                if (!TryConsumeNextRow(out LevelBlockSpawnRow spawnRow))
                {
                    break;
                }

                float spawnDistance = GetNextSpawnDistance();

                if (!TryResolveRowSpawnDistance(ref spawnDistance))
                {
                    sequenceIndex--;
                    break;
                }

                if (!SpawnRow(spawnRow, spawnDistance))
                {
                    break;
                }

                missingRows--;
            }

            queueDisplayService?.Refresh(blockSpawnRows, sequenceIndex);
        }

        private int GetMissingRowCount()
        {
            int beltCapacity = GetBeltCapacity();

            if (beltCapacity <= 0)
            {
                return 0;
            }

            int missingBlocks = Mathf.Max(0, beltCapacity - activeBlocks.Count);
            return missingBlocks / laneCount;
        }

        private int GetBeltCapacity()
        {
            return BeltLaneLayout.GetTotalBlockCapacity(
                beltPath.TotalLength,
                rowSpacing,
                laneCount);
        }

        private float GetNextSpawnDistance()
        {
            if (activeBlocks.Count == 0)
            {
                return 0f;
            }

            float minimumDistance = activeBlocks[0].View.BeltDistance;

            for (int i = 1; i < activeBlocks.Count; i++)
            {
                float currentDistance = activeBlocks[i].View.BeltDistance;

                if (currentDistance < minimumDistance)
                {
                    minimumDistance = currentDistance;
                }
            }

            return minimumDistance - rowSpacing;
        }

        private bool TryResolveRowSpawnDistance(ref float spawnDistance)
        {
            if (isInitialMainPathPrefill)
            {
                return true;
            }

            if (queueMergeDistances.Count == 0)
            {
                return true;
            }

            BuildOccupiedRowSlots();
            int preferredSlot = GetPreferredMergeSlot(spawnDistance);

            if (preferredSlot < 0)
            {
                return false;
            }

            spawnDistance = GetDistanceForRowSlot(preferredSlot);
            return true;
        }

        private void BuildOccupiedRowSlots()
        {
            occupiedRowSlots.Clear();
            int rowCapacity = GetRowCapacity();

            if (rowCapacity <= 0)
            {
                return;
            }

            for (int i = 0; i < activeBlocks.Count; i++)
            {
                int slotIndex = GetNearestRowSlotIndex(activeBlocks[i].View.BeltDistance);

                if (slotIndex < 0)
                {
                    continue;
                }

                occupiedRowSlots.Add(slotIndex);
            }
        }

        private int GetPreferredMergeSlot(float preferredDistance)
        {
            int preferredSlot = GetNearestRowSlotIndex(preferredDistance);
            int bestMergeSlot = -1;
            int bestSlotDelta = int.MaxValue;
            int rowCapacity = GetRowCapacity();

            for (int i = 0; i < queueMergeDistances.Count; i++)
            {
                int mergeSlot = GetNearestRowSlotIndex(queueMergeDistances[i]);

                if (!IsMergeSlotAvailable(mergeSlot))
                {
                    continue;
                }

                if (preferredSlot < 0)
                {
                    return mergeSlot;
                }

                int slotDelta = Mathf.Abs(mergeSlot - preferredSlot);
                slotDelta = Mathf.Min(slotDelta, rowCapacity - slotDelta);

                if (slotDelta >= bestSlotDelta)
                {
                    continue;
                }

                bestSlotDelta = slotDelta;
                bestMergeSlot = mergeSlot;
            }

            return bestMergeSlot;
        }

        private bool IsMergeSlotAvailable(int mergeSlot)
        {
            if (mergeSlot < 0)
            {
                return false;
            }

            return !occupiedRowSlots.Contains(mergeSlot);
        }

        private int GetNearestRowSlotIndex(float distance)
        {
            int rowCapacity = GetRowCapacity();

            if (rowCapacity <= 0)
            {
                return -1;
            }

            float normalizedDistance = NormalizeDistanceOnMainBelt(distance);
            int slotIndex = Mathf.RoundToInt(normalizedDistance / rowSpacing) % rowCapacity;

            if (slotIndex < 0)
            {
                slotIndex += rowCapacity;
            }

            return slotIndex;
        }

        private float GetDistanceForRowSlot(int slotIndex)
        {
            int rowCapacity = GetRowCapacity();

            if (rowCapacity <= 0)
            {
                return 0f;
            }

            int normalizedSlotIndex = slotIndex % rowCapacity;

            if (normalizedSlotIndex < 0)
            {
                normalizedSlotIndex += rowCapacity;
            }

            return normalizedSlotIndex * rowSpacing;
        }

        private int GetRowCapacity()
        {
            if (beltPath.TotalLength <= Mathf.Epsilon || rowSpacing <= Mathf.Epsilon)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.FloorToInt(beltPath.TotalLength / rowSpacing));
        }

        private float NormalizeDistanceOnMainBelt(float distance)
        {
            if (beltPath.TotalLength <= Mathf.Epsilon)
            {
                return 0f;
            }

            return beltPath.NormalizeDistance(distance) * beltPath.TotalLength;
        }

        private bool SpawnRow(LevelBlockSpawnRow spawnRow, float rowDistance)
        {
            if (spawnRow == null)
            {
                return false;
            }

            bool spawnedAny = false;

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                BoxVisualProfile visualProfile = spawnRow.GetLaneProfile(laneIndex);

                if (visualProfile == null)
                {
                    continue;
                }

                BlockModel model = blockFactory.CreateModel(visualProfile);

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
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                ReleaseBlockAt(i);
            }
        }

        private bool TryConsumeNextRow(out LevelBlockSpawnRow spawnRow)
        {
            spawnRow = null;

            if (sequenceIndex >= blockSpawnRows.Count)
            {
                return false;
            }

            spawnRow = blockSpawnRows[sequenceIndex];
            sequenceIndex++;

            return spawnRow != null;
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
