using System.Collections.Generic;
using UnityEngine;

namespace FlowBlast.Data
{
    [System.Serializable]
    public sealed class LevelBlockSpawnRow
    {
        [SerializeField] private List<BoxVisualProfile> laneProfiles = new List<BoxVisualProfile>();

        public IReadOnlyList<BoxVisualProfile> LaneProfiles => laneProfiles;

        public BoxVisualProfile GetLaneProfile(int laneIndex)
        {
            if (laneProfiles == null || laneIndex < 0 || laneIndex >= laneProfiles.Count)
            {
                return null;
            }

            return laneProfiles[laneIndex];
        }

        public void EnsureLaneCount(int laneCount)
        {
            int safeLaneCount = Mathf.Max(1, laneCount);

            if (laneProfiles == null)
            {
                laneProfiles = new List<BoxVisualProfile>(safeLaneCount);
            }

            while (laneProfiles.Count < safeLaneCount)
            {
                laneProfiles.Add(null);
            }

            while (laneProfiles.Count > safeLaneCount)
            {
                laneProfiles.RemoveAt(laneProfiles.Count - 1);
            }
        }

        public void Fill(BoxVisualProfile visualProfile, int laneCount)
        {
            EnsureLaneCount(laneCount);

            for (int laneIndex = 0; laneIndex < laneProfiles.Count; laneIndex++)
            {
                laneProfiles[laneIndex] = visualProfile;
            }
        }

        public void SetLaneProfile(int laneIndex, BoxVisualProfile visualProfile)
        {
            if (laneIndex < 0)
            {
                return;
            }

            EnsureLaneCount(laneIndex + 1);
            laneProfiles[laneIndex] = visualProfile;
        }
    }
}
