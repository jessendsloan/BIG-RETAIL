using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using NUnit.Framework;

namespace BigRetail.Map.View.Tests
{
    public sealed class IsometricViewProjectionTests
    {
        private static readonly IsometricMapFootprint
            RectangularFootprint =
                new IsometricMapFootprint(
                    minimumX: -3,
                    minimumY: 7,
                    maximumX: 1,
                    maximumY: 9,
                    logicalLevel: 2);


        [Test]
        public void FootprintFromMapDefinitionUsesRequestedLevel()
        {
            GridMapDefinition map =
                new GridMapDefinition(
                    "test.map",
                    new[]
                    {
                        new GridPosition(-4, 6, 1),
                        new GridPosition(8, 12, 1),
                        new GridPosition(-3, 7, 2),
                        new GridPosition(1, 9, 2)
                    });

            IsometricMapFootprint footprint =
                IsometricMapFootprint.FromMapDefinition(
                    map,
                    logicalLevel: 2);

            Assert.That(footprint.MinimumX, Is.EqualTo(-3));
            Assert.That(footprint.MinimumY, Is.EqualTo(7));
            Assert.That(footprint.MaximumX, Is.EqualTo(1));
            Assert.That(footprint.MaximumY, Is.EqualTo(9));
            Assert.That(footprint.Width, Is.EqualTo(5));
            Assert.That(footprint.Height, Is.EqualTo(3));
        }


        [Test]
        public void FootprintFromCellsUsesOccupiedRequestedLevelEnvelope()
        {
            IsometricMapFootprint footprint =
                IsometricMapFootprint.FromCells(
                    new[]
                    {
                        new GridPosition(-400, -400, 1),
                        new GridPosition(-263, -112, 0),
                        new GridPosition(-256, -108, 0),
                        new GridPosition(500, 500, 2)
                    },
                    logicalLevel: 0);

            Assert.That(footprint.MinimumX, Is.EqualTo(-263));
            Assert.That(footprint.MinimumY, Is.EqualTo(-112));
            Assert.That(footprint.MaximumX, Is.EqualTo(-256));
            Assert.That(footprint.MaximumY, Is.EqualTo(-108));
            Assert.That(footprint.Width, Is.EqualTo(8));
            Assert.That(footprint.Height, Is.EqualTo(5));
        }


        [Test]
        public void FootprintFromCellsRejectsMissingRequestedLevel()
        {
            Assert.Throws<InvalidOperationException>(
                () => IsometricMapFootprint.FromCells(
                    new[]
                    {
                        new GridPosition(-263, -112, 1)
                    },
                    logicalLevel: 0));
        }


        [TestCase(IsometricViewOrientation.North, 8, 5)]
        [TestCase(IsometricViewOrientation.East, 5, 8)]
        [TestCase(IsometricViewOrientation.South, 8, 5)]
        [TestCase(IsometricViewOrientation.West, 5, 8)]
        public void OccupiedWorldFootprintProjectsAcrossOrientation(
            IsometricViewOrientation orientation,
            int expectedWidth,
            int expectedHeight)
        {
            IsometricMapFootprint footprint =
                IsometricMapFootprint.FromCells(
                    new[]
                    {
                        new GridPosition(-263, -112, 0),
                        new GridPosition(-256, -108, 0)
                    },
                    logicalLevel: 0);

            IsometricViewProjection projection =
                new IsometricViewProjection(
                    footprint,
                    orientation);

            GridPosition logicalCell =
                new GridPosition(
                    -260,
                    -110,
                    0);

            Assert.That(
                projection.DisplayWidth,
                Is.EqualTo(expectedWidth));

            Assert.That(
                projection.DisplayHeight,
                Is.EqualTo(expectedHeight));

            Assert.That(
                projection.ToLogicalCell(
                    projection.ToDisplayCell(
                        logicalCell)),
                Is.EqualTo(logicalCell));
        }


        [TestCase(IsometricViewOrientation.North, -3, 7)]
        [TestCase(IsometricViewOrientation.East, -3, 11)]
        [TestCase(IsometricViewOrientation.South, 1, 9)]
        [TestCase(IsometricViewOrientation.West, -1, 7)]
        public void MinimumLogicalCornerProjectsToExpectedDisplayCell(
            IsometricViewOrientation orientation,
            int expectedX,
            int expectedY)
        {
            IsometricViewProjection projection =
                CreateProjection(
                    orientation);

            GridPosition displayCell =
                projection.ToDisplayCell(
                    new GridPosition(
                        -3,
                        7,
                        2));

            Assert.That(displayCell.X, Is.EqualTo(expectedX));
            Assert.That(displayCell.Y, Is.EqualTo(expectedY));
            Assert.That(displayCell.Level, Is.EqualTo(2));
        }


        [TestCase(IsometricViewOrientation.North, 5, 3)]
        [TestCase(IsometricViewOrientation.East, 3, 5)]
        [TestCase(IsometricViewOrientation.South, 5, 3)]
        [TestCase(IsometricViewOrientation.West, 3, 5)]
        public void DisplayDimensionsMatchOrientation(
            IsometricViewOrientation orientation,
            int expectedWidth,
            int expectedHeight)
        {
            IsometricViewProjection projection =
                CreateProjection(
                    orientation);

            Assert.That(
                projection.DisplayWidth,
                Is.EqualTo(expectedWidth));

            Assert.That(
                projection.DisplayHeight,
                Is.EqualTo(expectedHeight));
        }


        [TestCase(IsometricViewOrientation.North)]
        [TestCase(IsometricViewOrientation.East)]
        [TestCase(IsometricViewOrientation.South)]
        [TestCase(IsometricViewOrientation.West)]
        public void ForwardAndInverseProjectionRoundTripEveryCell(
            IsometricViewOrientation orientation)
        {
            IsometricViewProjection projection =
                CreateProjection(
                    orientation);

            for (int x = RectangularFootprint.MinimumX;
                 x <= RectangularFootprint.MaximumX;
                 x++)
            {
                for (int y = RectangularFootprint.MinimumY;
                     y <= RectangularFootprint.MaximumY;
                     y++)
                {
                    GridPosition logicalCell =
                        new GridPosition(
                            x,
                            y,
                            RectangularFootprint.LogicalLevel);

                    GridPosition roundTrippedCell =
                        projection.ToLogicalCell(
                            projection.ToDisplayCell(
                                logicalCell));

                    Assert.That(
                        roundTrippedCell,
                        Is.EqualTo(logicalCell));
                }
            }
        }


        [Test]
        public void FourClockwiseTurnsReturnToNorth()
        {
            GridPosition logicalCell =
                new GridPosition(
                    -1,
                    8,
                    2);

            IsometricViewProjection projection =
                CreateProjection(
                    IsometricViewOrientation.North);

            GridPosition originalDisplayCell =
                projection.ToDisplayCell(
                    logicalCell);

            for (int turn = 0;
                 turn < 4;
                 turn++)
            {
                projection =
                    projection.WithOrientation(
                        projection.Orientation
                            .RotateClockwise());
            }

            Assert.That(
                projection.Orientation,
                Is.EqualTo(
                    IsometricViewOrientation.North));

            Assert.That(
                projection.ToDisplayCell(
                    logicalCell),
                Is.EqualTo(
                    originalDisplayCell));
        }


        [Test]
        public void ClockwiseThenCounterClockwiseReturnsToStart()
        {
            foreach (
                IsometricViewOrientation orientation
                in EnumerateOrientations())
            {
                Assert.That(
                    orientation
                        .RotateClockwise()
                        .RotateCounterClockwise(),
                    Is.EqualTo(orientation));
            }
        }


        [TestCase(IsometricViewOrientation.North)]
        [TestCase(IsometricViewOrientation.East)]
        [TestCase(IsometricViewOrientation.South)]
        [TestCase(IsometricViewOrientation.West)]
        public void ProjectedEdgeStillTouchesProjectedCells(
            IsometricViewOrientation orientation)
        {
            IsometricViewProjection projection =
                CreateProjection(
                    orientation);

            CellEdge logicalEdge =
                new CellEdge(
                    new GridPosition(
                        -1,
                        8,
                        2),
                    CellEdgeDirection.NorthEast);

            CellEdge displayEdge =
                projection.ToDisplayEdge(
                    logicalEdge);

            Assert.That(
                displayEdge.TouchesCell(
                    projection.ToDisplayCell(
                        logicalEdge.FirstCell)),
                Is.True);

            Assert.That(
                displayEdge.TouchesCell(
                    projection.ToDisplayCell(
                        logicalEdge.SecondCell)),
                Is.True);
        }


        [Test]
        public void ViewerFacingWallCellChangesWithOppositeView()
        {
            CellEdge logicalEdge =
                new CellEdge(
                    new GridPosition(
                        -1,
                        8,
                        2),
                    CellEdgeDirection.NorthEast);

            GridPosition northFacingCell =
                CreateProjection(
                    IsometricViewOrientation.North)
                    .GetViewerFacingCell(
                        logicalEdge);

            GridPosition southFacingCell =
                CreateProjection(
                    IsometricViewOrientation.South)
                    .GetViewerFacingCell(
                        logicalEdge);

            Assert.That(
                northFacingCell,
                Is.Not.EqualTo(
                    southFacingCell));

            Assert.That(
                logicalEdge.TouchesCell(
                    northFacingCell),
                Is.True);

            Assert.That(
                logicalEdge.TouchesCell(
                    southFacingCell),
                Is.True);
        }


        [TestCase(IsometricViewOrientation.North)]
        [TestCase(IsometricViewOrientation.East)]
        [TestCase(IsometricViewOrientation.South)]
        [TestCase(IsometricViewOrientation.West)]
        public void CellsOutsideFootprintStillRoundTripForTargetRejection(
            IsometricViewOrientation orientation)
        {
            IsometricViewProjection projection =
                CreateProjection(
                    orientation);

            GridPosition outsideCell =
                new GridPosition(
                    -20,
                    40,
                    2);

            Assert.That(
                projection.ToLogicalCell(
                    projection.ToDisplayCell(
                        outsideCell)),
                Is.EqualTo(outsideCell));
        }


        private static IsometricViewProjection CreateProjection(
            IsometricViewOrientation orientation)
        {
            return new IsometricViewProjection(
                RectangularFootprint,
                orientation);
        }


        private static IEnumerable<
            IsometricViewOrientation>
            EnumerateOrientations()
        {
            yield return IsometricViewOrientation.North;
            yield return IsometricViewOrientation.East;
            yield return IsometricViewOrientation.South;
            yield return IsometricViewOrientation.West;
        }
    }
}
