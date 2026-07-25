using System;
using BigRetail.Construction.Unity.History;
using BigRetail.Construction.Unity.Input;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Coordinates straight wall-run construction.
    ///
    /// Mouse:
    /// - Press Confirm to begin.
    /// - Drag to choose the run.
    /// - Release Confirm to commit.
    ///
    /// Gamepad:
    /// - Press Confirm to begin.
    /// - Move the virtual cursor.
    /// - Press Confirm again to commit.
    ///
    /// Successful state changes are recorded as neutral construction
    /// history actions.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class WallConstructionToolController :
        MonoBehaviour
    {
        [Header("Input")]

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private ConstructionPointerController pointerController;

        [SerializeField]
        private string constructionActionMapName =
            "Construction";

        [SerializeField]
        private string confirmActionName =
            "Confirm";

        [SerializeField]
        private string cancelActionName =
            "Cancel";


        [Header("Wall Tool")]

        [SerializeField]
        private WallTargetResolver targetResolver;

        [SerializeField]
        private WallPreviewView previewView;

        [SerializeField]
        private WallRunPreviewView runPreviewView;

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;


        [Header("Starting State")]

        [SerializeField]
        private bool startActive = false;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logPlacementResults = true;


        public bool IsActive { get; private set; }

        public bool IsPlanningRun { get; private set; }

        public CellEdge StartEdge { get; private set; }

        public WallRunPlanResult CurrentRunPlan
        {
            get;
            private set;
        }


        public event Action<bool> ToolActiveChanged;

        public event Action<bool> RunPlanningChanged;


        private InputAction confirmAction;

        private InputAction cancelAction;

        private CellEdge currentEndEdge;

        private bool hasCurrentEndEdge;

        private bool runStartedWithGamepad;

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

            if (!IsPlanningRun)
            {
                if (confirmAction.WasPressedThisFrame())
                {
                    BeginRun();
                }

                return;
            }

            RefreshRunPlan();

            if (runStartedWithGamepad)
            {
                if (confirmAction.WasPressedThisFrame())
                {
                    TryCommitCurrentRun();
                }
            }
            else if (confirmAction.WasReleasedThisFrame())
            {
                TryCommitCurrentRun();
            }
        }


        [ContextMenu("Activate Wall Tool")]
        public void ActivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The wall construction tool can only be activated " +
                    "during Play Mode.",
                    this);

                return;
            }

            SetToolActive(true);
        }


        [ContextMenu("Deactivate Wall Tool")]
        public void DeactivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The wall construction tool can only be deactivated " +
                    "during Play Mode.",
                    this);

                return;
            }

            SetToolActive(false);
        }


        private void BeginRun()
        {
            if (!targetResolver.HasTarget)
            {
                LogWarning(
                    "Wall run could not begin because no target exists.");

                return;
            }

            if (!mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                Debug.LogError(
                    "WallConstructionToolController could not access " +
                    "an initialized WallConstructionService.",
                    this);

                return;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "WallConstructionToolController could not access " +
                    "an initialized ConstructionHistory.",
                    this);

                return;
            }

            StartEdge =
                targetResolver.CurrentTarget.Edge;

            runStartedWithGamepad =
                pointerController.IsUsingGamepad;

            IsPlanningRun = true;
            hasCurrentEndEdge = false;

            previewView.SetToolActive(false);

            RefreshRunPlan(
                forceRefresh: true);

            RunPlanningChanged?.Invoke(true);

            if (logPlacementResults)
            {
                Debug.Log(
                    $"Wall run started at {StartEdge}.",
                    this);
            }
        }


        private void RefreshRunPlan(
            bool forceRefresh = false)
        {
            if (!IsPlanningRun
                || !targetResolver.HasTarget)
            {
                return;
            }

            CellEdge alignedEndEdge =
                StraightWallRunEndpointResolver.Resolve(
                    StartEdge,
                    targetResolver);

            if (!forceRefresh
                && hasCurrentEndEdge
                && alignedEndEdge == currentEndEdge)
            {
                return;
            }

            currentEndEdge =
                alignedEndEdge;

            hasCurrentEndEdge = true;

            CurrentRunPlan =
                StraightWallRunPlanner.Plan(
                    StartEdge,
                    currentEndEdge);

            if (CurrentRunPlan.Succeeded)
            {
                runPreviewView.ShowPlan(
                    CurrentRunPlan);
            }
            else
            {
                runPreviewView.Hide();
            }
        }


        private bool TryCommitCurrentRun()
        {
            if (!CurrentRunPlan.Succeeded)
            {
                LogWarning(
                    "The current wall run has no valid geometry.");

                return false;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "The current wall run could not be recorded " +
                    "because wall history is unavailable.",
                    this);

                return false;
            }

            WallEnsureResult result =
                mapHost.WallConstruction
                    .TryEnsureWalls(
                        CurrentRunPlan.Edges);

            if (!result.Succeeded)
            {
                LogRejectedRun(
                    result);

                if (!runStartedWithGamepad)
                {
                    FinishRun();
                }

                return false;
            }

            if (!result.Edit.IsEmpty)
            {
                historyHost.History.Record(
                    new ReversibleWallEditAction(
                        mapHost.WallConstruction,
                        result.Edit));
            }

            if (logPlacementResults)
            {
                Debug.Log(
                    $"Wall run processed. " +
                    $"Requested: {result.RequestedCount}. " +
                    $"Created: {result.ChangedCount}. " +
                    $"Already existing: " +
                    $"{result.AlreadyExistingCount}. " +
                    $"Skipped outside map: " +
                    $"{result.SkippedOutsideMapCount}. " +
                    $"Skipped outside construction area: " +
                    $"{result.SkippedOutsideConstructionAreaCount}.",
                    this);
            }

            bool anySegmentSatisfied =
                result.SatisfiedCount > 0;

            if (!anySegmentSatisfied
                && runStartedWithGamepad)
            {
                RefreshRunPlan(
                    forceRefresh: true);

                return false;
            }

            FinishRun();

            return result.ChangedCount > 0;
        }


        private void HandleCancel()
        {
            if (IsPlanningRun)
            {
                CancelCurrentRun();
                return;
            }

            SetToolActive(false);
        }


        private void CancelCurrentRun()
        {
            if (!IsPlanningRun)
            {
                return;
            }

            if (logPlacementResults)
            {
                Debug.Log(
                    "Current wall run cancelled.",
                    this);
            }

            FinishRun();
        }


        private void FinishRun()
        {
            IsPlanningRun = false;
            CurrentRunPlan = default;
            hasCurrentEndEdge = false;

            runPreviewView.Hide();

            if (IsActive)
            {
                previewView.SetToolActive(true);
            }

            RunPlanningChanged?.Invoke(false);
        }


        private void SetToolActive(
            bool isActive)
        {
            if (IsActive == isActive)
            {
                if (IsActive && !IsPlanningRun)
                {
                    previewView.SetToolActive(true);
                }

                return;
            }

            if (!isActive && IsPlanningRun)
            {
                FinishRun();
            }

            IsActive = isActive;

            previewView.SetToolActive(
                IsActive);

            if (!IsActive)
            {
                runPreviewView.Hide();
            }

            ToolActiveChanged?.Invoke(
                IsActive);

            if (logPlacementResults)
            {
                Debug.Log(
                    IsActive
                        ? "Wall construction tool activated."
                        : "Wall construction tool deactivated.",
                    this);
            }
        }


        private void HandlePointerModeChanged(
            bool isUsingGamepad)
        {
            if (IsPlanningRun)
            {
                CancelCurrentRun();
            }
        }


        private bool TryResolveActions()
        {
            if (playerInput.actions == null)
            {
                Debug.LogError(
                    "WallConstructionToolController could not find an " +
                    "Input Actions asset on PlayerInput.",
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
                    $"WallConstructionToolController requires actions " +
                    $"named '{confirmActionName}' and " +
                    $"'{cancelActionName}' inside the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void LogRejectedRun(
            WallEnsureResult result)
        {
            if (!logPlacementResults)
            {
                return;
            }

            Debug.LogWarning(
                $"Wall run could not be processed. " +
                $"Reason: {result.Failure}. " +
                $"Failed edge: {result.FailedEdge}.",
                this);
        }


        private void LogWarning(
            string message)
        {
            if (logPlacementResults)
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
                    "WallConstructionToolController has no " +
                    "PlayerInput assigned.",
                    this);

                isValid = false;
            }

            if (pointerController == null)
            {
                Debug.LogError(
                    "WallConstructionToolController has no " +
                    "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallConstructionToolController has no " +
                    "WallTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (previewView == null)
            {
                Debug.LogError(
                    "WallConstructionToolController has no " +
                    "WallPreviewView assigned.",
                    this);

                isValid = false;
            }

            if (runPreviewView == null)
            {
                Debug.LogError(
                    "WallConstructionToolController has no " +
                    "WallRunPreviewView assigned.",
                    this);

                isValid = false;
            }

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallConstructionToolController has no " +
                    "GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "WallConstructionToolController has no " +
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
            IsPlanningRun = false;

            if (previewView != null)
            {
                previewView.SetToolActive(false);
            }

            if (runPreviewView != null)
            {
                runPreviewView.Hide();
            }
        }
    }
}
