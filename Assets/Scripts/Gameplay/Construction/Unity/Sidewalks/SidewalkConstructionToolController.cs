using System;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.History;
using BigRetail.Construction.Unity.Input;
using BigRetail.Map.Domain;
using BigRetail.Map.Sidewalks;
using BigRetail.Map.Unity.Sidewalks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Sidewalks
{
    /// <summary>
    /// Coordinates rectangular Sidewalk construction through the shared
    /// construction pointer.
    ///
    /// Mouse gestures press, drag, and release. Gamepad gestures select the
    /// first corner and then confirm the opposite corner. Successful changes
    /// are recorded in the shared construction history.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class SidewalkConstructionToolController :
        MonoBehaviour
    {
        [Header("Input")]

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private ConstructionPointerController pointerController;

        [SerializeField]
        private string constructionActionMapName = "Construction";

        [SerializeField]
        private string confirmActionName = "Confirm";

        [SerializeField]
        private string cancelActionName = "Cancel";


        [Header("Sidewalk Tool")]

        [SerializeField]
        private GridCellTargetResolver cellTargetResolver;

        [SerializeField]
        private SidewalkAreaPreviewView previewView;

        [SerializeField]
        private SidewalkRuntimeHost sidewalkRuntimeHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;


        [Header("Starting State")]

        [SerializeField]
        private bool startActive;


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
            if (!ValidateReferences()
                || !TryResolveActions())
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
            SetToolActive(startActive);
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


        [ContextMenu("Activate Sidewalk Construction Tool")]
        public void ActivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The Sidewalk construction tool can only be " +
                    "activated during Play Mode.",
                    this);

                return;
            }

            SetToolActive(true);
        }


        [ContextMenu("Deactivate Sidewalk Construction Tool")]
        public void DeactivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The Sidewalk construction tool can only be " +
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
                    "Sidewalk construction could not begin because " +
                    "no grid cell is currently targeted.");

                return;
            }

            if (!sidewalkRuntimeHost.TryInitialize()
                || sidewalkRuntimeHost.SidewalkConstruction == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController could not " +
                    "access an initialized SidewalkConstructionService.",
                    this);

                return;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "SidewalkConstructionToolController could not " +
                    "access an initialized ConstructionHistory.",
                    this);

                return;
            }

            StartCell = cellTargetResolver.CurrentCell;
            areaStartedWithGamepad = pointerController.IsUsingGamepad;
            IsPlanningArea = true;
            hasCurrentEndCell = false;

            RefreshAreaPlan(forceRefresh: true);
            AreaPlanningChanged?.Invoke(true);

            if (logConstructionResults)
            {
                Debug.Log(
                    $"Sidewalk area started at {StartCell}.",
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

            currentIdleCell = targetedCell;
            hasCurrentIdleCell = true;
            previewView.ShowCell(currentIdleCell);
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

            currentEndCell = endCell;
            hasCurrentEndCell = true;
            CurrentAreaPlan =
                RectangularCellAreaPlanner.Plan(
                    StartCell,
                    currentEndCell);

            if (CurrentAreaPlan.Succeeded)
            {
                previewView.ShowPlan(CurrentAreaPlan);
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
                    "The current Sidewalk area has no valid geometry.");

                return false;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "The current Sidewalk area could not be recorded " +
                    "because construction history is unavailable.",
                    this);

                return false;
            }

            SidewalkEnsureResult result =
                sidewalkRuntimeHost.SidewalkConstruction
                    .TryEnsureSidewalks(CurrentAreaPlan.Cells);

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
                    new ReversibleSidewalkEditAction(
                        sidewalkRuntimeHost.SidewalkConstruction,
                        result.Edit));
            }

            if (logConstructionResults)
            {
                Debug.Log(
                    $"Sidewalk area processed. " +
                    $"Requested: {result.RequestedCount}. " +
                    $"Created: {result.ChangedCount}. " +
                    $"Already existing: {result.AlreadyExistingCount}. " +
                    $"Skipped outside map: " +
                    $"{result.SkippedOutsideMapCount}. " +
                    $"Skipped outside construction area: " +
                    $"{result.SkippedOutsideConstructionAreaCount}.",
                    this);
            }

            if (result.SatisfiedCount == 0
                && areaStartedWithGamepad)
            {
                RefreshAreaPlan(forceRefresh: true);
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
                    "Current Sidewalk area cancelled.",
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

            ToolActiveChanged?.Invoke(IsActive);

            if (logConstructionResults)
            {
                Debug.Log(
                    IsActive
                        ? "Sidewalk construction tool activated."
                        : "Sidewalk construction tool deactivated.",
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
                    "SidewalkConstructionToolController could not find " +
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
                    $"SidewalkConstructionToolController requires " +
                    $"actions named '{confirmActionName}' and " +
                    $"'{cancelActionName}' inside the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void LogRejectedArea(
            SidewalkEnsureResult result)
        {
            if (!logConstructionResults)
            {
                return;
            }

            Debug.LogWarning(
                $"Sidewalk area could not be processed. " +
                $"Reason: {result.Failure}. " +
                $"Failed cell: {result.FailedCell}.",
                this);
        }


        private void LogWarning(
            string message)
        {
            if (logConstructionResults)
            {
                Debug.LogWarning(message, this);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (playerInput == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController has no " +
                    "PlayerInput assigned.",
                    this);

                isValid = false;
            }

            if (pointerController == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController has no " +
                    "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (cellTargetResolver == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController has no " +
                    "GridCellTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (previewView == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController has no " +
                    "SidewalkAreaPreviewView assigned.",
                    this);

                isValid = false;
            }

            if (sidewalkRuntimeHost == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController has no " +
                    "SidewalkRuntimeHost assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "SidewalkConstructionToolController has no " +
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
