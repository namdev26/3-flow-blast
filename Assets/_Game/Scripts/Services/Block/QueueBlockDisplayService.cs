using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Data;
using FlowBlast.Domain;
using FlowBlast.Patterns.Factory;
using FlowBlast.Presentation.Block;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Services.Block
{
    public sealed class QueueBlockDisplayService
    {
        private readonly IReadOnlyList<IBeltPath> queuePaths;
        private readonly BlockFactory blockFactory;
        private readonly List<QueueBlockEntry> displayedBlocks = new List<QueueBlockEntry>();

        private float rowSpacing = GameConstants.FallbackBlockSpacing;
        private int laneCount = GameConstants.BeltLaneCount;
        private float laneSpacing = GameConstants.FallbackBlockSpacing;
        private int lastRefreshedFromIndex = -1;

        public QueueBlockDisplayService(IReadOnlyList<IBeltPath> queuePaths, BlockFactory blockFactory)
        {
            this.queuePaths = queuePaths;
            this.blockFactory = blockFactory;
        }

        public void ConfigureLayout(float spacing, int lanes, float laneSpc)
        {
            rowSpacing = Mathf.Max(0.01f, spacing);
            laneCount = Mathf.Max(1, lanes);
            laneSpacing = laneSpc;
        }

        public void Refresh(IReadOnlyList<BoxVisualProfile> sequence, int fromIndex)
        {
            if (fromIndex == lastRefreshedFromIndex)
            {
                return;
            }

            lastRefreshedFromIndex = fromIndex;
            ReleaseAllDisplayedBlocks();

            if (queuePaths == null || queuePaths.Count == 0 || sequence == null)
            {
                return;
            }

            int sequenceCursor = fromIndex;

            for (int pathIndex = 0; pathIndex < queuePaths.Count; pathIndex++)
            {
                IBeltPath path = queuePaths[pathIndex];

                if (path == null)
                {
                    continue;
                }

                int rowCapacity = GetRowCapacityForPath(path);
                int remaining = sequence.Count - sequenceCursor;
                int rowsToDisplay = Mathf.Min(rowCapacity, remaining);

                for (int rowIndex = 0; rowIndex < rowsToDisplay; rowIndex++)
                {
                    BoxVisualProfile profile = sequence[sequenceCursor + rowIndex];
                    SpawnQueueRow(profile, path, rowIndex * rowSpacing);
                }

                sequenceCursor += rowsToDisplay;

                if (sequenceCursor >= sequence.Count)
                {
                    break;
                }
            }
        }

        public void Clear()
        {
            ReleaseAllDisplayedBlocks();
            lastRefreshedFromIndex = -1;
        }

        private void SpawnQueueRow(BoxVisualProfile profile, IBeltPath path, float rowDistance)
        {
            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                BlockModel model = blockFactory.CreateModel(profile);

                if (!blockFactory.TryCreateView(model, out BlockView view))
                {
                    return;
                }

                view.Configure(path);
                view.ActivateOnBelt(rowDistance, laneIndex, laneCount, laneSpacing);
                displayedBlocks.Add(new QueueBlockEntry(model, view));
            }
        }

        private void ReleaseAllDisplayedBlocks()
        {
            for (int i = 0; i < displayedBlocks.Count; i++)
            {
                blockFactory.ReleaseView(displayedBlocks[i].View);
            }

            displayedBlocks.Clear();
        }

        private int GetRowCapacityForPath(IBeltPath path)
        {
            if (path == null || rowSpacing <= 0f)
            {
                return 0;
            }

            return Mathf.FloorToInt(path.TotalLength / rowSpacing);
        }

        private readonly struct QueueBlockEntry
        {
            public BlockModel Model { get; }
            public BlockView View { get; }

            public QueueBlockEntry(BlockModel model, BlockView view)
            {
                Model = model;
                View = view;
            }
        }
    }
}
