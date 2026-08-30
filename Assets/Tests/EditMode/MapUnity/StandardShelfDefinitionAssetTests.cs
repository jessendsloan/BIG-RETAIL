using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class StandardShelfDefinitionAssetTests
    {
        private const string AssetPath =
            "Assets/Design/Fixtures/StandardShelf.asset";


        [Test]
        public void AuthoredStandardShelf_DefinesTwoIndependentDisplayFaces()
        {
            FixtureDefinition definition =
                LoadAsset().CreateDomainDefinition();

            Assert.That(
                definition.MerchandisingProfile.DisplayFaceCount,
                Is.EqualTo(2));

            AssertFace(definition, FixtureSide.North);
            AssertFace(definition, FixtureSide.South);

            int totalSlots =
                definition.MerchandisingProfile.DisplayFaceCount
                * FixtureMerchandisingProfile.StandardShelfRunCount
                * FixtureMerchandisingProfile.StandardFrontageUnitsPerRun;

            Assert.That(totalSlots, Is.EqualTo(30));
        }

        [TestCase(
            IsometricViewOrientation.North,
            FixtureSide.South,
            FixtureSide.North,
            "Fixture_2x1_StandardShelf01_RisingRight")]
        [TestCase(
            IsometricViewOrientation.East,
            FixtureSide.South,
            FixtureSide.North,
            "Fixture_2x1_StandardShelf01_RisingLeft")]
        [TestCase(
            IsometricViewOrientation.South,
            FixtureSide.North,
            FixtureSide.South,
            "Fixture_2x1_StandardShelf01_RisingRight")]
        [TestCase(
            IsometricViewOrientation.West,
            FixtureSide.North,
            FixtureSide.South,
            "Fixture_2x1_StandardShelf01_RisingLeft")]
        public void AuthoredStandardShelf_CameraViewSelectsStableLogicalFace(
            IsometricViewOrientation viewOrientation,
            FixtureSide visibleSide,
            FixtureSide hiddenSide,
            string expectedDirectionName)
        {
            FixtureDefinitionAsset asset = LoadAsset();

            IReadOnlyList<Sprite> visibleMasks =
                asset.GetMerchandisingShelfMasks(
                    visibleSide,
                    FixtureOrientation.North,
                    viewOrientation);

            Assert.That(visibleMasks.Count, Is.EqualTo(3));
            Assert.That(
                visibleMasks[0].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("ShelfMask01_Top"));
            Assert.That(
                visibleMasks[1].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("ShelfMask02_Middle"));
            Assert.That(
                visibleMasks[2].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("ShelfMask03_Bottom"));

            Assert.That(
                asset.GetMerchandisingShelfMasks(
                    hiddenSide,
                    FixtureOrientation.North,
                    viewOrientation),
                Is.Empty);
        }

        [Test]
        public void AuthoredStandardShelf_MasksPassDefinitionValidation()
        {
            Assert.That(
                () => LoadAsset().ValidateConfiguration(),
                Throws.Nothing);
        }


        private static void AssertFace(
            FixtureDefinition definition,
            FixtureSide side)
        {
            Assert.That(
                definition.MerchandisingProfile.TryGetDisplayFace(
                    side,
                    out FixtureDisplayFaceDefinition face),
                Is.True);
            Assert.That(
                face.ShelfRunCount,
                Is.EqualTo(3));
            Assert.That(
                face.FrontageUnitsPerRun,
                Is.EqualTo(5));
        }

        private static FixtureDefinitionAsset LoadAsset()
        {
            FixtureDefinitionAsset asset =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    FixtureDefinitionAsset>(AssetPath);

            Assert.That(asset, Is.Not.Null);
            return asset;
        }
    }
}
