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
            IReadOnlyList<Sprite> presentationLayers =
                asset.GetPresentationLayers(
                    FixtureOrientation.North,
                    viewOrientation);

            Assert.That(shelfMasks.Count, Is.EqualTo(3));
            Assert.That(presentationLayers.Count, Is.EqualTo(3));
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
                presentationLayers[0].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer00_LowerShelf"));
            Assert.That(
                presentationLayers[1].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer20_MiddleShelf"));
            Assert.That(
                presentationLayers[2].name,
                Does.StartWith(expectedDirectionName)
                    .And.Contain("Layer40_UpperShelf"));

            Sprite combined =
                asset.GetSprite(
                    FixtureOrientation.North,
                    viewOrientation);

            Assert.That(shelfMasks[0].rect, Is.EqualTo(combined.rect));

            for (int index = 0;
                 index < presentationLayers.Count;
                 index++)
            {
                Assert.That(
                    presentationLayers[index].rect,
                    Is.EqualTo(combined.rect));
                Assert.That(
                    presentationLayers[index].pivot,
                    Is.EqualTo(combined.pivot));
                Assert.That(
                    presentationLayers[index].pixelsPerUnit,
                    Is.EqualTo(combined.pixelsPerUnit));
            }
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
            Assert.That(
                asset.GetPresentationLayers(
                    FixtureOrientation.North,
                    viewOrientation),
                Is.Empty);
        }


        [TestCase(2, 1)]
        [TestCase(1, 9)]
        [TestCase(0, 17)]
        public void LayeredFrontShelf_DrawsEachProductRowAfterItsShelfLayer(
            int shelfIndex,
            int expectedSortingOrder)
        {
            int presentationLayerSortingStride =
                FixtureViewSystem
                    .ResolveDisplayPresentationLayerSortingStride(
                        frontageUnitCount: 5);

            Assert.That(
                FixtureViewSystem.ResolveStockedDisplayMarkerSortingOrder(
                    baseSortingOrder: 0,
                    shelfIndex: shelfIndex,
                    shelfCount: 3,
                    presentationLayerCount: 3,
                    presentationLayerSortingStride:
                        presentationLayerSortingStride,
                    isViewerNear: true),
                Is.EqualTo(expectedSortingOrder));
        }


        [Test]
        public void LayeredFrontShelf_DrawsNearerFrontageOverFartherFrontage()
        {
            const int shelfSortingOrder = 17;

            Assert.That(
                FixtureViewSystem
                    .ResolveStockedDisplayFrontageSortingOrder(
                        shelfSortingOrder,
                        visualFrontageIndex: 0,
                        frontageUnitCount: 5,
                        majorAxisAngleDegrees: 26.565f),
                Is.EqualTo(21));
            Assert.That(
                FixtureViewSystem
                    .ResolveStockedDisplayFrontageSortingOrder(
                        shelfSortingOrder,
                        visualFrontageIndex: 4,
                        frontageUnitCount: 5,
                        majorAxisAngleDegrees: 26.565f),
                Is.EqualTo(17));
            Assert.That(
                FixtureViewSystem
                    .ResolveStockedDisplayFrontageSortingOrder(
                        shelfSortingOrder,
                        visualFrontageIndex: 0,
                        frontageUnitCount: 5,
                        majorAxisAngleDegrees: -26.565f),
                Is.EqualTo(17));
            Assert.That(
                FixtureViewSystem
                    .ResolveStockedDisplayFrontageSortingOrder(
                        shelfSortingOrder,
                        visualFrontageIndex: 4,
                        frontageUnitCount: 5,
                        majorAxisAngleDegrees: -26.565f),
                Is.EqualTo(21));
        }


        [Test]
        public void AuthoredProductScale_RespectsSlotAndShelfClearance()
        {
            Bounds chipBagBounds =
                new Bounds(
                    Vector3.zero,
                    new Vector3(0.165f, 0.278f, 0f));

            float scale =
                FixtureViewSystem.ResolveAuthoredProductUniformScale(
                    chipBagBounds,
                    maximumWidth: 0.122f,
                    maximumHeight: 0.193f);

            Assert.That(
                chipBagBounds.size.x * scale,
                Is.LessThanOrEqualTo(0.122f));
            Assert.That(
                chipBagBounds.size.y * scale,
                Is.EqualTo(0.193f).Within(0.0001f));
        }


        [Test]
        public void AuthoredProductCenters_AreTighterLeftAndViewerward()
        {
            IReadOnlyList<Sprite> shelfMasks =
                LoadAsset().GetMerchandisingShelfMasks(
                    FixtureSide.South,
                    FixtureOrientation.North,
                    IsometricViewOrientation.North);

            Assert.That(
                FixtureShelfMaskGeometry.TryCreate(
                    shelfMasks[0],
                    out FixtureShelfMaskGeometry geometry),
                Is.True);

            Vector2 defaultFirst = geometry.GetFrontageCenter(0, 5);
            Vector2 defaultLast = geometry.GetFrontageCenter(4, 5);
            Vector2 adjustedFirst =
                FixtureViewSystem.ResolveAuthoredDisplayProductCenter(
                    geometry,
                    visualFrontageIndex: 0,
                    frontageUnitCount: 5);
            Vector2 adjustedLast =
                FixtureViewSystem.ResolveAuthoredDisplayProductCenter(
                    geometry,
                    visualFrontageIndex: 4,
                    frontageUnitCount: 5);

            Assert.That(
                Vector2.Distance(adjustedFirst, adjustedLast),
                Is.LessThan(Vector2.Distance(defaultFirst, defaultLast)));
            Assert.That(
                (adjustedFirst.x + adjustedLast.x) * 0.5f,
                Is.LessThan((defaultFirst.x + defaultLast.x) * 0.5f));

            float defaultRowY =
                (defaultFirst.y + defaultLast.y) * 0.5f;
            float adjustedRowY =
                (adjustedFirst.y + adjustedLast.y) * 0.5f;
            float forwardShift = defaultRowY - adjustedRowY;

            Assert.That(
                forwardShift,
                Is.GreaterThan(0f));
            Assert.That(
                forwardShift,
                Is.LessThan(geometry.MinorLength * 0.5f));
        }


        [Test]
        public void AuthoredHalfShelf_MasksPassDefinitionValidation()
        {
            Assert.That(
                () => LoadAsset().ValidateConfiguration(),
                Throws.Nothing);
        }


        [Test]
        public void AuthoredHalfShelf_ProvidesFiveFrontagesPerShelfRun()
        {
            FixtureDefinition definition =
                LoadAsset().CreateDomainDefinition();

            Assert.That(
                definition.MerchandisingProfile.TryGetDisplayFace(
                    FixtureSide.South,
                    out FixtureDisplayFaceDefinition face),
                Is.True);
            Assert.That(face.ShelfRunCount, Is.EqualTo(3));
            Assert.That(face.FrontageUnitsPerRun, Is.EqualTo(5));
        }


        [TestCase(IsometricViewOrientation.North)]
        [TestCase(IsometricViewOrientation.East)]
        public void AuthoredHalfShelf_FrontViewsProvideFifteenProductAnchors(
            IsometricViewOrientation viewOrientation)
        {
            FixtureDefinitionAsset asset = LoadAsset();
            IReadOnlyList<Vector2> anchors =
                asset.GetMerchandisingProductAnchors(
                    FixtureSide.South,
                    FixtureOrientation.North,
                    viewOrientation);

            Assert.That(anchors.Count, Is.EqualTo(15));

            for (int shelfIndex = 0; shelfIndex < 3; shelfIndex++)
            {
                for (int slotIndex = 0; slotIndex < 5; slotIndex++)
                {
                    int anchorIndex = shelfIndex * 5 + slotIndex;

                    Assert.That(
                        asset.TryGetMerchandisingProductAnchor(
                            FixtureSide.South,
                            FixtureOrientation.North,
                            viewOrientation,
                            shelfIndex,
                            slotIndex,
                            frontageUnitsPerShelf: 5,
                            out Vector2 anchor),
                        Is.True);
                    Assert.That(anchor, Is.EqualTo(anchors[anchorIndex]));
                }
            }
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
