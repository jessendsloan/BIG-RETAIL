using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using NUnit.Framework;

namespace BigRetail.Map.View.Tests
{
    public sealed class FoundationCutawayMapTests
    {
        private const int LogicalLevel = 0;

        private static readonly IsometricMapFootprint Footprint =
            new IsometricMapFootprint(
                minimumX: 0,
                minimumY: 0,
                maximumX: 6,
                maximumY: 6,
                logicalLevel: LogicalLevel);


        [Test]
        public void Calculate_RejectsMissingInputs()
        {
            IsometricViewProjection projection =
                CreateProjection(
                    IsometricViewOrientation.North);

            Assert.Throws<ArgumentNullException>(
                () => FoundationCutawayMap.Calculate(
                    null,
                    Array.Empty<GridPosition>()));

            Assert.Throws<ArgumentNullException>(
                () => FoundationCutawayMap.Calculate(
                    projection,
                    null));

            Assert.Throws<ArgumentNullException>(
                () => FoundationCutawayMap.Calculate(
                    projection,
                    Array.Empty<GridPosition>(),
                    null));
        }


        [Test]
        public void ShouldLowerWall_LowersFrontPerimeterWall()
        {
            GridPosition foundation =
                Cell(
                    2,
                    2);

            FoundationCutawayMap cutawayMap =
                CalculateNorth(
                    foundation);

            CellEdge frontWall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.SouthWest);

            Assert.That(
                cutawayMap.ShouldLowerWall(frontWall),
                Is.True);
        }


        [Test]
        public void ShouldLowerWall_KeepsRearPerimeterWallFull()
        {
            GridPosition foundation =
                Cell(
                    2,
                    2);

            FoundationCutawayMap cutawayMap =
                CalculateNorth(
                    foundation);

            CellEdge rearWall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.NorthEast);

            Assert.That(
                cutawayMap.ShouldLowerWall(rearWall),
                Is.False);
        }


        [Test]
        public void ShouldLowerWall_LowersInteriorPartition()
        {
            GridPosition nearFoundation =
                Cell(
                    2,
                    2);

            GridPosition farFoundation =
                Cell(
                    3,
                    2);

            FoundationCutawayMap cutawayMap =
                CalculateNorth(
                    nearFoundation,
                    farFoundation);

            CellEdge interiorWall =
                new CellEdge(
                    nearFoundation,
                    CellEdgeDirection.NorthEast);

            Assert.That(
                cutawayMap.ShouldLowerWall(interiorWall),
                Is.True);
        }


        [Test]
        public void ShouldLowerWall_LShapeScansAcrossApronGap()
        {
            FoundationCutawayMap cutawayMap =
                CalculateNorth(
                    CreateLFoundation());

            CellEdge concaveBaseWall =
                new CellEdge(
                    Cell(
                        2,
                        1),
                    CellEdgeDirection.NorthWest);

            CellEdge rearLegWall =
                new CellEdge(
                    Cell(
                        4,
                        4),
                    CellEdgeDirection.NorthWest);

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    concaveBaseWall),
                Is.True,
                "The scan must continue through the empty/apron notch "
                + "to the upright leg of the L.");

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    rearLegWall),
                Is.False,
                "The final rear boundary has no foundation farther back.");
        }


        [Test]
        public void ShouldLowerWall_IgnoresFoundationInDifferentLane()
        {
            GridPosition wallFoundation =
                Cell(
                    2,
                    2);

            FoundationCutawayMap cutawayMap =
                CalculateNorth(
                    wallFoundation,
                    Cell(
                        5,
                        1));

            CellEdge rearWall =
                new CellEdge(
                    wallFoundation,
                    CellEdgeDirection.NorthEast);

            Assert.That(
                cutawayMap.ShouldLowerWall(rearWall),
                Is.False);
        }


        [Test]
        public void ShouldLowerWall_OppositeViewReevaluatesRearWall()
        {
            GridPosition foundation =
                Cell(
                    2,
                    2);

            CellEdge wall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.NorthEast);

            FoundationCutawayMap northMap =
                FoundationCutawayMap.Calculate(
                    CreateProjection(
                        IsometricViewOrientation.North),
                    new[] { foundation });

            FoundationCutawayMap southMap =
                FoundationCutawayMap.Calculate(
                    CreateProjection(
                        IsometricViewOrientation.South),
                    new[] { foundation });

            Assert.That(
                northMap.ShouldLowerWall(wall),
                Is.False);

            Assert.That(
                southMap.ShouldLowerWall(wall),
                Is.True);
        }


        [Test]
        public void ShouldLowerWall_LowersFullCornerCapBetweenLowWalls()
        {
            GridPosition cornerFoundation =
                Cell(
                    2,
                    2);

            GridPosition foundationBehindSecondNeighbor =
                Cell(
                    2,
                    3);

            CellEdge cornerCap =
                new CellEdge(
                    cornerFoundation,
                    CellEdgeDirection.NorthEast);

            CellEdge firstLowNeighbor =
                new CellEdge(
                    cornerFoundation,
                    CellEdgeDirection.SouthEast);

            CellEdge secondLowNeighbor =
                new CellEdge(
                    cornerFoundation,
                    CellEdgeDirection.NorthWest);

            FoundationCutawayMap cutawayMap =
                FoundationCutawayMap.Calculate(
                    CreateProjection(
                        IsometricViewOrientation.North),
                    new[]
                    {
                        cornerFoundation,
                        foundationBehindSecondNeighbor
                    },
                    new[]
                    {
                        cornerCap,
                        firstLowNeighbor,
                        secondLowNeighbor
                    });

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    firstLowNeighbor),
                Is.True);

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    secondLowNeighbor),
                Is.True);

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    cornerCap),
                Is.True,
                "A full panel joined to low walls at both endpoints "
                + "would render as an isolated vertical tooth.");
        }


        [Test]
        public void ShouldLowerWall_PreservesConnectedRearCorner()
        {
            GridPosition foundation =
                Cell(
                    2,
                    2);

            CellEdge firstRearWall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.NorthEast);

            CellEdge secondRearWall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.NorthWest);

            CellEdge firstFrontWall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.SouthEast);

            CellEdge secondFrontWall =
                new CellEdge(
                    foundation,
                    CellEdgeDirection.SouthWest);

            FoundationCutawayMap cutawayMap =
                FoundationCutawayMap.Calculate(
                    CreateProjection(
                        IsometricViewOrientation.North),
                    new[] { foundation },
                    new[]
                    {
                        firstRearWall,
                        secondRearWall,
                        firstFrontWall,
                        secondFrontWall
                    });

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    firstRearWall),
                Is.False);

            Assert.That(
                cutawayMap.ShouldLowerWall(
                    secondRearWall),
                Is.False);
        }


        private static FoundationCutawayMap CalculateNorth(
            params GridPosition[] foundationCells)
        {
            return FoundationCutawayMap.Calculate(
                CreateProjection(
                    IsometricViewOrientation.North),
                foundationCells);
        }


        private static FoundationCutawayMap CalculateNorth(
            IEnumerable<GridPosition> foundationCells)
        {
            return FoundationCutawayMap.Calculate(
                CreateProjection(
                    IsometricViewOrientation.North),
                foundationCells);
        }


        private static IReadOnlyList<GridPosition>
            CreateLFoundation()
        {
            List<GridPosition> foundations =
                new List<GridPosition>();

            for (int x = 1;
                 x <= 4;
                 x++)
            {
                foundations.Add(
                    Cell(
                        x,
                        1));
            }

            for (int y = 2;
                 y <= 4;
                 y++)
            {
                foundations.Add(
                    Cell(
                        4,
                        y));
            }

            return foundations;
        }


        private static IsometricViewProjection CreateProjection(
            IsometricViewOrientation orientation)
        {
            return new IsometricViewProjection(
                Footprint,
                orientation);
        }


        private static GridPosition Cell(
            int x,
            int y)
        {
            return new GridPosition(
                x,
                y,
                LogicalLevel);
        }
    }
}
