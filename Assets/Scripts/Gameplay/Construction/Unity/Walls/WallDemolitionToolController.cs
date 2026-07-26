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
    /// Coordinates straight wall-run demolition.
    ///
    /// Empty edges are skipped, while exact removed walls are recorded
    /// as neutral construction-history actions.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class WallDemolitionToolController :
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


        [Header("Demolition Tool")]

        [SerializeField]
        private WallTargetResolver targetResolver;

        [SerializeField]
        private WallDemolitionPreviewView previewView;

        [SerializeField]
        private WallDemolitionRunPreviewView runPreviewView;

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;


        [Header("Starting State")]

        [SerializeField]
        private bool startActive = false;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logDemolitionResults = true;


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


        [ContextMenu("Activate Wall Demolition Tool")]
        public void ActivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The wall demolition tool can only be " +
                    "activated during Play Mode.",
                    this);

                return;
            }

            SetToolActive(true);
        }


        [ContextMenu("Deactivate Wall Demolition Tool")]
        public void DeactivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The wall demolition tool can only be " +
                    "deactivated during Play Mode.",
                    this);

                return;
            }

            SetToolActive(false);
        }


        public void CancelCurrentGesture()
        {
            if (IsPlanningRun)
            {
                CancelCurrentRun();
            }
        }


        private void BeginRun()
        {
            if (!targetResolver.HasTarget)
            {
                LogWarning(
                    "Wall demolition could not begin because " +
                    "no target exists.");

                return;
            }

            if (!mapHost.IsInitialized
                || mapHost.WallConstruction == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController could not access " +
                    "an initialized WallConstructionService.",
                    this);

                return;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "WallDemolitionToolController could not access " +
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

            if (logDemolitionResults)
            {
                Debug.Log(
                    $"Wall demolition run started at {StartEdge}.",
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
                    "The current demolition run has no " +
                    "valid geometry.");

                return false;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "The current demolition run could not be recorded " +
                    "because wall history is unavailable.",
                    this);

                return false;
            }

            WallClearResult result =
                mapHost.WallConstruction
                    .TryClearWalls(
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

            if (logDemolitionResults)
            {
                Debug.Log(
                    $"Wall demolition run processed. " +
                    $"Requested: {result.RequestedCount}. " +
                    $"Removed: {result.RemovedCount}. " +
                    $"Already empty: {result.AlreadyEmptyCount}.",
                    this);
            }

            if (result.RemovedCount == 0
                && runStartedWithGamepad)
            {
                RefreshRunPlan(
                    forceRefresh: true);

                return false;
            }

            FinishRun();

            return result.RemovedCount > 0;
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

            if (logDemolitionResults)
            {
                Debug.Log(
                    "Current wall demolition run cancelled.",
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

            if (logDemolitionResults)
            {
                Debug.Log(
                    IsActive
                        ? "Wall demolition tool activated."
                        : "Wall demolition tool deactivated.",
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
                    "WallDemolitionToolController could not find " +
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
                    $"WallDemolitionToolController requires actions " +
                    $"named '{confirmActionName}' and " +
                    $"'{cancelActionName}' inside the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void LogRejectedRun(
            WallClearResult result)
        {
            if (!logDemolitionResults)
            {
                return;
            }

            Debug.LogWarning(
                $"Wall demolition run could not be processed. " +
                $"Reason: {result.Failure}. " +
                $"Failed edge: {result.FailedEdge}.",
                this);
        }


        private void LogWarning(
            string message)
        {
            if (logDemolitionResults)
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
                    "WallDemolitionToolController has no " +
                    "PlayerInput assigned.",
                    this);

                isValid = false;
            }

            if (pointerController == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController has no " +
                    "ConstructionPointerController assigned.",
                    this);

                isValid = false;
            }

            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController has no " +
                    "WallTargetResolver assigned.",
                    this);

                isValid = false;
            }

            if (previewView == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController has no " +
                    "WallDemolitionPreviewView assigned.",
                    this);

                isValid = false;
            }

            if (runPreviewView == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController has no " +
                    "WallDemolitionRunPreviewView assigned.",
                    this);

                isValid = false;
            }

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController has no " +
                    "GridMapHost assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController has no " +
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
