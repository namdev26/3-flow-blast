using System.Collections.Generic;
using FlowBlast.Services.Belt;
using UnityEngine;

namespace FlowBlast.Data
{
    [CreateAssetMenu(fileName = "LevelMapLayout", menuName = "FlowBlast/Level Map Layout")]
    public sealed class LevelMapLayout : ScriptableObject
    {
        [SerializeField] private string layoutId = "MapLayout_01";
        [SerializeField] private bool isMainPathClosedLoop = true;
        [SerializeField] private List<Vector3> mainWaypointLocalPositions = new List<Vector3>();
        [SerializeField] private Vector3 boxQueueLocalPosition = new Vector3(-2f, 0f, -3f);
        [SerializeField] private Vector3 collectionPointLocalPosition = new Vector3(0f, 0f, 3f);
        [SerializeField] private float mainCurveStrength = 1f;
        [SerializeField] private List<QueuePathLayout> queuePaths = new List<QueuePathLayout>();

        public string LayoutId => layoutId;
        public bool IsMainPathClosedLoop => isMainPathClosedLoop;
        public IReadOnlyList<Vector3> MainWaypointLocalPositions => mainWaypointLocalPositions;
        public Vector3 BoxQueueLocalPosition => boxQueueLocalPosition;
        public Vector3 CollectionPointLocalPosition => collectionPointLocalPosition;
        public float MainCurveStrength => mainCurveStrength;
        public IReadOnlyList<QueuePathLayout> QueuePaths => queuePaths;

        public void SetMainPathData(
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            float curveStrength)
        {
            CopyPositions(localPositions, mainWaypointLocalPositions);
            isMainPathClosedLoop = closedLoop;
            mainCurveStrength = Mathf.Clamp01(curveStrength);
        }

        public void SetQueuePathData(
            string queueId,
            string displayName,
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            float curveStrength)
        {
            QueuePathLayout queuePath = GetOrCreateQueuePath(queueId, displayName);
            queuePath.SetIdentity(queueId, displayName);
            queuePath.SetPathData(localPositions, closedLoop, curveStrength);
        }

        public bool TryGetQueuePath(string queueId, out QueuePathLayout queuePath)
        {
            for (int i = 0; i < queuePaths.Count; i++)
            {
                if (queuePaths[i] != null && queuePaths[i].QueueId == queueId)
                {
                    queuePath = queuePaths[i];
                    return true;
                }
            }

            queuePath = null;
            return false;
        }

        public void RemoveMissingQueuePaths(IReadOnlyCollection<string> validQueueIds)
        {
            for (int i = queuePaths.Count - 1; i >= 0; i--)
            {
                if (queuePaths[i] == null || ContainsQueueId(validQueueIds, queuePaths[i].QueueId))
                {
                    continue;
                }

                queuePaths.RemoveAt(i);
            }
        }

        private static bool ContainsQueueId(IReadOnlyCollection<string> queueIds, string queueId)
        {
            if (queueIds == null)
            {
                return false;
            }

            foreach (string currentQueueId in queueIds)
            {
                if (currentQueueId == queueId)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetBoxQueueLocalPosition(Vector3 localPosition)
        {
            boxQueueLocalPosition = localPosition;
        }

        public void SetCollectionPointLocalPosition(Vector3 localPosition)
        {
            collectionPointLocalPosition = localPosition;
        }

        public void ApplyTo(
            BeltPath mainBeltPath,
            IReadOnlyList<BeltPath> queueBeltPaths,
            Transform boxQueueParent,
            BeltWaypointMarker waypointPrefab,
            Transform collectionPointMarker = null)
        {
            MapLayoutApplicator.Apply(this, mainBeltPath, queueBeltPaths, boxQueueParent, waypointPrefab, collectionPointMarker);
        }

        public void CaptureFrom(
            BeltPath mainBeltPath,
            IReadOnlyList<BeltPath> queueBeltPaths,
            Transform boxQueueParent,
            Transform collectionPointMarker = null)
        {
            MapLayoutApplicator.Capture(this, mainBeltPath, queueBeltPaths, boxQueueParent, collectionPointMarker);
        }

        private QueuePathLayout GetOrCreateQueuePath(string queueId, string displayName)
        {
            if (TryGetQueuePath(queueId, out QueuePathLayout existingQueuePath))
            {
                return existingQueuePath;
            }

            QueuePathLayout newQueuePath = new QueuePathLayout();
            newQueuePath.SetIdentity(queueId, displayName);
            queuePaths.Add(newQueuePath);
            return newQueuePath;
        }

        private static void CopyPositions(IReadOnlyList<Vector3> source, List<Vector3> target)
        {
            target.Clear();

            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }
    }
}
