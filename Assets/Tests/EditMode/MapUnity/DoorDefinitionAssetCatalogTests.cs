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
