using System;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Oriented local-space bounds derived from one authored shelf mask.
    /// The long axis divides the shelf into frontage units while the sprite
    /// mesh remains the precise pointer-hit surface.
    /// </summary>
    public readonly struct FixtureShelfMaskGeometry
    {
        private const float MinimumExtent = 0.0001f;

        private readonly Vector2 majorAxis;
        private readonly Vector2 minorAxis;
        private readonly float minimumMajor;
        private readonly float maximumMajor;
        private readonly float middleMinor;
        private readonly float minorLength;


        public float MajorLength =>
            maximumMajor - minimumMajor;

        public float MinorLength =>
            minorLength;

        public float MajorAxisAngleDegrees =>
            Mathf.Atan2(majorAxis.y, majorAxis.x) * Mathf.Rad2Deg;


        private FixtureShelfMaskGeometry(
            Vector2 majorAxis,
            Vector2 minorAxis,
            float minimumMajor,
            float maximumMajor,
            float middleMinor,
            float minorLength)
        {
            this.majorAxis = majorAxis;
            this.minorAxis = minorAxis;
            this.minimumMajor = minimumMajor;
            this.maximumMajor = maximumMajor;
            this.middleMinor = middleMinor;
            this.minorLength = minorLength;
        }


        public Vector2 GetFrontageCenter(
            int visualFrontageIndex,
            int frontageUnitCount)
        {
            if (frontageUnitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frontageUnitCount));
            }

            if (visualFrontageIndex < 0
                || visualFrontageIndex >= frontageUnitCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(visualFrontageIndex));
            }

            float unitT =
                (visualFrontageIndex + 0.5f)
                / frontageUnitCount;
            float majorPosition =
                Mathf.Lerp(
                    minimumMajor,
                    maximumMajor,
                    unitT);

            return (majorAxis * majorPosition)
                + (minorAxis * middleMinor);
        }


        public int ResolveVisualFrontageIndex(
            Vector2 localPoint,
            int frontageUnitCount)
        {
            if (frontageUnitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frontageUnitCount));
            }

            float majorPosition =
                Vector2.Dot(localPoint, majorAxis);
            float normalized =
                Mathf.InverseLerp(
                    minimumMajor,
                    maximumMajor,
                    majorPosition);

            return Mathf.Clamp(
                Mathf.FloorToInt(normalized * frontageUnitCount),
                0,
                frontageUnitCount - 1);
        }


        public static bool TryCreate(
            Sprite shelfMask,
            out FixtureShelfMaskGeometry geometry)
        {
            geometry = default;

            if (shelfMask == null)
            {
                return false;
            }

            Vector2[] vertices = shelfMask.vertices;

            if (vertices == null || vertices.Length < 3)
            {
                return false;
            }

            Vector2 mean = Vector2.zero;

            for (int index = 0; index < vertices.Length; index++)
            {
                mean += vertices[index];
            }

            mean /= vertices.Length;

            float xx = 0f;
            float xy = 0f;
            float yy = 0f;

            for (int index = 0; index < vertices.Length; index++)
            {
                Vector2 delta = vertices[index] - mean;
                xx += delta.x * delta.x;
                xy += delta.x * delta.y;
                yy += delta.y * delta.y;
            }

            float angle =
                0.5f * Mathf.Atan2(2f * xy, xx - yy);
            Vector2 majorAxis =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle));

            if (majorAxis.x < 0f
                || (Mathf.Approximately(majorAxis.x, 0f)
                    && majorAxis.y < 0f))
            {
                majorAxis = -majorAxis;
            }

            Vector2 minorAxis =
                new Vector2(-majorAxis.y, majorAxis.x);

            ResolveProjectionExtents(
                vertices,
                majorAxis,
                out float minimumMajor,
                out float maximumMajor);
            ResolveProjectionExtents(
                vertices,
                minorAxis,
                out float minimumMinor,
                out float maximumMinor);

            float majorLength = maximumMajor - minimumMajor;
            float minorLength = maximumMinor - minimumMinor;

            if (majorLength <= MinimumExtent
                || minorLength <= MinimumExtent)
            {
                return false;
            }

            geometry =
                new FixtureShelfMaskGeometry(
                    majorAxis,
                    minorAxis,
                    minimumMajor,
                    maximumMajor,
                    (minimumMinor + maximumMinor) * 0.5f,
                    minorLength);
            return true;
        }


        public static bool ContainsLocalPoint(
            Sprite shelfMask,
            Vector2 localPoint)
        {
            if (shelfMask == null)
            {
                return false;
            }

            Vector2[] vertices = shelfMask.vertices;
            ushort[] triangles = shelfMask.triangles;

            if (vertices == null
                || triangles == null
                || triangles.Length < 3)
            {
                return false;
            }

            for (int index = 0;
                 index <= triangles.Length - 3;
                 index += 3)
            {
                if (ContainsPoint(
                        localPoint,
                        vertices[triangles[index]],
                        vertices[triangles[index + 1]],
                        vertices[triangles[index + 2]]))
                {
                    return true;
                }
            }

            return false;
        }


        private static void ResolveProjectionExtents(
            Vector2[] vertices,
            Vector2 axis,
            out float minimum,
            out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;

            for (int index = 0; index < vertices.Length; index++)
            {
                float projection =
                    Vector2.Dot(vertices[index], axis);
                minimum = Mathf.Min(minimum, projection);
                maximum = Mathf.Max(maximum, projection);
            }
        }


        private static bool ContainsPoint(
            Vector2 point,
            Vector2 first,
            Vector2 second,
            Vector2 third)
        {
            float firstSign = Cross(point, first, second);
            float secondSign = Cross(point, second, third);
            float thirdSign = Cross(point, third, first);

            bool hasNegative =
                firstSign < 0f
                || secondSign < 0f
                || thirdSign < 0f;
            bool hasPositive =
                firstSign > 0f
                || secondSign > 0f
                || thirdSign > 0f;

            return !(hasNegative && hasPositive);
        }


        private static float Cross(
            Vector2 point,
            Vector2 first,
            Vector2 second)
        {
            return ((point.x - second.x) * (first.y - second.y))
                - ((first.x - second.x) * (point.y - second.y));
        }
    }
}
