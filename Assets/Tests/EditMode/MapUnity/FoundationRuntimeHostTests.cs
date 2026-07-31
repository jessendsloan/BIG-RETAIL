using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FoundationRuntimeHostTests
    {
        [Test]
        public void TryInitialize_ActivatedMap_CreatesFoundationSubsystem()
        {
            GameObject mapObject =
                new GameObject("Foundation Runtime Map");

            GameObject hostObject =
                new GameObject("Foundation Runtime Host");

            mapObject.SetActive(false);
            hostObject.SetActive(false);

            try
            {
                GridMapHost mapHost =
                    mapObject.AddComponent<GridMapHost>();
                ConfigureInitializedMapHost(mapHost);

                FoundationRuntimeHost host =
                    hostObject.AddComponent<FoundationRuntimeHost>();

                SetPrivateField(
                    host,
                    "mapHost",
                    mapHost);

                int initializedEventCount = 0;
                host.Initialized +=
                    initializedHost =>
                        initializedEventCount++;

                bool initialized =
                    host.TryInitialize();

                Assert.That(initialized, Is.True);
                Assert.That(host.IsInitialized, Is.True);
                Assert.That(host.FoundationState, Is.Not.Null);
                Assert.That(host.FoundationConstruction, Is.Not.Null);
                Assert.That(host.MapDefinition, Is.Not.Null);
                Assert.That(
                    host.MapDefinition.MapId,
                    Is.EqualTo(
                        "foundation.runtime.test"));
                Assert.That(
                    host.FoundationState.FoundationCount,
                    Is.EqualTo(0));
                Assert.That(
                    initializedEventCount,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
                Object.DestroyImmediate(mapObject);
            }
        }

        [Test]
        public void TryInitialize_RepeatedCall_PreservesSubsystemInstances()
        {
            GameObject mapObject =
                new GameObject("Foundation Runtime Map");

            GameObject hostObject =
                new GameObject("Foundation Runtime Host");

            mapObject.SetActive(false);
            hostObject.SetActive(false);

            try
            {
                GridMapHost mapHost =
                    mapObject.AddComponent<GridMapHost>();

                ConfigureInitializedMapHost(mapHost);

                FoundationRuntimeHost host =
                    hostObject.AddComponent<FoundationRuntimeHost>();

                SetPrivateField(
                    host,
                    "mapHost",
                    mapHost);

                Assert.That(
                    host.TryInitialize(),
                    Is.True);

                FoundationState initialState =
                    host.FoundationState;

                FoundationConstructionService initialService =
                    host.FoundationConstruction;

                Assert.That(
                    host.TryInitialize(),
                    Is.True);

                Assert.That(
                    host.FoundationState,
                    Is.SameAs(initialState));

                Assert.That(
                    host.FoundationConstruction,
                    Is.SameAs(initialService));
            }
            finally
            {
                Object.DestroyImmediate(hostObject);
                Object.DestroyImmediate(mapObject);
            }
        }


        [Test]
        public void ValidateRemoval_FloorOccupiesCell_IsBlocked()
        {
            GameObject mapObject =
                new GameObject("Supported Floor Map");

            GameObject foundationObject =
                new GameObject("Foundation Runtime Host");

            GameObject floorObject =
                new GameObject("Floor Runtime Host");

            mapObject.SetActive(false);
            foundationObject.SetActive(false);
            floorObject.SetActive(false);

            try
            {
                GridMapHost mapHost =
                    mapObject.AddComponent<GridMapHost>();

                ConfigureInitializedMapHost(mapHost);

                FoundationRuntimeHost foundationHost =
                    foundationObject
                        .AddComponent<FoundationRuntimeHost>();

                FloorRuntimeHost floorHost =
                    floorObject.AddComponent<FloorRuntimeHost>();

                SetPrivateField(
                    foundationHost,
                    "mapHost",
                    mapHost);

                SetPrivateField(
                    foundationHost,
                    "floorRuntimeHost",
                    floorHost);

                SetPrivateField(
                    floorHost,
                    "mapHost",
                    mapHost);

                SetPrivateField(
                    floorHost,
                    "foundationRuntimeHost",
                    foundationHost);

                Assert.That(
                    foundationHost.TryInitialize(),
                    Is.True);

                Assert.That(
                    floorHost.TryInitialize(),
                    Is.True);

                GridPosition cell =
                    new GridPosition(0, 0);

                Assert.That(
                    foundationHost.FoundationConstruction
                        .TryEnsureFoundations(
                            new[] { cell })
                        .Succeeded,
                    Is.True);

                Assert.That(
                    floorHost.FloorConstruction
                        .TryEnsureFloors(
                            new[] { cell })
                        .Succeeded,
                    Is.True);

                FoundationRemovalValidation validation =
                    foundationHost.ValidateRemoval(
                        new[] { cell });

                Assert.That(validation.IsAllowed, Is.False);
                Assert.That(validation.BlockedCell, Is.EqualTo(cell));
            }
            finally
            {
                Object.DestroyImmediate(floorObject);
                Object.DestroyImmediate(foundationObject);
                Object.DestroyImmediate(mapObject);
            }
        }


        [Test]
        public void ValidateRemoval_WallRequiresAtLeastOneRemainingFoundation()
        {
            GameObject mapObject =
                new GameObject("Supported Wall Map");

            GameObject foundationObject =
                new GameObject("Foundation Runtime Host");

            GameObject floorObject =
                new GameObject("Floor Runtime Host");

            mapObject.SetActive(false);
            foundationObject.SetActive(false);
            floorObject.SetActive(false);

            try
            {
                GridMapHost mapHost =
                    mapObject.AddComponent<GridMapHost>();

                ConfigureInitializedMapHost(mapHost);

                CellEdge wall =
                    new CellEdge(
                        new GridPosition(0, 0),
                        CellEdgeDirection.NorthEast);

                SetAutoPropertyBackingField(
                    mapHost,
                    "WallState",
                    new WallState(
                        new[] { wall }));

                FoundationRuntimeHost foundationHost =
                    foundationObject
                        .AddComponent<FoundationRuntimeHost>();

                FloorRuntimeHost floorHost =
                    floorObject.AddComponent<FloorRuntimeHost>();

                SetPrivateField(
                    foundationHost,
                    "mapHost",
                    mapHost);

                SetPrivateField(
                    foundationHost,
                    "floorRuntimeHost",
                    floorHost);

                SetPrivateField(
                    floorHost,
                    "mapHost",
                    mapHost);

                SetPrivateField(
                    floorHost,
                    "foundationRuntimeHost",
                    foundationHost);

                Assert.That(
                    foundationHost.TryInitialize(),
                    Is.True);

                Assert.That(
                    floorHost.TryInitialize(),
                    Is.True);

                Assert.That(
                    foundationHost.FoundationConstruction
                        .TryEnsureFoundations(
                            new[] { wall.FirstCell })
                        .Succeeded,
                    Is.True);

                FoundationRemovalValidation blockedValidation =
                    foundationHost.ValidateRemoval(
                        new[] { wall.FirstCell });

                Assert.That(
                    blockedValidation.IsAllowed,
                    Is.False);

                Assert.That(
                    foundationHost.FoundationConstruction
                        .TryEnsureFoundations(
                            new[] { wall.SecondCell })
                        .Succeeded,
                    Is.True);

                FoundationRemovalValidation allowedValidation =
                    foundationHost.ValidateRemoval(
                        new[] { wall.FirstCell });

                Assert.That(
                    allowedValidation.IsAllowed,
                    Is.True);

                FoundationRemovalValidation batchValidation =
                    foundationHost.ValidateRemoval(
                        new[]
                        {
                            wall.FirstCell,
                            wall.SecondCell
                        });

                Assert.That(
                    batchValidation.IsAllowed,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(floorObject);
                Object.DestroyImmediate(foundationObject);
                Object.DestroyImmediate(mapObject);
            }
        }

        private static void ConfigureInitializedMapHost(
            GridMapHost mapHost)
        {
            GridPosition[] validCells =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1)
            };

            GridMapDefinition mapDefinition =
                new GridMapDefinition(
                    "foundation.runtime.test",
                    validCells);

            ConstructionAreaDefinition constructionArea =
                new ConstructionAreaDefinition(
                    mapDefinition,
                    validCells);

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
                "WallState",
                new WallState());

            SetAutoPropertyBackingField(
                mapHost,
                "IsInitialized",
                true);
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
    }
}
