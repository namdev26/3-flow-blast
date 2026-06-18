using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
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
        [SerializeField] private List<LevelBoxPlacement> boxPlacements = new List<LevelBoxPlacement>();
        [SerializeField] private List<BlockColor> blockSequence = new List<BlockColor>();
        [SerializeField] private List<BoxDefinition> boxQueue = new List<BoxDefinition>();

        public string LevelId => levelId;
        public float BeltSpeed => beltSpeed;
        public int MaxBeltSlots => maxBeltSlots;
        public int MaxBacklogBlocks => maxBacklogBlocks;
        public int BeltLaneCount => beltLaneCount;
        public bool AutoBuildBlockSequenceFromBoxes => autoBuildBlockSequenceFromBoxes;
        public IReadOnlyList<LevelBoxPlacement> BoxPlacements => boxPlacements;
        public IReadOnlyList<BlockColor> BlockSequence => autoBuildBlockSequenceFromBoxes
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
