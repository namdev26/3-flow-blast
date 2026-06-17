using System;
using FlowBlast.Core.Enums;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "BlockColorPalette", menuName = "FlowBlast/Block Color Palette")]
    public class BlockColorPalette : ScriptableObject
    {
        [Serializable]
        public class ColorEntry
        {
            public BlockColor BlockColor;
            public Color UnityColor = Color.white;
        }

        [SerializeField] private ColorEntry[] entries = Array.Empty<ColorEntry>();

        public Color GetUnityColor(BlockColor blockColor)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].BlockColor == blockColor)
                {
                    return entries[i].UnityColor;
                }
            }

            return Color.magenta;
        }
    }
}
