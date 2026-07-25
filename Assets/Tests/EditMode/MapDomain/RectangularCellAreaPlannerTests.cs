using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Domain.Tests
{
    public sealed class RectangularCellAreaPlannerTests
    {
        [Test]
        public void Plan_SameCell_ReturnsOneCell()
        {
            GridPosition cell =
                new GridPosition(
                    4,
                    7,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    cell,
                    cell);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.Width,
                Is.EqualTo(1));

            Assert.That(
                result.Height,
                Is.EqualTo(1));

            Assert.That(
                result.CellCount,
                Is.EqualTo(1));

            Assert.That(
                result.Cells[0],
                Is.EqualTo(cell));
        }


        [Test]
        public void Plan_IncreasingCorners_ReturnsInclusiveRectangle()
        {
            GridPosition start =
                new GridPosition(
                    2,
                    3,
                    0);

            GridPosition end =
                new GridPosition(
                    4,
                    5,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.MinimumX,
                Is.EqualTo(2));

            Assert.That(
                result.MaximumX,
                Is.EqualTo(4));

            Assert.That(
                result.MinimumY,
                Is.EqualTo(3));

            Assert.That(
                result.MaximumY,
                Is.EqualTo(5));

            Assert.That(
                result.Width,
                Is.EqualTo(3));

            Assert.That(
                result.Height,
                Is.EqualTo(3));

            Assert.That(
                result.CellCount,
                Is.EqualTo(9));

            Assert.That(
                result.Cells[0],
                Is.EqualTo(
                    new GridPosition(
                        2,
                        3,
                        0)));

            Assert.That(
                result.Cells[8],
                Is.EqualTo(
                    new GridPosition(
                        4,
                        5,
                        0)));
        }


        [Test]
        public void Plan_ReversedCorners_ReturnsSameNormalizedOrder()
        {
            GridPosition first =
                new GridPosition(
                    2,
                    3,
                    0);

            GridPosition second =
                new GridPosition(
                    4,
                    5,
                    0);

            RectangularCellAreaPlanResult forward =
                RectangularCellAreaPlanner.Plan(
                    first,
                    second);

            RectangularCellAreaPlanResult reverse =
                RectangularCellAreaPlanner.Plan(
                    second,
                    first);

            Assert.That(
                forward.Succeeded,
                Is.True);

            Assert.That(
                reverse.Succeeded,
                Is.True);

            Assert.That(
                reverse.CellCount,
                Is.EqualTo(
                    forward.CellCount));

            for (int index = 0;
                 index < forward.CellCount;
                 index++)
            {
                Assert.That(
                    reverse.Cells[index],
                    Is.EqualTo(
                        forward.Cells[index]));
            }
        }


        [Test]
        public void Plan_HorizontalSelection_ReturnsXLine()
        {
            GridPosition start =
                new GridPosition(
                    2,
                    6,
                    0);

            GridPosition end =
                new GridPosition(
                    5,
                    6,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.Width,
                Is.EqualTo(4));

            Assert.That(
                result.Height,
                Is.EqualTo(1));

            Assert.That(
                result.CellCount,
                Is.EqualTo(4));

            for (int index = 0;
                 index < result.CellCount;
                 index++)
            {
                Assert.That(
                    result.Cells[index],
                    Is.EqualTo(
                        new GridPosition(
                            2 + index,
                            6,
                            0)));
            }
        }


        [Test]
        public void Plan_VerticalSelection_ReturnsYLine()
        {
            GridPosition start =
                new GridPosition(
                    3,
                    2,
                    0);

            GridPosition end =
                new GridPosition(
                    3,
                    5,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.Width,
                Is.EqualTo(1));

            Assert.That(
                result.Height,
                Is.EqualTo(4));

            Assert.That(
                result.CellCount,
                Is.EqualTo(4));

            for (int index = 0;
                 index < result.CellCount;
                 index++)
            {
                Assert.That(
                    result.Cells[index],
                    Is.EqualTo(
                        new GridPosition(
                            3,
                            2 + index,
                            0)));
            }
        }


        [Test]
        public void Plan_NegativeCoordinates_AreSupported()
        {
            GridPosition start =
                new GridPosition(
                    -2,
                    -1,
                    0);

            GridPosition end =
                new GridPosition(
                    0,
                    1,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.MinimumX,
                Is.EqualTo(-2));

            Assert.That(
                result.MaximumX,
                Is.EqualTo(0));

            Assert.That(
                result.MinimumY,
                Is.EqualTo(-1));

            Assert.That(
                result.MaximumY,
                Is.EqualTo(1));

            Assert.That(
                result.CellCount,
                Is.EqualTo(9));
        }


        [Test]
        public void Plan_DifferentLevels_IsRejected()
        {
            GridPosition start =
                new GridPosition(
                    2,
                    3,
                    0);

            GridPosition end =
                new GridPosition(
                    4,
                    5,
                    1);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            Assert.That(
                result.Succeeded,
                Is.False);

            Assert.That(
                result.Failure,
                Is.EqualTo(
                    RectangularCellAreaPlanFailure
                        .DifferentLevel));

            Assert.That(
                result.CellCount,
                Is.EqualTo(0));

            Assert.That(
                result.Width,
                Is.EqualTo(0));

            Assert.That(
                result.Height,
                Is.EqualTo(0));
        }


        [Test]
        public void Plan_RectangleContainsEveryCellExactlyOnce()
        {
            GridPosition start =
                new GridPosition(
                    1,
                    4,
                    0);

            GridPosition end =
                new GridPosition(
                    3,
                    5,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            HashSet<GridPosition> uniqueCells =
                new HashSet<GridPosition>();

            for (int index = 0;
                 index < result.CellCount;
                 index++)
            {
                uniqueCells.Add(
                    result.Cells[index]);
            }

            Assert.That(
                result.Succeeded,
                Is.True);

            Assert.That(
                result.CellCount,
                Is.EqualTo(6));

            Assert.That(
                uniqueCells.Count,
                Is.EqualTo(6));

            for (int y = 4; y <= 5; y++)
            {
                for (int x = 1; x <= 3; x++)
                {
                    Assert.That(
                        uniqueCells.Contains(
                            new GridPosition(
                                x,
                                y,
                                0)),
                        Is.True);
                }
            }
        }


        [Test]
        public void Plan_Order_IsYThenX()
        {
            GridPosition start =
                new GridPosition(
                    1,
                    5,
                    0);

            GridPosition end =
                new GridPosition(
                    2,
                    6,
                    0);

            RectangularCellAreaPlanResult result =
                RectangularCellAreaPlanner.Plan(
                    start,
                    end);

            GridPosition[] expectedOrder =
            {
                new GridPosition(1, 5, 0),
                new GridPosition(2, 5, 0),
                new GridPosition(1, 6, 0),
                new GridPosition(2, 6, 0)
            };

            Assert.That(
                result.CellCount,
                Is.EqualTo(
                    expectedOrder.Length));

            for (int index = 0;
                 index < expectedOrder.Length;
                 index++)
            {
                Assert.That(
                    result.Cells[index],
                    Is.EqualTo(
                        expectedOrder[index]));
            }
        }
    }
}