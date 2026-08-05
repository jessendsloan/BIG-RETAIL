using System;
using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FoundationAreaPreviewViewTests
    {
        private const string PreviewTypeName =
            "BigRetail.Construction.Unity.Foundations." +
            "FoundationAreaPreviewView, Assembly-CSharp";

        private const string PylonTypeName =
            "BigRetail.Construction.Unity.Cells." +
            "TilePlacementPylonView, Assembly-CSharp";


        [Test]
        public void ShowCell_BuildableCell_UsesBuildablePreview()
        {
            PreviewFixture fixture =
                CreateFixture();

            try
            {
                InvokePublicMethod(
                    fixture.Preview,
                    "ShowCell",
                    new GridPosition(2, 2));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleCellCount"),
                    Is.EqualTo(1));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "BuildableCellCount"),
                    Is.EqualTo(1));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisiblePylonCount"),
                    Is.EqualTo(1));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronCount"),
                    Is.EqualTo(8));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronPylonCount"),
                    Is.EqualTo(8));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleBorderSegmentCount"),
                    Is.EqualTo(4));

                Assert.That(
                    fixture.Tilemap.GetTile(
                        new Vector3Int(2, 2, 0)),
                    Is.SameAs(fixture.PreviewTile));

                Assert.That(
                    fixture.Tilemap.GetColor(
                        new Vector3Int(2, 2, 0)),
                    Is.EqualTo(
                        new Color(
                            1f,
                            1f,
                            1f,
                            0.5f)));

                Assert.That(
                    fixture.Tilemap.GetTile(
                        new Vector3Int(1, 1, 0)),
                    Is.SameAs(fixture.PreviewApronTile));

                Assert.That(
                    fixture.Tilemap.GetColor(
                        new Vector3Int(1, 1, 0)),
                    Is.EqualTo(
                        new Color(
                            1f,
                            1f,
                            1f,
                            0.5f)));

                SpriteRenderer[] pylons =
                    fixture.Tilemap.GetComponentsInChildren<
                        SpriteRenderer>(true);

                Assert.That(
                    pylons,
                    Has.Length.EqualTo(9));

                Vector3 foundationCenter =
                    fixture.Tilemap.GetCellCenterWorld(
                        new Vector3Int(2, 2, 0));

                bool hasCenteredPylon = false;

                for (int index = 0;
                     index < pylons.Length;
                     index++)
                {
                    Assert.That(
                        pylons[index].color.a,
                        Is.EqualTo(1f));

                    if (Vector3.Distance(
                            pylons[index].transform.position,
                            foundationCenter)
                        < 0.0001f)
                    {
                        hasCenteredPylon = true;
                    }
                }

                Assert.That(
                    hasCenteredPylon,
                    Is.True);
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void ShowCell_ExistingFoundation_UsesExistingPreview()
        {
            PreviewFixture fixture =
                CreateFixture();

            try
            {
                GridPosition cell =
                    new GridPosition(2, 2);

                Assert.That(
                    fixture.RuntimeHost.FoundationConstruction
                        .TryEnsureFoundations(
                            new[] { cell })
                        .Succeeded,
                    Is.True);

                InvokePublicMethod(
                    fixture.Preview,
                    "ShowCell",
                    cell);

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "ExistingCellCount"),
                    Is.EqualTo(1));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "BuildableCellCount"),
                    Is.EqualTo(0));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronCount"),
                    Is.EqualTo(8));
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void ShowCell_OutsideMap_UsesInvalidPreview()
        {
            PreviewFixture fixture =
                CreateFixture();

            try
            {
                InvokePublicMethod(
                    fixture.Preview,
                    "ShowCell",
                    new GridPosition(8, 8));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "InvalidCellCount"),
                    Is.EqualTo(1));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleCellCount"),
                    Is.EqualTo(1));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronCount"),
                    Is.EqualTo(0));
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void Hide_ClearsOnlyOwnedPreviewCells()
        {
            PreviewFixture fixture =
                CreateFixture();

            Tile foreignTile =
                ScriptableObject.CreateInstance<Tile>();

            try
            {
                InvokePublicMethod(
                    fixture.Preview,
                    "ShowCell",
                    new GridPosition(2, 2));

                Vector3Int foreignCell =
                    new Vector3Int(4, 4, 0);

                fixture.Tilemap.SetTile(
                    foreignCell,
                    foreignTile);

                InvokePublicMethod(
                    fixture.Preview,
                    "Hide");

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleCellCount"),
                    Is.EqualTo(0));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisiblePylonCount"),
                    Is.EqualTo(0));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronCount"),
                    Is.EqualTo(0));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronPylonCount"),
                    Is.EqualTo(0));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleBorderSegmentCount"),
                    Is.EqualTo(0));

                Assert.That(
                    fixture.Tilemap.GetTile(
                        new Vector3Int(2, 2, 0)),
                    Is.Null);

                Assert.That(
                    fixture.Tilemap.GetTile(
                        new Vector3Int(1, 1, 0)),
                    Is.Null);

                Assert.That(
                    fixture.Tilemap.GetTile(foreignCell),
                    Is.SameAs(foreignTile));
            }
            finally
            {
                Object.DestroyImmediate(foreignTile);
                fixture.Dispose();
            }
        }


        [Test]
        public void OccupiedUnownedCell_IsNeverOverwrittenOrCleared()
        {
            PreviewFixture fixture =
                CreateFixture();

            Tile foreignTile =
                ScriptableObject.CreateInstance<Tile>();

            try
            {
                Vector3Int occupiedCell =
                    new Vector3Int(2, 2, 0);

                fixture.Tilemap.SetTile(
                    occupiedCell,
                    foreignTile);

                LogAssert.Expect(
                    LogType.Error,
                    "FoundationAreaPreviewView refused to overwrite " +
                    "an unowned tile at (2, 2, 0). Assign a dedicated " +
                    "empty Foundation Preview Tilemap.");

                InvokePublicMethod(
                    fixture.Preview,
                    "ShowCell",
                    new GridPosition(2, 2));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleCellCount"),
                    Is.EqualTo(0));

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleApronCount"),
                    Is.EqualTo(0));

                InvokePublicMethod(
                    fixture.Preview,
                    "Hide");

                Assert.That(
                    fixture.Tilemap.GetTile(occupiedCell),
                    Is.SameAs(foreignTile));
            }
            finally
            {
                Object.DestroyImmediate(foreignTile);
                fixture.Dispose();
            }
        }


        [Test]
        public void OrientationChanging_ClearsPreviewBeforeProjectionChanges()
        {
            PreviewFixture fixture =
                CreateFixture();

            try
            {
                InvokePublicMethod(
                    fixture.Preview,
                    "ShowCell",
                    new GridPosition(2, 2));

                InvokePrivateMethod(
                    fixture.Preview,
                    "HandleOrientationChanging",
                    IsometricViewOrientation.North,
                    IsometricViewOrientation.East);

                Assert.That(
                    GetPublicProperty<int>(
                        fixture.Preview,
                        "VisibleCellCount"),
                    Is.EqualTo(0));

                Assert.That(
                    fixture.Tilemap.GetTile(
                        new Vector3Int(2, 2, 0)),
                    Is.Null);
            }
            finally
            {
                fixture.Dispose();
            }
        }


        private static PreviewFixture CreateFixture()
        {
            Type previewType =
                Type.GetType(PreviewTypeName);

            Type pylonType =
                Type.GetType(PylonTypeName);

            Assert.That(
                previewType,
                Is.Not.Null,
                $"Could not resolve {PreviewTypeName}.");

            Assert.That(
                pylonType,
                Is.Not.Null,
                $"Could not resolve {PylonTypeName}.");

            GameObject mapObject =
                new GameObject("Foundation Preview Map");

            mapObject.SetActive(false);

            GridMapHost mapHost =
                mapObject.AddComponent<GridMapHost>();

            GridMapDefinition mapDefinition =
                CreateMapDefinition();

            ConstructionAreaDefinition constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    mapDefinition.EnumerateValidCells());

            SetAutoPropertyBackingField(
                mapHost,
                "MapDefinition",
                mapDefinition);

            SetAutoPropertyBackingField(
                mapHost,
                "ConstructionArea",
                constructionArea);

            SetAutoPropertyBackingField(
                mapHost,
                "IsInitialized",
                true);

            GameObject runtimeHostObject =
                new GameObject("Foundation Runtime Host");

            runtimeHostObject.SetActive(false);

            FoundationRuntimeHost runtimeHost =
                runtimeHostObject.AddComponent<FoundationRuntimeHost>();

            SetPrivateField(
                runtimeHost,
                "mapHost",
                mapHost);

            Assert.That(
                runtimeHost.TryInitialize(),
                Is.True);

            GameObject viewHostObject =
                new GameObject("Isometric View Host");

            viewHostObject.SetActive(false);

            IsometricViewHost viewHost =
                viewHostObject.AddComponent<IsometricViewHost>();

            IsometricViewProjection projection =
                new IsometricViewProjection(
                    new IsometricMapFootprint(
                        0,
                        0,
                        4,
                        4),
                    IsometricViewOrientation.North);

            SetAutoPropertyBackingField(
                viewHost,
                "Projection",
                projection);

            GameObject gridObject =
                new GameObject("Foundation Preview Grid");

            gridObject.SetActive(false);
            gridObject.AddComponent<Grid>();

            GameObject previewObject =
                new GameObject("Foundation Preview");

            previewObject.transform.SetParent(
                gridObject.transform,
                false);

            Tilemap tilemap =
                previewObject.AddComponent<Tilemap>();

            previewObject.AddComponent<TilemapRenderer>();

            Component preview =
                previewObject.AddComponent(
                    previewType);

            Tile previewTile =
                ScriptableObject.CreateInstance<Tile>();

            Tile previewApronTile =
                ScriptableObject.CreateInstance<Tile>();

            Texture2D pylonTexture =
                new Texture2D(1, 1);

            Sprite pylonSprite =
                Sprite.Create(
                    pylonTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0f));

            GameObject pylonObject =
                new GameObject("Tile Placement Pylon Prefab");

            pylonObject.SetActive(false);

            SpriteRenderer pylonRenderer =
                pylonObject.AddComponent<SpriteRenderer>();

            pylonRenderer.sprite =
                pylonSprite;

            Component pylonPrefab =
                pylonObject.AddComponent(
                    pylonType);

            SetPrivateField(
                pylonPrefab,
                "spriteRenderer",
                pylonRenderer);

            GameObject apronPylonObject =
                new GameObject(
                    "Apron Tile Placement Pylon Prefab");

            apronPylonObject.SetActive(false);

            SpriteRenderer apronPylonRenderer =
                apronPylonObject.AddComponent<SpriteRenderer>();

            apronPylonRenderer.sprite =
                pylonSprite;

            Component apronPylonPrefab =
                apronPylonObject.AddComponent(
                    pylonType);

            SetPrivateField(
                apronPylonPrefab,
                "spriteRenderer",
                apronPylonRenderer);

            SetPrivateField(
                preview,
                "foundationRuntimeHost",
                runtimeHost);

            SetPrivateField(
                preview,
                "previewTilemap",
                tilemap);

            SetPrivateField(
                preview,
                "previewTile",
                previewTile);

            SetPrivateField(
                preview,
                "previewApronTile",
                previewApronTile);

            SetPrivateField(
                preview,
                "viewHost",
                viewHost);

            SetPrivateField(
                preview,
                "pylonPrefab",
                pylonPrefab);

            SetPrivateField(
                preview,
                "apronPylonPrefab",
                apronPylonPrefab);

            InvokePrivateMethod(
                preview,
                "Awake");

            return new PreviewFixture(
                mapObject,
                runtimeHostObject,
                viewHostObject,
                gridObject,
                pylonObject,
                apronPylonObject,
                tilemap,
                previewTile,
                previewApronTile,
                pylonTexture,
                pylonSprite,
                runtimeHost,
                preview);
        }


        private static GridMapDefinition CreateMapDefinition()
        {
            List<GridPosition> validCells =
                new List<GridPosition>();

            for (int y = 0;
                 y <= 4;
                 y++)
            {
                for (int x = 0;
                     x <= 4;
                     x++)
                {
                    validCells.Add(
                        new GridPosition(
                            x,
                            y));
                }
            }

            return new GridMapDefinition(
                "foundation.preview.test",
                validCells);
        }


        private static T GetPublicProperty<T>(
            object target,
            string propertyName)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance
                    | BindingFlags.Public);

            Assert.That(
                property,
                Is.Not.Null,
                $"Missing public property '{propertyName}'.");

            return (T)property.GetValue(target);
        }


        private static object InvokePublicMethod(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance
                    | BindingFlags.Public);

            Assert.That(
                method,
                Is.Not.Null,
                $"Missing public method '{methodName}'.");

            return method.Invoke(
                target,
                arguments);
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

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing private field '{fieldName}'.");

            field.SetValue(target, value);
        }


        private static void SetAutoPropertyBackingField(
            object target,
            string propertyName,
            object value)
        {
            SetPrivateField(
                target,
                $"<{propertyName}>k__BackingField",
                value);
        }


        private static object InvokePrivateMethod(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.That(
                method,
                Is.Not.Null,
                $"Missing private method '{methodName}'.");

            return method.Invoke(
                target,
                arguments);
        }


        private sealed class PreviewFixture
        {
            private readonly GameObject mapObject;
            private readonly GameObject runtimeHostObject;
            private readonly GameObject viewHostObject;
            private readonly GameObject gridObject;
            private readonly GameObject pylonObject;
            private readonly GameObject apronPylonObject;
            private readonly Texture2D pylonTexture;
            private readonly Sprite pylonSprite;

            public Tilemap Tilemap { get; }
            public Tile PreviewTile { get; }
            public Tile PreviewApronTile { get; }
            public FoundationRuntimeHost RuntimeHost { get; }
            public Component Preview { get; }


            public PreviewFixture(
                GameObject mapObject,
                GameObject runtimeHostObject,
                GameObject viewHostObject,
                GameObject gridObject,
                GameObject pylonObject,
                GameObject apronPylonObject,
                Tilemap tilemap,
                Tile previewTile,
                Tile previewApronTile,
                Texture2D pylonTexture,
                Sprite pylonSprite,
                FoundationRuntimeHost runtimeHost,
                Component preview)
            {
                this.mapObject = mapObject;
                this.runtimeHostObject = runtimeHostObject;
                this.viewHostObject = viewHostObject;
                this.gridObject = gridObject;
                this.pylonObject = pylonObject;
                this.apronPylonObject = apronPylonObject;
                this.pylonTexture = pylonTexture;
                this.pylonSprite = pylonSprite;
                Tilemap = tilemap;
                PreviewTile = previewTile;
                PreviewApronTile = previewApronTile;
                RuntimeHost = runtimeHost;
                Preview = preview;
            }


            public void Dispose()
            {
                Object.DestroyImmediate(PreviewTile);
                Object.DestroyImmediate(PreviewApronTile);
                Object.DestroyImmediate(gridObject);
                Object.DestroyImmediate(pylonObject);
                Object.DestroyImmediate(apronPylonObject);
                Object.DestroyImmediate(pylonSprite);
                Object.DestroyImmediate(pylonTexture);
                Object.DestroyImmediate(viewHostObject);
                Object.DestroyImmediate(runtimeHostObject);
                Object.DestroyImmediate(mapObject);
            }
        }
    }
}
