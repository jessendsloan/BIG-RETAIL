using System;
using BigRetail.Core.Session;
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
        private const int GrayboxPurchaseCaseUnitCount = 24;
        private const long GrayboxOpeningCashCents = 250000;
        private const long WorkshopDisplayedCashCents = 999999999;

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

        public FixtureSalesService Sales { get; private set; }

        public FixtureCheckoutService Checkout { get; private set; }

        public StorageLocationId BackstockLocationId =>
            GrayboxBackstockLocationId;

        public FixturePlanogramState PlanogramState =>
            Planograms?.State;

        public event Action<FixturePlanogramRuntimeHost> Initialized;


        public bool TryGetProductAsset(
            ProductId productId,
            out ProductDefinitionAsset productAsset)
        {
            if (productCatalogAsset == null)
            {
                productAsset = null;
                return false;
            }

            return productCatalogAsset.TryGetAsset(
                productId,
                out productAsset);
        }


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

            Checkout?.Dispose();
            Checkout = null;
            Sales = null;

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

            // A rack provides storage capacity, not free merchandise. Product
            // enters the store only through purchasing and receiving.
            Inventory =
                new InventoryState(
                    Products,
                    new[] { backstockLocation });

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

            Cash = MapWorkshopSession.IsActive
                ? StoreCashState.CreateUnlimited(
                    WorkshopDisplayedCashCents)
                : new StoreCashState(GrayboxOpeningCashCents);

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

            Sales =
                new FixtureSalesService(
                    Products,
                    Cash);

            Checkout =
                new FixtureCheckoutService(
                    fixtureRuntimeHost.FixtureState,
                    Sales);

            IsInitialized = true;
            Initialized?.Invoke(this);

            string cashSummary = Cash.IsUnlimited
                ? "unlimited Map Workshop cash"
                : $"{Cash.BalanceCents} cents opening cash";

            Debug.Log(
                $"Activated fixture merchandising graybox with "
                + $"{Products.Count} placeholder product(s), "
                + $"{Backstock.StoredUnitCount} unit(s) stored in physical "
                + $"racks, {Backstock.UnallocatedUnitCount} inbound/overflow "
                + $"unit(s), {Backstock.OccupiedCaseSlotCount} of "
                + $"{Backstock.CaseSlotCapacity} physical case slot(s) "
                + $"occupied, and {cashSummary}.",
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
