using BigRetail.Map.Domain;
using BigRetail.Map.View;
using NUnit.Framework;

namespace BigRetail.Map.View.Tests
{
    public sealed class WallPresentationSelectionTests
    {
        private static readonly IsometricMapFootprint Footprint =
            new IsometricMapFootprint(
                minimumX: -3,
                minimumY: 7,
                maximumX: 1,
                maximumY: 9,
                logicalLevel: 2);

        private static readonly CellEdge LogicalEdge =
            new CellEdge(
                new GridPosition(
                    -1,
                    8,
                    2),
                CellEdgeDirection.NorthEast);


        [TestCase(
            IsometricViewOrientation.North,
            true,
            WallDisplaySlope.RisingLeft)]
        [TestCase(
            IsometricViewOrientation.East,
            false,
            WallDisplaySlope.RisingRight)]
        [TestCase(
            IsometricViewOrientation.South,
            false,
            WallDisplaySlope.RisingLeft)]
        [TestCase(
            IsometricViewOrientation.West,
            true,
            WallDisplaySlope.RisingRight)]
        public void Select_ReturnsExpectedFaceAndSlope(
            IsometricViewOrientation orientation,
            bool expectFirstCell,
            WallDisplaySlope expectedSlope)
        {
            WallPresentationSelection selection =
                CreateProjection(
                    orientation)
                    .SelectWallPresentation(
                        LogicalEdge);

            GridPosition expectedFacingCell =
                expectFirstCell
                    ? LogicalEdge.FirstCell
                    : LogicalEdge.SecondCell;

            Assert.That(
                selection.ViewerFacingCell,
                Is.EqualTo(
                    expectedFacingCell));

            Assert.That(
                selection.DisplaySlope,
                Is.EqualTo(
                    expectedSlope));

            Assert.That(
                selection.DisplayEdge.TouchesCell(
                    CreateProjection(
                        orientation)
                        .ToDisplayCell(
                            LogicalEdge.FirstCell)),
                Is.True);

            Assert.That(
                selection.DisplayEdge.TouchesCell(
                    CreateProjection(
                        orientation)
                        .ToDisplayCell(
                            LogicalEdge.SecondCell)),
                Is.True);
        }


        [Test]
        public void OppositeViews_SelectOppositeFaces()
        {
            WallPresentationSelection north =
                CreateProjection(
                    IsometricViewOrientation.North)
                    .SelectWallPresentation(
                        LogicalEdge);

            WallPresentationSelection south =
                CreateProjection(
                    IsometricViewOrientation.South)
                    .SelectWallPresentation(
                        LogicalEdge);

            Assert.That(
                north.ViewerFacingCell,
                Is.Not.EqualTo(
                    south.ViewerFacingCell));
        }


        [Test]
        public void QuarterTurns_AlternateDisplaySlope()
        {
            WallPresentationSelection north =
                CreateProjection(
                    IsometricViewOrientation.North)
                    .SelectWallPresentation(
                        LogicalEdge);

            WallPresentationSelection east =
                CreateProjection(
                    IsometricViewOrientation.East)
                    .SelectWallPresentation(
                        LogicalEdge);

            Assert.That(
                north.DisplaySlope,
                Is.Not.EqualTo(
                    east.DisplaySlope));
        }


        [Test]
        public void FourTurns_RestoreOriginalPresentation()
        {
            IsometricViewProjection projection =
                CreateProjection(
                    IsometricViewOrientation.North);

            WallPresentationSelection original =
                projection.SelectWallPresentation(
                    LogicalEdge);

            for (int turn = 0;
                 turn < 4;
                 turn++)
            {
                projection =
                    projection.WithOrientation(
                        projection.Orientation
                            .RotateClockwise());
            }

            WallPresentationSelection restored =
                projection.SelectWallPresentation(
                    LogicalEdge);

            Assert.That(
                restored.DisplayEdge,
                Is.EqualTo(
                    original.DisplayEdge));

            Assert.That(
                restored.ViewerFacingCell,
                Is.EqualTo(
                    original.ViewerFacingCell));

            Assert.That(
                restored.DisplaySlope,
                Is.EqualTo(
                    original.DisplaySlope));
        }


        [Test]
        public void WallsUp_AlwaysUsesFullStructuralWall()
        {
            WallPresentationHeight height =
                WallPresentationHeightResolver.Resolve(
                    WallDisplayMode.WallsUp,
                    wallOccludesFoundation: true);

            Assert.That(height, Is.EqualTo(WallPresentationHeight.Full));
        }


        [Test]
        public void WallsDown_AlwaysUsesLowStructuralWall()
        {
            WallPresentationHeight height =
                WallPresentationHeightResolver.Resolve(
                    WallDisplayMode.WallsDown,
                    wallOccludesFoundation: false);

            Assert.That(height, Is.EqualTo(WallPresentationHeight.Low));
        }


        [Test]
        public void Cutaway_LowersWallThatOccludesFoundation()
        {
            WallPresentationHeight height =
                WallPresentationHeightResolver.Resolve(
                    WallDisplayMode.Cutaway,
                    wallOccludesFoundation: true);

            Assert.That(height, Is.EqualTo(WallPresentationHeight.Low));
        }


        [Test]
        public void Cutaway_KeepsWallThatDoesNotOccludeFoundationFull()
        {
            WallPresentationHeight height =
                WallPresentationHeightResolver.Resolve(
                    WallDisplayMode.Cutaway,
                    wallOccludesFoundation: false);

            Assert.That(
                height,
                Is.EqualTo(WallPresentationHeight.Full));
        }


        [Test]
        public void WallDisplayModeCycle_VisitsAllModesAndWraps()
        {
            WallDisplayMode cutaway =
                WallDisplayModeCycle.Next(
                    WallDisplayMode.WallsUp);

            WallDisplayMode wallsDown =
                WallDisplayModeCycle.Next(
                    cutaway);

            WallDisplayMode wallsUp =
                WallDisplayModeCycle.Next(
                    wallsDown);

            Assert.That(cutaway, Is.EqualTo(WallDisplayMode.Cutaway));
            Assert.That(wallsDown, Is.EqualTo(WallDisplayMode.WallsDown));
            Assert.That(wallsUp, Is.EqualTo(WallDisplayMode.WallsUp));
        }


        private static IsometricViewProjection CreateProjection(
            IsometricViewOrientation orientation)
        {
            return new IsometricViewProjection(
                Footprint,
                orientation);
        }
    }


    internal static class WallPresentationSelectionTestExtensions
    {
        public static WallPresentationSelection SelectWallPresentation(
            this IsometricViewProjection projection,
            CellEdge logicalEdge)
        {
            return WallPresentationSelector.Select(
                logicalEdge,
                projection);
        }
    }
}
