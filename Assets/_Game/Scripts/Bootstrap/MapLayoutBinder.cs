using FlowBlast.Core.Constants;
using FlowBlast.Core.Utilities;
using FlowBlast.Data;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Bootstrap
{
    [DefaultExecutionOrder(-100)]
    public sealed class MapLayoutBinder : MonoBehaviour
    {
        [SerializeField] private LevelMapLayout mapLayout;
        [SerializeField] private BeltPath beltPath;
        [SerializeField] private Transform boxQueueParent;
        [SerializeField] private BeltWaypointMarker waypointPrefab;
        [SerializeField] private bool applyOnAwake = true;
        [SerializeField] private bool applyInEditMode = true;

        public LevelMapLayout MapLayout => mapLayout;
        public BeltPath BeltPath => beltPath;

        private void Awake()
        {
            if (!applyOnAwake)
            {
                return;
            }

            ApplyLayout();
        }

        public void ApplyLayout()
        {
            ResolveReferences();

            if (mapLayout == null || beltPath == null)
            {
                return;
            }

            MapLayoutApplicator.Apply(mapLayout, beltPath, boxQueueParent, waypointPrefab);

#if UNITY_EDITOR
            lastAppliedLayout = mapLayout;
            UnityEditor.EditorUtility.SetDirty(this);

            if (beltPath != null)
            {
                UnityEditor.EditorUtility.SetDirty(beltPath);
            }

            if (boxQueueParent != null)
            {
                UnityEditor.EditorUtility.SetDirty(boxQueueParent);
            }
#endif
        }

        public void CaptureLayout()
        {
            ResolveReferences();

            if (mapLayout == null || beltPath == null)
            {
                return;
            }

            MapLayoutApplicator.Capture(mapLayout, beltPath, boxQueueParent);
        }

        public void SetMapLayout(LevelMapLayout layout)
        {
            mapLayout = layout;
            ApplyLayout();
        }

        private void ResolveReferences()
        {
            if (beltPath == null)
            {
                beltPath = GetComponentInChildren<BeltPath>();
            }

            if (boxQueueParent == null)
            {
                boxQueueParent = TransformHierarchyUtility.FindChildRecursive(
                    transform,
                    GameplayZoneNames.BoxQueueParent);
            }

#if UNITY_EDITOR
            if (waypointPrefab == null)
            {
                GameObject prefabObject = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Game/Prefabs/BeltWaypointMarker.prefab");

                if (prefabObject != null)
                {
                    waypointPrefab = prefabObject.GetComponent<BeltWaypointMarker>();
                }
            }
#endif
        }

#if UNITY_EDITOR
        [SerializeField] [HideInInspector] private LevelMapLayout lastAppliedLayout;

        private void OnValidate()
        {
            if (!applyInEditMode || mapLayout == null || Application.isPlaying)
            {
                return;
            }

            if (mapLayout == lastAppliedLayout)
            {
                return;
            }

            ApplyLayout();
            lastAppliedLayout = mapLayout;
        }
#endif
    }
}
