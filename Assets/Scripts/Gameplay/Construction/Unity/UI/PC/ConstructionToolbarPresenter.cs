using BigRetail.CameraControl;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects PC construction-rail intent to authoritative construction and
    /// wall-presentation services, then mirrors their state back into the UI.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(350)]
    public sealed class ConstructionToolbarPresenter : MonoBehaviour
    {
        [Header("Toolbar")]

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;


        [Header("Construction Services")]

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;


        [Header("View Services")]

        [SerializeField]
        private WallViewSystem wallViewSystem;

        [SerializeField]
        private IsometricViewRotationController viewRotationController;


        private ConstructionToolbarView boundView;
        private bool referencesAreValid;
        private bool isDemolitionPickerRequested;


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

            toolCoordinator.ModeChanged +=
                HandleModeChanged;

            wallViewSystem.DisplayModeChanged +=
                HandleWallDisplayModeChanged;

            viewRotationController.ViewOrientationChanged +=
                HandleCameraViewOrientationChanged;

            if (documentHost.HasView)
            {
                BindView(
                    documentHost.View);
            }
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

                case ConstructionToolbarSection.Foundations:
                    requestedMode =
                        ConstructionToolMode.BuildFoundations;
                    break;

                case ConstructionToolbarSection.Floors:
                    requestedMode =
                        ConstructionToolMode.BuildFloors;
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


        private void HandleDemolitionTargetRequested(
            ConstructionToolbarDemolitionTarget target)
        {
            ConstructionToolMode requestedMode = target switch
            {
                ConstructionToolbarDemolitionTarget.Foundations =>
                    ConstructionToolMode.DemolishFoundations,

                ConstructionToolbarDemolitionTarget.Floors =>
                    ConstructionToolMode.DemolishFloors,

                ConstructionToolbarDemolitionTarget.Walls =>
                    ConstructionToolMode.DemolishWalls,

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


        private static bool IsDemolitionMode(
            ConstructionToolMode mode)
        {
            return mode == ConstructionToolMode.DemolishFoundations
                || mode == ConstructionToolMode.DemolishFloors
                || mode == ConstructionToolMode.DemolishWalls;
        }


        private static ConstructionToolbarDemolitionTarget?
            ToDemolitionTarget(ConstructionToolMode mode)
        {
            return mode switch
            {
                ConstructionToolMode.DemolishFoundations =>
                    ConstructionToolbarDemolitionTarget.Foundations,

                ConstructionToolMode.DemolishFloors =>
                    ConstructionToolbarDemolitionTarget.Floors,

                ConstructionToolMode.DemolishWalls =>
                    ConstructionToolbarDemolitionTarget.Walls,

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
