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

        public float TotalLength => pathSampler.TotalLength;

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
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
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
