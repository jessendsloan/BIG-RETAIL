using System;
using BigRetail.Construction.Unity.History;
using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Doors
{
    /// <summary>
    /// Places one window on the existing wall beneath the shared construction
    /// pointer. Its hover, click, cancellation, and refresh lifecycle mirrors
    /// door placement. The committed non-passable assembly keeps the wall's
    /// finish intact while the presentation cuts a masked aperture beneath
    /// the window artwork.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class WindowConstructionToolController : MonoBehaviour
    {
        [Header("Input")]

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private string constructionActionMapName = "Construction";

        [SerializeField]
        private string confirmActionName = "Confirm";

        [SerializeField]
        private string cancelActionName = "Cancel";


        [Header("Window Tool")]

        [Tooltip(
            "Provides UI-gated pointer position and projected wall poses.")]
        [SerializeField]
        private WallVertexTargetResolver targetResolver;

        [Tooltip(
            "Identifies the existing wall segment directly beneath the pointer.")]
        [SerializeField]
        private WallTargetResolver wallTargetResolver;

        [SerializeField]
        private DoorRunPreviewView placementPreviewView;

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;

        [SerializeField]
        private DoorDefinitionAsset windowDefinition;


        [Header("Starting State")]

        [SerializeField]
        private bool startActive;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logPlacementResults = true;


        public bool IsActive { get; private set; }

        public bool HasPlacementPreview { get; private set; }

        public WallVertexRunPlanResult CurrentPlacementPlan
        {
            get;
            private set;
        }


        public event Action<bool> ToolActiveChanged;


        private InputAction confirmAction;
        private InputAction cancelAction;
        private WallState subscribedWallState;
        private WallFinishService subscribedWallFinishService;
        private DoorAssemblyState subscribedDoorAssemblyState;
        private CellEdge currentHoveredEdge;
        private bool previewDirty = true;
        private bool isInitialized;


        private void Awake()
        {
            if (!ValidateReferences()
                || !TryResolveActions())
            {
                enabled = false;
                return;
            }

            try
            {
                windowDefinition.ValidateConfiguration();

                if (windowDefinition.SegmentCount != 1
                    || windowDefinition.HasPassageSegments)
                {
                    throw new InvalidOperationException(
                        "Window placement requires a one-segment, "
                        + "non-passable definition.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    windowDefinition);
                enabled = false;
                return;
            }

            isInitialized = true;
        }


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }
        }


        private void Start()
        {
            AttachToRuntimeStates();

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
                SetToolActive(false);
                return;
            }

            RefreshPlacementPreview();

            if (confirmAction.WasPressedThisFrame())
            {
                TryCommitCurrentPlacement();
            }
        }


        public void ActivateTool()
        {
            if (Application.isPlaying)
            {
                SetToolActive(true);
            }
        }


        public void DeactivateTool()
        {
            if (Application.isPlaying)
            {
                SetToolActive(false);
            }
        }


        /// <summary>
        /// Clears the transient hover preview. View rotation calls this before
        /// the presentation changes; the preview resnaps on the next frame.
        /// </summary>
        public void ClearPlacementPreview()
        {
            HasPlacementPreview = false;
            CurrentPlacementPlan = default;
            currentHoveredEdge = default;
            previewDirty = true;

            if (placementPreviewView != null)
            {
                placementPreviewView.Hide();
            }
        }


        private void RefreshPlacementPreview(
            bool forceRefresh = false)
        {
            if (!mapHost.IsInitialized
                || mapHost.WallState == null
                || mapHost.DoorConstruction == null
                || mapHost.WallFinishes == null
                || mapHost.DoorAssemblies == null
                || !targetResolver.HasTarget
                || !wallTargetResolver.HasTarget)
            {
                ClearPlacementPreview();
                return;
            }

            CellEdge hoveredEdge =
                wallTargetResolver.CurrentTarget.Edge;

            if (!mapHost.WallState.HasWall(
                    hoveredEdge))
            {
                ClearPlacementPreview();
                return;
            }

            if (!forceRefresh
                && !previewDirty
                && HasPlacementPreview
                && hoveredEdge == currentHoveredEdge)
            {
                return;
            }

            WallVertexRunPlanResult plan =
                DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    hoveredEdge.FirstVertex,
                    segmentCount: 1);

            if (!plan.Succeeded)
            {
                ClearPlacementPreview();
                return;
            }

            currentHoveredEdge = hoveredEdge;
            CurrentPlacementPlan = plan;
            HasPlacementPreview = true;
            previewDirty = false;

            placementPreviewView.ShowPlan(
                plan,
                windowDefinition);
        }


        private bool TryCommitCurrentPlacement()
        {
            if (!HasPlacementPreview
                || !CurrentPlacementPlan.Succeeded)
            {
                return false;
            }

            if (!placementPreviewView.IsPlanValid)
            {
                LogWarning(
                    $"Window placement rejected: "
                    + $"{placementPreviewView.CurrentFailure}.");
                return false;
            }

            if (!mapHost.IsInitialized
                || mapHost.DoorConstruction == null
                || mapHost.DoorAssemblies == null
                || !historyHost.TryInitialize())
            {
                Debug.LogError(
                    "WindowConstructionToolController could not access its "
                    + "initialized runtime services.",
                    this);
                return false;
            }

            DoorAssemblyId assemblyId =
                new DoorAssemblyId(
                    Guid.NewGuid().ToString("N"));

            DoorAssemblyChangeResult result =
                mapHost.DoorConstruction.TryPlaceAssembly(
                    assemblyId,
                    windowDefinition.Id,
                    CurrentPlacementPlan.Edges);

            if (!result.Succeeded)
            {
                LogWarning(
                    $"Window placement rejected: {result.Failure}. "
                    + $"Edge: {result.FailedEdge}.");

                previewDirty = true;
                RefreshPlacementPreview(
                    forceRefresh: true);
                return false;
            }

            historyHost.History.Record(
                new ReversibleDoorAssemblyEditAction(
                    mapHost.DoorConstruction,
                    result.Assembly));

            if (logPlacementResults)
            {
                Debug.Log(
                    $"Placed a window on "
                    + $"{CurrentPlacementPlan.Edges[0]}.",
                    this);
            }

            ClearPlacementPreview();
            return true;
        }


        private void SetToolActive(
            bool isActive)
        {
            if (IsActive == isActive)
            {
                if (isActive)
                {
                    previewDirty = true;
                }

                return;
            }

            IsActive = isActive;

            if (IsActive)
            {
                previewDirty = true;
            }
            else
            {
                ClearPlacementPreview();
            }

            ToolActiveChanged?.Invoke(
                IsActive);
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            AttachToRuntimeStates();
            previewDirty = true;
        }


        private void HandleWallChanged(
            CellEdge edge)
        {
            previewDirty = true;
        }


        private void HandleDoorAssemblyChanged(
            DoorAssembly assembly)
        {
            previewDirty = true;
        }


        private void HandleEffectiveWallFinishChanged(
            WallFaceKey face,
            WallFinishId finishId)
        {
            previewDirty = true;
        }


        private void AttachToRuntimeStates()
        {
            if (mapHost == null
                || !mapHost.IsInitialized
                || mapHost.WallState == null
                || mapHost.WallFinishes == null
                || mapHost.DoorAssemblies == null)
            {
                return;
            }

            if (subscribedWallState == mapHost.WallState
                && subscribedWallFinishService == mapHost.WallFinishes
                && subscribedDoorAssemblyState
                    == mapHost.DoorAssemblies)
            {
                return;
            }

            DetachFromRuntimeStates();

            subscribedWallState =
                mapHost.WallState;

            subscribedWallFinishService =
                mapHost.WallFinishes;

            subscribedDoorAssemblyState =
                mapHost.DoorAssemblies;

            subscribedWallState.WallAdded +=
                HandleWallChanged;

            subscribedWallState.WallRemoved +=
                HandleWallChanged;

            subscribedWallFinishService.EffectiveFinishChanged +=
                HandleEffectiveWallFinishChanged;

            subscribedDoorAssemblyState.AssemblyAdded +=
                HandleDoorAssemblyChanged;

            subscribedDoorAssemblyState.AssemblyRemoved +=
                HandleDoorAssemblyChanged;
        }


        private void DetachFromRuntimeStates()
        {
            if (subscribedWallState != null)
            {
                subscribedWallState.WallAdded -=
                    HandleWallChanged;

                subscribedWallState.WallRemoved -=
                    HandleWallChanged;
            }

            if (subscribedWallFinishService != null)
            {
                subscribedWallFinishService.EffectiveFinishChanged -=
                    HandleEffectiveWallFinishChanged;
            }

            if (subscribedDoorAssemblyState != null)
            {
                subscribedDoorAssemblyState.AssemblyAdded -=
                    HandleDoorAssemblyChanged;

                subscribedDoorAssemblyState.AssemblyRemoved -=
                    HandleDoorAssemblyChanged;
            }

            subscribedWallState = null;
            subscribedWallFinishService = null;
            subscribedDoorAssemblyState = null;
        }


        private bool TryResolveActions()
        {
            if (playerInput.actions == null)
            {
                return false;
            }

            InputActionMap actionMap =
                playerInput.actions.FindActionMap(
                    constructionActionMapName,
                    throwIfNotFound: false);

            if (actionMap == null)
            {
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

            if (confirmAction != null
                && cancelAction != null)
            {
                return true;
            }

            Debug.LogError(
                "WindowConstructionToolController could not resolve its "
                + "Confirm and Cancel actions.",
                this);

            return false;
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            isValid &= RequireReference(playerInput, "PlayerInput");
            isValid &= RequireReference(
                targetResolver,
                "WallVertexTargetResolver");
            isValid &= RequireReference(
                wallTargetResolver,
                "WallTargetResolver");
            isValid &= RequireReference(
                placementPreviewView,
                "DoorRunPreviewView");
            isValid &= RequireReference(mapHost, "GridMapHost");
            isValid &= RequireReference(
                historyHost,
                "ConstructionHistoryHost");
            isValid &= RequireReference(
                windowDefinition,
                "window DoorDefinitionAsset");

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
                $"WindowConstructionToolController has no {label} assigned.",
                this);

            return false;
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


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }

            DetachFromRuntimeStates();

            IsActive = false;
            ClearPlacementPreview();
        }
    }
}
