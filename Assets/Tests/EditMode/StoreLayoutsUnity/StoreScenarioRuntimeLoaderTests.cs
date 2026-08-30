using System;
using System.Collections.Generic;
using BigRetail.Departments.Unity;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.Floors;
using BigRetail.Map.Unity.Foundations;
using BigRetail.Map.Unity.Sidewalks;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Unity;
using BigRetail.Simulation.Time.Domain;
using BigRetail.Simulation.Time.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.StoreLayouts.Unity.Tests
{
    public sealed class StoreScenarioRuntimeLoaderTests
    {
        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string LayoutPath =
            "Assets/Design/StoreLayouts/FrankStoreLayoutV1.asset";

        private const string ScenarioPath =
            "Assets/Design/StoreScenarios/FrankOpeningShiftScenarioV1.asset";

        private const string OpeningMerchandiseFixtureId =
            "D58D297252D749968D57BA9B107DBA1A";

        private const string OpeningProductId =
            "RIDGEWAY-ORIGINAL-CHIPS-SINGLE";


        [Test]
        public void FrankOpeningScenario_PlansOneEmptyChipFixture()
        {
            StoreScenarioAsset asset =
                AssetDatabase.LoadAssetAtPath<StoreScenarioAsset>(
                    ScenarioPath);

            Assert.That(asset, Is.Not.Null);
            StoreScenarioData scenario = asset.CreateRuntimeCopy();

            Assert.That(scenario.PlanogramAssignments.Count, Is.EqualTo(15));

            for (int index = 0;
                 index < scenario.PlanogramAssignments.Count;
                 index++)
            {
                StorePlanogramAssignmentData assignment =
                    scenario.PlanogramAssignments[index];

                Assert.That(
                    assignment.FixtureInstanceId,
                    Is.EqualTo(OpeningMerchandiseFixtureId));
                Assert.That(
                    assignment.ProductId,
                    Is.EqualTo(OpeningProductId));
                Assert.That(
                    assignment.DisplayFaceIndex,
                    Is.Zero);
            }

            Assert.That(scenario.DisplayInventory.Count, Is.EqualTo(1));
            Assert.That(
                scenario.DisplayInventory[0].FixtureInstanceId,
                Is.EqualTo(OpeningMerchandiseFixtureId));
            Assert.That(
                scenario.DisplayInventory[0].ProductId,
                Is.EqualTo(OpeningProductId));
            Assert.That(scenario.DisplayInventory[0].Quantity, Is.Zero);
            Assert.That(scenario.BackstockInventory, Is.Empty);
        }


        [Test]
        public void FrankOpeningScenario_LoadsOperationalStartingState()
        {
            Scene scene = OpenFrankScene();

            try
            {
                RuntimeFixture runtime = CreateRuntime(scene);
                string assetBefore =
                    EditorJsonUtility.ToJson(runtime.ScenarioAsset);

                StoreScenarioLoadResult result =
                    runtime.ScenarioLoader.Load(
                        runtime.ScenarioAsset,
                        runtime.LayoutAsset);

                Assert.That(result.Succeeded, Is.True, result.Message);
                AssertOpeningState(runtime);
                Assert.That(
                    EditorJsonUtility.ToJson(runtime.ScenarioAsset),
                    Is.EqualTo(assetBefore));
            }
            finally
            {
                OpenEmptyScene();
            }
        }

        [Test]
        public void FrankOpeningScenario_ReloadResetsDeterministically()
        {
            Scene scene = OpenFrankScene();

            try
            {
                RuntimeFixture runtime = CreateRuntime(scene);
                StoreScenarioLoadResult first =
                    runtime.ScenarioLoader.Load(
                        runtime.ScenarioAsset,
                        runtime.LayoutAsset);

                Assert.That(first.Succeeded, Is.True, first.Message);

                StoreScenarioData expected =
                    runtime.ScenarioAsset.CreateRuntimeCopy();

                runtime.Merchandising.Cash.Credit(777);
                runtime.Time.Clock.Advance(15d);

                FixtureInstanceId openCheckout =
                    FindExpectedOpenCheckout(expected);
                Assert.That(
                    runtime.Merchandising.Checkout.TrySetOpen(
                        openCheckout,
                        false),
                    Is.True);

                StoreScenarioLoadResult second =
                    runtime.ScenarioLoader.Load(
                        runtime.ScenarioAsset,
                        runtime.LayoutAsset);

                Assert.That(second.Succeeded, Is.True, second.Message);
                AssertOpeningState(runtime);
                Assert.That(
                    runtime.ScenarioLoader.ActiveScenarioId,
                    Is.EqualTo(expected.ScenarioId));
                Assert.That(
                    runtime.ScenarioLoader.ActiveDeterministicSeed,
                    Is.EqualTo(expected.DeterministicSeed));
            }
            finally
            {
                OpenEmptyScene();
            }
        }

        [Test]
        public void FrankOpeningScenario_InvalidProductDoesNotMutateRuntime()
        {
            Scene scene = OpenFrankScene();

            try
            {
                RuntimeFixture runtime = CreateRuntime(scene);
                StoreScenarioLoadResult first =
                    runtime.ScenarioLoader.Load(
                        runtime.ScenarioAsset,
                        runtime.LayoutAsset);

                Assert.That(first.Succeeded, Is.True, first.Message);

                StoreScenarioData expected =
                    runtime.ScenarioAsset.CreateRuntimeCopy();
                StoreDisplayInventoryData observedLine =
                    expected.DisplayInventory[0];
                FixtureInstanceId observedFixture =
                    new FixtureInstanceId(
                        observedLine.FixtureInstanceId);
                ProductId observedProduct =
                    new ProductId(observedLine.ProductId);
                int quantityBefore =
                    runtime.Merchandising.Inventory.GetQuantity(
                        FixtureDisplayInventoryService
                            .GetDisplayLocationId(observedFixture),
                        observedProduct);
                long cashBefore =
                    runtime.Merchandising.Cash.BalanceCents;
                SimulationClockState timeBefore =
                    runtime.Time.CaptureState();

                StoreScenarioData invalid =
                    runtime.ScenarioAsset.CreateRuntimeCopy();
                invalid.PlanogramAssignments[0].ProductId =
                    "MISSING-PRODUCT";

                StoreScenarioLoadResult rejected =
                    runtime.ScenarioLoader.Load(
                        invalid,
                        runtime.LayoutAsset.CreateRuntimeCopy());

                Assert.That(rejected.Succeeded, Is.False);
                Assert.That(
                    rejected.Failure,
                    Is.EqualTo(
                        StoreScenarioLoadFailure.ValidationFailed));
                Assert.That(
                    runtime.Merchandising.Cash.BalanceCents,
                    Is.EqualTo(cashBefore));
                Assert.That(
                    runtime.Time.CaptureState().TotalGameSeconds,
                    Is.EqualTo(timeBefore.TotalGameSeconds));
                Assert.That(
                    runtime.Merchandising.Inventory.GetQuantity(
                        FixtureDisplayInventoryService
                            .GetDisplayLocationId(observedFixture),
                        observedProduct),
                    Is.EqualTo(quantityBefore));
                AssertOpeningState(runtime);
            }
            finally
            {
                OpenEmptyScene();
            }
        }


        private static void AssertOpeningState(
            RuntimeFixture runtime)
        {
            StoreScenarioData expected =
                runtime.ScenarioAsset.CreateRuntimeCopy();

            Assert.That(
                runtime.Time.Clock.CurrentTime.TotalGameSeconds,
                Is.EqualTo(expected.StartingGameSeconds));
            Assert.That(
                runtime.Time.Clock.Speed,
                Is.EqualTo(
                    (SimulationSpeed)
                        expected.StartingSimulationSpeed));
            Assert.That(
                runtime.Merchandising.Cash.BalanceCents,
                Is.EqualTo(expected.StartingStoreCashCents));
            Assert.That(
                runtime.Merchandising.PlanogramState
                    .AssignedShelfRunCount,
                Is.GreaterThan(0));

            for (int index = 0;
                 index < expected.DisplayInventory.Count;
                 index++)
            {
                StoreDisplayInventoryData line =
                    expected.DisplayInventory[index];
                FixtureInstanceId fixtureId =
                    new FixtureInstanceId(line.FixtureInstanceId);

                Assert.That(
                    runtime.Merchandising.Inventory.GetQuantity(
                        FixtureDisplayInventoryService
                            .GetDisplayLocationId(fixtureId),
                        new ProductId(line.ProductId)),
                    Is.EqualTo(line.Quantity),
                    $"Display stock {line.FixtureInstanceId} / "
                    + line.ProductId);
            }

            int expectedBackstock = 0;

            for (int index = 0;
                 index < expected.BackstockInventory.Count;
                 index++)
            {
                expectedBackstock +=
                    expected.BackstockInventory[index].Quantity;
            }

            Assert.That(
                runtime.Merchandising.Backstock.StoredUnitCount
                + runtime.Merchandising.Backstock
                    .UnallocatedUnitCount,
                Is.EqualTo(expectedBackstock));

            AssertOpeningDelivery(runtime, expected);

            for (int index = 0;
                 index < expected.Checkouts.Count;
                 index++)
            {
                StoreCheckoutData checkout =
                    expected.Checkouts[index];

                Assert.That(
                    runtime.Merchandising.Checkout.IsOpen(
                        new FixtureInstanceId(
                            checkout.FixtureInstanceId)),
                    Is.EqualTo(checkout.IsOpen),
                    checkout.FixtureInstanceId);
            }
        }

        private static void AssertOpeningDelivery(
            RuntimeFixture runtime,
            StoreScenarioData expected)
        {
            Assert.That(expected.Deliveries.Count, Is.EqualTo(1));
            Assert.That(
                runtime.Purchasing.Fulfillment
                    .ReadyToReceiveOrderCount,
                Is.EqualTo(1));
            Assert.That(
                runtime.Merchandising.Backstock.GetAvailableQuantity(
                    new ProductId(
                        "RIDGEWAY-ORIGINAL-CHIPS-SINGLE")),
                Is.EqualTo(0));
            Assert.That(
                runtime.Merchandising.DisplayInventory
                    .GetDisplayedQuantity(
                        new ProductId(
                            "RIDGEWAY-ORIGINAL-CHIPS-SINGLE")),
                Is.EqualTo(0));

            List<InboundDeliveryLoad> loads =
                new List<InboundDeliveryLoad>(
                    runtime.Purchasing.Fulfillment
                        .EnumerateReadyDeliveries());

            Assert.That(loads.Count, Is.EqualTo(1));
            Assert.That(loads[0].SupplierId.Value, Is.EqualTo("BIG"));
            Assert.That(loads[0].PurchasePackCount, Is.EqualTo(4));
            Assert.That(loads[0].RemainingUnitCount, Is.EqualTo(48));
            Assert.That(loads[0].Lines.Count, Is.EqualTo(1));
            Assert.That(
                loads[0].Lines[0].ProductId.Value,
                Is.EqualTo("RIDGEWAY-ORIGINAL-CHIPS-SINGLE"));
            Assert.That(
                loads[0].Lines[0].PurchasePackCount,
                Is.EqualTo(4));
        }

        private static FixtureInstanceId FindExpectedOpenCheckout(
            StoreScenarioData scenario)
        {
            for (int index = 0;
                 index < scenario.Checkouts.Count;
                 index++)
            {
                if (scenario.Checkouts[index].IsOpen)
                {
                    return new FixtureInstanceId(
                        scenario.Checkouts[index]
                            .FixtureInstanceId);
                }
            }

            Assert.Fail(
                "Frank's scenario has no expected open checkout.");
            return default;
        }

        private static RuntimeFixture CreateRuntime(
            Scene scene)
        {
            GridMapHost mapHost = FindRequired<GridMapHost>(scene);
            FoundationRuntimeHost foundationHost =
                FindRequired<FoundationRuntimeHost>(scene);
            SidewalkRuntimeHost sidewalkHost =
                FindRequired<SidewalkRuntimeHost>(scene);
            FloorRuntimeHost floorHost =
                FindRequired<FloorRuntimeHost>(scene);
            FixtureRuntimeHost fixtureHost =
                FindRequired<FixtureRuntimeHost>(scene);
            FixturePlanogramRuntimeHost merchandisingHost =
                FindRequired<FixturePlanogramRuntimeHost>(scene);
            DepartmentRuntimeHost departmentHost =
                FindRequired<DepartmentRuntimeHost>(scene);
            ReceivingAreaRuntimeHost receivingHost =
                FindRequired<ReceivingAreaRuntimeHost>(scene);
            SimulationTimeRuntimeHost timeHost =
                FindRequired<SimulationTimeRuntimeHost>(scene);
            PurchasingRuntimeHost purchasingHost =
                FindRequired<PurchasingRuntimeHost>(scene);
            StoreLayoutAsset layout =
                AssetDatabase.LoadAssetAtPath<StoreLayoutAsset>(
                    LayoutPath);
            StoreScenarioAsset scenario =
                AssetDatabase.LoadAssetAtPath<StoreScenarioAsset>(
                    ScenarioPath);

            Assert.That(layout, Is.Not.Null);
            Assert.That(scenario, Is.Not.Null);

            mapHost.Initialize();
            Assert.That(fixtureHost.TryInitialize(), Is.True);

            StoreLayoutRuntimeLoader layoutLoader =
                new StoreLayoutRuntimeLoader(
                    mapHost,
                    foundationHost,
                    sidewalkHost,
                    floorHost,
                    fixtureHost,
                    new FixtureEquipmentPlanState(),
                    departmentHost,
                    receivingHost);
            StoreLayoutLoadResult layoutResult =
                layoutLoader.Load(layout);

            Assert.That(
                layoutResult.Succeeded,
                Is.True,
                layoutResult.Message);
            Assert.That(
                merchandisingHost.TryInitialize(),
                Is.True);
            timeHost.Initialize();
            Assert.That(
                purchasingHost.TryInitialize(),
                Is.True,
                purchasingHost.InitializationError);

            return new RuntimeFixture(
                layout,
                scenario,
                merchandisingHost,
                timeHost,
                purchasingHost,
                new StoreScenarioRuntimeLoader(
                    fixtureHost,
                    merchandisingHost,
                    timeHost,
                    purchasingHost));
        }

        private static Scene OpenFrankScene()
        {
            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
        }

        private static T FindRequired<T>(
            Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component =
                    root.GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            Assert.Fail(
                $"Scene '{scene.path}' is missing "
                + $"{typeof(T).FullName}.");
            return null;
        }

        private static void OpenEmptyScene()
        {
            try
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
            catch (InvalidOperationException)
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.DefaultGameObjects,
                    NewSceneMode.Single);
            }
        }


        private sealed class RuntimeFixture
        {
            public RuntimeFixture(
                StoreLayoutAsset layoutAsset,
                StoreScenarioAsset scenarioAsset,
                FixturePlanogramRuntimeHost merchandising,
                SimulationTimeRuntimeHost time,
                PurchasingRuntimeHost purchasing,
                StoreScenarioRuntimeLoader scenarioLoader)
            {
                LayoutAsset = layoutAsset;
                ScenarioAsset = scenarioAsset;
                Merchandising = merchandising;
                Time = time;
                Purchasing = purchasing;
                ScenarioLoader = scenarioLoader;
            }


            public StoreLayoutAsset LayoutAsset { get; }

            public StoreScenarioAsset ScenarioAsset { get; }

            public FixturePlanogramRuntimeHost Merchandising { get; }

            public SimulationTimeRuntimeHost Time { get; }

            public PurchasingRuntimeHost Purchasing { get; }

            public StoreScenarioRuntimeLoader ScenarioLoader { get; }
        }
    }
}
