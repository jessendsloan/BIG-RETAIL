using System;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FixtureMerchandisingSlotLayoutSetTests
    {
        [Test]
        public void ProductAnchors_AreAddressedByShelfThenVisualFrontage()
        {
            FixtureMerchandisingSlotLayoutSet layout =
                JsonUtility.FromJson<FixtureMerchandisingSlotLayoutSet>(
                    "{\"localDisplaySide\":2,"
                    + "\"northProductAnchors\":["
                    + "{\"x\":1,\"y\":2},{\"x\":3,\"y\":4},"
                    + "{\"x\":5,\"y\":6},{\"x\":7,\"y\":8}]}"
                );

            Assert.That(
                layout.TryGetProductAnchor(
                    FixtureOrientation.North,
                    IsometricViewOrientation.North,
                    shelfIndex: 1,
                    visualFrontageIndex: 0,
                    frontageUnitsPerShelf: 2,
                    out Vector2 anchor),
                Is.True);
            Assert.That(anchor, Is.EqualTo(new Vector2(5f, 6f)));
        }


        [Test]
        public void ProductAnchors_FollowFixturePresentationDirectionSelection()
        {
            FixtureMerchandisingSlotLayoutSet layout =
                JsonUtility.FromJson<FixtureMerchandisingSlotLayoutSet>(
                    "{\"localDisplaySide\":2,"
                    + "\"westProductAnchors\":[{\"x\":9,\"y\":10}]}"
                );

            Assert.That(
                layout.TryGetProductAnchor(
                    FixtureOrientation.North,
                    IsometricViewOrientation.East,
                    shelfIndex: 0,
                    visualFrontageIndex: 0,
                    frontageUnitsPerShelf: 1,
                    out Vector2 anchor),
                Is.True);
            Assert.That(anchor, Is.EqualTo(new Vector2(9f, 10f)));
        }


        [Test]
        public void Validation_RejectsPartiallyAuthoredDirectionalLayout()
        {
            FixtureMerchandisingSlotLayoutSet layout =
                JsonUtility.FromJson<FixtureMerchandisingSlotLayoutSet>(
                    "{\"localDisplaySide\":2,"
                    + "\"northProductAnchors\":[{\"x\":1,\"y\":2}]}"
                );
            FixtureMerchandisingProfile profile =
                new FixtureMerchandisingProfile(
                    new[]
                    {
                        new FixtureDisplayFaceDefinition(
                            FixtureSide.South,
                            shelfRunCount: 3,
                            frontageUnitsPerRun: 5)
                    });

            Assert.That(
                () => layout.ValidateConfiguration(
                    "Test Shelf",
                    profile),
                Throws.TypeOf<InvalidOperationException>());
        }
    }
}
