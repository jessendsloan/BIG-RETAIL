using System.Collections.Generic;
using System.Globalization;
using BigRetail.Economy.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the fixture inspector to logical selection and planogram
    /// services. UI requests domain operations and never edits their state
    /// directly.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(365)]
    public sealed class FixtureMerchandisingInspectorPresenter : MonoBehaviour
    {
        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private FixtureMerchandisingSelectionHost selectionHost;

        [SerializeField]
        private PurchasingRuntimeHost purchasingRuntimeHost;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;


        private FixtureMerchandisingInspectorView boundView;
        private ConstructionToolbarView boundToolbarView;
        private FixturePlanogramState subscribedPlanogramState;
        private FixtureDisplayInventoryService subscribedDisplayInventory;
        private FixtureBackstockService subscribedBackstock;
        private FixturePurchasingService subscribedPurchasing;
        private StoreCashState subscribedCash;
        private FixtureSalesService subscribedSales;
        private FixtureCheckoutService subscribedCheckout;
        private ReceivingAreaState subscribedReceivingState;
        private bool productsAreBound;
        private string purchasingStatus;


        private void Reset()
        {
            documentHost = GetComponent<ConstructionToolbarDocumentHost>();
        }

        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost = GetComponent<ConstructionToolbarDocumentHost>();
            }
        }

        private void OnEnable()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            documentHost.FixtureMerchandisingInspectorViewReady +=
                HandleViewReady;
            documentHost.ViewReady += HandleToolbarViewReady;
            selectionHost.SelectionChanged += HandleSelectionChanged;
            planogramRuntimeHost.Initialized += HandlePlanogramInitialized;

            if (purchasingRuntimeHost != null)
            {
                purchasingRuntimeHost.Initialized +=
                    HandlePurchasingRuntimeInitialized;
                purchasingRuntimeHost.DeliveriesChanged +=
                    HandleSupplierDeliveriesChanged;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized +=
                    HandleReceivingRuntimeInitialized;
            }

            AttachToPlanogramState();
            AttachToDisplayInventory();
            AttachToBackstock();
            AttachToPurchasing();
            AttachToCash();
            AttachToSales();
            AttachToCheckout();
            AttachToReceivingState();

            if (documentHost.HasView)
            {
                BindToolbarView(documentHost.View);
            }

            if (documentHost.HasFixtureMerchandisingInspectorView)
            {
                BindView(documentHost.FixtureMerchandisingInspectorView);
            }
        }

        private void Start()
        {
            RefreshView();
        }

        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.FixtureMerchandisingInspectorViewReady -=
                    HandleViewReady;
                documentHost.ViewReady -= HandleToolbarViewReady;
            }

            if (selectionHost != null)
            {
                selectionHost.SelectionChanged -= HandleSelectionChanged;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -= HandlePlanogramInitialized;
            }

            if (purchasingRuntimeHost != null)
            {
                purchasingRuntimeHost.Initialized -=
                    HandlePurchasingRuntimeInitialized;
                purchasingRuntimeHost.DeliveriesChanged -=
                    HandleSupplierDeliveriesChanged;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized -=
                    HandleReceivingRuntimeInitialized;
            }

            DetachFromPlanogramState();
            DetachFromDisplayInventory();
            DetachFromBackstock();
            DetachFromPurchasing();
            DetachFromCash();
            DetachFromSales();
            DetachFromCheckout();
            DetachFromReceivingState();
            UnbindToolbarView();
            UnbindView();
        }


        private void HandleViewReady(
            FixtureMerchandisingInspectorView view)
        {
            BindView(view);
        }

        private void HandleToolbarViewReady(
            ConstructionToolbarView view)
        {
            BindToolbarView(view);
        }

        private void HandleSelectionChanged()
        {
            purchasingStatus = null;
            RefreshView();
        }

        private void HandlePlanogramInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            AttachToPlanogramState();
            AttachToDisplayInventory();
            AttachToBackstock();
            AttachToPurchasing();
            AttachToCash();
            AttachToSales();
            AttachToCheckout();
            productsAreBound = false;
            RefreshCashHud();
            RefreshView();
        }

        private void HandleShelfRunChanged(FixtureShelfRunKey shelfRun)
        {
            if (selectionHost.HasSelectedFixture
                && shelfRun.FixtureId == selectionHost.SelectedFixtureId)
            {
                RefreshView();
            }
        }

        private void HandleFixtureStockChanged(
            FixtureInstanceId fixtureId)
        {
            if (selectionHost.HasSelectedFixture
                && (fixtureId == selectionHost.SelectedFixtureId
                    || IsSelectedStorageFixture()))
            {
                RefreshView();
            }
        }

        private void HandleBackstockCapacityChanged()
        {
            if (IsSelectedStorageFixture())
            {
                RefreshView();
            }
        }

        private void HandlePurchasingChanged()
        {
            if (IsSelectedStorageFixture())
            {
                RefreshView();
            }
        }

        private void HandlePurchasingRuntimeInitialized(
            PurchasingRuntimeHost initializedHost)
        {
            purchasingStatus = null;
            RefreshView();
        }

        private void HandleSupplierDeliveriesChanged()
        {
            if (IsSelectedStorageFixture())
            {
                RefreshView();
            }
        }

        private void HandleReceivingRuntimeInitialized(
            ReceivingAreaRuntimeHost initializedHost)
        {
            AttachToReceivingState();
            RefreshView();
        }

        private void HandleReceivingStateChanged()
        {
            if (IsSelectedStorageFixture())
            {
                RefreshView();
            }
        }

        private void HandleCashBalanceChanged()
        {
            RefreshCashHud();
        }

        private void HandleSalesChanged(
            FixtureInstanceId fixtureId)
        {
            if (selectionHost.HasSelectedFixture
                && fixtureId == selectionHost.SelectedFixtureId)
            {
                RefreshView();
            }
        }

        private void HandleCheckoutAvailabilityChanged()
        {
            if (selectionHost.HasSelectedFixture)
            {
                RefreshView();
            }
        }

        private void HandleEditRequested()
        {
            selectionHost.BeginEditing();
        }

        private void HandleRestockRequested()
        {
            if (!selectionHost.HasSelectedFixture
                || planogramRuntimeHost.DisplayInventory == null)
            {
                return;
            }

            FixtureRestockResult result =
                planogramRuntimeHost.DisplayInventory
                    .TryRestockFixture(
                        selectionHost.SelectedFixtureId);

            RefreshView();

            boundView?.SetRestockStatus(
                DescribeRestockResult(result));
        }

        private void HandleDoneRequested()
        {
            selectionHost.EndEditing();
        }

        private void HandleCloseRequested()
        {
            selectionHost.ClearSelection();
        }

        private void HandleProductRequested(ProductId productId)
        {
            if (!TryGetSelectedFrontage(
                    out FixtureShelfRunKey shelfRun,
                    out int frontageUnitIndex))
            {
                return;
            }

            FixturePlanogramState state =
                planogramRuntimeHost.PlanogramState;

            bool succeeded;
            FixturePlanogramFailure failure;

            if (state != null
                && state.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex,
                    out _))
            {
                succeeded =
                    planogramRuntimeHost.Planograms
                        .TryReplaceFacingProduct(
                            shelfRun,
                            frontageUnitIndex,
                            productId,
                            out failure);
            }
            else
            {
                int maximumFrontageUnitCount =
                    planogramRuntimeHost.Planograms
                        .GetMaximumFrontageUnitCount(
                            shelfRun,
                            frontageUnitIndex);

                if (maximumFrontageUnitCount <= 0)
                {
                    RefreshView();
                    return;
                }

                int requestedFrontageUnitCount =
                    Mathf.Clamp(
                        selectionHost.RequestedFrontageUnitCount,
                        1,
                        maximumFrontageUnitCount);

                succeeded =
                    planogramRuntimeHost.Planograms.TryAssignFrontage(
                        shelfRun,
                        frontageUnitIndex,
                        requestedFrontageUnitCount,
                        productId,
                        out failure);
            }

            if (!succeeded)
            {
                boundView?.SetStatus(
                    DescribeFailure(failure),
                    isError: true);
                return;
            }

            selectionHost.ClearFrontageSelection();
        }

        private void HandleWidthDeltaRequested(int delta)
        {
            if (!TryGetSelectedFrontage(
                    out FixtureShelfRunKey shelfRun,
                    out int frontageUnitIndex))
            {
                return;
            }

            FixturePlanogramState state =
                planogramRuntimeHost.PlanogramState;

            if (state != null
                && state.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex,
                    out ProductFacing facing))
            {
                int maximumFrontageUnitCount =
                    planogramRuntimeHost.Planograms
                        .GetMaximumFrontageUnitCount(
                            shelfRun,
                            facing.StartFrontageUnit,
                            facing.ProductId);

                int desiredCount =
                    Mathf.Clamp(
                        facing.FrontageUnitCount + delta,
                        1,
                        Mathf.Max(1, maximumFrontageUnitCount));

                if (desiredCount == facing.FrontageUnitCount)
                {
                    return;
                }

                bool succeeded =
                    planogramRuntimeHost.Planograms.TryResizeFacing(
                        shelfRun,
                        frontageUnitIndex,
                        desiredCount,
                        out FixturePlanogramFailure failure);

                if (!succeeded)
                {
                    boundView?.SetStatus(
                        DescribeFailure(failure),
                        isError: true);
                }

                return;
            }

            int maximumRequestedFrontageUnitCount =
                planogramRuntimeHost.Planograms
                    .GetMaximumFrontageUnitCount(
                        shelfRun,
                        frontageUnitIndex);

            if (maximumRequestedFrontageUnitCount <= 0)
            {
                return;
            }

            selectionHost.SetRequestedFrontageUnitCount(
                Mathf.Clamp(
                    selectionHost.RequestedFrontageUnitCount + delta,
                    1,
                    maximumRequestedFrontageUnitCount));
        }

        private void HandleClearRequested()
        {
            if (!TryGetSelectedFrontage(
                    out FixtureShelfRunKey shelfRun,
                    out int frontageUnitIndex))
            {
                return;
            }

            if (!planogramRuntimeHost.Planograms.TryClearFacing(
                    shelfRun,
                    frontageUnitIndex,
                    out FixturePlanogramFailure failure)
                && failure != FixturePlanogramFailure.NoFacing)
            {
                boundView?.SetStatus(
                    DescribeFailure(failure),
                    isError: true);
                return;
            }

            RefreshView();
        }

        private void HandlePurchaseCaseRequested(ProductId productId)
        {
            FixturePurchasingService purchasing =
                planogramRuntimeHost.Purchasing;

            if (purchasing == null)
            {
                purchasingStatus = "Purchasing unavailable.";
                boundView?.SetPurchasingStatus(purchasingStatus);
                return;
            }

            if (!purchasing.TryPlaceCaseOrder(
                    productId,
                    out FixturePurchaseFailure failure))
            {
                purchasingStatus =
                    DescribePurchaseFailure(productId, failure);
                boundView?.SetPurchasingStatus(purchasingStatus);
                return;
            }

            ProductDefinition product =
                planogramRuntimeHost.Products.GetRequired(productId);

            purchasingStatus =
                $"Added one {purchasing.CaseUnitCount}-unit "
                + $"{ResolveProductName(productId)} case. "
                + $"Spent {FormatMoney(product.WholesaleCaseCostCents)}; "
                + $"{FormatMoney(purchasing.CashBalanceCents)} remains.";

            boundView?.SetPurchasingStatus(purchasingStatus);
        }

        private void HandleReceiveDeliveryRequested()
        {
            if (purchasingRuntimeHost != null
                && purchasingRuntimeHost.IsInitialized)
            {
                PurchaseOrderReceivingResult supplierReceipt =
                    purchasingRuntimeHost.ReceiveAvailableDeliveries();

                if (supplierReceipt.ReceivedUnitCount <= 0)
                {
                    purchasingStatus =
                        "There are no arrived supplier deliveries to receive.";
                }
                else if (supplierReceipt.FailedUnitCount > 0)
                {
                    purchasingStatus =
                        $"Received {supplierReceipt.ReceivedUnitCount} units; "
                        + $"{supplierReceipt.FailedUnitCount} units still need space.";
                }
                else if (planogramRuntimeHost.Backstock != null
                         && planogramRuntimeHost.Backstock.UnallocatedUnitCount > 0)
                {
                    purchasingStatus =
                        $"Received {supplierReceipt.ReceivedUnitCount} units. "
                        + $"{planogramRuntimeHost.Backstock.UnallocatedUnitCount} "
                        + "units await rack space.";
                }
                else
                {
                    purchasingStatus =
                        $"Received {supplierReceipt.ReceivedUnitCount} supplier units into storage.";
                }

                boundView?.SetPurchasingStatus(purchasingStatus);
                return;
            }

            FixturePurchasingService purchasing =
                planogramRuntimeHost.Purchasing;

            if (purchasing == null)
            {
                purchasingStatus = "Receiving unavailable.";
                boundView?.SetPurchasingStatus(purchasingStatus);
                return;
            }

            FixtureDeliveryReceipt receipt =
                purchasing.ReceivePendingDelivery();

            if (receipt.ReceivedUnitCount <= 0)
            {
                purchasingStatus = "There is no pending delivery to receive.";
            }
            else if (receipt.FailedUnitCount > 0)
            {
                purchasingStatus =
                    $"Received {receipt.ReceivedUnitCount} units; "
                    + $"{receipt.FailedUnitCount} units remain pending.";
            }
            else if (planogramRuntimeHost.Backstock != null
                     && planogramRuntimeHost.Backstock.UnallocatedUnitCount > 0)
            {
                purchasingStatus =
                    $"Received {receipt.ReceivedUnitCount} units. "
                    + $"{planogramRuntimeHost.Backstock.UnallocatedUnitCount} "
                    + "units await rack space.";
            }
            else
            {
                purchasingStatus =
                    $"Received {receipt.ReceivedUnitCount} units into storage.";
            }

            boundView?.SetPurchasingStatus(purchasingStatus);
        }

        private void BindView(FixtureMerchandisingInspectorView view)
        {
            UnbindView();
            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.EditRequested += HandleEditRequested;
            boundView.RestockRequested += HandleRestockRequested;
            boundView.DoneRequested += HandleDoneRequested;
            boundView.CloseRequested += HandleCloseRequested;
            boundView.ProductRequested += HandleProductRequested;
            boundView.WidthDeltaRequested += HandleWidthDeltaRequested;
            boundView.ClearRequested += HandleClearRequested;
            boundView.PurchaseCaseRequested += HandlePurchaseCaseRequested;
            boundView.ReceiveDeliveryRequested +=
                HandleReceiveDeliveryRequested;
            productsAreBound = false;
            RefreshView();
        }

        private void BindToolbarView(ConstructionToolbarView view)
        {
            boundToolbarView = view;
            RefreshCashHud();
        }

        private void UnbindToolbarView()
        {
            boundToolbarView = null;
        }

        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.EditRequested -= HandleEditRequested;
                boundView.RestockRequested -= HandleRestockRequested;
                boundView.DoneRequested -= HandleDoneRequested;
                boundView.CloseRequested -= HandleCloseRequested;
                boundView.ProductRequested -= HandleProductRequested;
                boundView.WidthDeltaRequested -= HandleWidthDeltaRequested;
                boundView.ClearRequested -= HandleClearRequested;
                boundView.PurchaseCaseRequested -=
                    HandlePurchaseCaseRequested;
                boundView.ReceiveDeliveryRequested -=
                    HandleReceiveDeliveryRequested;
            }

            boundView = null;
            productsAreBound = false;
        }

        private void RefreshView()
        {
            if (boundView == null)
            {
                return;
            }

            FixtureInstance fixture = null;

            bool isVisible =
                selectionHost.HasSelectedFixture
                && fixtureRuntimeHost.FixtureState != null
                && fixtureRuntimeHost.FixtureState.TryGetFixture(
                    selectionHost.SelectedFixtureId,
                    out fixture);

            boundView.SetVisible(isVisible);

            if (!isVisible)
            {
                return;
            }

            boundView.SetFixtureTitle(fixture.Definition.DisplayName);

            if (fixture.Definition.StorageProfile.ProvidesBackstockStorage)
            {
                boundView.SetStorageMode(true);
                RefreshStorageSummary(fixture);
                return;
            }

            boundView.SetStorageMode(false);
            EnsureProductsAreBound();
            RefreshPlanogramSummary(fixture);
            RefreshInventorySummary(fixture);
            boundView.SetEditing(selectionHost.IsEditing);
            boundView.SetFrontageControlsVisible(
                selectionHost.IsEditing
                && selectionHost.HasSelectedFrontageUnit);

            if (!selectionHost.IsEditing)
            {
                boundView.SetStatus(
                    "Fixture planogram. Choose Edit Products to merchandise its shelves.");
                return;
            }

            if (!selectionHost.HasSelectedFrontageUnit)
            {
                boundView.SetStatus(
                    "Click a shelf slot to choose where the product should go.");
                boundView.SetSelectedProduct(default);
                return;
            }

            FixtureShelfRunKey shelfRun = selectionHost.SelectedShelfRun;
            int frontageUnitIndex =
                selectionHost.SelectedFrontageUnitIndex;

            boundView.SetShelfLabel(
                $"{DescribeFace(shelfRun.LocalDisplaySide)} face · "
                + $"Shelf {shelfRun.ShelfRunIndex + 1} · "
                + $"Slot {frontageUnitIndex + 1}");

            if (planogramRuntimeHost.PlanogramState != null
                && planogramRuntimeHost.PlanogramState.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex,
                    out ProductFacing facing))
            {
                int maximumFrontageUnitCount =
                    planogramRuntimeHost.Planograms
                        .GetMaximumFrontageUnitCount(
                            shelfRun,
                            facing.StartFrontageUnit,
                            facing.ProductId);

                boundView.SetWidth(
                    facing.FrontageUnitCount,
                    maximumFrontageUnitCount);
                boundView.SetSelectedProduct(facing.ProductId);
                boundView.SetStatus(
                    $"Assigned: {GetProductName(facing.ProductId)}.");
            }
            else
            {
                int maximumFrontageUnitCount =
                    planogramRuntimeHost.Planograms
                        .GetMaximumFrontageUnitCount(
                            shelfRun,
                            frontageUnitIndex);

                int requestedFrontageUnitCount =
                    Mathf.Clamp(
                        selectionHost.RequestedFrontageUnitCount,
                        1,
                        Mathf.Max(1, maximumFrontageUnitCount));

                if (requestedFrontageUnitCount
                    != selectionHost.RequestedFrontageUnitCount)
                {
                    selectionHost.SetRequestedFrontageUnitCount(
                        requestedFrontageUnitCount);
                    return;
                }

                boundView.SetWidth(
                    requestedFrontageUnitCount,
                    maximumFrontageUnitCount);
                boundView.SetSelectedProduct(default);
                boundView.SetStatus(
                    "Empty slot. Choose a product.");
            }
        }

        private void RefreshCashHud()
        {
            boundToolbarView?.SetCashBalance(
                planogramRuntimeHost.Cash?.BalanceCents ?? 0);
        }

        private void EnsureProductsAreBound()
        {
            if (productsAreBound
                || planogramRuntimeHost.Products == null)
            {
                return;
            }

            boundView.SetProducts(
                planogramRuntimeHost.Products.EnumerateDefinitions());
            productsAreBound = true;
        }

        private void RefreshPlanogramSummary(FixtureInstance fixture)
        {
            int totalFrontageUnitCount = 0;
            int assignedFrontageUnitCount = 0;
            HashSet<ProductId> assignedProducts =
                new HashSet<ProductId>();

            FixtureMerchandisingProfile profile =
                fixture.Definition.MerchandisingProfile;

            for (int faceIndex = 0;
                 faceIndex < profile.DisplayFaceCount;
                 faceIndex++)
            {
                FixtureDisplayFaceDefinition displayFace =
                    profile.GetDisplayFace(faceIndex);

                totalFrontageUnitCount +=
                    displayFace.ShelfRunCount
                    * displayFace.FrontageUnitsPerRun;

                for (int shelfRunIndex = 0;
                     shelfRunIndex < displayFace.ShelfRunCount;
                     shelfRunIndex++)
                {
                    FixtureShelfRunKey shelfRun =
                        new FixtureShelfRunKey(
                            fixture.Id,
                            displayFace.LocalSide,
                            shelfRunIndex);

                    CountAssignedFrontage(
                        shelfRun,
                        displayFace.FrontageUnitsPerRun,
                        assignedProducts,
                        ref assignedFrontageUnitCount);
                }
            }

            boundView.SetPlanogramSummary(
                assignedFrontageUnitCount,
                totalFrontageUnitCount,
                assignedProducts.Count);
        }

        private void RefreshInventorySummary(FixtureInstance fixture)
        {
            FixtureDisplayInventoryService displayInventory =
                planogramRuntimeHost.DisplayInventory;

            if (displayInventory == null
                || !displayInventory.TryGetSnapshot(
                    fixture.Id,
                    out FixtureDisplayStockSnapshot snapshot))
            {
                boundView.SetInventorySummary(
                    0,
                    0,
                    0,
                    canRestock: false);
                boundView.SetSalesToday(0);
                boundView.SetRestockStatus("Inventory unavailable");
                return;
            }

            boundView.SetInventorySummary(
                snapshot.StockedUnitCount,
                snapshot.CapacityUnitCount,
                snapshot.BackstockUnitCount,
                snapshot.CanRestock);

            boundView.SetSalesToday(
                planogramRuntimeHost.Sales
                    ?.GetFixtureSalesTodayCents(fixture.Id)
                ?? 0);

            string status =
                snapshot.StockedUnitCount > 0
                    && planogramRuntimeHost.Checkout
                        ?.HasOperationalCheckout != true
                    ? "Checkout needed"
                    : snapshot.CapacityUnitCount == 0
                    ? "Awaiting planogram"
                    : snapshot.MissingUnitCount == 0
                        ? "Display full"
                        : snapshot.BackstockUnitCount == 0
                            ? "Backstock empty"
                            : snapshot.StockedUnitCount == 0
                                ? "Ready to stock"
                                : $"{snapshot.MissingUnitCount} units needed";

            boundView.SetRestockStatus(status);
        }

        private void RefreshStorageSummary(FixtureInstance fixture)
        {
            FixtureStorageProfile storageProfile =
                fixture.Definition.StorageProfile;

            FixtureBackstockService backstock =
                planogramRuntimeHost.Backstock;

            if (backstock == null)
            {
                boundView.SetStorageSummary(
                    0,
                    storageProfile.BackstockCaseSlotCapacity,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    "Storage unavailable",
                    isWarning: true);
                boundView.SetStorageContents(null);
                boundView.SetPurchasingProducts(null);
                boundView.SetPurchasingSummary(
                    cashBalanceCents: 0,
                    pendingUnitCount: 0,
                    canReceive: false);
                boundView.SetPurchasingStatus("Purchasing unavailable.");
                return;
            }

            int rackCaseSlotCapacity =
                backstock.GetRackCaseSlotCapacity(fixture.Id);
            int rackOccupiedCaseSlotCount =
                backstock.GetRackOccupiedCaseSlotCount(fixture.Id);
            int rackStoredUnitCount =
                backstock.GetRackStoredUnitCount(fixture.Id);
            int storedUnitCount = backstock.StoredUnitCount;
            int totalCaseSlotCapacity = backstock.CaseSlotCapacity;
            int occupiedCaseSlotCount = backstock.OccupiedCaseSlotCount;
            int unallocatedUnitCount = backstock.UnallocatedUnitCount;

            string status;
            bool isWarning;

            if (backstock.HasStockAwaitingStorage)
            {
                status =
                    $"{unallocatedUnitCount} units await storage";
                isWarning = true;
            }
            else if (!backstock.IsOperational)
            {
                status = "No physical storage";
                isWarning = true;
            }
            else if (backstock.AvailableCaseSlotCount == 0)
            {
                status = "Full";
                isWarning = false;
            }
            else
            {
                status = "Operational";
                isWarning = false;
            }

            boundView.SetStorageSummary(
                rackOccupiedCaseSlotCount,
                rackCaseSlotCapacity,
                rackStoredUnitCount,
                occupiedCaseSlotCount,
                totalCaseSlotCapacity,
                storedUnitCount,
                unallocatedUnitCount,
                backstock.AvailableCaseSlotCount,
                status,
                isWarning);

            List<StorageContentRow> contents =
                new List<StorageContentRow>();

            foreach (
                FixtureBackstockProductSnapshot content
                in backstock.EnumerateRackContents(fixture.Id))
            {
                string productName = content.ProductId.Value;

                if (planogramRuntimeHost.Products != null
                    && planogramRuntimeHost.Products.TryGet(
                        content.ProductId,
                        out ProductDefinition product))
                {
                    productName = product.DisplayName;
                }

                contents.Add(
                    new StorageContentRow(
                        productName,
                        content.Quantity,
                        FixtureMerchandisingGrayboxPalette
                            .ResolveProductColor(content.ProductId)));
            }

            boundView.SetStorageContents(contents);
            RefreshPurchasingSummary();
        }

        private void RefreshPurchasingSummary()
        {
            if (purchasingRuntimeHost != null)
            {
                RefreshSupplierDeliverySummary();
                return;
            }

            FixturePurchasingService purchasing =
                planogramRuntimeHost.Purchasing;
            ProductCatalog products = planogramRuntimeHost.Products;

            if (purchasing == null || products == null)
            {
                boundView.SetPurchasingProducts(null);
                boundView.SetPurchasingSummary(
                    cashBalanceCents: 0,
                    pendingUnitCount: 0,
                    canReceive: false);
                boundView.SetPurchasingStatus("Purchasing unavailable.");
                return;
            }

            List<PurchaseProductRow> rows =
                new List<PurchaseProductRow>();

            foreach (ProductDefinition product in products.EnumerateDefinitions())
            {
                rows.Add(
                    new PurchaseProductRow(
                        product.Id,
                        product.DisplayName,
                        purchasing.CaseUnitCount,
                        product.WholesaleCaseCostCents,
                        purchasing.GetPendingUnitCount(product.Id),
                            product.WholesaleCaseCostCents > 0
                                && purchasing.CashBalanceCents
                                    >= product.WholesaleCaseCostCents,
                        FixtureMerchandisingGrayboxPalette
                            .ResolveProductColor(product.Id)));
            }

            boundView.SetPurchasingProducts(rows);
            boundView.SetPurchasingSummary(
                purchasing.CashBalanceCents,
                purchasing.PendingUnitCount,
                purchasing.HasPendingDelivery);
            boundView.SetPurchasingStatus(purchasingStatus);
        }

        private void RefreshSupplierDeliverySummary()
        {
            boundView.SetPurchasingProducts(null);

            if (!purchasingRuntimeHost.IsInitialized
                || purchasingRuntimeHost.Fulfillment == null)
            {
                boundView.SetPurchasingSummary(
                    cashBalanceCents:
                        purchasingRuntimeHost.Cash?.BalanceCents ?? 0,
                    pendingUnitCount: 0,
                    canReceive: false);
                boundView.SetPurchasingStatus(
                    string.IsNullOrEmpty(
                        purchasingRuntimeHost.InitializationError)
                        ? "Supplier deliveries are waiting for the store session."
                        : purchasingRuntimeHost.InitializationError);
                return;
            }

            PurchaseOrderFulfillmentService fulfillment =
                purchasingRuntimeHost.Fulfillment;
            boundView.SetSupplierReceivingSummary(
                purchasingRuntimeHost.Cash?.BalanceCents ?? 0,
                purchasingRuntimeHost.StagedReadyOrderCount,
                purchasingRuntimeHost.StagedReadyUnitCount,
                canReceive: false);

            string status = purchasingStatus;

            if (string.IsNullOrWhiteSpace(status))
            {
                status = purchasingRuntimeHost.StagedReadyOrderCount > 0
                    ? "Use the Merchandise tool: click a staged supplier "
                        + "pallet to take one case, then click the storage "
                        + "rack that should hold it."
                    : purchasingRuntimeHost
                        .WaitingForReceivingSpaceOrderCount > 0
                        ? purchasingRuntimeHost
                            .WaitingForReceivingSpaceOrderCount == 1
                            ? "1 arrived supplier order is waiting for Receiving space."
                            : $"{purchasingRuntimeHost.WaitingForReceivingSpaceOrderCount} arrived supplier orders are waiting for Receiving space."
                    : fulfillment.ScheduledOrderCount > 0
                        ? fulfillment.ScheduledOrderCount == 1
                            ? "1 supplier delivery is scheduled."
                            : $"{fulfillment.ScheduledOrderCount} supplier deliveries are scheduled."
                        : "Place purchase orders to schedule supplier deliveries.";
            }

            boundView.SetPurchasingStatus(status);
        }

        private void AttachToReceivingState()
        {
            ReceivingAreaState nextState = receivingAreaRuntimeHost != null
                && receivingAreaRuntimeHost.IsInitialized
                    ? receivingAreaRuntimeHost.State
                    : null;

            if (subscribedReceivingState == nextState)
            {
                return;
            }

            DetachFromReceivingState();
            subscribedReceivingState = nextState;

            if (subscribedReceivingState != null)
            {
                subscribedReceivingState.AreaChanged +=
                    HandleReceivingStateChanged;
                subscribedReceivingState.ReservationsChanged +=
                    HandleReceivingStateChanged;
            }
        }

        private void DetachFromReceivingState()
        {
            if (subscribedReceivingState == null)
            {
                return;
            }

            subscribedReceivingState.AreaChanged -=
                HandleReceivingStateChanged;
            subscribedReceivingState.ReservationsChanged -=
                HandleReceivingStateChanged;
            subscribedReceivingState = null;
        }

        private string ResolveProductName(ProductId productId)
        {
            if (planogramRuntimeHost.Products != null
                && planogramRuntimeHost.Products.TryGet(
                    productId,
                    out ProductDefinition product))
            {
                return product.DisplayName;
            }

            return productId.Value;
        }

        private string DescribePurchaseFailure(
            ProductId productId,
            FixturePurchaseFailure failure)
        {
            switch (failure)
            {
                case FixturePurchaseFailure.InsufficientFunds:
                    if (planogramRuntimeHost.Products != null
                        && planogramRuntimeHost.Products.TryGet(
                            productId,
                            out ProductDefinition product))
                    {
                        return
                            $"Not enough cash for a {product.DisplayName} case "
                            + $"({FormatMoney(product.WholesaleCaseCostCents)}).";
                    }

                    return "There is not enough cash for that case.";

                case FixturePurchaseFailure.InvalidCaseCost:
                    return "That product does not have a valid case price.";

                case FixturePurchaseFailure.PendingCapacityExceeded:
                    return "The pending order is already at its supported limit.";

                case FixturePurchaseFailure.UnknownProduct:
                    return "That product is no longer in the store catalog.";

                default:
                    return "That case could not be ordered.";
            }
        }

        private static string FormatMoney(long amountCents)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "${0:N2}",
                amountCents / 100m);
        }

        private void CountAssignedFrontage(
            FixtureShelfRunKey shelfRun,
            int frontageUnitCount,
            HashSet<ProductId> assignedProducts,
            ref int assignedFrontageUnitCount)
        {
            FixturePlanogramState state =
                planogramRuntimeHost.PlanogramState;

            if (state == null)
            {
                return;
            }

            for (int frontageUnitIndex = 0;
                 frontageUnitIndex < frontageUnitCount;
                 frontageUnitIndex++)
            {
                if (!state.TryGetProductAt(
                        shelfRun,
                        frontageUnitIndex,
                        out ProductId productId))
                {
                    continue;
                }

                assignedFrontageUnitCount++;
                assignedProducts.Add(productId);
            }
        }

        private void AttachToPlanogramState()
        {
            FixturePlanogramState nextState =
                planogramRuntimeHost.PlanogramState;

            if (subscribedPlanogramState == nextState)
            {
                return;
            }

            DetachFromPlanogramState();
            subscribedPlanogramState = nextState;

            if (subscribedPlanogramState != null)
            {
                subscribedPlanogramState.ShelfRunChanged +=
                    HandleShelfRunChanged;
            }
        }

        private void DetachFromPlanogramState()
        {
            if (subscribedPlanogramState == null)
            {
                return;
            }

            subscribedPlanogramState.ShelfRunChanged -=
                HandleShelfRunChanged;
            subscribedPlanogramState = null;
        }

        private void AttachToDisplayInventory()
        {
            FixtureDisplayInventoryService nextService =
                planogramRuntimeHost.DisplayInventory;

            if (subscribedDisplayInventory == nextService)
            {
                return;
            }

            DetachFromDisplayInventory();
            subscribedDisplayInventory = nextService;

            if (subscribedDisplayInventory != null)
            {
                subscribedDisplayInventory.FixtureStockChanged +=
                    HandleFixtureStockChanged;
            }
        }

        private void DetachFromDisplayInventory()
        {
            if (subscribedDisplayInventory == null)
            {
                return;
            }

            subscribedDisplayInventory.FixtureStockChanged -=
                HandleFixtureStockChanged;
            subscribedDisplayInventory = null;
        }

        private void AttachToBackstock()
        {
            FixtureBackstockService nextService =
                planogramRuntimeHost.Backstock;

            if (subscribedBackstock == nextService)
            {
                return;
            }

            DetachFromBackstock();
            subscribedBackstock = nextService;

            if (subscribedBackstock != null)
            {
                subscribedBackstock.CapacityChanged +=
                    HandleBackstockCapacityChanged;
                subscribedBackstock.ContentsChanged +=
                    HandleBackstockCapacityChanged;
            }
        }

        private void DetachFromBackstock()
        {
            if (subscribedBackstock == null)
            {
                return;
            }

            subscribedBackstock.CapacityChanged -=
                HandleBackstockCapacityChanged;
            subscribedBackstock.ContentsChanged -=
                HandleBackstockCapacityChanged;
            subscribedBackstock = null;
        }

        private void AttachToPurchasing()
        {
            FixturePurchasingService nextService =
                planogramRuntimeHost.Purchasing;

            if (subscribedPurchasing == nextService)
            {
                return;
            }

            DetachFromPurchasing();
            subscribedPurchasing = nextService;

            if (subscribedPurchasing != null)
            {
                subscribedPurchasing.OrdersChanged +=
                    HandlePurchasingChanged;
            }
        }

        private void DetachFromPurchasing()
        {
            if (subscribedPurchasing == null)
            {
                return;
            }

            subscribedPurchasing.OrdersChanged -=
                HandlePurchasingChanged;
            subscribedPurchasing = null;
        }

        private void AttachToCash()
        {
            StoreCashState nextState =
                planogramRuntimeHost.Cash;

            if (subscribedCash == nextState)
            {
                return;
            }

            DetachFromCash();
            subscribedCash = nextState;

            if (subscribedCash != null)
            {
                subscribedCash.BalanceChanged +=
                    HandleCashBalanceChanged;
            }
        }

        private void DetachFromCash()
        {
            if (subscribedCash == null)
            {
                return;
            }

            subscribedCash.BalanceChanged -=
                HandleCashBalanceChanged;
            subscribedCash = null;
        }

        private void AttachToSales()
        {
            FixtureSalesService nextService =
                planogramRuntimeHost.Sales;

            if (subscribedSales == nextService)
            {
                return;
            }

            DetachFromSales();
            subscribedSales = nextService;

            if (subscribedSales != null)
            {
                subscribedSales.SalesChanged +=
                    HandleSalesChanged;
            }
        }

        private void DetachFromSales()
        {
            if (subscribedSales == null)
            {
                return;
            }

            subscribedSales.SalesChanged -=
                HandleSalesChanged;
            subscribedSales = null;
        }

        private void AttachToCheckout()
        {
            FixtureCheckoutService nextService =
                planogramRuntimeHost.Checkout;

            if (subscribedCheckout == nextService)
            {
                return;
            }

            DetachFromCheckout();
            subscribedCheckout = nextService;

            if (subscribedCheckout != null)
            {
                subscribedCheckout.AvailabilityChanged +=
                    HandleCheckoutAvailabilityChanged;
            }
        }

        private void DetachFromCheckout()
        {
            if (subscribedCheckout == null)
            {
                return;
            }

            subscribedCheckout.AvailabilityChanged -=
                HandleCheckoutAvailabilityChanged;
            subscribedCheckout = null;
        }

        private bool IsSelectedStorageFixture()
        {
            return selectionHost.HasSelectedFixture
                && fixtureRuntimeHost.FixtureState != null
                && fixtureRuntimeHost.FixtureState.TryGetFixture(
                    selectionHost.SelectedFixtureId,
                    out FixtureInstance fixture)
                && fixture.Definition.StorageProfile
                    .ProvidesBackstockStorage;
        }

        private bool TryGetSelectedFrontage(
            out FixtureShelfRunKey shelfRun,
            out int frontageUnitIndex)
        {
            if (!selectionHost.HasSelectedFrontageUnit
                || planogramRuntimeHost.Planograms == null)
            {
                shelfRun = default;
                frontageUnitIndex = 0;
                return false;
            }

            shelfRun = selectionHost.SelectedShelfRun;
            frontageUnitIndex = selectionHost.SelectedFrontageUnitIndex;
            return true;
        }

        private string GetProductName(ProductId productId)
        {
            return planogramRuntimeHost.Products != null
                && planogramRuntimeHost.Products.TryGet(
                    productId,
                    out ProductDefinition product)
                    ? product.DisplayName
                    : productId.Value;
        }

        private static string DescribeFace(FixtureSide localSide)
        {
            return localSide switch
            {
                FixtureSide.North => "Back",
                FixtureSide.South => "Front",
                FixtureSide.East => "Right",
                FixtureSide.West => "Left",
                _ => localSide.ToString()
            };
        }

        private static string DescribeFailure(
            FixturePlanogramFailure failure)
        {
            return failure switch
            {
                FixturePlanogramFailure.InvalidFrontageRange =>
                    "That selection would extend past the shelf.",
                FixturePlanogramFailure.FrontageOccupied =>
                    "Those shelf slots are already assigned.",
                FixturePlanogramFailure.UnknownProduct =>
                    "That product is not in this store's catalog.",
                _ => $"Planogram edit failed: {failure}."
            };
        }

        private static string DescribeRestockResult(
            FixtureRestockResult result)
        {
            return result.Outcome switch
            {
                FixtureRestockOutcome.Restocked =>
                    result.RemainingShortfall > 0
                        ? $"Moved {result.MovedUnitCount} units; {result.RemainingShortfall} still needed"
                        : $"Restocked {result.MovedUnitCount} units",
                FixtureRestockOutcome.NothingAssigned =>
                    "Assign products before restocking",
                FixtureRestockOutcome.AlreadyFull =>
                    "Display is already full",
                FixtureRestockOutcome.BackstockUnavailable =>
                    "No matching backstock available",
                _ => "Restock unavailable"
            };
        }

        private bool ValidateReferences()
        {
            if (documentHost != null
                && fixtureRuntimeHost != null
                && planogramRuntimeHost != null
                && selectionHost != null)
            {
                return true;
            }

            Debug.LogError(
                "FixtureMerchandisingInspectorPresenter requires document, fixture, planogram, and selection hosts.",
                this);
            return false;
        }
    }
}
