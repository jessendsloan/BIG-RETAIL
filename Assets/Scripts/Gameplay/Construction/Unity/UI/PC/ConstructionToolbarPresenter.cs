using BigRetail.CameraControl;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using BigRetail.Purchasing.Unity;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects PC construction-rail intent to authoritative construction and
    /// wall-presentation services, then mirrors their state back into the UI.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [RequireComponent(typeof(ConstructionUiInputGate))]
    [DefaultExecutionOrder(350)]
    public sealed class ConstructionToolbarPresenter : MonoBehaviour
    {
        [Header("Toolbar")]

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;


        [Header("Construction Services")]

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private ReceivingAreaRuntimeHost receivingAreaRuntimeHost;

        [SerializeField]
        private PurchasingRuntimeHost purchasingRuntimeHost;


        [Header("View Services")]

        [SerializeField]
        private WallViewSystem wallViewSystem;

        [SerializeField]
        private IsometricViewRotationController viewRotationController;


        private ConstructionToolbarView boundView;
        private ConstructionUiInputGate uiInputGate;
        private bool referencesAreValid;
        private bool isDemolitionPickerRequested;
        private ReceivingAreaState subscribedReceivingState;


        private void Reset()
        {
            documentHost =
                GetComponent<ConstructionToolbarDocumentHost>();
        }


        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost =
                    GetComponent<ConstructionToolbarDocumentHost>();
            }

            uiInputGate =
                GetComponent<ConstructionUiInputGate>();

            referencesAreValid =
                ValidateReferences();
        }


        private void OnEnable()
        {
            if (!referencesAreValid)
            {
                return;
            }

            documentHost.ViewReady +=
                HandleViewReady;

            uiInputGate.CancelRequested +=
                HandleCancelRequested;

            toolCoordinator.ModeChanged +=
                HandleModeChanged;

            receivingAreaRuntimeHost.Initialized +=
                HandleReceivingRuntimeInitialized;

            purchasingRuntimeHost.Initialized +=
                HandlePurchasingRuntimeInitialized;
            purchasingRuntimeHost.DeliveriesChanged +=
                HandleDeliveriesChanged;

            wallViewSystem.DisplayModeChanged +=
                HandleWallDisplayModeChanged;

            viewRotationController.ViewOrientationChanged +=
                HandleCameraViewOrientationChanged;

            if (documentHost.HasView)
            {
                BindView(
                    documentHost.View);
            }

            AttachReceivingState();
        }


        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.ViewReady -=
                    HandleViewReady;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -=
                    HandleModeChanged;
            }

            if (receivingAreaRuntimeHost != null)
            {
                receivingAreaRuntimeHost.Initialized -=
                    HandleReceivingRuntimeInitialized;
            }

            if (purchasingRuntimeHost != null)
            {
                purchasingRuntimeHost.Initialized -=
                    HandlePurchasingRuntimeInitialized;
                purchasingRuntimeHost.DeliveriesChanged -=
                    HandleDeliveriesChanged;
            }

            if (uiInputGate != null)
            {
                uiInputGate.CancelRequested -=
                    HandleCancelRequested;
            }

            if (wallViewSystem != null)
            {
                wallViewSystem.DisplayModeChanged -=
                    HandleWallDisplayModeChanged;
            }

            if (viewRotationController != null)
            {
                viewRotationController.ViewOrientationChanged -=
                    HandleCameraViewOrientationChanged;
            }

            UnbindView();
            DetachReceivingState();
        }


        private void HandleViewReady(
            ConstructionToolbarView view)
        {
            BindView(
                view);
        }


        private void HandleSectionRequested(
            ConstructionToolbarSection section)
        {
            ConstructionToolMode requestedMode;

            switch (section)
            {
                case ConstructionToolbarSection.Walls:
                    requestedMode =
                        ConstructionToolMode.BuildWalls;
                    break;

                case ConstructionToolbarSection.Windows:
                    requestedMode =
                        ConstructionToolMode.BuildWindows;
                    break;

                case ConstructionToolbarSection.Foundations:
                    requestedMode =
                        ConstructionToolMode.BuildFoundations;
                    break;

                case ConstructionToolbarSection.Sidewalks:
                    requestedMode =
                        ConstructionToolMode.BuildSidewalks;
                    break;

                case ConstructionToolbarSection.Floors:
                    requestedMode =
                        ConstructionToolMode.BuildFloors;
                    break;

                case ConstructionToolbarSection.Doors:
                    requestedMode =
                        ConstructionToolMode.BuildDoors;
                    break;

                case ConstructionToolbarSection.Fixtures:
                    requestedMode =
                        ConstructionToolMode.BuildFixtures;
                    break;

                default:
                    return;
            }

            isDemolitionPickerRequested = false;

            toolCoordinator.SetMode(
                requestedMode);
        }


        private void HandleDemolitionPickerRequested()
        {
            // Demolition is a category choice, not a default destructive tool.
            // Clearing the active build mode closes its contextual picker before
            // this drawer asks the player to choose a demolition layer.
            toolCoordinator.SetMode(
                ConstructionToolMode.None);

            isDemolitionPickerRequested = true;

            RefreshDemolitionPicker(
                toolCoordinator.CurrentMode);
        }

        private void HandleDepartmentsRequested()
        {
            // Department Planning is a separate rail section. It must close
            // the last construction drawer even when the coordinator is
            // already in None and therefore does not publish ModeChanged.
            isDemolitionPickerRequested = false;
            toolCoordinator.SetMode(ConstructionToolMode.None);
            RefreshDemolitionPicker(toolCoordinator.CurrentMode);
        }

        private void HandleMerchandiseToolRequested()
        {
            isDemolitionPickerRequested = false;

            ConstructionToolMode requestedMode =
                toolCoordinator.CurrentMode
                    == ConstructionToolMode.MerchandiseFixtures
                        ? ConstructionToolMode.None
                        : ConstructionToolMode.MerchandiseFixtures;

            toolCoordinator.SetMode(requestedMode);
            RefreshDemolitionPicker(toolCoordinator.CurrentMode);
        }

        private void HandleReceivingAreaRequested()
        {
            isDemolitionPickerRequested = false;

            ConstructionToolMode requestedMode =
                toolCoordinator.CurrentMode
                    == ConstructionToolMode.PlanReceivingArea
                        ? ConstructionToolMode.None
                        : ConstructionToolMode.PlanReceivingArea;

            toolCoordinator.SetMode(requestedMode);
            RefreshDemolitionPicker(toolCoordinator.CurrentMode);
        }


        private void HandleDemolitionTargetRequested(
            ConstructionToolbarDemolitionTarget target)
        {
            ConstructionToolMode requestedMode = target switch
            {
                ConstructionToolbarDemolitionTarget.Foundations =>
                    ConstructionToolMode.DemolishFoundations,

                ConstructionToolbarDemolitionTarget.Sidewalks =>
                    ConstructionToolMode.DemolishSidewalks,

                ConstructionToolbarDemolitionTarget.Floors =>
                    ConstructionToolMode.DemolishFloors,

                ConstructionToolbarDemolitionTarget.Walls =>
                    ConstructionToolMode.DemolishWalls,

                ConstructionToolbarDemolitionTarget.Fixtures =>
                    ConstructionToolMode.DemolishFixtures,

                _ => ConstructionToolMode.None
            };

            if (requestedMode == ConstructionToolMode.None)
            {
                return;
            }

            isDemolitionPickerRequested = true;
            toolCoordinator.SetMode(requestedMode);
        }


        private void HandleModeChanged(
            ConstructionToolMode mode)
        {
            if (!IsDemolitionMode(mode))
            {
                isDemolitionPickerRequested = false;
            }

            RefreshSelection(
                mode);

            RefreshDemolitionPicker(mode);
        }

        private void HandleReceivingRuntimeInitialized(
            ReceivingAreaRuntimeHost initializedHost)
        {
            AttachReceivingState();
            RefreshReceivingStatus();
        }

        private void HandlePurchasingRuntimeInitialized(
            PurchasingRuntimeHost initializedHost)
        {
            RefreshReceivingStatus();
        }

        private void HandleDeliveriesChanged()
        {
            RefreshReceivingStatus();
        }

        private void HandleReceivingStateChanged()
        {
            RefreshReceivingStatus();
        }


        private void HandleCancelRequested()
        {
            ConstructionToolMode currentMode =
                toolCoordinator.CurrentMode;

            if (!isDemolitionPickerRequested
                && !IsDemolitionMode(currentMode))
            {
                return;
            }

            isDemolitionPickerRequested = false;

            if (currentMode != ConstructionToolMode.None)
            {
                toolCoordinator.SetMode(
                    ConstructionToolMode.None);

                return;
            }

            RefreshDemolitionPicker(currentMode);
        }


        private void HandleWallDisplayModeRequested(
            WallDisplayMode displayMode)
        {
            wallViewSystem.TrySetDisplayMode(
                displayMode);
        }


        private void HandleWallDisplayModeChanged(
            WallDisplayMode previousMode,
            WallDisplayMode currentMode)
        {
            RefreshWallDisplayMode(
                currentMode);
        }


        private void HandleCameraViewOrientationRequested(
            IsometricViewOrientation orientation)
        {
            viewRotationController.SetViewOrientation(
                orientation);
        }


        private void HandleCameraViewOrientationChanged(
            IsometricViewOrientation orientation)
        {
            RefreshCameraViewOrientation(
                orientation);
        }


        private void BindView(
            ConstructionToolbarView view)
        {
            UnbindView();

            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.SectionRequested +=
                HandleSectionRequested;

            boundView.DepartmentsRequested +=
                HandleDepartmentsRequested;

            boundView.MerchandiseToolRequested +=
                HandleMerchandiseToolRequested;

            boundView.ReceivingAreaRequested +=
                HandleReceivingAreaRequested;

            boundView.DemolitionPickerRequested +=
                HandleDemolitionPickerRequested;

            boundView.DemolitionTargetRequested +=
                HandleDemolitionTargetRequested;

            boundView.WallDisplayModeRequested +=
                HandleWallDisplayModeRequested;

            boundView.CameraViewOrientationRequested +=
                HandleCameraViewOrientationRequested;

            RefreshSelection(
                toolCoordinator.CurrentMode);

            RefreshDemolitionPicker(
                toolCoordinator.CurrentMode);

            RefreshWallDisplayMode(
                wallViewSystem.CurrentDisplayMode);

            RefreshCameraViewOrientation(
                viewRotationController.CurrentOrientation);

            RefreshReceivingStatus();
        }


        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SectionRequested -=
                HandleSectionRequested;

            boundView.DepartmentsRequested -=
                HandleDepartmentsRequested;

            boundView.MerchandiseToolRequested -=
                HandleMerchandiseToolRequested;

            boundView.ReceivingAreaRequested -=
                HandleReceivingAreaRequested;

            boundView.DemolitionPickerRequested -=
                HandleDemolitionPickerRequested;

            boundView.DemolitionTargetRequested -=
                HandleDemolitionTargetRequested;

            boundView.WallDisplayModeRequested -=
                HandleWallDisplayModeRequested;

            boundView.CameraViewOrientationRequested -=
                HandleCameraViewOrientationRequested;

            boundView = null;
        }


        private void RefreshSelection(
            ConstructionToolMode mode)
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetSelectedSection(
                ConstructionToolbarModeMapper.ToSection(
                    mode));

            boundView.SetMerchandiseToolActive(
                mode == ConstructionToolMode.MerchandiseFixtures);

            boundView.SetReceivingAreaActive(
                mode == ConstructionToolMode.PlanReceivingArea);
        }


        private void RefreshDemolitionPicker(
            ConstructionToolMode mode)
        {
            if (boundView == null)
            {
                return;
            }

            bool isDemolitionMode = IsDemolitionMode(mode);
            boundView.SetDemolitionPickerVisible(
                isDemolitionMode || isDemolitionPickerRequested);
            boundView.SetSelectedDemolitionTarget(
                ToDemolitionTarget(mode));
        }


        private void RefreshWallDisplayMode(
            WallDisplayMode displayMode)
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetWallDisplayMode(
                displayMode);
        }


        private void RefreshCameraViewOrientation(
            IsometricViewOrientation orientation)
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetCameraViewOrientation(
                orientation);
        }

        private void RefreshReceivingStatus()
        {
            if (boundView == null)
            {
                return;
            }

            int operationalCells =
                receivingAreaRuntimeHost != null
                    ? receivingAreaRuntimeHost.OperationalCellCount
                    : 0;
            int occupiedCells =
                receivingAreaRuntimeHost?.State?.ReservationCount ?? 0;
            int waitingDeliveries =
                purchasingRuntimeHost != null
                    ? purchasingRuntimeHost
                        .WaitingForReceivingSpaceOrderCount
                    : 0;

            boundView.SetReceivingAreaStatus(
                operationalCells,
                occupiedCells,
                waitingDeliveries);
        }

        private void AttachReceivingState()
        {
            ReceivingAreaState nextState =
                receivingAreaRuntimeHost != null
                && receivingAreaRuntimeHost.IsInitialized
                    ? receivingAreaRuntimeHost.State
                    : null;

            if (subscribedReceivingState == nextState)
            {
                return;
            }

            DetachReceivingState();
            subscribedReceivingState = nextState;

            if (subscribedReceivingState != null)
            {
                subscribedReceivingState.AreaChanged +=
                    HandleReceivingStateChanged;
                subscribedReceivingState.ReservationsChanged +=
                    HandleReceivingStateChanged;
            }
        }

        private void DetachReceivingState()
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


        private static bool IsDemolitionMode(
            ConstructionToolMode mode)
        {
            return mode == ConstructionToolMode.DemolishFoundations
                || mode == ConstructionToolMode.DemolishSidewalks
                || mode == ConstructionToolMode.DemolishFloors
                || mode == ConstructionToolMode.DemolishWalls
                || mode == ConstructionToolMode.DemolishFixtures;
        }


        private static ConstructionToolbarDemolitionTarget?
            ToDemolitionTarget(ConstructionToolMode mode)
        {
            return mode switch
            {
                ConstructionToolMode.DemolishFoundations =>
                    ConstructionToolbarDemolitionTarget.Foundations,

                ConstructionToolMode.DemolishSidewalks =>
                    ConstructionToolbarDemolitionTarget.Sidewalks,

                ConstructionToolMode.DemolishFloors =>
                    ConstructionToolbarDemolitionTarget.Floors,

                ConstructionToolMode.DemolishWalls =>
                    ConstructionToolbarDemolitionTarget.Walls,

                ConstructionToolMode.DemolishFixtures =>
                    ConstructionToolbarDemolitionTarget.Fixtures,

                _ => null
            };
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "ConstructionToolbarDocumentHost assigned.",
                    this);

                isValid = false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "ConstructionToolCoordinator assigned.",
                    this);

                isValid = false;
            }

            if (receivingAreaRuntimeHost == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "ReceivingAreaRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (purchasingRuntimeHost == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "PurchasingRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (uiInputGate == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "ConstructionUiInputGate assigned.",
                    this);

                isValid = false;
            }

            if (wallViewSystem == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "WallViewSystem assigned.",
                    this);

                isValid = false;
            }

            if (viewRotationController == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "IsometricViewRotationController assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
