using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Simulation.Time.Unity;
using BigRetail.StoreLayouts;
using BigRetail.StoreLayouts.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.Editor.StoreLayouts
{
    /// <summary>
    /// Creates Frank's first authored operating state and keeps the campaign
    /// scene wired to it without hand-editing serialized scene YAML.
    /// Existing scenario content is never overwritten by this setup command.
    /// </summary>
    public static class FrankOpeningScenarioSetup
    {
        public const string ScenarioAssetPath =
            "Assets/Design/StoreScenarios/FrankOpeningShiftScenarioV1.asset";

        private const string ScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        private const string LayoutAssetPath =
            "Assets/Design/StoreLayouts/FrankStoreLayoutV1.asset";

        private const string MenuPath =
            "Big Retail/Campaign/Create or Validate Frank Opening Scenario";

        private const int OpeningHour = 6;
        private const int OpeningMinute = 45;
        private const int OpeningBackstockUnitsPerProduct = 24;
        private const int OpeningDeliveryCaseCount = 4;
        private const long OpeningCashCents = 250000;
        private const int DeterministicSeed = 104729;
        private const string OpeningDeliveryId =
            "FRANK-OPENING-RIDGEWAY-CASES";
        private const string OpeningDeliverySupplierId = "BIG";
        private const string OpeningDeliveryProductId =
            "RIDGEWAY-ORIGINAL-CHIPS-SINGLE";


        [MenuItem(MenuPath)]
        public static void CreateOrValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling)
            {
                throw new InvalidOperationException(
                    "Frank's opening scenario setup requires Edit Mode "
                    + "after Unity finishes compiling.");
            }

            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
            StoreLayoutAsset layout =
                AssetDatabase.LoadAssetAtPath<StoreLayoutAsset>(
                    LayoutAssetPath);

            if (layout == null)
            {
                throw new InvalidOperationException(
                    $"Frank's saved layout is missing at "
                    + $"'{LayoutAssetPath}'.");
            }

            ConfigureScene(scene, layout);

            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Unity could not save '{ScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Frank Opening Shift Scenario v1 is authored and wired.");
        }


        internal static void ConfigureScene(
            Scene scene,
            StoreLayoutAsset layout)
        {
            FixtureRuntimeHost fixtureHost =
                FindRequired<FixtureRuntimeHost>(scene);
            FixturePlanogramRuntimeHost merchandisingHost =
                FindRequired<FixturePlanogramRuntimeHost>(scene);
            SimulationTimeRuntimeHost timeHost =
                FindRequired<SimulationTimeRuntimeHost>(scene);
            PurchasingRuntimeHost purchasingHost =
                FindRequired<PurchasingRuntimeHost>(scene);

            GridMapHost mapHost =
                FindRequired<GridMapHost>(scene);
            mapHost.Initialize();

            if (!mapHost.IsInitialized
                || !fixtureHost.TryInitialize()
                || !merchandisingHost.TryInitialize())
            {
                throw new InvalidOperationException(
                    "Frank's fixture and product catalogs could not "
                    + "initialize for scenario authoring.");
            }

            timeHost.Initialize();

            if (!purchasingHost.TryInitialize()
                || purchasingHost.Catalog == null)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(
                        purchasingHost.InitializationError)
                        ? "Frank's Purchasing runtime could not initialize "
                            + "for scenario authoring."
                        : purchasingHost.InitializationError);
            }

            StoreScenarioAsset scenario =
                AssetDatabase.LoadAssetAtPath<StoreScenarioAsset>(
                    ScenarioAssetPath);

            if (scenario == null)
            {
                EnsureScenarioFolder();
                scenario =
                    ScriptableObject.CreateInstance<StoreScenarioAsset>();
                scenario.ReplaceData(
                    CreateOpeningScenario(
                        layout.CreateRuntimeCopy(),
                        fixtureHost,
                        merchandisingHost.Products,
                        purchasingHost.Catalog));
                AssetDatabase.CreateAsset(
                    scenario,
                    ScenarioAssetPath);
            }

            StoreLayoutSceneBootstrap bootstrap =
                FindRequired<StoreLayoutSceneBootstrap>(scene);
            SerializedObject serialized =
                new SerializedObject(bootstrap);

            SetReference(serialized, "initialScenario", scenario);
            SetReference(
                serialized,
                "simulationTimeRuntimeHost",
                timeHost);
            SetReference(
                serialized,
                "fixturePlanogramRuntimeHost",
                merchandisingHost);
            SetReference(
                serialized,
                "purchasingRuntimeHost",
                purchasingHost);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
        }


        private static StoreScenarioData CreateOpeningScenario(
            StoreLayoutData layout,
            FixtureRuntimeHost fixtureHost,
            ProductCatalog products,
            CommercialCatalog commercialCatalog)
        {
            StoreScenarioData scenario =
                new StoreScenarioData
                {
                    ScenarioId =
                        "bigretail.scenario.frank.opening_shift.v1",
                    DisplayName = "Frank Opening Shift",
                    MapId = layout.MapId,
                    LayoutId = layout.LayoutId,
                    StartingGameSeconds =
                        OpeningHour * 60L * 60L
                        + OpeningMinute * 60L,
                    StartingSimulationSpeed = 1,
                    StartingStoreCashCents = OpeningCashCents,
                    DeterministicSeed = DeterministicSeed
                };

            List<ProductDefinition> productDefinitions =
                new List<ProductDefinition>();

            foreach (
                ProductDefinition product
                in products.EnumerateDefinitions())
            {
                productDefinitions.Add(product);
            }

            List<StoreFixtureData> merchandiseFixtures =
                new List<StoreFixtureData>();
            List<StoreFixtureData> checkoutFixtures =
                new List<StoreFixtureData>();

            for (int index = 0;
                 index < layout.Fixtures.Count;
                 index++)
            {
                StoreFixtureData fixtureData =
                    layout.Fixtures[index];

                if (!fixtureHost.Definitions.TryGetDefinition(
                        new FixtureDefinitionId(
                            fixtureData.DefinitionId),
                        out FixtureDefinition definition))
                {
                    throw new InvalidOperationException(
                        $"Frank's layout references missing fixture "
                        + $"definition '{fixtureData.DefinitionId}'.");
                }

                if (definition.MerchandisingProfile.HasDisplayFaces)
                {
                    merchandiseFixtures.Add(fixtureData);
                }

                if (HasCheckoutAccess(definition.AccessProfile))
                {
                    checkoutFixtures.Add(fixtureData);
                }
            }

            merchandiseFixtures.Sort(CompareFixtures);
            checkoutFixtures.Sort(CompareFixtures);

            if (productDefinitions.Count == 0
                || merchandiseFixtures.Count
                    < productDefinitions.Count)
            {
                throw new InvalidOperationException(
                    "Frank's opening scenario requires at least one "
                    + "merchandise fixture per opening product.");
            }

            for (int index = 0;
                 index < productDefinitions.Count;
                 index++)
            {
                ProductDefinition product =
                    productDefinitions[index];
                StoreFixtureData fixtureData =
                    merchandiseFixtures[index];
                if (!fixtureHost.Definitions.TryGetDefinition(
                        new FixtureDefinitionId(
                            fixtureData.DefinitionId),
                        out FixtureDefinition definition))
                {
                    throw new InvalidOperationException(
                        $"Frank's layout references missing fixture "
                        + $"definition '{fixtureData.DefinitionId}'.");
                }
                int displayQuantity =
                    AddFullFixturePlanogram(
                        scenario,
                        fixtureData.InstanceId,
                        definition.MerchandisingProfile,
                        product.Id);

                scenario.DisplayInventory.Add(
                    new StoreDisplayInventoryData
                    {
                        FixtureInstanceId =
                            fixtureData.InstanceId,
                        ProductId = product.Id.Value,
                        Quantity = displayQuantity
                    });
                if (!string.Equals(
                        product.Id.Value,
                        OpeningDeliveryProductId,
                        StringComparison.Ordinal))
                {
                    scenario.BackstockInventory.Add(
                        new StoreInventoryLineData
                        {
                            ProductId = product.Id.Value,
                            Quantity =
                                OpeningBackstockUnitsPerProduct
                        });
                }
            }

            AddOpeningDelivery(
                scenario,
                commercialCatalog);

            for (int index = 0;
                 index < checkoutFixtures.Count;
                 index++)
            {
                scenario.Checkouts.Add(
                    new StoreCheckoutData
                    {
                        FixtureInstanceId =
                            checkoutFixtures[index].InstanceId,
                        IsOpen = index == 0
                    });
            }

            return scenario;
        }

        private static void AddOpeningDelivery(
            StoreScenarioData scenario,
            CommercialCatalog commercialCatalog)
        {
            SupplierId supplierId =
                new SupplierId(OpeningDeliverySupplierId);
            ProductId productId =
                new ProductId(OpeningDeliveryProductId);
            SupplierOfferDefinition match = null;

            foreach (
                SupplierOfferDefinition offer
                in commercialCatalog.Offers.EnumerateForSupplier(
                    supplierId,
                    availableOnly: false))
            {
                if (offer.ProductId != productId)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"Frank's opener found multiple "
                        + $"'{supplierId}' offers for '{productId}'.");
                }

                match = offer;
            }

            if (match == null)
            {
                throw new InvalidOperationException(
                    $"Frank's opener requires a '{supplierId}' offer "
                    + $"for '{productId}'.");
            }

            scenario.Deliveries.Add(
                new StoreDeliveryData
                {
                    DeliveryId = OpeningDeliveryId,
                    SupplierId = supplierId.Value,
                    ArrivalGameSeconds =
                        scenario.StartingGameSeconds,
                    Status = StoreDeliveryStatus.ReadyToReceive,
                    Lines =
                    {
                        new StoreInventoryLineData
                        {
                            ProductId = productId.Value,
                            Quantity = checked(
                                match.PurchasePackQuantity
                                * OpeningDeliveryCaseCount)
                        }
                    }
                });
        }

        private static int AddFullFixturePlanogram(
            StoreScenarioData scenario,
            string fixtureId,
            FixtureMerchandisingProfile profile,
            ProductId productId)
        {
            int assignedFrontageUnits = 0;

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureDisplayFaceDefinition face =
                    profile.GetDisplayFace(faceIndex);

                for (int shelfRunIndex = 0;
                     shelfRunIndex < face.ShelfRunCount;
                     shelfRunIndex++)
                {
                    for (int frontageIndex = 0;
                         frontageIndex < face.FrontageUnitsPerRun;
                         frontageIndex++)
                    {
                        scenario.PlanogramAssignments.Add(
                            new StorePlanogramAssignmentData
                            {
                                FixtureInstanceId = fixtureId,
                                DisplayFaceIndex = faceIndex,
                                ShelfRunIndex = shelfRunIndex,
                                FrontageUnitIndex = frontageIndex,
                                ProductId = productId.Value
                            });
                        assignedFrontageUnits++;
                    }
                }
            }

            return assignedFrontageUnits
                * FixtureDisplayInventoryService.UnitsPerFrontageUnit;
        }

        private static bool HasCheckoutAccess(
            FixtureAccessProfile profile)
        {
            bool hasCustomer = false;
            bool hasEmployee = false;

            for (FixtureSide side = FixtureSide.North;
                 side <= FixtureSide.West;
                 side++)
            {
                FixtureAccessMode mode = profile.GetMode(side);
                hasCustomer |=
                    mode.Includes(
                        FixtureAccessMode.CustomerCheckout);
                hasEmployee |=
                    mode.Includes(
                        FixtureAccessMode.EmployeeCheckout);
            }

            return hasCustomer && hasEmployee;
        }

        private static int CompareFixtures(
            StoreFixtureData left,
            StoreFixtureData right)
        {
            return string.Compare(
                left.InstanceId,
                right.InstanceId,
                StringComparison.Ordinal);
        }

        private static void EnsureScenarioFolder()
        {
            const string parent = "Assets/Design";
            const string folder = "Assets/Design/StoreScenarios";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(
                    parent,
                    "StoreScenarios");
            }
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"StoreLayoutSceneBootstrap is missing serialized "
                    + $"field '{propertyName}'.");
            }

            property.objectReferenceValue = value;
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

            throw new InvalidOperationException(
                $"Scene '{scene.path}' is missing "
                + $"{typeof(T).FullName}.");
        }
    }
}
