using System;
using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls.Tests
{
    public sealed class WallFinishPresentationResolverTests
    {
        private readonly List<UnityEngine.Object> createdObjects =
            new List<UnityEngine.Object>();

        private CellEdge edge;
        private WallFinishAsset defaultAsset;
        private WallFinishAsset brickAsset;
        private Sprite brickRisingLeft;
        private Sprite brickRisingRight;
        private WallFinishAssetCatalog assetCatalog;
        private WallState wallState;
        private WallFinishService finishService;
        private WallFinishPresentationResolver resolver;


        [SetUp]
        public void SetUp()
        {
            edge =
                new CellEdge(
                    new GridPosition(0, 0),
                    CellEdgeDirection.NorthEast);

            defaultAsset =
                CreateFinishAsset(
                    "default",
                    out _,
                    out _);

            brickAsset =
                CreateFinishAsset(
                    "brick",
                    out brickRisingLeft,
                    out brickRisingRight);

            assetCatalog =
                CreateAssetCatalog(
                    defaultAsset,
                    brickAsset);

            WallFinishCatalog domainCatalog =
                assetCatalog.CreateDomainCatalog();

            wallState =
                new WallState(
                    new[]
                    {
                        edge
                    });

            WallFinishState finishState =
                new WallFinishState();

            finishService =
                new WallFinishService(
                    wallState,
                    domainCatalog,
                    finishState);

            resolver =
                new WallFinishPresentationResolver(
                    finishService,
                    assetCatalog);
        }


        [TearDown]
        public void TearDown()
        {
            finishService?.Dispose();

            for (int index = createdObjects.Count - 1;
                 index >= 0;
                 index--)
            {
                UnityEngine.Object createdObject =
                    createdObjects[index];

                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        createdObject);
                }
            }

            createdObjects.Clear();
        }


        [Test]
        public void ResolveAsset_NoOverride_ReturnsDefaultAsset()
        {
            WallFinishAsset resolved =
                resolver.ResolveAsset(
                    edge,
                    edge.FirstCell);

            Assert.That(
                resolved,
                Is.SameAs(defaultAsset));
        }


        [Test]
        public void ResolveAsset_FaceOverride_ReturnsMatchingAsset()
        {
            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    edge,
                    edge.FirstCell,
                    brickAsset.Id);

            WallFinishAsset resolved =
                resolver.ResolveAsset(
                    edge,
                    edge.FirstCell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(resolved, Is.SameAs(brickAsset));
        }


        [Test]
        public void ResolveAsset_OppositeFaceOverride_DoesNotBleedAcrossWall()
        {
            WallFinishChangeResult result =
                finishService.TrySetFinish(
                    edge,
                    edge.SecondCell,
                    brickAsset.Id);

            WallFinishAsset firstFace =
                resolver.ResolveAsset(
                    edge,
                    edge.FirstCell);

            WallFinishAsset secondFace =
                resolver.ResolveAsset(
                    edge,
                    edge.SecondCell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(firstFace, Is.SameAs(defaultAsset));
            Assert.That(secondFace, Is.SameAs(brickAsset));
        }


        [Test]
        public void ResolveSprite_UsesRequestedDirectionalSprite()
        {
            finishService.TrySetFinish(
                edge,
                edge.FirstCell,
                brickAsset.Id);

            Sprite risingLeft =
                resolver.ResolveSprite(
                    edge,
                    edge.FirstCell,
                    WallDisplaySlope.RisingLeft);

            Sprite risingRight =
                resolver.ResolveSprite(
                    edge,
                    edge.FirstCell,
                    WallDisplaySlope.RisingRight);

            Assert.That(risingLeft, Is.SameAs(brickRisingLeft));
            Assert.That(risingRight, Is.SameAs(brickRisingRight));
        }


        [Test]
        public void GetSprite_LowHeight_UsesLowDirectionalSprite()
        {
            Sprite lowRisingLeft =
                CreateSprite();

            Sprite lowRisingRight =
                CreateSprite();

            SetPrivateField(
                brickAsset,
                "lowRisingLeft",
                lowRisingLeft);

            SetPrivateField(
                brickAsset,
                "lowRisingRight",
                lowRisingRight);

            Assert.That(
                brickAsset.GetSprite(
                    WallDisplaySlope.RisingLeft,
                    WallPresentationHeight.Low),
                Is.SameAs(lowRisingLeft));

            Assert.That(
                brickAsset.GetSprite(
                    WallDisplaySlope.RisingRight,
                    WallPresentationHeight.Low),
                Is.SameAs(lowRisingRight));
        }


        [Test]
        public void GetSprite_MissingLowSprite_FallsBackToFullWall()
        {
            Assert.That(
                brickAsset.GetSprite(
                    WallDisplaySlope.RisingLeft,
                    WallPresentationHeight.Low),
                Is.SameAs(brickRisingLeft));

            Assert.That(
                brickAsset.GetSprite(
                    WallDisplaySlope.RisingRight,
                    WallPresentationHeight.Low),
                Is.SameAs(brickRisingRight));
        }


        [Test]
        public void GetAsset_UnknownFinishId_Throws()
        {
            Assert.That(
                () => assetCatalog.GetAsset(
                    new WallFinishId("metal")),
                Throws.TypeOf<KeyNotFoundException>());
        }


        [Test]
        public void CreateDomainCatalog_DuplicateNormalizedId_Throws()
        {
            WallFinishAsset duplicateDefault =
                CreateFinishAsset(
                    " DEFAULT ",
                    out _,
                    out _);

            WallFinishAssetCatalog duplicateCatalog =
                CreateAssetCatalog(
                    defaultAsset,
                    duplicateDefault);

            Assert.That(
                () => duplicateCatalog.CreateDomainCatalog(),
                Throws.TypeOf<InvalidOperationException>());
        }


        private WallFinishAsset CreateFinishAsset(
            string finishId,
            out Sprite risingLeft,
            out Sprite risingRight)
        {
            WallFinishAsset asset =
                ScriptableObject.CreateInstance<WallFinishAsset>();

            asset.name =
                $"{finishId} Test Finish";

            risingLeft =
                CreateSprite();

            risingRight =
                CreateSprite();

            SetPrivateField(
                asset,
                "finishId",
                finishId);

            SetPrivateField(
                asset,
                "risingLeft",
                risingLeft);

            SetPrivateField(
                asset,
                "risingRight",
                risingRight);

            createdObjects.Add(asset);

            return asset;
        }


        private WallFinishAssetCatalog CreateAssetCatalog(
            WallFinishAsset defaultFinish,
            params WallFinishAsset[] additionalFinishes)
        {
            WallFinishAssetCatalog catalog =
                ScriptableObject
                    .CreateInstance<WallFinishAssetCatalog>();

            catalog.name =
                "Test Wall Finish Catalog";

            SetPrivateField(
                catalog,
                "defaultFinish",
                defaultFinish);

            SetPrivateField(
                catalog,
                "additionalFinishes",
                additionalFinishes);

            createdObjects.Add(catalog);

            return catalog;
        }


        private Sprite CreateSprite()
        {
            Texture2D texture =
                new Texture2D(
                    1,
                    1);

            texture.SetPixel(
                0,
                0,
                Color.white);

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


        private static void SetPrivateField<TTarget, TValue>(
            TTarget target,
            string fieldName,
            TValue value)
        {
            FieldInfo field =
                typeof(TTarget).GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Could not find private field '{fieldName}' on "
                + $"{typeof(TTarget).Name}.");

            field.SetValue(
                target,
                value);
        }
    }
}
