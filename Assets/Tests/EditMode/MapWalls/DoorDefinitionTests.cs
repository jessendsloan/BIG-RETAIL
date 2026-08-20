using System;
using System.Collections.Generic;
using BigRetail.Map.Walls;
using NUnit.Framework;

namespace BigRetail.Map.Walls.Tests
{
    public sealed class DoorDefinitionTests
    {
        [Test]
        public void FourPanelDefinition_IdentifiesTwoMiddlePassages()
        {
            DoorDefinition definition =
                new DoorDefinition(
                    new DoorDefinitionId("automatic-front"),
                    4,
                    new[] { 1, 2 });

            Assert.That(definition.SegmentCount, Is.EqualTo(4));
            Assert.That(definition.PassageSegmentCount, Is.EqualTo(2));
            Assert.That(definition.IsPassageSegment(0), Is.False);
            Assert.That(definition.IsPassageSegment(1), Is.True);
            Assert.That(definition.IsPassageSegment(2), Is.True);
            Assert.That(definition.IsPassageSegment(3), Is.False);
        }


        [Test]
        public void FixedWindowDefinition_HasNoPassageSegments()
        {
            DoorDefinition definition =
                new DoorDefinition(
                    new DoorDefinitionId("fixed-window"),
                    1,
                    Array.Empty<int>());

            Assert.That(definition.SegmentCount, Is.EqualTo(1));
            Assert.That(definition.PassageSegmentCount, Is.EqualTo(0));
            Assert.That(definition.IsPassageSegment(0), Is.False);
        }


        [Test]
        public void Constructor_DuplicatePassageIndex_IsRejected()
        {
            Assert.That(
                () => new DoorDefinition(
                    new DoorDefinitionId("automatic-front"),
                    4,
                    new[] { 1, 1 }),
                Throws.TypeOf<ArgumentException>());
        }


        [Test]
        public void Constructor_PassageOutsideSpan_IsRejected()
        {
            Assert.That(
                () => new DoorDefinition(
                    new DoorDefinitionId("automatic-front"),
                    4,
                    new[] { 1, 4 }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }


        [Test]
        public void Catalog_DuplicateDefinitionId_IsRejected()
        {
            DoorDefinition first =
                new DoorDefinition(
                    new DoorDefinitionId("single"),
                    1,
                    new[] { 0 });

            DoorDefinition duplicate =
                new DoorDefinition(
                    new DoorDefinitionId(" SINGLE "),
                    1,
                    new[] { 0 });

            Assert.That(
                () => new DoorDefinitionCatalog(
                    new List<DoorDefinition>
                    {
                        first,
                        duplicate
                    }),
                Throws.TypeOf<ArgumentException>());
        }
    }
}
