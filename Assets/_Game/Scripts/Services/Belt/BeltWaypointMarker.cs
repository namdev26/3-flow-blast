using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltWaypointMarker : MonoBehaviour
    {
        [SerializeField] private int waypointIndex;

        public int WaypointIndex => waypointIndex;

        public void SetWaypointIndex(int index)
        {
            waypointIndex = index;
            gameObject.name = $"Waypoint_{index}";
        }
    }
}
