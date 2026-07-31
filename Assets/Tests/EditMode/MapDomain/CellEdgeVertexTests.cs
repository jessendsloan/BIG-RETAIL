using System;
using NUnit.Framework;

namespace BigRetail.Map.Domain.Tests
{
    public sealed class CellEdgeVertexTests
    {
        [Test]
        public void NorthEastEdge_ExposesYAlignedEndpoints()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(5, 8, 2),
                    CellEdgeDirection.NorthEast);

            Assert.That(
                edge.FirstVertex,
                Is.EqualTo(
                    new GridVertex(5, 7, 2)));

            Assert.That(
                edge.SecondVertex,
                Is.EqualTo(
                    new GridVertex(5, 8, 2)));
        }


        [Test]
        public void NorthWestEdge_ExposesXAlignedEndpoints()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(5, 8, 2),
                    CellEdgeDirection.NorthWest);

            Assert.That(
                edge.FirstVertex,
                Is.EqualTo(
                    new GridVertex(4, 8, 2)));

            Assert.That(
                edge.SecondVertex,
                Is.EqualTo(
                    new GridVertex(5, 8, 2)));
        }


        [Test]
        public void YAdjacentVertices_CreateNorthEastEdge()
        {
            CellEdge edge =
                new CellEdge(
                    new GridVertex(5, 7, 2),
                    new GridVertex(5, 8, 2));

            Assert.That(
                edge.AnchorCell,
                Is.EqualTo(
                    new GridPosition(5, 8, 2)));

            Assert.That(
                edge.CanonicalDirection,
                Is.EqualTo(
                    CellEdgeDirection.NorthEast));
        }


        [Test]
        public void XAdjacentVertices_CreateNorthWestEdge()
        {
            CellEdge edge =
                new CellEdge(
                    new GridVertex(4, 8, 2),
                    new GridVertex(5, 8, 2));

            Assert.That(
                edge.AnchorCell,
                Is.EqualTo(
                    new GridPosition(5, 8, 2)));

            Assert.That(
                edge.CanonicalDirection,
                Is.EqualTo(
                    CellEdgeDirection.NorthWest));
        }


        [Test]
        public void ReversedVertexOrder_CreatesSameCanonicalEdge()
        {
            GridVertex first =
                new GridVertex(5, 7, 2);

            GridVertex second =
                new GridVertex(5, 8, 2);

            CellEdge forward =
                new CellEdge(first, second);

            CellEdge reverse =
                new CellEdge(second, first);

            Assert.That(reverse, Is.EqualTo(forward));
            Assert.That(forward.TouchesVertex(first), Is.True);
            Assert.That(forward.TouchesVertex(second), Is.True);
        }


        [Test]
        public void DiagonalVertices_AreRejected()
        {
            Assert.Throws<ArgumentException>(
                () => new CellEdge(
                    new GridVertex(2, 3),
                    new GridVertex(3, 4)));
        }


        [Test]
        public void VerticesOnDifferentLevels_AreRejected()
        {
            Assert.Throws<ArgumentException>(
                () => new CellEdge(
                    new GridVertex(2, 3, 0),
                    new GridVertex(2, 4, 1)));
        }
    }
}
