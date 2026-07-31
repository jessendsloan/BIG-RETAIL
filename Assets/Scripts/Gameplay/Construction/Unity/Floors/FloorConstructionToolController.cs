using System;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.History;
using BigRetail.Construction.Unity.Input;
using BigRetail.Map.Domain;
using BigRetail.Map.Floors;
using BigRetail.Map.Unity.Floors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Floors
{
    /// <summary>
    /// Coordinates rectangular floor construction.
    ///
    /// Mouse:
    /// - Press Confirm to choose the first corner.
    /// - Drag to choose the opposite corner.
    /// - Release Confirm to construct every valid cell.
    ///
    /// Gamepad:
    /// - Press Confirm to choose the first corner.
    /// - Move the virtual cursor.
    /// - Press Confirm again to construct.
    ///
    /// Existing and invalid cells do not reject valid cells.
    /// Successful state changes are recorded as neutral construction
    /// history actions.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class FloorConstructionToolController :
        MonoBehaviour
    {
        [Header("Input")]

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private ConstructionPointerController
            pointerController;

        [SerializeField]
        private string constructionActionMapName =
            "Construction";

        [SerializeField]
        private string confirmActionName =
            "Confirm";

        [SerializeField]
        private string cancelActionName =
            "Cancel";


        [Header("Floor Tool")]

        [SerializeField]
        private GridCellTargetResolver
            cellTargetResolver;

        [SerializeField]
        private FloorAreaPreviewView
            previewView;

        [SerializeField]
        private FloorRuntimeHost
            floorRuntimeHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;


        [Header("Starting State")]

        [SerializeField]
        private bool startActive = false;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logConstructionResults = true;


        public bool IsActive { get; private set; }

        public bool IsPlanningArea { get; private set; }

        public GridPosition StartCell { get; private set; }

        public RectangularCellAreaPlanResult CurrentAreaPlan
        {
            get;
            private set;
        }


        public event Action<bool> ToolActiveChanged;

        public event Action<bool> AreaPlanningChanged;


        private InputAction confirmAction;
        private InputAction cancelAction;

        private GridPosition currentEndCell;
        private bool hasCurrentEndCell;

        private GridPosition currentIdleCell;
        private bool hasCurrentIdleCell;

        private bool areaStartedWithGamepad;
        private bool isInitialized;


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (!TryResolveActions())
            {
                enabled = false;
                return;
            }

            isInitialized = true;
        }


        private void OnEnable()
        {
            if (pointerController != null)
            {
                pointerController.PointerModeChanged +=
                    HandlePointerModeChanged;
            }
        }


        private void Start()
        {
            SetToolActive(
                startActive);
        }


        private void LateUpdate()
        {
            if (!isInitialized
                || !IsActive)
            {
                return;
            }

            if (cancelAction.WasPressedThisFrame())
            {
                HandleCancel();
                return;
            }

            if (!IsPlanningArea)
            {
                RefreshIdlePreview();

                if (confirmAction.WasPressedThisFrame())
                {
                    BeginArea();
                }

                return;
            }

            RefreshAreaPlan();

            if (areaStartedWithGamepad)
            {
                if (confirmAction.WasPressedThisFrame())
                {
                    TryCommitCurrentArea();
                }
            }
            else if (confirmAction.WasReleasedThisFrame())
            {
                TryCommitCurrentArea();
            }
        }


        [ContextMenu("Activate Floor Construction Tool")]
        public void ActivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The floor construction tool can only be " +
                    "activated during Play Mode.",
                    this);

                return;
            }

            SetToolActive(true);
        }


        [ContextMenu("Deactivate Floor Construction Tool")]
        public void DeactivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The floor construction tool can only be " +
                    "deactivated during Play Mode.",
                    this);

                return;
            }

            SetToolActive(false);
        }


        public void CancelCurrentGesture()
        {
            if (IsPlanningArea)
            {
                CancelCurrentArea();
            }

            hasCurrentIdleCell = false;
            previewView.Hide();
        }


        private void BeginArea()
        {
            if (!cellTargetResolver.HasTarget)
            {
                LogWarning(
                    "Floor construction could not begin because " +
                    "no grid cell is currently targeted.");

                return;
            }

            if (!floorRuntimeHost.TryInitialize()
                || floorRuntimeHost.FloorConstruction == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController could not " +
                    "access an initialized FloorConstructionService.",
                    this);

                return;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "FloorConstructionToolController could not access " +
                    "an initialized ConstructionHistory.",
                    this);

                return;
            }

            StartCell =
                cellTargetResolver.CurrentCell;

            areaStartedWithGamepad =
                pointerController.IsUsingGamepad;

            IsPlanningArea = true;

            hasCurrentEndCell = false;

            RefreshAreaPlan(
                forceRefresh: true);

            AreaPlanningChanged?.Invoke(true);

            if (logConstructionResults)
            {
                Debug.Log(
                    $"Floor area started at {StartCell}.",
                    this);
            }
        }


        private void RefreshIdlePreview()
        {
            if (!cellTargetResolver.HasTarget)
            {
                previewView.Hide();

                hasCurrentIdleCell = false;

                return;
            }

            GridPosition targetedCell =
                cellTargetResolver.CurrentCell;

            if (hasCurrentIdleCell
                && targetedCell == currentIdleCell)
            {
                return;
            }

            currentIdleCell =
                targetedCell;

            hasCurrentIdleCell = true;

            previewView.ShowCell(
                currentIdleCell);
        }


        private void RefreshAreaPlan(
            bool forceRefresh = false)
        {
            if (!IsPlanningArea
                || !cellTargetResolver.HasTarget)
            {
                return;
            }

            GridPosition endCell =
                cellTargetResolver.CurrentCell;

            if (!forceRefresh
                && hasCurrentEndCell
                && endCell == currentEndCell)
            {
                return;
            }

            currentEndCell =
                endCell;

            hasCurrentEndCell = true;

            CurrentAreaPlan =
                RectangularCellAreaPlanner.Plan(
                    StartCell,
                    currentEndCell);

            if (CurrentAreaPlan.Succeeded)
            {
                previewView.ShowPlan(
                    CurrentAreaPlan);
            }
            else
            {
                previewView.Hide();
            }
        }


        private bool TryCommitCurrentArea()
        {
            if (!CurrentAreaPlan.Succeeded)
            {
                LogWarning(
                    "The current floor area has no valid geometry.");

                return false;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "The current floor area could not be recorded " +
                    "because construction history is unavailable.",
                    this);

                return false;
            }

            FloorEnsureResult result =
                floorRuntimeHost.FloorConstruction
                    .TryEnsureFloors(
                        CurrentAreaPlan.Cells);

            if (!result.Succeeded)
            {
                LogRejectedArea(result);

                if (!areaStartedWithGamepad)
                {
                    FinishArea();
                }

                return false;
            }

            if (!result.Edit.IsEmpty)
            {
                historyHost.History.Record(
                    new ReversibleFloorEditAction(
                        floorRuntimeHost.FloorConstruction,
                        result.Edit));
            }

            if (logConstructionResults)
            {
                Debug.Log(
                    $"Floor area processed. " +
                    $"Requested: {result.RequestedCount}. " +
                    $"Created: {result.ChangedCount}. " +
                    $"Already existing: " +
                    $"{result.AlreadyExistingCount}. " +
                    $"Skipped outside map: " +
                    $"{result.SkippedOutsideMapCount}. " +
                    $"Skipped outside construction area: " +
                    $"{result.SkippedOutsideConstructionAreaCount}. " +
                    $"Skipped without Foundation: " +
                    $"{result.SkippedMissingFoundationCount}.",
                    this);
            }

            bool anyCellSatisfied =
                result.SatisfiedCount > 0;

            if (!anyCellSatisfied
                && areaStartedWithGamepad)
            {
                RefreshAreaPlan(
                    forceRefresh: true);

                return false;
            }

            FinishArea();

            return result.ChangedCount > 0;
        }


        private void HandleCancel()
        {
            if (IsPlanningArea)
            {
                CancelCurrentArea();
                return;
            }

            SetToolActive(false);
        }


        private void CancelCurrentArea()
        {
            if (!IsPlanningArea)
            {
                return;
            }

            if (logConstructionResults)
            {
                Debug.Log(
                    "Current floor area cancelled.",
                    this);
            }

            FinishArea();
        }


        private void FinishArea()
        {
            IsPlanningArea = false;

            CurrentAreaPlan = default;

            hasCurrentEndCell = false;
            hasCurrentIdleCell = false;

            if (IsActive)
            {
                RefreshIdlePreview();
            }
            else
            {
                previewView.Hide();
            }

            AreaPlanningChanged?.Invoke(false);
        }


        private void SetToolActive(
            bool isActive)
        {
            if (IsActive == isActive)
            {
                if (IsActive
                    && !IsPlanningArea)
                {
                    hasCurrentIdleCell = false;

                    RefreshIdlePreview();
                }

                return;
            }

            if (!isActive
                && IsPlanningArea)
            {
                FinishArea();
            }

            IsActive = isActive;

            hasCurrentIdleCell = false;

            if (IsActive)
            {
                RefreshIdlePreview();
            }
            else
            {
                previewView.Hide();
            }

            ToolActiveChanged?.Invoke(
                IsActive);

            if (logConstructionResults)
            {
                Debug.Log(
                    IsActive
                        ? "Floor construction tool activated."
                        : "Floor construction tool deactivated.",
                    this);
            }
        }


        private void HandlePointerModeChanged(
            bool isUsingGamepad)
        {
            if (IsPlanningArea)
            {
                CancelCurrentArea();
            }

            hasCurrentIdleCell = false;
        }


        private bool TryResolveActions()
        {
            if (playerInput.actions == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController could not find " +
                    "an Input Actions asset on PlayerInput.",
                    this);

                return false;
            }

            InputActionMap actionMap =
                playerInput.actions.FindActionMap(
                    constructionActionMapName,
                    throwIfNotFound: false);

            if (actionMap == null)
            {
                Debug.LogError(
                    $"Could not find an Action Map named " +
                    $"'{constructionActionMapName}'.",
                    this);

                return false;
            }

            confirmAction =
                actionMap.FindAction(
                    confirmActionName,
                    throwIfNotFound: false);

            cancelAction =
                actionMap.FindAction(
                    cancelActionName,
                    throwIfNotFound: false);

            if (confirmAction == null
                || cancelAction == null)
            {
                Debug.LogError(
                    $"FloorConstructionToolController requires " +
                    $"actions named '{confirmActionName}' and " +
                    $"'{cancelActionName}' inside the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void LogRejectedArea(
            FloorEnsureResult result)
        {
            if (!logConstructionResults)
            {
                return;
            }

            Debug.LogWarning(
                $"Floor area could not be processed. " +
                $"Reason: {result.Failure}. " +
                $"Failed cell: {result.FailedCell}.",
                this);
        }


        private void LogWarning(
            string message)
        {
            if (logConstructionResults)
            {
                Debug.LogWarning(
                    message,
                    this);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (playerInput == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController has no " +
                    "PlayerInput assigned.",
                    this);

                isValid = false;
            }

            if (pointerController == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController has no " +
                    "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (cellTargetResolver == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController has no " +
                    "GridCellTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (previewView == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController has no " +
                    "FloorAreaPreviewView assigned.",
                    this);

                isValid = false;
            }

            if (floorRuntimeHost == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController has no " +
                    "FloorRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "FloorConstructionToolController has no " +
                    "ConstructionHistoryHost assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private void OnDisable()
        {
            if (pointerController != null)
            {
                pointerController.PointerModeChanged -=
                    HandlePointerModeChanged;
            }

            IsActive = false;
            IsPlanningArea = false;

            if (previewView != null)
            {
                previewView.Hide();
            }
        }
    }
}
