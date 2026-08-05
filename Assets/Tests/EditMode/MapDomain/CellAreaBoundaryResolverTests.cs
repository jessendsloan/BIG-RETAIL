using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.Map.Domain.Tests
{
    public sealed class CellAreaBoundaryResolverTests
    {
        [Test]
        public void Resolve_SingleCell_ReturnsAllFourEdges()
        {
            GridPosition cell =
                new GridPosition(2, 3);

            IReadOnlyList<CellEdge> result =
                CellAreaBoundaryResolver.Resolve(
                    new[] { cell });

            Assert.That(
                result,
                Is.EquivalentTo(
                    new[]
                    {
                        new CellEdge(
                            cell,
                            CellEdgeDirection.NorthWest),
                        new CellEdge(
                            cell,
                            CellEdgeDirection.NorthEast),
                        new CellEdge(
                            cell,
                            CellEdgeDirection.SouthEast),
                        new CellEdge(
                            cell,
                            CellEdgeDirection.SouthWest)
                    }));
        }


        [Test]
        public void Resolve_TwoByTwoArea_RemovesAllInternalEdges()
        {
            GridPosition[] cells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1)
            };

            IReadOnlyList<CellEdge> result =
                CellAreaBoundaryResolver.Resolve(cells);

            Assert.That(
                result,
                Has.Count.EqualTo(8));

            Assert.That(
                result,
                Has.None.EqualTo(
                    new CellEdge(
                        cells[0],
                        CellEdgeDirection.NorthEast)));

            Assert.That(
                result,
                Has.None.EqualTo(
                    new CellEdge(
                        cells[0],
                        CellEdgeDirection.NorthWest)));
        }


        [Test]
        public void Resolve_LShape_FollowsConcaveOuterBoundary()
        {
            IReadOnlyList<CellEdge> result =
                CellAreaBoundaryResolver.Resolve(
                    new[]
                    {
                        new GridPosition(0, 0),
                        new GridPosition(1, 0),
                        new GridPosition(0, 1)
                    });

            Assert.That(
                result,
                Has.Count.EqualTo(8));
        }


        [Test]
        public void Resolve_DuplicateCells_DoNotCancelTheirOwnEdges()
        {
            GridPosition cell =
                new GridPosition(2, 3);

            IReadOnlyList<CellEdge> result =
                CellAreaBoundaryResolver.Resolve(
                    new[]
                    {
                        cell,
                        cell
                    });

            Assert.That(
                result,
                Has.Count.EqualTo(4));
        }


        [Test]
        public void Resolve_NullCells_Throws()
        {
            Assert.That(
                () => CellAreaBoundaryResolver.Resolve(null),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}
