using UnityEngine;

namespace FlowBlast.Data
{
    [System.Serializable]
    public sealed class LevelBlockSequenceItem
    {
        [SerializeField] private BoxVisualProfile visualProfile;

        public BoxVisualProfile VisualProfile
        {
            get => visualProfile;
            set => visualProfile = value;
        }
    }
}
