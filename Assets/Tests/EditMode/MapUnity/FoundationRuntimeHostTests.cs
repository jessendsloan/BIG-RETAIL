using System.Collections.Generic;
using System.Reflection;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.Unity.Foundations;
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
