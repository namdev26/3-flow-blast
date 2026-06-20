using System.Collections.Generic;
using System.Text;
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

        private const bool EnableSpawnDebugLogs = false;
        private const float SpawnSpacingSafetyRatio = 0.6f;

        private readonly List<BlockRuntimeEntry> activeBlocks = new List<BlockRuntimeEntry>();
        private readonly List<LevelBlockSpawnRow> blockSpawnRows = new List<LevelBlockSpawnRow>();
        private readonly List<float> queueMergeDistances = new List<float>();

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

                if (!TryGetNextSpawnDistance(out float spawnDistance))
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
            int rowCapacity = GetRowCapacity();

            if (rowCapacity <= 0)
            {
                return 0;
            }

            bool[] occupiedSlots = new bool[rowCapacity];
            BuildOccupiedRowSlots(occupiedSlots);
            int missingRows = 0;

            for (int slotIndex = 0; slotIndex < occupiedSlots.Length; slotIndex++)
            {
                if (!occupiedSlots[slotIndex])
                {
                    missingRows++;
                }
            }

            return missingRows;
        }

        private int GetBeltCapacity()
        {
            return BeltLaneLayout.GetTotalBlockCapacity(
                beltPath.TotalLength,
                rowSpacing,
                laneCount);
        }

        private bool TryGetNextSpawnDistance(out float spawnDistance)
        {
            spawnDistance = 0f;
            int rowCapacity = GetRowCapacity();

            if (rowCapacity <= 0)
            {
                LogSpawnDebug("Spawn search aborted: row capacity is 0.");
                return false;
            }

            bool[] occupiedSlots = new bool[rowCapacity];
            BuildOccupiedRowSlots(occupiedSlots);
            string diagnostics = string.Empty;

            if (TryGetClosestValidEmptySlotToMerge(occupiedSlots, out int slotIndex, out diagnostics))
            {
                spawnDistance = slotIndex * rowSpacing;
                LogSpawnDebug($"Spawn slot selected. slot={slotIndex}, distance={spawnDistance:F3}. {diagnostics}");
                return true;
            }

            LogSpawnDebug($"No valid spawn slot found. {diagnostics}");
            return false;
        }

        private bool TryGetClosestValidEmptySlotToMerge(bool[] occupiedSlots, out int slotIndex, out string diagnostics)
        {
            slotIndex = -1;
            diagnostics = string.Empty;

            if (occupiedSlots == null || occupiedSlots.Length == 0)
            {
                diagnostics = "Occupied slot buffer is empty.";
                return false;
            }

            float bestScore = float.MaxValue;
            bool foundValidSlot = false;
            StringBuilder builder = EnableSpawnDebugLogs ? new StringBuilder() : null;

            if (builder != null)
            {
                builder.Append("Slots[");
            }

            for (int i = 0; i < occupiedSlots.Length; i++)
            {
                bool isValid = TryGetSpawnSlotScore(occupiedSlots, i, out float score, out string reason);

                if (builder != null)
                {
                    if (i > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append(i);
                    builder.Append(':');
                    builder.Append(reason);

                    if (isValid)
                    {
                        builder.Append(" score=");
                        builder.Append(score.ToString("F3"));
                    }
                }

                if (!isValid)
                {
                    continue;
                }

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                slotIndex = i;
                foundValidSlot = true;
            }

            if (builder != null)
            {
                builder.Append(']');
                diagnostics = builder.ToString();
            }

            return foundValidSlot;
        }

        private bool TryGetSpawnSlotScore(bool[] occupiedSlots, int slotIndex, out float score, out string reason)
        {
            score = float.MaxValue;
            reason = string.Empty;

            if (occupiedSlots == null || slotIndex < 0 || slotIndex >= occupiedSlots.Length)
            {
                reason = "out-of-range";
                return false;
            }

            if (occupiedSlots[slotIndex])
            {
                reason = "occupied";
                return false;
            }

            float spawnDistance = slotIndex * rowSpacing;

            if (IsTooCloseToExistingRow(spawnDistance))
            {
                reason = "too-close";
                return false;
            }

            if (!TryResolveRowSpawnDistance(spawnDistance))
            {
                reason = "merge-window-rejected";
                return false;
            }

            if (queueMergeDistances.Count == 0)
            {
                score = slotIndex;
                reason = "valid-no-merge";
                return true;
            }

            float bestMergeDelta = float.MaxValue;

            for (int i = 0; i < queueMergeDistances.Count; i++)
            {
                float mergeDelta = GetWrappedDistanceDelta(spawnDistance, queueMergeDistances[i]);

                if (mergeDelta < bestMergeDelta)
                {
                    bestMergeDelta = mergeDelta;
                }
            }

            score = bestMergeDelta;
            reason = "valid";
            return true;
        }

        private bool IsTooCloseToExistingRow(float spawnDistance)
        {
            float minSpacing = rowSpacing * SpawnSpacingSafetyRatio;

            for (int i = 0; i < activeBlocks.Count; i++)
            {
                float distanceDelta = GetWrappedDistanceDelta(spawnDistance, activeBlocks[i].View.BeltDistance);

                if (distanceDelta < minSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildOccupiedRowSlots(bool[] occupiedSlots)
        {
            if (occupiedSlots == null)
            {
                return;
            }

            for (int i = 0; i < occupiedSlots.Length; i++)
            {
                occupiedSlots[i] = false;
            }

            for (int i = 0; i < activeBlocks.Count; i++)
            {
                if (!TryGetRowSlotIndex(activeBlocks[i].View.BeltDistance, occupiedSlots.Length, out int slotIndex))
                {
                    continue;
                }

                occupiedSlots[slotIndex] = true;
            }
        }

        private bool TryGetRowSlotIndex(float distance, int rowCapacity, out int slotIndex)
        {
            slotIndex = -1;

            if (rowCapacity <= 0 || rowSpacing <= Mathf.Epsilon)
            {
                return false;
            }

            float normalizedDistance = NormalizeDistanceOnMainBelt(distance);
            int nearestSlotIndex = Mathf.RoundToInt(normalizedDistance / rowSpacing);
            slotIndex = WrapRowSlotIndex(nearestSlotIndex, rowCapacity);
            return true;
        }

        private int WrapRowSlotIndex(int slotIndex, int rowCapacity)
        {
            if (rowCapacity <= 0)
            {
                return 0;
            }

            int wrappedSlotIndex = slotIndex % rowCapacity;

            if (wrappedSlotIndex < 0)
            {
                wrappedSlotIndex += rowCapacity;
            }

            return wrappedSlotIndex;
        }

        private bool TryResolveRowSpawnDistance(float spawnDistance)
        {
            return true;
        }

        private void LogSpawnDebug(string message)
        {
            if (!EnableSpawnDebugLogs)
            {
                return;
            }

            Debug.Log($"[FlowBlast][BlockSpawnService] {message}");
        }

        private float GetWrappedDistanceDelta(float firstDistance, float secondDistance)
        {
            float pathLength = beltPath.TotalLength;

            if (pathLength <= Mathf.Epsilon)
            {
                return Mathf.Abs(firstDistance - secondDistance);
            }

            float firstNormalized = NormalizeDistanceOnMainBelt(firstDistance);
            float secondNormalized = NormalizeDistanceOnMainBelt(secondDistance);
            float delta = Mathf.Abs(firstNormalized - secondNormalized);
            return Mathf.Min(delta, pathLength - delta);
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
