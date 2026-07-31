using System;
using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.Floors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FloorFinishAssetCatalogTests
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
                UnityEngine.Object.DestroyImmediate(
                    createdObjects[index]);
            }

            createdObjects.Clear();
        }


        [Test]
        public void CreateDomainCatalog_ContainsEveryAuthoredFinish()
        {
            FloorFinishAsset concrete =
                CreateFinish("concrete");

            FloorFinishAsset wood =
                CreateFinish("wood");

            FloorFinishAssetCatalog assetCatalog =
                CreateCatalog(
                    concrete,
                    wood);

            FloorFinishCatalog catalog =
                assetCatalog.CreateDomainCatalog();

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(
                catalog.DefaultFinishId,
                Is.EqualTo(concrete.Id));
            Assert.That(catalog.Contains(wood.Id), Is.True);
        }


        [Test]
        public void EnumerateAssets_ReturnsDefaultBeforeAdditionalFinishes()
        {
            FloorFinishAsset concrete =
                CreateFinish("concrete");

            FloorFinishAsset wood =
                CreateFinish("wood");

            FloorFinishAssetCatalog catalog =
                CreateCatalog(
                    concrete,
                    wood);

            List<FloorFinishAsset> assets =
                new List<FloorFinishAsset>(
                    catalog.EnumerateAssets());

            Assert.That(assets.Count, Is.EqualTo(2));
            Assert.That(assets[0], Is.SameAs(concrete));
            Assert.That(assets[1], Is.SameAs(wood));
        }


        [Test]
        public void GetAsset_ResolvesRegisteredFinish()
        {
            FloorFinishAsset concrete =
                CreateFinish("concrete");

            FloorFinishAsset wood =
                CreateFinish("wood");

            FloorFinishAssetCatalog catalog =
                CreateCatalog(
                    concrete,
                    wood);

            Assert.That(
                catalog.GetAsset(wood.Id),
                Is.SameAs(wood));
        }


        [Test]
        public void ValidateConfiguration_RejectsDuplicateIdentifier()
        {
            FloorFinishAsset first =
                CreateFinish("wood");

            FloorFinishAsset duplicate =
                CreateFinish("WOOD");

            FloorFinishAssetCatalog catalog =
                CreateCatalogWithoutValidation(
                    first,
                    duplicate);

            Assert.Throws<InvalidOperationException>(
                catalog.ValidateConfiguration);
        }


        [Test]
        public void ValidateConfiguration_RejectsMissingTile()
        {
            FloorFinishAsset invalid =
                CreateFinish(
                    "invalid",
                    includeTile: false);

            FloorFinishAssetCatalog catalog =
                CreateCatalogWithoutValidation(
                    invalid);

            Assert.Throws<InvalidOperationException>(
                catalog.ValidateConfiguration);
        }


        private FloorFinishAsset CreateFinish(
            string finishId,
            bool includeTile = true)
        {
            FloorFinishAsset asset =
                ScriptableObject.CreateInstance<FloorFinishAsset>();

            asset.name =
                $"{finishId}.test";

            SetField(
                asset,
                "finishId",
                finishId);

            if (includeTile)
            {
                Tile tile =
                    ScriptableObject.CreateInstance<Tile>();

                createdObjects.Add(tile);

                SetField(
                    asset,
                    "floorTile",
                    tile);
            }

            createdObjects.Add(asset);
            return asset;
        }


        private FloorFinishAssetCatalog CreateCatalog(
            FloorFinishAsset defaultFinish,
            params FloorFinishAsset[] additionalFinishes)
        {
            FloorFinishAssetCatalog catalog =
                CreateCatalogWithoutValidation(
                    defaultFinish,
                    additionalFinishes);

            catalog.ValidateConfiguration();
            return catalog;
        }


        private FloorFinishAssetCatalog CreateCatalogWithoutValidation(
            FloorFinishAsset defaultFinish,
            params FloorFinishAsset[] additionalFinishes)
        {
            FloorFinishAssetCatalog catalog =
                ScriptableObject
                    .CreateInstance<FloorFinishAssetCatalog>();

            SetField(
                catalog,
                "defaultFinish",
                defaultFinish);

            SetField(
                catalog,
                "additionalFinishes",
                additionalFinishes);

            createdObjects.Add(catalog);
            return catalog;
        }


        private static void SetField(
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
    }
}
