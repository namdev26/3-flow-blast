using System.Collections.Generic;
using FlowBlast.Core.Constants;
using UnityEngine;

namespace FlowBlast.Services.Belt
{
    public sealed class CatmullRomPathSampler
    {
        private struct ArcLengthEntry
        {
            public float Distance;
            public int SegmentIndex;
            public float T;
        }

        private readonly List<Vector3> controlPoints = new List<Vector3>();
        private readonly List<ArcLengthEntry> arcLengthTable = new List<ArcLengthEntry>();
        private readonly List<Vector3> sampledPositions = new List<Vector3>();
        private float totalLength;
        private bool isClosedLoop;

        public float TotalLength => totalLength;

        public void Rebuild(IReadOnlyList<Vector3> points, bool closedLoop)
        {
            controlPoints.Clear();
            arcLengthTable.Clear();
            sampledPositions.Clear();
            totalLength = 0f;
            isClosedLoop = closedLoop;

            if (points == null || points.Count < 2)
            {
                return;
            }

            controlPoints.AddRange(points);

            int segmentCount = closedLoop ? controlPoints.Count : controlPoints.Count - 1;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                int startSampleIndex = segmentIndex == 0 ? 0 : 1;

                for (int sampleIndex = startSampleIndex; sampleIndex <= GameConstants.BeltPathSamplesPerSegment; sampleIndex++)
                {
                    float t = sampleIndex / (float)GameConstants.BeltPathSamplesPerSegment;
                    Vector3 point = EvaluateCatmullRom(controlPoints, segmentIndex, t, closedLoop);
                    AppendArcLengthEntry(segmentIndex, t, point);
                }
            }
        }

        public Vector3 GetPositionAtDistance(float distance)
        {
            if (arcLengthTable.Count == 0)
            {
                return Vector3.zero;
            }

            if (!TryResolveSplineParameter(distance, out int segmentIndex, out float t))
            {
                return EvaluateCatmullRom(controlPoints, arcLengthTable[0].SegmentIndex, arcLengthTable[0].T, isClosedLoop);
            }

            return EvaluateCatmullRom(controlPoints, segmentIndex, t, isClosedLoop);
        }

        public Vector3 GetTangentAtDistance(float distance)
        {
            if (arcLengthTable.Count == 0)
            {
                return Vector3.forward;
            }

            if (!TryResolveSplineParameter(distance, out int segmentIndex, out float t))
            {
                return EvaluateCatmullRomDerivative(
                    controlPoints,
                    arcLengthTable[0].SegmentIndex,
                    arcLengthTable[0].T,
                    isClosedLoop);
            }

            Vector3 tangent = EvaluateCatmullRomDerivative(controlPoints, segmentIndex, t, isClosedLoop);

            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return tangent.normalized;
        }

        public Quaternion GetRotationAtDistance(float distance)
        {
            Vector3 tangent = GetTangentAtDistance(distance);

            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(tangent, Vector3.up);
        }

        public float NormalizeDistance(float distance)
        {
            if (totalLength <= Mathf.Epsilon)
            {
                return 0f;
            }

            float normalized = distance / totalLength;
            normalized -= Mathf.Floor(normalized);
            return normalized;
        }

        public IReadOnlyList<Vector3> GetSampledPositions()
        {
            return sampledPositions;
        }

        public float GetClosestDistance(Vector3 worldPosition)
        {
            if (sampledPositions.Count == 0)
            {
                return 0f;
            }

            float closestDistance = 0f;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < sampledPositions.Count; i++)
            {
                float sqrDistance = (sampledPositions[i] - worldPosition).sqrMagnitude;

                if (sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                closestDistance = arcLengthTable[i].Distance;
            }

            return closestDistance;
        }

        private void AppendArcLengthEntry(int segmentIndex, float t, Vector3 point)
        {
            if (sampledPositions.Count > 0)
            {
                float delta = Vector3.Distance(sampledPositions[sampledPositions.Count - 1], point);

                if (delta <= Mathf.Epsilon)
                {
                    return;
                }

                totalLength += delta;
            }

            arcLengthTable.Add(new ArcLengthEntry
            {
                Distance = totalLength,
                SegmentIndex = segmentIndex,
                T = t
            });
            sampledPositions.Add(point);
        }

        private bool TryResolveSplineParameter(float distance, out int segmentIndex, out float t)
        {
            segmentIndex = 0;
            t = 0f;

            float targetDistance = WrapDistance(distance);

            if (targetDistance <= 0f || arcLengthTable.Count == 1)
            {
                segmentIndex = arcLengthTable[0].SegmentIndex;
                t = arcLengthTable[0].T;
                return true;
            }

            for (int i = 1; i < arcLengthTable.Count; i++)
            {
                ArcLengthEntry current = arcLengthTable[i];

                if (current.Distance < targetDistance)
                {
                    continue;
                }

                ArcLengthEntry previous = arcLengthTable[i - 1];
                float segmentLength = current.Distance - previous.Distance;

                if (segmentLength <= Mathf.Epsilon)
                {
                    segmentIndex = current.SegmentIndex;
                    t = current.T;
                    return true;
                }

                float blend = (targetDistance - previous.Distance) / segmentLength;

                if (previous.SegmentIndex == current.SegmentIndex)
                {
                    segmentIndex = previous.SegmentIndex;
                    t = Mathf.Lerp(previous.T, current.T, blend);
                    return true;
                }

                segmentIndex = blend < 0.5f ? previous.SegmentIndex : current.SegmentIndex;
                t = blend < 0.5f ? previous.T : current.T;
                return true;
            }

            ArcLengthEntry last = arcLengthTable[arcLengthTable.Count - 1];
            segmentIndex = last.SegmentIndex;
            t = last.T;
            return true;
        }

        private float WrapDistance(float distance)
        {
            if (totalLength <= Mathf.Epsilon)
            {
                return 0f;
            }

            float wrapped = distance % totalLength;

            if (wrapped < 0f)
            {
                wrapped += totalLength;
            }

            return wrapped;
        }

        private static Vector3 EvaluateCatmullRom(
            IReadOnlyList<Vector3> points,
            int segmentIndex,
            float t,
            bool closedLoop)
        {
            ResolveControlPoints(points, segmentIndex, closedLoop, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);

            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static Vector3 EvaluateCatmullRomDerivative(
            IReadOnlyList<Vector3> points,
            int segmentIndex,
            float t,
            bool closedLoop)
        {
            ResolveControlPoints(points, segmentIndex, closedLoop, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3);

            float t2 = t * t;

            return 0.5f * (
                (-p0 + p2) +
                (4f * p0 - 10f * p1 + 8f * p2 - 2f * p3) * t +
                (-3f * p0 + 9f * p1 - 9f * p2 + 3f * p3) * t2);
        }

        private static void ResolveControlPoints(
            IReadOnlyList<Vector3> points,
            int segmentIndex,
            bool closedLoop,
            out Vector3 p0,
            out Vector3 p1,
            out Vector3 p2,
            out Vector3 p3)
        {
            int pointCount = points.Count;
            int index0 = ResolveControlIndex(segmentIndex - 1, pointCount, closedLoop);
            int index1 = ResolveControlIndex(segmentIndex, pointCount, closedLoop);
            int index2 = ResolveControlIndex(segmentIndex + 1, pointCount, closedLoop);
            int index3 = ResolveControlIndex(segmentIndex + 2, pointCount, closedLoop);

            p0 = points[index0];
            p1 = points[index1];
            p2 = points[index2];
            p3 = points[index3];
        }

        private static int ResolveControlIndex(int index, int pointCount, bool isClosedLoop)
        {
            if (isClosedLoop)
            {
                int wrapped = index % pointCount;

                if (wrapped < 0)
                {
                    wrapped += pointCount;
                }

                return wrapped;
            }

            return Mathf.Clamp(index, 0, pointCount - 1);
        }
    }
}
