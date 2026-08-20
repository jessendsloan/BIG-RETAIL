using System;
using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class DoorDefinitionAssetCatalogTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();


        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1;
                 index >= 0;
                 index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }


        [Test]
        public void CreateDomainCatalog_PreservesFourPanelTopology()
        {
            DoorDefinitionAsset asset =
                CreateDefinitionAsset(
                    "automatic-front-door",
                    4,
                    new[] { 1, 2 });

            DoorDefinitionAssetCatalog assetCatalog =
                CreateCatalog(asset);

            DoorDefinitionCatalog catalog =
                assetCatalog.CreateDomainCatalog();

            Assert.That(
                catalog.TryGetDefinition(
                    asset.Id,
                    out DoorDefinition definition),
                Is.True);

            Assert.That(definition.SegmentCount, Is.EqualTo(4));
            Assert.That(definition.IsPassageSegment(0), Is.False);
            Assert.That(definition.IsPassageSegment(1), Is.True);
            Assert.That(definition.IsPassageSegment(2), Is.True);
            Assert.That(definition.IsPassageSegment(3), Is.False);
        }


        [Test]
        public void CreateDomainCatalog_PreservesSinglePanelTopology()
        {
            DoorDefinitionAsset asset =
                CreateDefinitionAsset(
                    "single-hinged-door",
                    1,
                    new[] { 0 });

            SetPrivateField(
                asset,
                "presentationStyle",
                DoorPresentationStyle.HingedSinglePanel);

            DoorDefinitionAssetCatalog assetCatalog =
                CreateCatalog(asset);

            DoorDefinitionCatalog catalog =
                assetCatalog.CreateDomainCatalog();

            Assert.That(
                catalog.TryGetDefinition(
                    asset.Id,
                    out DoorDefinition definition),
                Is.True);

            Assert.That(definition.SegmentCount, Is.EqualTo(1));
            Assert.That(definition.IsPassageSegment(0), Is.True);
        }


        [Test]
        public void TryGetAssemblySprites_CompleteSet_ResolvesExactLayers()
        {
            DoorDefinitionAsset asset =
                CreateDefinitionAsset(
                    "automatic-front-door",
                    4,
                    new[] { 1, 2 });

            DoorAssemblySpriteSet visuals =
                CreateCompleteVisuals();

            Sprite expectedFrame =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightFrame");

            Sprite expectedAperture =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightAperture");

            Sprite expectedLeftGlass =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightLeftGlass");

            Sprite expectedLeftDoor =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightLeftDoor");

            Sprite expectedRightDoor =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightRightDoor");

            Sprite expectedRightGlass =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightRightGlass");

            SetPrivateField(
                asset,
                "assemblyVisuals",
                visuals);

            asset.ValidateConfiguration();

            bool resolved =
                asset.TryGetAssemblySprites(
                    WallDisplaySlope.RisingRight,
                    out DoorAssemblySprites sprites);

            Assert.That(resolved, Is.True);
            Assert.That(sprites.Frame, Is.SameAs(expectedFrame));
            Assert.That(sprites.Aperture, Is.SameAs(expectedAperture));
            Assert.That(sprites.LeftGlass, Is.SameAs(expectedLeftGlass));
            Assert.That(sprites.LeftDoor, Is.SameAs(expectedLeftDoor));
            Assert.That(sprites.RightDoor, Is.SameAs(expectedRightDoor));
            Assert.That(sprites.RightGlass, Is.SameAs(expectedRightGlass));
        }


        [Test]
        public void TryGetAssemblySprites_IncompleteSet_ReturnsFallbackSignal()
        {
            DoorDefinitionAsset asset =
                CreateDefinitionAsset(
                    "automatic-front-door",
                    4,
                    new[] { 1, 2 });

            DoorAssemblySpriteSet incomplete =
                new DoorAssemblySpriteSet();

            SetPrivateField(
                incomplete,
                "risingLeftFrame",
                CreateSprite());

            SetPrivateField(
                asset,
                "assemblyVisuals",
                incomplete);

            Assert.That(
                asset.TryGetAssemblySprites(
                    WallDisplaySlope.RisingLeft,
                    out DoorAssemblySprites sprites),
                Is.False);

            Assert.That(sprites.Frame, Is.Null);
        }


        [Test]
        public void TryGetHingedSprites_CompleteSet_ResolvesExactLayers()
        {
            DoorDefinitionAsset asset =
                CreateDefinitionAsset(
                    "single-hinged-door",
                    1,
                    new[] { 0 });

            HingedDoorSpriteSet visuals =
                CreateCompleteHingedVisuals();

            Sprite expectedFrame =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingLeftFrame");

            Sprite expectedDoor =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingLeftDoor");

            SetPrivateField(
                asset,
                "presentationStyle",
                DoorPresentationStyle.HingedSinglePanel);

            SetPrivateField(
                asset,
                "hingedVisuals",
                visuals);

            asset.ValidateConfiguration();

            bool resolved =
                asset.TryGetHingedSprites(
                    WallDisplaySlope.RisingLeft,
                    out HingedDoorSprites sprites);

            Assert.That(resolved, Is.True);
            Assert.That(sprites.Frame, Is.SameAs(expectedFrame));
            Assert.That(sprites.Door, Is.SameAs(expectedDoor));
        }


        [Test]
        public void TryGetDoorwaySprites_CompleteSet_ResolvesFrameAndAperture()
        {
            DoorDefinitionAsset asset =
                CreateDefinitionAsset(
                    "double-open-doorway",
                    2,
                    new[] { 0, 1 });

            DoorwaySpriteSet visuals =
                CreateCompleteDoorwayVisuals();

            Sprite expectedFrame =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightFrame");

            Sprite expectedAperture =
                GetPrivateField<Sprite>(
                    visuals,
                    "risingRightAperture");

            SetPrivateField(
                asset,
                "presentationStyle",
                DoorPresentationStyle.StaticDoorway);

            SetPrivateField(
                asset,
                "doorwayVisuals",
                visuals);

            asset.ValidateConfiguration();

            bool resolved =
                asset.TryGetDoorwaySprites(
                    WallDisplaySlope.RisingRight,
                    out DoorwaySprites sprites);

            Assert.That(resolved, Is.True);
            Assert.That(sprites.Frame, Is.SameAs(expectedFrame));
            Assert.That(sprites.Aperture, Is.SameAs(expectedAperture));
        }


        [Test]
        public void ValidateConfiguration_DuplicateNormalizedId_Throws()
        {
            DoorDefinitionAsset first =
                CreateDefinitionAsset(
                    "automatic-front-door",
                    4,
                    new[] { 1, 2 });

            DoorDefinitionAsset duplicate =
                CreateDefinitionAsset(
                    " AUTOMATIC-FRONT-DOOR ",
                    4,
                    new[] { 1, 2 });

            DoorDefinitionAssetCatalog catalog =
                CreateCatalog(
                    first,
                    duplicate);

            Assert.That(
                catalog.ValidateConfiguration,
                Throws.TypeOf<InvalidOperationException>());
        }


        [Test]
        public void AuthoredCatalog_ContainsOnePanelHingedDoor()
        {
            DoorDefinitionAssetCatalog catalog =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    DoorDefinitionAssetCatalog>(
                    "Assets/Design/Doors/DoorDefinitionCatalog.asset");

            Assert.That(
                catalog,
                Is.Not.Null);

            Assert.That(
                catalog.TryGetAsset(
                    new DoorDefinitionId("single-hinged-door"),
                    out DoorDefinitionAsset asset),
                Is.True);

            Assert.That(asset.SegmentCount, Is.EqualTo(1));
            Assert.That(
                asset.PresentationStyle,
                Is.EqualTo(
                    DoorPresentationStyle.HingedSinglePanel));
            Assert.That(asset.HasCompleteHingedVisuals, Is.True);
        }


        [TestCase("single-open-doorway", 1)]
        [TestCase("double-open-doorway", 2)]
        public void AuthoredCatalog_ContainsStaticOpenDoorway(
            string definitionId,
            int expectedSegmentCount)
        {
            DoorDefinitionAssetCatalog catalog =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    DoorDefinitionAssetCatalog>(
                    "Assets/Design/Doors/DoorDefinitionCatalog.asset");

            Assert.That(
                catalog,
                Is.Not.Null);

            Assert.That(
                catalog.TryGetAsset(
                    new DoorDefinitionId(definitionId),
                    out DoorDefinitionAsset asset),
                Is.True);

            Assert.That(
                asset.SegmentCount,
                Is.EqualTo(expectedSegmentCount));

            Assert.That(
                asset.PresentationStyle,
                Is.EqualTo(
                    DoorPresentationStyle.StaticDoorway));

            Assert.That(
                asset.HasCompleteDoorwayVisuals,
                Is.True);

            DoorDefinition definition =
                asset.CreateDomainDefinition();

            for (int index = 0;
                 index < expectedSegmentCount;
                 index++)
            {
                Assert.That(
                    definition.IsPassageSegment(index),
                    Is.True);
            }
        }


        [Test]
        public void AuthoredCatalog_ContainsNonPassableFixedWindow()
        {
            DoorDefinitionAssetCatalog catalog =
                UnityEditor.AssetDatabase.LoadAssetAtPath<
                    DoorDefinitionAssetCatalog>(
                    "Assets/Design/Doors/DoorDefinitionCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryGetAsset(
                    new DoorDefinitionId("fixed-window"),
                    out DoorDefinitionAsset asset),
                Is.True);
            Assert.That(asset.SegmentCount, Is.EqualTo(1));
            Assert.That(asset.HasPassageSegments, Is.False);
            Assert.That(
                asset.PresentationStyle,
                Is.EqualTo(DoorPresentationStyle.StaticDoorway));
            Assert.That(asset.HasCompleteDoorwayVisuals, Is.True);
            Assert.That(
                asset.CreateDomainDefinition().PassageSegmentCount,
                Is.EqualTo(0));
        }


        private DoorDefinitionAsset CreateDefinitionAsset(
            string definitionId,
            int segmentCount,
            int[] passageIndices)
        {
            DoorDefinitionAsset asset =
                ScriptableObject.CreateInstance<DoorDefinitionAsset>();

            asset.name =
                $"{definitionId}.test";

            SetPrivateField(
                asset,
                "definitionId",
                definitionId);

            SetPrivateField(
                asset,
                "segmentCount",
                segmentCount);

            SetPrivateField(
                asset,
                "passageSegmentIndices",
                passageIndices);

            createdObjects.Add(asset);
            return asset;
        }


        private DoorDefinitionAssetCatalog CreateCatalog(
            DoorDefinitionAsset defaultDefinition,
            params DoorDefinitionAsset[] additionalDefinitions)
        {
            DoorDefinitionAssetCatalog catalog =
                ScriptableObject
                    .CreateInstance<DoorDefinitionAssetCatalog>();

            SetPrivateField(
                catalog,
                "defaultDefinition",
                defaultDefinition);

            SetPrivateField(
                catalog,
                "additionalDefinitions",
                additionalDefinitions);

            createdObjects.Add(catalog);
            return catalog;
        }


        private DoorAssemblySpriteSet CreateCompleteVisuals()
        {
            DoorAssemblySpriteSet visuals =
                new DoorAssemblySpriteSet();

            SetPrivateField(
                visuals,
                "risingLeftFrame",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftAperture",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftLeftGlass",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftLeftDoor",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftRightDoor",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftRightGlass",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightFrame",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightAperture",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightLeftGlass",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightLeftDoor",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightRightDoor",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightRightGlass",
                CreateSprite());

            return visuals;
        }


        private HingedDoorSpriteSet CreateCompleteHingedVisuals()
        {
            HingedDoorSpriteSet visuals =
                new HingedDoorSpriteSet();

            SetPrivateField(
                visuals,
                "risingLeftFrame",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftDoor",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightFrame",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightDoor",
                CreateSprite());

            return visuals;
        }


        private DoorwaySpriteSet CreateCompleteDoorwayVisuals()
        {
            DoorwaySpriteSet visuals =
                new DoorwaySpriteSet();

            SetPrivateField(
                visuals,
                "risingLeftFrame",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingLeftAperture",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightFrame",
                CreateSprite());

            SetPrivateField(
                visuals,
                "risingRightAperture",
                CreateSprite());

            return visuals;
        }


        private Sprite CreateSprite()
        {
            Texture2D texture =
                new Texture2D(1, 1);

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));

            createdObjects.Add(texture);
            createdObjects.Add(sprite);

            return sprite;
        }


        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);

            field.SetValue(
                target,
                value);
        }


        private static TValue GetPrivateField<TValue>(
            object target,
            string fieldName)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);

            return (TValue)field.GetValue(
                target);
        }
    }
}
