using FlowBlast.Core.Constants;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "MapEditorSettings", menuName = "FlowBlast/Map Editor Settings")]
    public sealed class MapEditorSettings : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField] private float gridCellSize = 0.4f;
        [SerializeField] private int gridExtentCells = 16;
        [SerializeField] private bool snapToGrid = true;
        [SerializeField] private bool showGrid = true;

        [Header("Path Preview")]
        [SerializeField] private bool showPathWhenNotSelected = true;
        [SerializeField] private bool showLanePreview = true;
        [SerializeField] private int laneCount = GameConstants.BeltLaneCount;
        [SerializeField] private float laneSpacing = GameConstants.FallbackBlockSpacing;
        [SerializeField] private float waypointHandleSize = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color gridColor = new Color(0.35f, 0.38f, 0.42f, 0.35f);
        [SerializeField] private Color mainPathColor = new Color(0.2f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color queuePathColor = new Color(1f, 0.45f, 0.2f, 0.95f);
        [SerializeField] private Color mainWaypointColor = new Color(1f, 0.9f, 0.2f, 0.95f);
        [SerializeField] private Color queueWaypointColor = new Color(1f, 0.7f, 0.25f, 0.95f);
        [SerializeField] private Color lanePreviewColor = new Color(0.3f, 1f, 0.45f, 0.45f);
        [SerializeField] private Color boxQueueColor = new Color(1f, 0.45f, 0.2f, 0.95f);

        public float GridCellSize => gridCellSize;
        public int GridExtentCells => gridExtentCells;
        public bool SnapToGrid => snapToGrid;
        public bool ShowGrid => showGrid;
        public bool ShowPathWhenNotSelected => showPathWhenNotSelected;
        public bool ShowLanePreview => showLanePreview;
        public int LaneCount => laneCount;
        public float LaneSpacing => laneSpacing;
        public float WaypointHandleSize => waypointHandleSize;
        public Color GridColor => gridColor;
        public Color MainPathColor => mainPathColor;
        public Color QueuePathColor => queuePathColor;
        public Color MainWaypointColor => mainWaypointColor;
        public Color QueueWaypointColor => queueWaypointColor;
        public Color LanePreviewColor => lanePreviewColor;
        public Color BoxQueueColor => boxQueueColor;
    }
}
