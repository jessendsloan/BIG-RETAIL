using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class AuthoredTileTransformProjectorTests
    {
        private const float Tolerance = 0.00001f;

        private GameObject gridObject;
        private Grid grid;


        [SetUp]
        public void SetUp()
        {
            gridObject =
                new GameObject(
                    "AuthoredTileTransformProjectorTests.Grid");

            grid =
                gridObject.AddComponent<Grid>();

            grid.cellLayout =
                GridLayout.CellLayout.Isometric;

            grid.cellSize =
                new Vector3(
                    1f,
                    0.5f,
                    1f);
        }


        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(
                gridObject);
        }


        [TestCase(IsometricViewOrientation.North)]
        [TestCase(IsometricViewOrientation.East)]
        [TestCase(IsometricViewOrientation.South)]
        [TestCase(IsometricViewOrientation.West)]
        public void Project_MapsGridAxesToViewOrientation(
            IsometricViewOrientation orientation)
        {
            Vector3 canonicalXAxis =
                GetCellAxis(
                    Vector3.right);

            Vector3 canonicalYAxis =
                GetCellAxis(
                    Vector3.up);

            Matrix4x4 projectedTransform =
                AuthoredTileTransformProjector.Project(
                    grid,
                    Matrix4x4.identity,
                    orientation);

            ResolveExpectedAxes(
                orientation,
                canonicalXAxis,
                canonicalYAxis,
                out Vector3 expectedXAxis,
                out Vector3 expectedYAxis);

            AssertVectorApproximately(
                projectedTransform.MultiplyVector(
                    canonicalXAxis),
                expectedXAxis);

            AssertVectorApproximately(
                projectedTransform.MultiplyVector(
                    canonicalYAxis),
                expectedYAxis);
        }


        [Test]
        public void Project_NorthPreservesCanonicalTransform()
        {
            Matrix4x4 canonicalTransform =
                Matrix4x4.TRS(
                    new Vector3(
                        0.25f,
                        -0.125f,
                        0f),
                    Quaternion.Euler(
                        0f,
                        0f,
                        12f),
                    new Vector3(
                        1.2f,
                        0.8f,
                        1f));

            Matrix4x4 projectedTransform =
                AuthoredTileTransformProjector.Project(
                    grid,
                    canonicalTransform,
                    IsometricViewOrientation.North);

            AssertMatrixApproximately(
                projectedTransform,
                canonicalTransform);
        }


        [Test]
        public void Project_FourEastTurnsReturnToIdentity()
        {
            Matrix4x4 eastTurn =
                AuthoredTileTransformProjector.Project(
                    grid,
                    Matrix4x4.identity,
                    IsometricViewOrientation.East);

            Matrix4x4 fourTurns =
                eastTurn
                * eastTurn
                * eastTurn
                * eastTurn;

            AssertMatrixApproximately(
                fourTurns,
                Matrix4x4.identity);
        }


        [Test]
        public void Project_TransformsAuthoredOffsetWithTile()
        {
            Matrix4x4 canonicalTransform =
                Matrix4x4.Translate(
                    new Vector3(
                        0.2f,
                        -0.1f,
                        0f));

            Matrix4x4 orientationTransform =
                AuthoredTileTransformProjector.Project(
                    grid,
                    Matrix4x4.identity,
                    IsometricViewOrientation.East);

            Matrix4x4 projectedTransform =
                AuthoredTileTransformProjector.Project(
                    grid,
                    canonicalTransform,
                    IsometricViewOrientation.East);

            Vector3 expectedPosition =
                orientationTransform.MultiplyPoint3x4(
                    canonicalTransform.MultiplyPoint3x4(
                        Vector3.zero));

            Vector3 actualPosition =
                projectedTransform.MultiplyPoint3x4(
                    Vector3.zero);

            AssertVectorApproximately(
                actualPosition,
                expectedPosition);
        }


        private Vector3 GetCellAxis(
            Vector3 cellAxis)
        {
            Vector3 localOrigin =
                grid.CellToLocalInterpolated(
                    Vector3.zero);

            return grid.CellToLocalInterpolated(
                       cellAxis)
                - localOrigin;
        }


        private static void ResolveExpectedAxes(
            IsometricViewOrientation orientation,
            Vector3 canonicalXAxis,
            Vector3 canonicalYAxis,
            out Vector3 expectedXAxis,
            out Vector3 expectedYAxis)
        {
            switch (orientation)
            {
                case IsometricViewOrientation.North:
                    expectedXAxis = canonicalXAxis;
                    expectedYAxis = canonicalYAxis;
                    return;

                case IsometricViewOrientation.East:
                    expectedXAxis = -canonicalYAxis;
                    expectedYAxis = canonicalXAxis;
                    return;

                case IsometricViewOrientation.South:
                    expectedXAxis = -canonicalXAxis;
                    expectedYAxis = -canonicalYAxis;
                    return;

                case IsometricViewOrientation.West:
                    expectedXAxis = canonicalYAxis;
                    expectedYAxis = -canonicalXAxis;
                    return;

                default:
                    throw new AssertionException(
                        "Unsupported test orientation.");
            }
        }


        private static void AssertVectorApproximately(
            Vector3 actual,
            Vector3 expected)
        {
            Assert.That(
                actual.x,
                Is.EqualTo(expected.x)
                    .Within(Tolerance));

            Assert.That(
                actual.y,
                Is.EqualTo(expected.y)
                    .Within(Tolerance));

            Assert.That(
                actual.z,
                Is.EqualTo(expected.z)
                    .Within(Tolerance));
        }


        private static void AssertMatrixApproximately(
            Matrix4x4 actual,
            Matrix4x4 expected)
        {
            for (int row = 0;
                 row < 4;
                 row++)
            {
                for (int column = 0;
                     column < 4;
                     column++)
                {
                    Assert.That(
                        actual[row, column],
                        Is.EqualTo(
                                expected[row, column])
                            .Within(Tolerance),
                        $"Matrix mismatch at [{row}, {column}].");
                }
            }
        }
    }
}
