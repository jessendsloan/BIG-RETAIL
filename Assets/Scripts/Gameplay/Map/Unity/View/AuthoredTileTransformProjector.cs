using System;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.View
{
    /// <summary>
    /// Projects one authored per-cell Tilemap transform through the same
    /// logical quarter turn used by the isometric cell projection.
    ///
    /// The conversion happens in Grid-local basis space. This preserves
    /// an isometric cell's diamond while turning directional artwork such
    /// as road markings together with its destination cell.
    /// </summary>
    public static class AuthoredTileTransformProjector
    {
        public static Matrix4x4 Project(
            GridLayout gridLayout,
            Matrix4x4 canonicalTransform,
            IsometricViewOrientation orientation)
        {
            if (gridLayout == null)
            {
                throw new ArgumentNullException(
                    nameof(gridLayout));
            }

            if (orientation == IsometricViewOrientation.North)
            {
                return canonicalTransform;
            }

            Vector3 localOrigin =
                gridLayout.CellToLocalInterpolated(
                    Vector3.zero);

            Vector3 canonicalXAxis =
                gridLayout.CellToLocalInterpolated(
                    Vector3.right)
                - localOrigin;

            Vector3 canonicalYAxis =
                gridLayout.CellToLocalInterpolated(
                    Vector3.up)
                - localOrigin;

            ResolveProjectedAxes(
                orientation,
                canonicalXAxis,
                canonicalYAxis,
                out Vector3 projectedXAxis,
                out Vector3 projectedYAxis);

            Matrix4x4 canonicalBasis =
                CreateBasis(
                    canonicalXAxis,
                    canonicalYAxis);

            if (Mathf.Abs(canonicalBasis.determinant)
                <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    "The Grid layout produced a singular cell basis.");
            }

            Matrix4x4 projectedBasis =
                CreateBasis(
                    projectedXAxis,
                    projectedYAxis);

            Matrix4x4 orientationTransform =
                projectedBasis
                * canonicalBasis.inverse;

            return orientationTransform
                * canonicalTransform;
        }


        private static void ResolveProjectedAxes(
            IsometricViewOrientation orientation,
            Vector3 canonicalXAxis,
            Vector3 canonicalYAxis,
            out Vector3 projectedXAxis,
            out Vector3 projectedYAxis)
        {
            switch (orientation)
            {
                case IsometricViewOrientation.North:
                    projectedXAxis = canonicalXAxis;
                    projectedYAxis = canonicalYAxis;
                    return;

                case IsometricViewOrientation.East:
                    projectedXAxis = -canonicalYAxis;
                    projectedYAxis = canonicalXAxis;
                    return;

                case IsometricViewOrientation.South:
                    projectedXAxis = -canonicalXAxis;
                    projectedYAxis = -canonicalYAxis;
                    return;

                case IsometricViewOrientation.West:
                    projectedXAxis = canonicalYAxis;
                    projectedYAxis = -canonicalXAxis;
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(orientation),
                        orientation,
                        "Unsupported isometric-view orientation.");
            }
        }


        private static Matrix4x4 CreateBasis(
            Vector3 xAxis,
            Vector3 yAxis)
        {
            Matrix4x4 basis =
                Matrix4x4.identity;

            basis.SetColumn(
                0,
                new Vector4(
                    xAxis.x,
                    xAxis.y,
                    xAxis.z,
                    0f));

            basis.SetColumn(
                1,
                new Vector4(
                    yAxis.x,
                    yAxis.y,
                    yAxis.z,
                    0f));

            basis.SetColumn(
                2,
                new Vector4(
                    0f,
                    0f,
                    1f,
                    0f));

            basis.SetColumn(
                3,
                new Vector4(
                    0f,
                    0f,
                    0f,
                    1f));

            return basis;
        }
    }
}
