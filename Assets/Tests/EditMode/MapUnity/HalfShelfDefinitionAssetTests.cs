using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class HalfShelfDefinitionAssetTests
    {
        private const string AssetPath =
            "Assets/Design/Fixtures/HalfShelf.asset";


        [TestCase(
            IsometricViewOrientation.North,
            "Fixture_2x1_HalfShelf01_RisingRight",
            FixtureSpriteAnchorCorner.ViewerBackLeft)]
        [TestCase(
            IsometricViewOrientation.East,
            "Fixture_2x1_HalfShelf01_RisingLeft",
            FixtureSpriteAnchorCorner.ViewerBackRight)]
        [TestCase(
            IsometricViewOrientation.South,
            "Fixture_2x1_HalfShelf01_Back_RisingRight",
            FixtureSpriteAnchorCorner.ViewerNearest)]
        [TestCase(
            IsometricViewOrientation.West,
            "Fixture_2x1_HalfShelf01_Back_RisingLeft",
            FixtureSpriteAnchorCorner.ViewerNearest)]
        public void AuthoredHalfShelf_RetainsApprovedCameraRotationSequence(
            IsometricViewOrientation viewOrientation,
            string expectedSpriteName,
            FixtureSpriteAnchorCorner expectedAnchorCorner)
        {
            FixtureDefinitionAsset asset =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    FixtureDefinitionAsset>(AssetPath);

            Assert.That(asset, Is.Not.Null);

            Assert.That(
                asset.GetSprite(
                    FixtureOrientation.North,
                    viewOrientation).name,
                Is.EqualTo(expectedSpriteName));

            Assert.That(
                asset.GetSpriteAnchorCorner(
                    FixtureOrientation.North,
                    viewOrientation),
                Is.EqualTo(expectedAnchorCorner));
        }


        [TestCase(
            IsometricViewOrientation.North,
            "Fixture_2x1_HalfShelf01_RisingRight")]
        [TestCase(
            IsometricViewOrientation.East,
            "Fixture_2x1_HalfShelf01_RisingLeft")]
        public void AuthoredHalfShelf_FrontViewsUseThreeAlignedShelfMasks(
            IsometricViewOrientation viewOrientation,
            string expectedDirectionName)
        {
            FixtureDefinitionAsset asset = LoadAsset();

            Assert.That(
                asset.HasMerchandisingShelfMasks(FixtureSide.South),
                Is.True);

            IReadOnlyList<Sprite> shelfMasks =
                asset.GetMerchandisingShelfMasks(
                    FixtureSide.South,
                    FixtureOrientation.North,
                    viewOrientation);

            Assert.That(shelfMasks, Has.Count.EqualTo(3));
            Assert.That(
                shelfMasks[0].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("ShelfMask01_Top"));
            Assert.That(
                shelfMasks[1].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("ShelfMask02_Middle"));
            Assert.That(
                shelfMasks[2].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("ShelfMask03_Bottom"));

            Assert.That(
                shelfMasks[0].rect,
                Is.EqualTo(
                    asset.GetSprite(
                        FixtureOrientation.North,
                        viewOrientation).rect));
        }


        [TestCase(IsometricViewOrientation.South)]
        [TestCase(IsometricViewOrientation.West)]
        public void AuthoredHalfShelf_BackViewsHideShelfEditingSurfaces(
            IsometricViewOrientation viewOrientation)
        {
            FixtureDefinitionAsset asset = LoadAsset();

            Assert.That(
                asset.GetMerchandisingShelfMasks(
                    FixtureSide.South,
                    FixtureOrientation.North,
                    viewOrientation),
                Is.Empty);
        }


        [Test]
        public void AuthoredHalfShelf_MasksPassDefinitionValidation()
        {
            Assert.That(
                () => LoadAsset().ValidateConfiguration(),
                Throws.Nothing);
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
