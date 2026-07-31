using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.View;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FoundationApronTilemapViewSystemTests
    {
        [Test]
        public void FoundationAdded_RebuildsApronOnceLateUpdateRuns()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            try
            {
                GridPosition foundation =
                    new GridPosition(2, 2);

                Assert.That(
                    fixture.Service.TryEnsureFoundations(
                        new[] { foundation }).Succeeded,
                    Is.True);

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(0));

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                IReadOnlyList<GridPosition> expectedApron =
                    FoundationApronResolver.Resolve(
                        fixture.MapDefinition,
                        new[] { foundation });

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(8));

                AssertApronTiles(
                    fixture,
                    expectedApron,
                    fixture.Projection);

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


        [Test]
        public void FoundationRemoved_ClearsDerivedApronAfterLateUpdate()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            try
            {
                GridPosition foundation =
                    new GridPosition(2, 2);

                Assert.That(
                    fixture.Service.TryEnsureFoundations(
                        new[] { foundation }).Succeeded,
                    Is.True);

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(8));

                Assert.That(
                    fixture.Service.TryClearFoundations(
                        new[] { foundation }).Succeeded,
                    Is.True);

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(0));

                Assert.That(
                    fixture.Tilemap.GetUsedTilesCount(),
                    Is.EqualTo(0));
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void BatchFoundationPlacement_ProducesOneCompleteOuterRing()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            try
            {
                GridPosition[] foundations =
                {
                    new GridPosition(1, 1),
                    new GridPosition(2, 1),
                    new GridPosition(1, 2),
                    new GridPosition(2, 2)
                };

                Assert.That(
                    fixture.Service.TryEnsureFoundations(
                        foundations).Succeeded,
                    Is.True);

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(0));

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(12));
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void OrientationChange_RebuildsApronAtProjectedCells()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            try
            {
                GridPosition foundation =
                    new GridPosition(2, 1);

                Assert.That(
                    fixture.Service.TryEnsureFoundations(
                        new[] { foundation }).Succeeded,
                    Is.True);

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "HandleOrientationChanging",
                    IsometricViewOrientation.North,
                    IsometricViewOrientation.East);

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(0));

                IsometricViewProjection eastProjection =
                    fixture.Projection.WithOrientation(
                        IsometricViewOrientation.East);

                SetAutoPropertyBackingField(
                    fixture.ViewHost,
                    "Projection",
                    eastProjection);

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "HandleOrientationChanged",
                    IsometricViewOrientation.North,
                    IsometricViewOrientation.East);

                IReadOnlyList<GridPosition> apron =
                    FoundationApronResolver.Resolve(
                        fixture.MapDefinition,
                        new[] { foundation });

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(apron.Count));

                AssertApronTiles(
                    fixture,
                    apron,
                    eastProjection);
            }
            finally
            {
                fixture.Dispose();
            }
        }


        [Test]
        public void OccupiedUnownedCell_IsNeverOverwrittenOrCleared()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            Tile foreignTile =
                ScriptableObject.CreateInstance<Tile>();

            try
            {
                GridPosition foundation =
                    new GridPosition(2, 2);

                Vector3Int occupiedApronCell =
                    new Vector3Int(1, 1, 0);

                fixture.Tilemap.SetTile(
                    occupiedApronCell,
                    foreignTile);

                Assert.That(
                    fixture.Service.TryEnsureFoundations(
                        new[] { foundation }).Succeeded,
                    Is.True);

                LogAssert.Expect(
                    LogType.Error,
                    "FoundationApronTilemapViewSystem refused to overwrite " +
                    "an unowned tile at (1, 1, 0). Assign a dedicated " +
                    "empty Foundation Apron Tilemap.");

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                Assert.That(
                    fixture.Tilemap.GetTile(occupiedApronCell),
                    Is.SameAs(foreignTile));

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(7));

                Assert.That(
                    fixture.Service.TryClearFoundations(
                        new[] { foundation }).Succeeded,
                    Is.True);

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                Assert.That(
                    fixture.Tilemap.GetTile(occupiedApronCell),
                    Is.SameAs(foreignTile));
            }
            finally
            {
                Object.DestroyImmediate(foreignTile);
                fixture.Dispose();
            }
        }


        [Test]
        public void OtherLogicalLevel_DoesNotRenderOnConfiguredLevel()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North,
                    levels: 2);

            try
            {
                Assert.That(
                    fixture.Service.TryEnsureFoundations(
                        new[]
                        {
                            new GridPosition(2, 2, 1)
                        }).Succeeded,
                    Is.True);

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "LateUpdate");

                Assert.That(
                    fixture.ViewSystem.VisibleApronCount,
                    Is.EqualTo(0));

                Assert.That(
                    fixture.Tilemap.GetUsedTilesCount(),
                    Is.EqualTo(0));
            }
            finally
            {
                fixture.Dispose();
            }
        }


        private static ViewFixture CreateFixture(
            IsometricViewOrientation orientation,
            int levels = 1)
        {
            GameObject gridObject =
                new GameObject("Foundation Apron View Grid");

            gridObject.SetActive(false);
            gridObject.AddComponent<Grid>();

            GameObject tilemapObject =
                new GameObject("Foundation Apron Views");

            tilemapObject.transform.SetParent(
                gridObject.transform,
                false);

            Tilemap tilemap =
                tilemapObject.AddComponent<Tilemap>();

            tilemapObject.AddComponent<TilemapRenderer>();

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
                    orientation);

            SetAutoPropertyBackingField(
                viewHost,
                "Projection",
                projection);

            GameObject runtimeHostObject =
                new GameObject("Foundation Runtime Host");

            runtimeHostObject.SetActive(false);

            FoundationRuntimeHost runtimeHost =
                runtimeHostObject.AddComponent<FoundationRuntimeHost>();

            FoundationApronTilemapViewSystem viewSystem =
                tilemapObject.AddComponent<
                    FoundationApronTilemapViewSystem>();

            Tile apronTile =
                ScriptableObject.CreateInstance<Tile>();

            SetPrivateField(
                viewSystem,
                "foundationRuntimeHost",
                runtimeHost);

            SetPrivateField(
                viewSystem,
                "apronTilemap",
                tilemap);

            SetPrivateField(
                viewSystem,
                "apronTile",
                apronTile);

            SetPrivateField(
                viewSystem,
                "viewHost",
                viewHost);

            GridMapDefinition mapDefinition =
                CreateMapDefinition(levels);

            ConstructionAreaDefinition constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    mapDefinition.EnumerateValidCells());

            FoundationState foundationState =
                new FoundationState();

            FoundationConstructionService service =
                new FoundationConstructionService(
                    mapDefinition,
                    constructionArea,
                    foundationState,
                    UnrestrictedFoundationRemovalValidator.Instance);

            InvokePrivateMethod(
                viewSystem,
                "AttachToFoundationState",
                foundationState,
                mapDefinition);

            return new ViewFixture(
                gridObject,
                viewHostObject,
                runtimeHostObject,
                tilemap,
                apronTile,
                viewHost,
                viewSystem,
                foundationState,
                mapDefinition,
                service,
                projection);
        }


        private static GridMapDefinition CreateMapDefinition(
            int levels)
        {
            List<GridPosition> validCells =
                new List<GridPosition>();

            for (int level = 0;
                 level < levels;
                 level++)
            {
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
                                y,
                                level));
                    }
                }
            }

            return new GridMapDefinition(
                "foundation.apron.view.test",
                validCells);
        }


        private static void AssertApronTiles(
            ViewFixture fixture,
            IReadOnlyList<GridPosition> apron,
            IsometricViewProjection projection)
        {
            for (int index = 0;
                 index < apron.Count;
                 index++)
            {
                GridPosition displayCell =
                    projection.ToDisplayCell(
                        apron[index]);

                Vector3Int unityCell =
                    new Vector3Int(
                        displayCell.X,
                        displayCell.Y,
                        0);

                Assert.That(
                    fixture.Tilemap.GetTile(unityCell),
                    Is.SameAs(fixture.ApronTile));
            }
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


        private static void InvokePrivateMethod(
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

            method.Invoke(target, arguments);
        }


        private sealed class ViewFixture
        {
            private readonly GameObject gridObject;
            private readonly GameObject viewHostObject;
            private readonly GameObject runtimeHostObject;

            public Tilemap Tilemap { get; }
            public Tile ApronTile { get; }
            public IsometricViewHost ViewHost { get; }
            public FoundationApronTilemapViewSystem ViewSystem { get; }
            public FoundationState FoundationState { get; }
            public GridMapDefinition MapDefinition { get; }
            public FoundationConstructionService Service { get; }
            public IsometricViewProjection Projection { get; }


            public ViewFixture(
                GameObject gridObject,
                GameObject viewHostObject,
                GameObject runtimeHostObject,
                Tilemap tilemap,
                Tile apronTile,
                IsometricViewHost viewHost,
                FoundationApronTilemapViewSystem viewSystem,
                FoundationState foundationState,
                GridMapDefinition mapDefinition,
                FoundationConstructionService service,
                IsometricViewProjection projection)
            {
                this.gridObject = gridObject;
                this.viewHostObject = viewHostObject;
                this.runtimeHostObject = runtimeHostObject;
                Tilemap = tilemap;
                ApronTile = apronTile;
                ViewHost = viewHost;
                ViewSystem = viewSystem;
                FoundationState = foundationState;
                MapDefinition = mapDefinition;
                Service = service;
                Projection = projection;
            }


            public void Dispose()
            {
                Object.DestroyImmediate(ApronTile);
                Object.DestroyImmediate(runtimeHostObject);
                Object.DestroyImmediate(viewHostObject);
                Object.DestroyImmediate(gridObject);
            }
        }
    }
}
