using System;
using System.Collections.Generic;
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
    /// Coordinates straight wall-run demolition from one selected grid vertex
    /// to another.
    ///
    /// Empty edges are skipped, while exact removed walls are recorded as one
    /// neutral construction-history action.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class WallDemolitionToolController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private ConstructionPointerController pointerController;
        [SerializeField] private string constructionActionMapName = "Construction";
        [SerializeField] private string confirmActionName = "Confirm";
        [SerializeField] private string cancelActionName = "Cancel";

        [Header("Demolition Tool")]
        [SerializeField] private WallVertexTargetResolver targetResolver;
        [SerializeField] private WallDemolitionPreviewView previewView;
        [SerializeField] private WallDemolitionRunPreviewView runPreviewView;
        [SerializeField] private GridMapHost mapHost;
        [SerializeField] private ConstructionHistoryHost historyHost;

        [Header("Starting State")]
        [SerializeField] private bool startActive = false;

        [Header("Diagnostics")]
        [SerializeField] private bool logDemolitionResults = true;

        public bool IsActive { get; private set; }
        public bool IsPlanningRun { get; private set; }
        public GridVertex StartVertex { get; private set; }
        public WallVertexRunPlanResult CurrentRunPlan { get; private set; }

        public event Action<bool> ToolActiveChanged;
        public event Action<bool> RunPlanningChanged;

        private InputAction confirmAction;
        private InputAction cancelAction;
        private GridVertex currentEndVertex;
        private bool hasCurrentEndVertex;
        private bool runStartedWithGamepad;
        private bool isInitialized;


        private void Awake()
        {
            if (!ValidateReferences() || !TryResolveActions())
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
            if (!isInitialized || !IsActive)
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
                    "The wall demolition tool can only be activated during "
                    + "Play Mode.",
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
                    "The wall demolition tool can only be deactivated during "
                    + "Play Mode.",
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
                    "Wall demolition could not begin because no vertex "
                    + "target exists.");

                return;
            }

            if (!mapHost.IsInitialized || mapHost.WallConstruction == null)
            {
                Debug.LogError(
                    "WallDemolitionToolController could not access an "
                    + "initialized WallConstructionService.",
                    this);

                return;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "WallDemolitionToolController could not access an "
                    + "initialized ConstructionHistory.",
                    this);

                return;
            }

            StartVertex =
                targetResolver.CurrentTarget.Vertex;

            runStartedWithGamepad =
                pointerController.IsUsingGamepad;

            IsPlanningRun = true;
            hasCurrentEndVertex = false;

            previewView.SetToolActive(false);
            runPreviewView.ShowAnchor(StartVertex);
            RefreshRunPlan(forceRefresh: true);
            RunPlanningChanged?.Invoke(true);

            if (logDemolitionResults)
            {
                Debug.Log(
                    $"Wall demolition run started at {StartVertex}.",
                    this);
            }
        }


        private void RefreshRunPlan(
            bool forceRefresh = false)
        {
            if (!IsPlanningRun || !targetResolver.HasTarget)
            {
                return;
            }

            GridVertex alignedEndVertex =
                StraightWallVertexRunEndpointResolver.Resolve(
                    StartVertex,
                    targetResolver);

            if (!forceRefresh
                && hasCurrentEndVertex
                && alignedEndVertex == currentEndVertex)
            {
                return;
            }

            currentEndVertex = alignedEndVertex;
            hasCurrentEndVertex = true;

            CurrentRunPlan =
                StraightWallVertexRunPlanner.Plan(
                    StartVertex,
                    currentEndVertex);

            if (CurrentRunPlan.Succeeded)
            {
                runPreviewView.ShowPlan(CurrentRunPlan);
                return;
            }

            if (CurrentRunPlan.Failure
                == WallVertexRunPlanFailure.SameVertex)
            {
                runPreviewView.ShowAnchor(StartVertex);
                return;
            }

            runPreviewView.Hide();
        }


        private bool TryCommitCurrentRun()
        {
            if (!CurrentRunPlan.Succeeded)
            {
                LogWarning(
                    "The current demolition run has no removable length.");

                // A mouse click without meaningful drag is a harmless
                // cancellation because the gesture ends on button release.
                if (!runStartedWithGamepad)
                {
                    FinishRun();
                }

                return false;
            }

            if (!historyHost.TryInitialize())
            {
                Debug.LogError(
                    "The current demolition run could not be recorded because "
                    + "wall history is unavailable.",
                    this);

                return false;
            }

            List<DoorAssembly> removedAssemblies =
                CollectSupportedAssemblies(
                    CurrentRunPlan.Edges);

            WallClearResult result =
                mapHost.WallConstruction
                    .TryClearWalls(CurrentRunPlan.Edges);

            if (!result.Succeeded)
            {
                LogRejectedRun(result);

                if (!runStartedWithGamepad)
                {
                    FinishRun();
                }

                return false;
            }

            if (!result.Edit.IsEmpty)
            {
                historyHost.History.Record(
                    new ReversibleWallDemolitionAction(
                        mapHost.WallConstruction,
                        mapHost.DoorConstruction,
                        result.Edit,
                        removedAssemblies));
            }

            if (logDemolitionResults)
            {
                Debug.Log(
                    $"Vertex wall demolition run processed. "
                    + $"Requested: {result.RequestedCount}. "
                    + $"Removed: {result.RemovedCount}. "
                    + $"Openings removed: {removedAssemblies.Count}. "
                    + $"Already empty: {result.AlreadyEmptyCount}.",
                    this);
            }

            if (result.RemovedCount == 0 && runStartedWithGamepad)
            {
                RefreshRunPlan(forceRefresh: true);
                return false;
            }

            FinishRun();
            return result.RemovedCount > 0;
        }


        private List<DoorAssembly> CollectSupportedAssemblies(
            IReadOnlyList<CellEdge> edges)
        {
            List<DoorAssembly> assemblies =
                new List<DoorAssembly>();

            if (mapHost.DoorAssemblies == null)
            {
                return assemblies;
            }

            HashSet<DoorAssemblyId> seenAssemblyIds =
                new HashSet<DoorAssemblyId>();

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                if (!mapHost.DoorAssemblies.TryGetAssemblyAtEdge(
                        edges[index],
                        out DoorAssembly assembly)
                    || !seenAssemblyIds.Add(assembly.Id))
                {
                    continue;
                }

                assemblies.Add(assembly);
            }

            return assemblies;
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
                    "Current vertex wall demolition run cancelled.",
                    this);
            }

            FinishRun();
        }


        private void FinishRun()
        {
            IsPlanningRun = false;
            CurrentRunPlan = default;
            StartVertex = default;
            currentEndVertex = default;
            hasCurrentEndVertex = false;

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
            previewView.SetToolActive(IsActive);

            if (!IsActive)
            {
                runPreviewView.Hide();
            }

            ToolActiveChanged?.Invoke(IsActive);

            if (logDemolitionResults)
            {
                Debug.Log(
                    IsActive
                        ? "Vertex wall demolition tool activated."
                        : "Vertex wall demolition tool deactivated.",
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
                    "WallDemolitionToolController could not find an Input "
                    + "Actions asset on PlayerInput.",
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
                    $"Could not find an Action Map named "
                    + $"'{constructionActionMapName}'.",
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

            if (confirmAction == null || cancelAction == null)
            {
                Debug.LogError(
                    $"WallDemolitionToolController requires actions named "
                    + $"'{confirmActionName}' and '{cancelActionName}' inside "
                    + $"the '{constructionActionMapName}' Action Map.",
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
                $"Vertex wall demolition run could not be processed. "
                + $"Reason: {result.Failure}. "
                + $"Failed edge: {result.FailedEdge}.",
                this);
        }


        private void LogWarning(
            string message)
        {
            if (logDemolitionResults)
            {
                Debug.LogWarning(message, this);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            isValid &= RequireReference(playerInput, "PlayerInput");
            isValid &= RequireReference(
                pointerController,
                "ConstructionPointerController");
            isValid &= RequireReference(
                targetResolver,
                "WallVertexTargetResolver");
            isValid &= RequireReference(
                previewView,
                "WallDemolitionPreviewView");
            isValid &= RequireReference(
                runPreviewView,
                "WallDemolitionRunPreviewView");
            isValid &= RequireReference(mapHost, "GridMapHost");
            isValid &= RequireReference(
                historyHost,
                "ConstructionHistoryHost");

            return isValid;
        }


        private bool RequireReference(
            UnityEngine.Object reference,
            string label)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"WallDemolitionToolController has no {label} assigned.",
                this);

            return false;
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
            StartVertex = default;
            CurrentRunPlan = default;

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
