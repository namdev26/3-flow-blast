using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltWaypointMarker : MonoBehaviour
    {
        [SerializeField] private int waypointIndex;
        [SerializeField] private BeltPathRole pathRole;

        public int WaypointIndex => waypointIndex;
        public BeltPathRole PathRole => pathRole;

        public void SetWaypointIndex(int index)
        {
            waypointIndex = index;
            gameObject.name = $"Waypoint_{index}";
        }

        public void SetPathRole(BeltPathRole value)
        {
            pathRole = value;
        }
    }
}
