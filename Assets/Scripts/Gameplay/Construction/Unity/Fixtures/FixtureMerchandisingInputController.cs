using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.Input;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Fixtures
{
    /// <summary>
    /// Routes the shared pointer into fixture selection and shelf-frontage
    /// selection while the merchandise tool owns the pointer.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(175)]
    public sealed class FixtureMerchandisingInputController : MonoBehaviour
    {
        private const float CarriedCaseTargetWidth = 0.64f;

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private GridCellTargetResolver targetResolver;

        [SerializeField]
        private ConstructionUiInputGate uiInputGate;

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;

        [SerializeField]
        private FixtureMerchandisingSelectionHost selectionHost;

        [SerializeField]
        private FixtureMerchandisingOverlayViewSystem overlayViewSystem;

        [SerializeField]
        private FixtureViewSystem fixtureViewSystem;

        [SerializeField]
        private FixtureMerchandisingHoverOutlineView hoverOutlineView;

        [SerializeField]
        private FixturePlanogramRuntimeHost planogramRuntimeHost;

        [SerializeField]
        private PurchasingRuntimeHost purchasingRuntimeHost;

        [SerializeField]
        private InboundDeliveryViewSystem inboundDeliveryViewSystem;

        [Header("Action Names")]

        [SerializeField]
        private string constructionActionMapName = "Construction";

        [SerializeField]
        private string confirmActionName = "Confirm";


        private InputAction confirmAction;
        private InboundPurchasePack carriedCase;
        private GameObject carriedCaseView;
        private bool hasCarriedCase;


        private void Awake()
        {
            ResolveRuntimeReferences();

            if (!ValidateReferences()
                || !TryResolveConfirmAction())
            {
                enabled = false;
            }
        }

        private void ResolveRuntimeReferences()
        {
            if (planogramRuntimeHost == null)
            {
                planogramRuntimeHost =
                    FindAnyObjectByType<FixturePlanogramRuntimeHost>(
                        FindObjectsInactive.Include);
            }

            if (purchasingRuntimeHost == null)
            {
                purchasingRuntimeHost =
                    FindAnyObjectByType<PurchasingRuntimeHost>(
                        FindObjectsInactive.Include);
            }

            if (inboundDeliveryViewSystem == null)
            {
                inboundDeliveryViewSystem =
                    FindAnyObjectByType<InboundDeliveryViewSystem>(
                        FindObjectsInactive.Include);
            }

            if (fixtureViewSystem == null)
            {
                fixtureViewSystem =
                    FindAnyObjectByType<FixtureViewSystem>(
                        FindObjectsInactive.Include);
            }

            if (hoverOutlineView != null || fixtureViewSystem == null)
            {
                return;
            }

            hoverOutlineView =
                fixtureViewSystem.GetComponent<
                    FixtureMerchandisingHoverOutlineView>();

            if (hoverOutlineView == null)
            {
                hoverOutlineView =
                    fixtureViewSystem.gameObject.AddComponent<
                        FixtureMerchandisingHoverOutlineView>();
            }
        }

        private void OnEnable()
        {
            if (uiInputGate != null)
            {
                uiInputGate.CancelRequested += HandleCancelRequested;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged += HandleToolModeChanged;
            }
        }

        private void OnDisable()
        {
            if (uiInputGate != null)
            {
                uiInputGate.CancelRequested -= HandleCancelRequested;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -= HandleToolModeChanged;
            }

            overlayViewSystem?.ClearHoveredMarker();
            hoverOutlineView?.Hide();
            inboundDeliveryViewSystem?.SetHighlightedOrder(null);
            ClearCarriedCase();
        }

        private void LateUpdate()
        {
            UpdateCarriedCaseViewPosition();

            if (toolCoordinator.CurrentMode
                    != ConstructionToolMode.MerchandiseFixtures
                || uiInputGate.IsPointerOverConstructionUi
                || !targetResolver.HasTarget)
            {
                overlayViewSystem.ClearHoveredMarker();
                hoverOutlineView.Hide();
                inboundDeliveryViewSystem.SetHighlightedOrder(null);
                return;
            }

            if (hasCarriedCase)
            {
                HandleCarriedCaseInteraction();
                return;
            }

            if (!selectionHost.IsEditing
                && inboundDeliveryViewSystem.TryGetLoadAtWorldPosition(
                    targetResolver.PointerWorldPosition,
                    out InboundDeliveryLoadView inboundLoad))
            {
                overlayViewSystem.ClearHoveredMarker();
                hoverOutlineView.Hide();
                inboundDeliveryViewSystem.SetHighlightedOrder(
                    inboundLoad.OrderNumber);

                if (confirmAction.WasPressedThisFrame())
                {
                    TryTakeCase(inboundLoad);
                }

                return;
            }

            inboundDeliveryViewSystem.SetHighlightedOrder(null);

            if (selectionHost.IsEditing
                && overlayViewSystem.TryHitTest(
                    targetResolver.PointerWorldPosition,
                    out FixtureShelfRunKey hoveredShelfRun,
                    out int hoveredUnitIndex))
            {
                overlayViewSystem.SetHoveredMarker(
                    hoveredShelfRun,
                    hoveredUnitIndex);
                hoverOutlineView.Hide();

                if (confirmAction.WasPressedThisFrame())
                {
                    selectionHost.SelectFrontageUnit(
                        hoveredShelfRun,
                        hoveredUnitIndex);
                }

                return;
            }

            overlayViewSystem.ClearHoveredMarker();

            if (selectionHost.IsEditing)
            {
                hoverOutlineView.Hide();
                return;
            }

            if (TryResolveHoveredFixture(out FixtureInstance fixture))
            {
                hoverOutlineView.ShowFixture(fixture.Id);
            }
            else
            {
                hoverOutlineView.Hide();
            }

            if (!confirmAction.WasPressedThisFrame()
                || selectionHost.IsEditing)
            {
                return;
            }

            if (fixture != null)
            {
                selectionHost.SelectFixture(fixture.Id);
            }
            else
            {
                selectionHost.ClearSelection();
            }
        }


        private void HandleCancelRequested()
        {
            if (hasCarriedCase)
            {
                ClearCarriedCase();
                return;
            }

            if (selectionHost.HasSelectedFrontageUnit)
            {
                selectionHost.ClearFrontageSelection();
                return;
            }

            if (selectionHost.IsEditing)
            {
                selectionHost.EndEditing();
                return;
            }

            if (selectionHost.HasSelectedFixture)
            {
                selectionHost.ClearSelection();
                return;
            }

            if (toolCoordinator.CurrentMode
                == ConstructionToolMode.MerchandiseFixtures)
            {
                toolCoordinator.SetMode(ConstructionToolMode.None);
            }
        }

        private void HandleToolModeChanged(ConstructionToolMode mode)
        {
            if (mode != ConstructionToolMode.MerchandiseFixtures)
            {
                ClearCarriedCase();
                selectionHost.ClearSelection();
                overlayViewSystem.ClearHoveredMarker();
                hoverOutlineView.Hide();
                inboundDeliveryViewSystem.SetHighlightedOrder(null);
            }
        }

        private void HandleCarriedCaseInteraction()
        {
            inboundDeliveryViewSystem.SetHighlightedOrder(null);
            overlayViewSystem.ClearHoveredMarker();

            if (!TryResolveHoveredStorageRack(
                    out FixtureInstance rack))
            {
                hoverOutlineView.Hide();
                return;
            }

            hoverOutlineView.ShowFixture(rack.Id);

            if (!confirmAction.WasPressedThisFrame())
            {
                return;
            }

            SupplierCaseStockingResult result =
                purchasingRuntimeHost.CaseStocking.TryStockCase(
                    carriedCase,
                    rack.Id);

            if (!result.Succeeded)
            {
                Debug.LogWarning(
                    DescribeStockingFailure(result.Failure),
                    this);
                return;
            }

            Debug.Log(
                $"Stocked one {result.ReceivedUnitCount}-unit supplier "
                + $"case on rack '{rack.Id.Value}'.",
                this);
            ClearCarriedCase();
            hoverOutlineView.Hide();
        }

        private bool TryTakeCase(InboundDeliveryLoadView inboundLoad)
        {
            if (inboundLoad == null
                || purchasingRuntimeHost.CaseStocking == null
                || !purchasingRuntimeHost.CaseStocking.TryGetNextCase(
                    inboundLoad.OrderNumber,
                    out InboundPurchasePack nextCase))
            {
                return false;
            }

            carriedCase = nextCase;
            hasCarriedCase = true;
            selectionHost.ClearSelection();
            inboundDeliveryViewSystem.SetHighlightedOrder(null);
            CreateCarriedCaseView(
                inboundLoad.LoadRenderer != null
                    ? inboundLoad.LoadRenderer.sprite
                    : null);

            Debug.Log(
                $"Picked up one {nextCase.UnitCount}-unit supplier case. "
                + "Choose a storage rack.",
                this);
            return true;
        }

        private void CreateCarriedCaseView(Sprite fallbackSprite)
        {
            ClearCarriedCaseView();

            Sprite caseSprite = fallbackSprite;

            if (planogramRuntimeHost.TryGetProductAsset(
                    carriedCase.ProductId,
                    out ProductDefinitionAsset productAsset))
            {
                caseSprite = productAsset.GetCaseImage(
                        risingLeft: false)
                    ?? productAsset.GetCaseImage(risingLeft: true)
                    ?? productAsset.CatalogImage
                    ?? fallbackSprite;
            }

            if (caseSprite == null)
            {
                return;
            }

            carriedCaseView = new GameObject("Carried Supplier Case");
            carriedCaseView.transform.SetParent(
                transform,
                worldPositionStays: true);
            SpriteRenderer renderer =
                carriedCaseView.AddComponent<SpriteRenderer>();
            renderer.sprite = caseSprite;
            renderer.color = new Color(1f, 1f, 1f, 0.94f);
            renderer.sortingOrder = 32760;

            float spriteWidth = caseSprite.bounds.size.x;

            if (spriteWidth > Mathf.Epsilon)
            {
                float scale = CarriedCaseTargetWidth / spriteWidth;
                carriedCaseView.transform.localScale =
                    new Vector3(scale, scale, 1f);
            }

            UpdateCarriedCaseViewPosition();
        }

        private void UpdateCarriedCaseViewPosition()
        {
            if (carriedCaseView == null || targetResolver == null)
            {
                return;
            }

            carriedCaseView.transform.position =
                targetResolver.PointerWorldPosition
                + new Vector3(0f, 0.46f, 0f);
        }

        private void ClearCarriedCase()
        {
            hasCarriedCase = false;
            carriedCase = default;
            ClearCarriedCaseView();
        }

        private void ClearCarriedCaseView()
        {
            if (carriedCaseView == null)
            {
                return;
            }

            Destroy(carriedCaseView);
            carriedCaseView = null;
        }

        private bool TryResolveHoveredStorageRack(
            out FixtureInstance rack)
        {
            if (TryResolveHoveredFixture(out FixtureInstance fixture)
                && fixture.Definition.StorageProfile
                    .ProvidesBackstockStorage)
            {
                rack = fixture;
                return true;
            }

            rack = null;
            return false;
        }

        private static string DescribeStockingFailure(
            SupplierCaseStockingFailure failure)
        {
            return failure switch
            {
                SupplierCaseStockingFailure
                    .NoAvailableRackCaseSlot =>
                    "That storage rack does not have room for this case.",

                SupplierCaseStockingFailure.UnknownRack =>
                    "That storage rack is no longer available.",

                SupplierCaseStockingFailure.DeliveryChanged =>
                    "That supplier case is no longer available.",

                _ => "The supplier case could not be stocked."
            };
        }

        private bool TryResolveHoveredFixture(
            out FixtureInstance fixture)
        {
            bool foundFixture =
                fixtureViewSystem.TryGetFixtureAtWorldPosition(
                    targetResolver.PointerWorldPosition,
                    out fixture)
                || fixtureRuntimeHost.FixtureState.TryGetFixtureAtCell(
                    targetResolver.CurrentCell,
                    out fixture);

            if (foundFixture
                && (fixture.Definition.MerchandisingProfile.HasDisplayFaces
                    || fixture.Definition.StorageProfile
                        .ProvidesBackstockStorage))
            {
                return true;
            }

            fixture = null;
            return false;
        }

        private bool TryResolveConfirmAction()
        {
            if (playerInput.actions == null)
            {
                Debug.LogError(
                    "FixtureMerchandisingInputController cannot access the Input Actions asset.",
                    this);
                return false;
            }

            InputActionMap actionMap =
                playerInput.actions.FindActionMap(
                    constructionActionMapName,
                    throwIfNotFound: false);

            confirmAction =
                actionMap?.FindAction(
                    confirmActionName,
                    throwIfNotFound: false);

            if (confirmAction != null)
            {
                return true;
            }

            Debug.LogError(
                $"FixtureMerchandisingInputController could not find '{constructionActionMapName}/{confirmActionName}'.",
                this);
            return false;
        }

        private bool ValidateReferences()
        {
            if (playerInput != null
                && targetResolver != null
                && uiInputGate != null
                && toolCoordinator != null
                && fixtureRuntimeHost != null
                && selectionHost != null
                && overlayViewSystem != null
                && fixtureViewSystem != null
                && hoverOutlineView != null
                && planogramRuntimeHost != null
                && purchasingRuntimeHost != null
                && inboundDeliveryViewSystem != null)
            {
                return true;
            }

            Debug.LogError(
                "FixtureMerchandisingInputController requires input, targeting, UI gate, tool, fixture, merchandise, purchasing, delivery-view, selection, overlay, fixture-view, and hover-outline references.",
                this);
            return false;
        }
    }
}
