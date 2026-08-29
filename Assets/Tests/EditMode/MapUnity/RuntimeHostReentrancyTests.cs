using System.Reflection;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Walls;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class RuntimeHostReentrancyTests
    {
        private const string FloorCatalogPath =
            "Assets/Design/Floors/Finishes/FloorFinishCatalog.asset";

        private const string FixtureCatalogPath =
            "Assets/Design/Fixtures/FixtureDefinitionCatalog.asset";


        [Test]
        public void TryInitialize_NestedDependencyEventsPreserveOneState()
        {
            GameObject root =
                new GameObject("Reentrant Runtime Hosts");
            root.SetActive(false);

            try
            {
                GridMapHost mapHost =
                    root.AddComponent<GridMapHost>();
                FoundationRuntimeHost foundationHost =
                    root.AddComponent<FoundationRuntimeHost>();
                FloorRuntimeHost floorHost =
                    root.AddComponent<FloorRuntimeHost>();
                FixtureRuntimeHost fixtureHost =
                    root.AddComponent<FixtureRuntimeHost>();

                ConfigureInitializedMapHost(mapHost);

                FloorFinishAssetCatalog floorCatalog =
                    AssetDatabase.LoadAssetAtPath<FloorFinishAssetCatalog>(
                        FloorCatalogPath);
                FixtureDefinitionAssetCatalog fixtureCatalog =
                    AssetDatabase.LoadAssetAtPath<
                        FixtureDefinitionAssetCatalog>(
                        FixtureCatalogPath);

                Assert.That(floorCatalog, Is.Not.Null);
                Assert.That(fixtureCatalog, Is.Not.Null);

                SetPrivateField(
                    foundationHost,
                    "mapHost",
                    mapHost);
                SetPrivateField(
                    floorHost,
                    "mapHost",
                    mapHost);
                SetPrivateField(
                    floorHost,
                    "foundationRuntimeHost",
                    foundationHost);
                SetPrivateField(
                    floorHost,
                    "floorFinishAssets",
                    floorCatalog);
                SetPrivateField(
                    fixtureHost,
                    "mapHost",
                    mapHost);
                SetPrivateField(
                    fixtureHost,
                    "floorRuntimeHost",
                    floorHost);
                SetPrivateField(
                    fixtureHost,
                    "definitionAssets",
                    fixtureCatalog);

                FloorState firstFloorState = null;
                FixtureState firstFixtureState = null;
                int floorInitializationCount = 0;
                int fixtureInitializationCount = 0;

                foundationHost.Initialized +=
                    _ => floorHost.TryInitialize();
                floorHost.Initialized +=
                    _ =>
                    {
                        floorInitializationCount++;
                        firstFloorState ??= floorHost.FloorState;
                        fixtureHost.TryInitialize();
                        firstFixtureState ??= fixtureHost.FixtureState;
                    };
                fixtureHost.Initialized +=
                    _ => fixtureInitializationCount++;

                Assert.That(fixtureHost.TryInitialize(), Is.True);

                Assert.That(floorInitializationCount, Is.EqualTo(1));
                Assert.That(fixtureInitializationCount, Is.EqualTo(1));
                Assert.That(
                    floorHost.FloorState,
                    Is.SameAs(firstFloorState));
                Assert.That(
                    fixtureHost.FixtureState,
                    Is.SameAs(firstFixtureState));
            }
            finally
            {
                Object.DestroyImmediate(root);
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
                    "runtime.reentrancy.test",
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
                "ConstructionEligibility",
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
