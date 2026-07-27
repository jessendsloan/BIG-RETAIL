using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.Map.Domain.Tests
{
    public sealed class GridVertexTests
    {
        [Test]
        public void EqualCoordinates_AreEqual()
        {
            GridVertex first =
                new GridVertex(4, 7, 2);

            GridVertex second =
                new GridVertex(4, 7, 2);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(second == first, Is.True);
        }


        [Test]
        public void DifferentLevel_IsDifferentVertex()
        {
            GridVertex first =
                new GridVertex(4, 7, 1);

            GridVertex second =
                new GridVertex(4, 7, 2);

            Assert.That(second, Is.Not.EqualTo(first));
        }


        [Test]
        public void Offset_CreatesRelativeVertex()
        {
            GridVertex vertex =
                new GridVertex(4, 7, 2);

            GridVertex offset =
                vertex.Offset(-2, 3, 1);

            Assert.That(
                offset,
                Is.EqualTo(
                    new GridVertex(2, 10, 3)));
        }


        [Test]
        public void EqualVertices_CreateOneHashSetEntry()
        {
            HashSet<GridVertex> vertices =
                new HashSet<GridVertex>
                {
                    new GridVertex(4, 7, 2),
                    new GridVertex(4, 7, 2)
                };

            Assert.That(vertices.Count, Is.EqualTo(1));
        }
    }
}
