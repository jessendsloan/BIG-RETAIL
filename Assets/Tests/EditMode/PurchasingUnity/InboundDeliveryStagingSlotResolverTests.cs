using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class InboundDeliveryStagingSlotResolverTests
    {
        [Test]
        public void Resolve_PrefersAContiguousFrontCurbStrip()
        {
            GridPosition propertyMinimum = new GridPosition(10, 20);
            List<GridPosition> mapCells = new List<GridPosition>
            {
                propertyMinimum,
                propertyMinimum.Offset(2, -1),
                propertyMinimum.Offset(3, -1),
                propertyMinimum.Offset(4, -1),
                propertyMinimum.Offset(-1, 2)
            };
            GridMapDefinition map =
                new GridMapDefinition("test.delivery.staging", mapCells);

            IReadOnlyList<GridPosition> slots =
                InboundDeliveryStagingSlotResolver.Resolve(
                    map,
                    propertyMinimum,
                    maximumSlotCount: 3);

            Assert.That(slots, Has.Count.EqualTo(3));
            Assert.That(slots[0], Is.EqualTo(propertyMinimum.Offset(2, -1)));
            Assert.That(slots[1], Is.EqualTo(propertyMinimum.Offset(3, -1)));
            Assert.That(slots[2], Is.EqualTo(propertyMinimum.Offset(4, -1)));
        }

        [Test]
        public void Resolve_FallsBackToTheOtherStreetEdge()
        {
            GridPosition propertyMinimum = new GridPosition(-5, -8);
            GridMapDefinition map =
                new GridMapDefinition(
                    "test.delivery.corner",
                    new[]
                    {
                        propertyMinimum,
                        propertyMinimum.Offset(-1, 2),
                        propertyMinimum.Offset(-1, 3)
                    });

            IReadOnlyList<GridPosition> slots =
                InboundDeliveryStagingSlotResolver.Resolve(
                    map,
                    propertyMinimum,
                    maximumSlotCount: 2);

            Assert.That(slots, Has.Count.EqualTo(2));
            Assert.That(slots[0], Is.EqualTo(propertyMinimum.Offset(-1, 2)));
            Assert.That(slots[1], Is.EqualTo(propertyMinimum.Offset(-1, 3)));
        }
    }
}
