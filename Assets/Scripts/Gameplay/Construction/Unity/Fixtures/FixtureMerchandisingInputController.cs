using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.Input;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
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

        [Header("Action Names")]

        [SerializeField]
        private string constructionActionMapName = "Construction";

        [SerializeField]
        private string confirmActionName = "Confirm";


        private InputAction confirmAction;


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
        }

        private void LateUpdate()
        {
            if (toolCoordinator.CurrentMode
                    != ConstructionToolMode.MerchandiseFixtures
                || uiInputGate.IsPointerOverConstructionUi
                || !targetResolver.HasTarget)
            {
                overlayViewSystem.ClearHoveredMarker();
                hoverOutlineView.Hide();
                return;
            }

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
                selectionHost.ClearSelection();
                overlayViewSystem.ClearHoveredMarker();
                hoverOutlineView.Hide();
            }
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
                && fixture.Definition.MerchandisingProfile.HasDisplayFaces)
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
                && hoverOutlineView != null)
            {
                return true;
            }

            Debug.LogError(
                "FixtureMerchandisingInputController requires input, targeting, UI gate, tool, fixture, selection, overlay, fixture-view, and hover-outline references.",
                this);
            return false;
        }
    }
}
