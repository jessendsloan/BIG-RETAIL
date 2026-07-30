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
    public sealed class FoundationTilemapViewSystemTests
    {
        [Test]
        public void AttachedState_AddAndRemove_SynchronizesTilemap()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            try
            {
                GridPosition cell =
                    new GridPosition(2, 1);

                FoundationConstructionService service =
                    CreateService(
                        fixture.FoundationState);

                Assert.That(
                    service.TryEnsureFoundations(
                        new[] { cell }).Succeeded,
                    Is.True);

                Vector3Int unityCell =
                    new Vector3Int(2, 1, 0);

                Assert.That(
                    fixture.Tilemap.GetTile(unityCell),
                    Is.SameAs(fixture.FoundationTile));

                Assert.That(
                    fixture.ViewSystem.VisibleFoundationCount,
                    Is.EqualTo(1));

                Assert.That(
                    service.TryClearFoundations(
                        new[] { cell }).Succeeded,
                    Is.True);

                Assert.That(
                    fixture.Tilemap.GetTile(unityCell),
                    Is.Null);

                Assert.That(
                    fixture.ViewSystem.VisibleFoundationCount,
                    Is.EqualTo(0));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void OrientationChange_RebuildsAtProjectedCell()
        {
            ViewFixture fixture =
                CreateFixture(
                    IsometricViewOrientation.North);

            try
            {
                GridPosition cell =
                    new GridPosition(3, 1);

                FoundationConstructionService service =
                    CreateService(
                        fixture.FoundationState);

                Assert.That(
                    service.TryEnsureFoundations(
                        new[] { cell }).Succeeded,
                    Is.True);

                Vector3Int northCell =
                    new Vector3Int(3, 1, 0);

                Assert.That(
                    fixture.Tilemap.GetTile(northCell),
                    Is.SameAs(fixture.FoundationTile));

                InvokePrivateMethod(
                    fixture.ViewSystem,
                    "HandleOrientationChanging",
                    IsometricViewOrientation.North,
                    IsometricViewOrientation.East);

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

                GridPosition projected =
                    eastProjection.ToDisplayCell(cell);

                Vector3Int eastCell =
                    new Vector3Int(
                        projected.X,
                        projected.Y,
                        0);

                Assert.That(
                    fixture.Tilemap.GetTile(northCell),
                    Is.Null);

                Assert.That(
                    fixture.Tilemap.GetTile(eastCell),
                    Is.SameAs(fixture.FoundationTile));

                Assert.That(
                    fixture.ViewSystem.VisibleFoundationCount,
                    Is.EqualTo(1));
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
                GridPosition cell =
                    new GridPosition(2, 1);

                Vector3Int unityCell =
                    new Vector3Int(2, 1, 0);

                fixture.Tilemap.SetTile(
                    unityCell,
                    foreignTile);

                FoundationConstructionService service =
                    CreateService(
                        fixture.FoundationState);

                LogAssert.Expect(
                    LogType.Error,
                    "FoundationTilemapViewSystem refused to overwrite " +
                    "an unowned tile at (2, 1, 0). Assign a dedicated " +
                    "empty Foundation Tilemap.");

                Assert.That(
                    service.TryEnsureFoundations(
                        new[] { cell }).Succeeded,
                    Is.True);

                Assert.That(
                    fixture.Tilemap.GetTile(unityCell),
                    Is.SameAs(foreignTile));

                Assert.That(
                    fixture.ViewSystem.VisibleFoundationCount,
                    Is.EqualTo(0));

                Assert.That(
                    service.TryClearFoundations(
                        new[] { cell }).Succeeded,
                    Is.True);

                Assert.That(
                    fixture.Tilemap.GetTile(unityCell),
                    Is.SameAs(foreignTile));
            }
            finally
            {
                Object.DestroyImmediate(foreignTile);
                fixture.Dispose();
            }
        }

        private static ViewFixture CreateFixture(
            IsometricViewOrientation orientation)
        {
            GameObject gridObject =
                new GameObject("Foundation View Grid");

            gridObject.SetActive(false);
            gridObject.AddComponent<Grid>();

            GameObject tilemapObject =
                new GameObject("Foundation Views");

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

            FoundationTilemapViewSystem viewSystem =
                tilemapObject.AddComponent<
                    FoundationTilemapViewSystem>();

            Tile foundationTile =
                ScriptableObject.CreateInstance<Tile>();

            SetPrivateField(
                viewSystem,
                "foundationRuntimeHost",
                runtimeHost);

            SetPrivateField(
                viewSystem,
                "foundationTilemap",
                tilemap);

            SetPrivateField(
                viewSystem,
                "foundationTile",
                foundationTile);

            SetPrivateField(
                viewSystem,
                "viewHost",
                viewHost);

            FoundationState foundationState =
                new FoundationState();

            InvokePrivateMethod(
                viewSystem,
                "AttachToFoundationState",
                foundationState);

            return new ViewFixture(
                gridObject,
                viewHostObject,
                runtimeHostObject,
                tilemap,
                foundationTile,
                viewHost,
                viewSystem,
                foundationState,
                projection);
        }

        private static FoundationConstructionService CreateService(
            FoundationState foundationState)
        {
            GridPosition[] validCells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(2, 0),
                new GridPosition(3, 0),
                new GridPosition(4, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(3, 1),
                new GridPosition(4, 1),
                new GridPosition(0, 2),
                new GridPosition(1, 2),
                new GridPosition(2, 2),
                new GridPosition(3, 2),
                new GridPosition(4, 2),
                new GridPosition(0, 3),
                new GridPosition(1, 3),
                new GridPosition(2, 3),
                new GridPosition(3, 3),
                new GridPosition(4, 3),
                new GridPosition(0, 4),
                new GridPosition(1, 4),
                new GridPosition(2, 4),
                new GridPosition(3, 4),
                new GridPosition(4, 4)
            };

            GridMapDefinition mapDefinition =
                new GridMapDefinition(
                    "foundation.view.test",
                    validCells);

            ConstructionAreaDefinition constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    validCells);

            return new FoundationConstructionService(
                mapDefinition,
                constructionArea,
                foundationState);
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
            public Tile FoundationTile { get; }
            public IsometricViewHost ViewHost { get; }
            public FoundationTilemapViewSystem ViewSystem { get; }
            public FoundationState FoundationState { get; }
            public IsometricViewProjection Projection { get; }

            public ViewFixture(
                GameObject gridObject,
                GameObject viewHostObject,
                GameObject runtimeHostObject,
                Tilemap tilemap,
                Tile foundationTile,
                IsometricViewHost viewHost,
                FoundationTilemapViewSystem viewSystem,
                FoundationState foundationState,
                IsometricViewProjection projection)
            {
                this.gridObject = gridObject;
                this.viewHostObject = viewHostObject;
                this.runtimeHostObject = runtimeHostObject;
                Tilemap = tilemap;
                FoundationTile = foundationTile;
                ViewHost = viewHost;
                ViewSystem = viewSystem;
                FoundationState = foundationState;
                Projection = projection;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(FoundationTile);
                Object.DestroyImmediate(runtimeHostObject);
                Object.DestroyImmediate(viewHostObject);
                Object.DestroyImmediate(gridObject);
            }
        }
    }
}
