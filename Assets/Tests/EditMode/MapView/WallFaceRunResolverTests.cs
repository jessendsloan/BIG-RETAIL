using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.View.Tests
{
    public sealed class WallFaceRunResolverTests
    {
        private static readonly IsometricMapFootprint Footprint =
            new IsometricMapFootprint(
                minimumX: -2,
                minimumY: -2,
                maximumX: 3,
                maximumY: 3,
                logicalLevel: 0);

        private static readonly CellEdge[] RunEdges =
        {
            new CellEdge(
                new GridVertex(0, 0),
                new GridVertex(0, 1)),
            new CellEdge(
                new GridVertex(0, 1),
                new GridVertex(0, 2))
        };


        [TestCase(IsometricViewOrientation.North)]
        [TestCase(IsometricViewOrientation.East)]
        [TestCase(IsometricViewOrientation.South)]
        [TestCase(IsometricViewOrientation.West)]
        public void ResolveViewerFacingFaces_MatchesPresentationSelection(
            IsometricViewOrientation orientation)
        {
            IsometricViewProjection projection =
                new IsometricViewProjection(
                    Footprint,
                    orientation);

            WallFaceKey[] faces =
                WallFaceRunResolver.ResolveViewerFacingFaces(
                    RunEdges,
                    projection);

            Assert.That(
                faces.Length,
                Is.EqualTo(RunEdges.Length));

            for (int index = 0;
                 index < RunEdges.Length;
                 index++)
            {
                WallPresentationSelection expected =
                    WallPresentationSelector.Select(
                        RunEdges[index],
                        projection);

                Assert.That(
                    faces[index].Edge,
                    Is.EqualTo(RunEdges[index]));

                Assert.That(
                    faces[index].FacingCell,
                    Is.EqualTo(expected.ViewerFacingCell));

                Assert.That(
                    faces[index].Edge.TouchesCell(
                        faces[index].FacingCell),
                    Is.True);
            }
        }
    }
}
