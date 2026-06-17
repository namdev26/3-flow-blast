using System.Collections.Generic;
using FlowBlast.Core.Constants;
using FlowBlast.Core.Enums;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "FlowBlast/Level Data")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private string levelId = "Level_01";
        [SerializeField] private float beltSpeed = GameConstants.DefaultBeltSpeed;
        [SerializeField] private int maxBeltSlots = GameConstants.DefaultMaxBeltSlots;
        [SerializeField] private int maxBacklogBlocks = GameConstants.DefaultMaxBacklogBlocks;
        [SerializeField] private int beltLaneCount = GameConstants.BeltLaneCount;
        [SerializeField] private List<BlockColor> blockSequence = new List<BlockColor>();
        [SerializeField] private List<BoxDefinition> boxQueue = new List<BoxDefinition>();

        public string LevelId => levelId;
        public float BeltSpeed => beltSpeed;
        public int MaxBeltSlots => maxBeltSlots;
        public int MaxBacklogBlocks => maxBacklogBlocks;
        public int BeltLaneCount => beltLaneCount;
        public IReadOnlyList<BlockColor> BlockSequence => blockSequence;
        public IReadOnlyList<BoxDefinition> BoxQueue => boxQueue;
    }
}
