using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class BackstockShelfDefinitionAssetTests
    {
        private const string AssetPath =
            "Assets/Design/Fixtures/BackstockShelf.asset";


        [TestCase(
            IsometricViewOrientation.North,
            "Fixture_2x1_BackstockShelf01_RisingRight")]
        [TestCase(
            IsometricViewOrientation.East,
            "Fixture_2x1_BackstockShelf01_RisingLeft")]
        [TestCase(
            IsometricViewOrientation.South,
            "Fixture_2x1_BackstockShelf01_RisingRight")]
        [TestCase(
            IsometricViewOrientation.West,
            "Fixture_2x1_BackstockShelf01_RisingLeft")]
        public void AuthoredBackstockShelf_InterleavesFourLayersAroundThreeRows(
            IsometricViewOrientation viewOrientation,
            string expectedDirectionName)
        {
            FixtureDefinitionAsset asset = LoadAsset();
            IReadOnlyList<Sprite> layers =
                asset.GetPresentationLayers(
                    FixtureOrientation.North,
                    viewOrientation);
            IReadOnlyList<Sprite> shelfMasks =
                asset.GetStorageShelfMasks(
                    FixtureOrientation.North,
                    viewOrientation);

            Assert.That(layers.Count, Is.EqualTo(4));
            Assert.That(shelfMasks.Count, Is.EqualTo(3));
            Assert.That(
                layers[0].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer00_BackAndBottomShelf"));
            Assert.That(
                layers[1].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer20_MiddleShelf"));
            Assert.That(
                layers[2].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer40_TopShelf"));
            Assert.That(
                layers[3].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer60_FrontFrame"));

            Sprite combined =
                asset.GetSprite(
                    FixtureOrientation.North,
                    viewOrientation);

            for (int index = 0;
                 index < layers.Count;
                 index++)
            {
                Assert.That(layers[index].rect, Is.EqualTo(combined.rect));
                Assert.That(layers[index].pivot, Is.EqualTo(combined.pivot));
                Assert.That(
                    layers[index].pixelsPerUnit,
                    Is.EqualTo(combined.pixelsPerUnit));
            }
        }


        [Test]
        public void AuthoredBackstockShelf_LayersPassDefinitionValidation()
        {
            FixtureDefinitionAsset asset = LoadAsset();

            Assert.That(
                () => asset.ValidateConfiguration(),
                Throws.Nothing);
            Assert.That(asset.BackstockCaseSlotCapacity, Is.EqualTo(12));
            Assert.That(asset.BackstockCasesPerShelf, Is.EqualTo(4));
            Assert.That(
                asset.BackstockCaseWidthPerSlot,
                Is.EqualTo(1.28f).Within(0.0001f));
            Assert.That(
                asset.BackstockCaseSpacingShare,
                Is.EqualTo(0.82f).Within(0.0001f));
            Assert.That(
                asset.BackstockCaseRowOffsetShare,
                Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(
                asset.BackstockCaseFrontOffsetShare,
                Is.EqualTo(0.27f).Within(0.0001f));
            Assert.That(
                asset.CreateDomainDefinition()
                    .StorageProfile.BackstockCaseSlotCapacity,
                Is.EqualTo(12));
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
