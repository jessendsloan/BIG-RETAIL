using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the fixture inspector to logical selection and planogram
    /// services. UI does not mutate stock or fixture placement.
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


        private FixtureMerchandisingInspectorView boundView;
        private FixturePlanogramState subscribedPlanogramState;
        private FixtureDisplayInventoryService subscribedDisplayInventory;
        private bool productsAreBound;


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
            selectionHost.SelectionChanged += HandleSelectionChanged;
            planogramRuntimeHost.Initialized += HandlePlanogramInitialized;

            AttachToPlanogramState();
            AttachToDisplayInventory();

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
            }

            if (selectionHost != null)
            {
                selectionHost.SelectionChanged -= HandleSelectionChanged;
            }

            if (planogramRuntimeHost != null)
            {
                planogramRuntimeHost.Initialized -= HandlePlanogramInitialized;
            }

            DetachFromPlanogramState();
            DetachFromDisplayInventory();
            UnbindView();
        }


        private void HandleViewReady(
            FixtureMerchandisingInspectorView view)
        {
            BindView(view);
        }

        private void HandleSelectionChanged()
        {
            RefreshView();
        }

        private void HandlePlanogramInitialized(
            FixturePlanogramRuntimeHost initializedHost)
        {
            AttachToPlanogramState();
            AttachToDisplayInventory();
            productsAreBound = false;
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
                && fixtureId == selectionHost.SelectedFixtureId)
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

        private void HandleDebugSaleRequested()
        {
            if (!selectionHost.HasSelectedFixture
                || planogramRuntimeHost.DisplayInventory == null)
            {
                return;
            }

            FixtureStockConsumptionResult result =
                planogramRuntimeHost.DisplayInventory
                    .TryConsumeFixtureStock(
                        selectionHost.SelectedFixtureId,
                        requestedUnitCount: 1);

            RefreshView();

            boundView?.SetRestockStatus(
                DescribeStockConsumptionResult(result));
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
            boundView.DebugSaleRequested += HandleDebugSaleRequested;
            boundView.DoneRequested += HandleDoneRequested;
            boundView.CloseRequested += HandleCloseRequested;
            boundView.ProductRequested += HandleProductRequested;
            boundView.WidthDeltaRequested += HandleWidthDeltaRequested;
            boundView.ClearRequested += HandleClearRequested;
            productsAreBound = false;
            RefreshView();
        }

        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.EditRequested -= HandleEditRequested;
                boundView.RestockRequested -= HandleRestockRequested;
                boundView.DebugSaleRequested -= HandleDebugSaleRequested;
                boundView.DoneRequested -= HandleDoneRequested;
                boundView.CloseRequested -= HandleCloseRequested;
                boundView.ProductRequested -= HandleProductRequested;
                boundView.WidthDeltaRequested -= HandleWidthDeltaRequested;
                boundView.ClearRequested -= HandleClearRequested;
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

            EnsureProductsAreBound();
            boundView.SetFixtureTitle(fixture.Definition.DisplayName);
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
                boundView.SetInventorySummary(0, 0, 0, false);
                boundView.SetRestockStatus("Inventory unavailable");
                return;
            }

            boundView.SetInventorySummary(
                snapshot.StockedUnitCount,
                snapshot.CapacityUnitCount,
                snapshot.BackstockUnitCount,
                snapshot.CanRestock);

            string status =
                snapshot.CapacityUnitCount == 0
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

        private static string DescribeStockConsumptionResult(
            FixtureStockConsumptionResult result)
        {
            return result.Outcome switch
            {
                FixtureStockConsumptionOutcome.Consumed =>
                    result.UnfulfilledUnitCount > 0
                        ? $"Sold {result.ConsumedUnitCount} test unit(s); display is now empty"
                        : $"Sold {result.ConsumedUnitCount} test unit(s)",
                FixtureStockConsumptionOutcome.DisplayEmpty =>
                    "Display is empty; restock it first",
                _ => "Test sale unavailable"
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
