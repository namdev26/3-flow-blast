using System.Collections.Generic;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class BeltPath : MonoBehaviour, IBeltPath
    {
        [SerializeField] private Transform[] waypoints = new Transform[0];
        [SerializeField] private bool isClosedLoop = true;

        private readonly CatmullRomPathSampler pathSampler = new CatmullRomPathSampler();
        private readonly List<Vector3> controlPointsBuffer = new List<Vector3>();
        private readonly List<Vector3> lastWaypointWorldPositions = new List<Vector3>();

        public float TotalLength => pathSampler.TotalLength;
        public bool IsClosedLoop => isClosedLoop;
        public Transform[] Waypoints => waypoints;

        public void ApplyLocalWaypoints(
            IReadOnlyList<Vector3> localPositions,
            bool closedLoop,
            BeltWaypointMarker waypointPrefab)
        {
            ClearWaypoints();
            isClosedLoop = closedLoop;

            if (localPositions == null || localPositions.Count == 0)
            {
                waypoints = System.Array.Empty<Transform>();
                RebuildPath();
                return;
            }

            waypoints = new Transform[localPositions.Count];

            for (int i = 0; i < localPositions.Count; i++)
            {
                waypoints[i] = CreateWaypointTransform(localPositions[i], i, waypointPrefab);
            }

            RebuildPath();
        }

        public void CaptureLocalWaypointPositions(List<Vector3> output)
        {
            output.Clear();

            if (waypoints == null)
            {
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                output.Add(waypoints[i].localPosition);
            }
        }

        private void Awake()
        {
            RebuildPath();
        }

        public void EnsureInitialized()
        {
            RebuildPath();
        }

        private void OnValidate()
        {
            RebuildPath();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (HasWaypointPositionsChanged())
            {
                RebuildPath();
            }
        }

        public Vector3 GetPositionAtDistance(float distance)
        {
            if (!HasValidPath())
            {
                return transform.position;
            }

            return pathSampler.GetPositionAtDistance(distance);
        }

        public Quaternion GetRotationAtDistance(float distance)
        {
            if (!HasValidPath())
            {
                return transform.rotation;
            }

            return pathSampler.GetRotationAtDistance(distance);
        }

        public float NormalizeDistance(float distance)
        {
            return pathSampler.NormalizeDistance(distance);
        }

        public float GetClosestDistance(Vector3 worldPosition)
        {
            return pathSampler.GetClosestDistance(worldPosition);
        }

        private bool HasValidPath()
        {
            return waypoints != null && waypoints.Length >= 2;
        }

        private void RebuildPath()
        {
            controlPointsBuffer.Clear();

            if (!HasValidPath())
            {
                pathSampler.Rebuild(controlPointsBuffer, isClosedLoop);
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                controlPointsBuffer.Add(waypoints[i].position);
            }

            pathSampler.Rebuild(controlPointsBuffer, isClosedLoop);
            CacheWaypointPositions();
        }

        private bool HasWaypointPositionsChanged()
        {
            if (!HasValidPath())
            {
                return lastWaypointWorldPositions.Count > 0;
            }

            int validIndex = 0;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Vector3 worldPosition = waypoints[i].position;

                if (validIndex >= lastWaypointWorldPositions.Count)
                {
                    return true;
                }

                if ((lastWaypointWorldPositions[validIndex] - worldPosition).sqrMagnitude > 0.000001f)
                {
                    return true;
                }

                validIndex++;
            }

            return validIndex != lastWaypointWorldPositions.Count;
        }

        private void ClearWaypoints()
        {
            if (waypoints == null)
            {
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                DestroyWaypointObject(waypoints[i].gameObject);
            }

            waypoints = System.Array.Empty<Transform>();
        }

        private Transform CreateWaypointTransform(
            Vector3 localPosition,
            int index,
            BeltWaypointMarker waypointPrefab)
        {
            GameObject waypointObject;

            if (waypointPrefab != null)
            {
                waypointObject = Instantiate(waypointPrefab.gameObject, transform);
            }
            else
            {
                waypointObject = new GameObject($"Waypoint_{index}");
                waypointObject.transform.SetParent(transform, false);
                waypointObject.AddComponent<BeltWaypointMarker>();
            }

            waypointObject.transform.localPosition = localPosition;

            BeltWaypointMarker marker = waypointObject.GetComponent<BeltWaypointMarker>();

            if (marker == null)
            {
                marker = waypointObject.AddComponent<BeltWaypointMarker>();
            }

            marker.SetWaypointIndex(index);
            return waypointObject.transform;
        }

        private void DestroyWaypointObject(GameObject waypointObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(waypointObject);
                return;
            }
#endif
            Destroy(waypointObject);
        }

        private void CacheWaypointPositions()
        {
            lastWaypointWorldPositions.Clear();

            if (!HasValidPath())
            {
                return;
            }

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                lastWaypointWorldPositions.Add(waypoints[i].position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying && HasWaypointPositionsChanged())
            {
                RebuildPath();
            }

            IReadOnlyList<Vector3> samples = pathSampler.GetSampledPositions();

            if (samples.Count < 2)
            {
                return;
            }

            Gizmos.color = Color.cyan;

            for (int i = 1; i < samples.Count; i++)
            {
                Gizmos.DrawLine(samples[i - 1], samples[i]);
            }

            if (isClosedLoop && samples.Count > 2)
            {
                Gizmos.DrawLine(samples[samples.Count - 1], samples[0]);
            }

            Gizmos.color = Color.yellow;

            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null)
                    {
                        continue;
                    }

                    Gizmos.DrawSphere(waypoints[i].position, 0.12f);
                }
            }
        }
    }
}
