using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public static class BeltPathVisualPalette
    {
        public static Color GetPathColor(BeltPathRole pathRole)
        {
            return pathRole == BeltPathRole.Queue
                ? new Color(1f, 0.45f, 0.2f, 0.95f)
                : new Color(0.2f, 0.85f, 1f, 0.95f);
        }

        public static Color GetWaypointColor(BeltPathRole pathRole)
        {
            return pathRole == BeltPathRole.Queue
                ? new Color(1f, 0.7f, 0.25f, 0.95f)
                : new Color(1f, 0.9f, 0.2f, 0.95f);
        }

        public static Color GetHandleColor(BeltPathRole pathRole)
        {
            return pathRole == BeltPathRole.Queue
                ? new Color(1f, 0.55f, 0.25f, 0.95f)
                : new Color(0.25f, 0.9f, 1f, 0.95f);
        }

        public static string GetDisplayName(BeltPathRole pathRole)
        {
            return pathRole == BeltPathRole.Queue
                ? "Queue Path"
                : "Main Path";
        }
    }
}
