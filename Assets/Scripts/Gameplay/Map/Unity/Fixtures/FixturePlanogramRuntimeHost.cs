using System;
using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Inventory.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Unity lifecycle host for fixture planogram state and editing rules.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-60)]
    public sealed class FixturePlanogramRuntimeHost : MonoBehaviour
    {
        private const int GrayboxBackstockUnitsPerProduct = 144;
        private const int GrayboxPurchaseCaseUnitCount = 24;
        private const long GrayboxOpeningCashCents = 250000;

        private static readonly StorageLocationId GrayboxBackstockLocationId =
            new StorageLocationId("GRAYBOX-BACKSTOCK");

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private ProductCatalogAsset productCatalogAsset;


        public bool IsInitialized { get; private set; }

        public ProductCatalog Products { get; private set; }

        public FixturePlanogramService Planograms { get; private set; }

        public InventoryState Inventory { get; private set; }

        public FixtureBackstockService Backstock { get; private set; }

        public FixturePurchasingService Purchasing { get; private set; }

        public StoreCashState Cash { get; private set; }

        public FixtureDisplayInventoryService DisplayInventory { get; private set; }

        public StorageLocationId BackstockLocationId =>
            GrayboxBackstockLocationId;

        public FixturePlanogramState PlanogramState =>
            Planograms?.State;

        public event Action<FixturePlanogramRuntimeHost> Initialized;


        private void OnEnable()
        {
            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized += HandleFixtureRuntimeInitialized;

                if (fixtureRuntimeHost.IsInitialized)
                {
                    TryInitialize();
                }
            }
        }

        private void Start()
        {
            if (!TryInitialize())
            {
                Debug.LogError(
                    "FixturePlanogramRuntimeHost could not initialize because fixture or product data is unavailable.",
                    this);
            }
        }

        private void OnDisable()
        {
            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized -= HandleFixtureRuntimeInitialized;
            }

            DisplayInventory?.Dispose();
            DisplayInventory = null;

            Backstock?.Dispose();
            Backstock = null;

            Purchasing = null;
            Cash = null;

            Planograms?.Dispose();
            Planograms = null;
            Inventory = null;
            Products = null;
            IsInitialized = false;
        }


        public bool TryInitialize()
        {
            if (IsInitialized)
            {
                return true;
            }

            if (fixtureRuntimeHost == null
                || !fixtureRuntimeHost.TryInitialize()
                || fixtureRuntimeHost.FixtureState == null
                || productCatalogAsset == null)
            {
                return false;
            }

            if (!productCatalogAsset.TryCreateCatalog(
                    out ProductCatalog productCatalog,
                    out string error))
            {
                Debug.LogError(error, productCatalogAsset);
                return false;
            }

            Products = productCatalog;

            StorageLocationDefinition backstockLocation =
                new StorageLocationDefinition(
                    GrayboxBackstockLocationId,
                    "Graybox Backstock",
                    StorageRole.Backroom);

            List<StockBalance> initialBalances =
                new List<StockBalance>();

            foreach (
                ProductDefinition product
                in Products.EnumerateDefinitions())
            {
                initialBalances.Add(
                    new StockBalance(
                        GrayboxBackstockLocationId,
                        product.Id,
                        GrayboxBackstockUnitsPerProduct));
            }

            Inventory =
                new InventoryState(
                    Products,
                    new[] { backstockLocation },
                    initialBalances);

            Planograms =
                new FixturePlanogramService(
                    fixtureRuntimeHost.FixtureState,
                    Products);

            Backstock =
                new FixtureBackstockService(
                    fixtureRuntimeHost.FixtureState,
                    Products,
                    Inventory,
                    GrayboxBackstockLocationId);

            Cash =
                new StoreCashState(GrayboxOpeningCashCents);

            Purchasing =
                new FixturePurchasingService(
                    Products,
                    Backstock,
                    Cash,
                    GrayboxPurchaseCaseUnitCount);

            DisplayInventory =
                new FixtureDisplayInventoryService(
                    fixtureRuntimeHost.FixtureState,
                    Planograms.State,
                    Products,
                    Inventory,
                    Backstock);

            IsInitialized = true;
            Initialized?.Invoke(this);

            Debug.Log(
                $"Activated fixture merchandising graybox with {Products.Count} placeholder product(s), {Backstock.StoredUnitCount} unit(s) stored in physical racks, {Backstock.UnallocatedUnitCount} inbound/overflow unit(s), {Backstock.CapacityUnitCount} total rack capacity, and {Cash.BalanceCents} cents opening cash.",
                this);

            return true;
        }


        private void HandleFixtureRuntimeInitialized(
            FixtureRuntimeHost initializedHost)
        {
            TryInitialize();
        }
    }
}
