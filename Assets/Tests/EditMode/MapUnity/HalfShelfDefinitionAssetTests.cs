using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.View;
using NUnit.Framework;

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
    }
}
