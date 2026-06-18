using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Services.Level;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "FlowBlast/Level Data")]
    public sealed class LevelData : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string levelId = "Level_01";
        [SerializeField] private float beltSpeed = GameConstants.DefaultBeltSpeed;
        [SerializeField] private int maxBeltSlots = GameConstants.DefaultMaxBeltSlots;
        [SerializeField] private int maxBacklogBlocks = GameConstants.DefaultMaxBacklogBlocks;
        [SerializeField] private int beltLaneCount = GameConstants.BeltLaneCount;
        [SerializeField] private bool autoBuildBlockSequenceFromBoxes = true;
        [SerializeField] private int editorGridColumns = 5;
        [SerializeField] private int editorGridRows = 5;
        [SerializeField] private float editorGridCellSpacing = 1.5f;
        [SerializeField] private List<LevelBoxPlacement> boxPlacements = new List<LevelBoxPlacement>();
        [SerializeField] private List<BoxVisualProfile> blockSequence = new List<BoxVisualProfile>();
        [SerializeField] private List<BoxDefinition> boxQueue = new List<BoxDefinition>();

        public string LevelId => levelId;
        public float BeltSpeed => beltSpeed;
        public int MaxBeltSlots => maxBeltSlots;
        public int MaxBacklogBlocks => maxBacklogBlocks;
        public int BeltLaneCount => beltLaneCount;
        public bool AutoBuildBlockSequenceFromBoxes => autoBuildBlockSequenceFromBoxes;
        public int EditorGridColumns => editorGridColumns;
        public int EditorGridRows => editorGridRows;
        public float EditorGridCellSpacing => editorGridCellSpacing;
        public IReadOnlyList<LevelBoxPlacement> BoxPlacements => boxPlacements;
        public IReadOnlyList<BoxVisualProfile> BlockSequence => autoBuildBlockSequenceFromBoxes
            ? LevelBlockSequenceBuilder.BuildFromPlacements(boxPlacements, beltLaneCount)
            : blockSequence;
        public int TotalBlockCount => GetTotalBlockCount();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            MigrateLegacyBoxQueue();
        }

        private int GetTotalBlockCount()
        {
            int totalBlockCount = 0;

            for (int i = 0; i < boxPlacements.Count; i++)
            {
                totalBlockCount += Mathf.Max(1, boxPlacements[i].Capacity);
            }

            return totalBlockCount;
        }

        private void MigrateLegacyBoxQueue()
        {
            if (boxPlacements.Count > 0 || boxQueue.Count == 0)
            {
                return;
            }

            for (int i = 0; i < boxQueue.Count; i++)
            {
                BoxDefinition definition = boxQueue[i];
                LevelBoxPlacement placement = new LevelBoxPlacement
                {
                    LocalPosition = BoardBoxLayout.GetLocalSpawnPosition(
                        i,
                        Mathf.Max(1, boxQueue.Count),
                        BoardBoxLayout.DefaultTestBoxCellSpacing)
                };
                placement.ApplyDefinition(definition);
                boxPlacements.Add(placement);
            }
        }
    }
}
