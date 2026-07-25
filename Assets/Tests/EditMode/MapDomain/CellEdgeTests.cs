using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.Map.Domain.Tests
{
    /// <summary>
    /// Locks down the fundamental topology rules used by CellEdge.
    ///
    /// These tests ensure opposite descriptions of one shared edge
    /// normalize into the same value.
    /// </summary>
    public sealed class CellEdgeTests
    {
        [Test]
        public void NorthEast_AndNeighborSouthWest_AreSameEdge()
        {
            GridPosition firstCell =
                new GridPosition(5, 8, 2);

            GridPosition neighboringCell =
                new GridPosition(6, 8, 2);

            CellEdge fromFirstCell =
                new CellEdge(
                    firstCell,
                    CellEdgeDirection.NorthEast);

            CellEdge fromNeighbor =
                new CellEdge(
                    neighboringCell,
                    CellEdgeDirection.SouthWest);

            Assert.That(
                fromNeighbor,
                Is.EqualTo(fromFirstCell));
        }

        [Test]
        public void NorthWest_AndNeighborSouthEast_AreSameEdge()
        {
            GridPosition firstCell =
                new GridPosition(5, 8, 2);

            GridPosition neighboringCell =
                new GridPosition(5, 9, 2);

            CellEdge fromFirstCell =
                new CellEdge(
                    firstCell,
                    CellEdgeDirection.NorthWest);

            CellEdge fromNeighbor =
                new CellEdge(
                    neighboringCell,
                    CellEdgeDirection.SouthEast);

            Assert.That(
                fromNeighbor,
                Is.EqualTo(fromFirstCell));
        }

        [Test]
        public void NorthEast_SecondCell_IsOnePositiveXCellAway()
        {
            GridPosition anchor =
                new GridPosition(5, 8, 2);

            CellEdge edge =
                new CellEdge(
                    anchor,
                    CellEdgeDirection.NorthEast);

            GridPosition expectedNeighbor =
                new GridPosition(6, 8, 2);

            Assert.That(
                edge.FirstCell,
                Is.EqualTo(anchor));

            Assert.That(
                edge.SecondCell,
                Is.EqualTo(expectedNeighbor));

            Assert.That(
                edge.CanonicalDirection,
                Is.EqualTo(CellEdgeDirection.NorthEast));
        }

        [Test]
        public void NorthWest_SecondCell_IsOnePositiveYCellAway()
        {
            GridPosition anchor =
                new GridPosition(5, 8, 2);

            CellEdge edge =
                new CellEdge(
                    anchor,
                    CellEdgeDirection.NorthWest);

            GridPosition expectedNeighbor =
                new GridPosition(5, 9, 2);

            Assert.That(
                edge.FirstCell,
                Is.EqualTo(anchor));

            Assert.That(
                edge.SecondCell,
                Is.EqualTo(expectedNeighbor));

            Assert.That(
                edge.CanonicalDirection,
                Is.EqualTo(CellEdgeDirection.NorthWest));
        }

        [Test]
        public void OppositeDescriptions_CreateOnlyOneHashSetEntry()
        {
            GridPosition firstCell =
                new GridPosition(5, 8);

            GridPosition neighboringCell =
                new GridPosition(6, 8);

            CellEdge fromFirstCell =
                new CellEdge(
                    firstCell,
                    CellEdgeDirection.NorthEast);

            CellEdge fromNeighbor =
                new CellEdge(
                    neighboringCell,
                    CellEdgeDirection.SouthWest);

            HashSet<CellEdge> edges =
                new HashSet<CellEdge>
                {
                    fromFirstCell,
                    fromNeighbor
                };

            Assert.That(
                edges.Count,
                Is.EqualTo(1));
        }
    }
}