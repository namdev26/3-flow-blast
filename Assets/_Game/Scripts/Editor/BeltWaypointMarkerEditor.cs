#if UNITY_EDITOR
using FlowBlast.Services.Belt;
using UnityEditor;
using UnityEngine;

namespace FlowBlast.Editor
{
    [CustomEditor(typeof(BeltWaypointMarker))]
    public sealed class BeltWaypointMarkerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            BeltWaypointMarker marker = (BeltWaypointMarker)target;
            Vector3 position = marker.transform.position;
            float size = HandleUtility.GetHandleSize(position) * 0.08f;
            Handles.color = BeltPathVisualPalette.GetWaypointColor(marker.PathRole);
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(
                position + Vector3.up * size * 2f,
                $"{BeltPathVisualPalette.GetDisplayName(marker.PathRole)} WP {marker.WaypointIndex}");
        }
    }
}
#endif
