using FlowBlast.Core.Enums;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "BoxVisualProfile", menuName = "FlowBlast/Box Visual Profile")]
    public sealed class BoxVisualProfile : ScriptableObject
    {
        [SerializeField] private BlockColor blockColor = BlockColor.Green;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private Texture2D baseTexture;

        public BlockColor BlockColor => blockColor;
        public Color TintColor => tintColor;
        public Texture2D BaseTexture => baseTexture;
    }
}
