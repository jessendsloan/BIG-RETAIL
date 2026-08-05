using System;
using BigRetail.Construction.Unity.History;
using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Doors
{
    /// <summary>
    /// Snaps the selected door definition onto the wall beneath the shared
    /// construction pointer and places the complete assembly with one click.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class DoorConstructionToolController : MonoBehaviour
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


        [Header("Door Tool")]

        [Tooltip(
            "Provides UI-gated pointer position and projected vertex poses.")]
        [SerializeField]
        private WallVertexTargetResolver targetResolver;

        [Tooltip(
            "Identifies the wall segment directly beneath the pointer.")]
        [SerializeField]
        private WallTargetResolver wallTargetResolver;

        [SerializeField]
        private DoorRunPreviewView runPreviewView;

        [SerializeField]
        private GridMapHost mapHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;

        [SerializeField]
        private DoorDefinitionSelectionHost definitionSelection;


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
        private GridVertex currentCenterVertex;
        private DoorDefinitionId currentDefinitionId;
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

            isInitialized = true;
        }


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }

            if (definitionSelection != null)
            {
                definitionSelection.SelectedDefinitionChanged +=
                    HandleDefinitionSelectionChanged;
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
            currentCenterVertex = default;
            currentDefinitionId = default;
            previewDirty = true;

            if (runPreviewView != null)
            {
                runPreviewView.Hide();
            }
        }


        private void RefreshPlacementPreview(
            bool forceRefresh = false)
        {
            if (!mapHost.IsInitialized
                || mapHost.WallState == null
                || mapHost.DoorConstruction == null
                || !definitionSelection.IsInitialized
                || !targetResolver.HasTarget
                || !wallTargetResolver.HasTarget)
            {
                ClearPlacementPreview();
                return;
            }

            DoorDefinitionAsset definition =
                definitionSelection.SelectedDefinitionAsset;

            if (definition == null)
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

            GridVertex centerVertex =
                ResolvePreferredCenterVertex(
                    hoveredEdge,
                    definition.SegmentCount);

            DoorDefinitionId definitionId =
                definition.Id;

            if (!forceRefresh
                && !previewDirty
                && HasPlacementPreview
                && hoveredEdge == currentHoveredEdge
                && centerVertex == currentCenterVertex
                && definitionId == currentDefinitionId)
            {
                return;
            }

            WallVertexRunPlanResult plan =
                DoorPlacementSpanPlanner.Plan(
                    hoveredEdge,
                    centerVertex,
                    definition.SegmentCount);

            if (!plan.Succeeded)
            {
                ClearPlacementPreview();
                return;
            }

            currentHoveredEdge = hoveredEdge;
            currentCenterVertex = centerVertex;
            currentDefinitionId = definitionId;
            CurrentPlacementPlan = plan;
            HasPlacementPreview = true;
            previewDirty = false;

            runPreviewView.ShowPlan(
                plan);
        }


        private GridVertex ResolvePreferredCenterVertex(
            CellEdge hoveredEdge,
            int segmentCount)
        {
            if (segmentCount % 2 != 0)
            {
                return hoveredEdge.FirstVertex;
            }

            GridVertex firstVertex =
                hoveredEdge.FirstVertex;

            GridVertex secondVertex =
                hoveredEdge.SecondVertex;

            GridVertexWorldPose firstPose =
                GridVertexWorldPose.Calculate(
                    firstVertex,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            GridVertexWorldPose secondPose =
                GridVertexWorldPose.Calculate(
                    secondVertex,
                    targetResolver.CoordinateTilemap,
                    targetResolver.LogicalLevel,
                    targetResolver.UnityCellZ,
                    targetResolver.ViewProjection);

            float firstSquaredDistance =
                (targetResolver.PointerWorldPosition
                    - firstPose.Position)
                .sqrMagnitude;

            float secondSquaredDistance =
                (targetResolver.PointerWorldPosition
                    - secondPose.Position)
                .sqrMagnitude;

            return firstSquaredDistance
                    <= secondSquaredDistance
                ? firstVertex
                : secondVertex;
        }


        private bool TryCommitCurrentPlacement()
        {
            if (!HasPlacementPreview
                || !CurrentPlacementPlan.Succeeded)
            {
                return false;
            }

            if (!runPreviewView.IsPlanValid)
            {
                LogWarning(
                    $"Door placement rejected: "
                    + $"{runPreviewView.CurrentFailure}.");

                return false;
            }

            if (!mapHost.IsInitialized
                || mapHost.DoorConstruction == null
                || !historyHost.TryInitialize())
            {
                Debug.LogError(
                    "DoorConstructionToolController could not access its "
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
                    currentDefinitionId,
                    CurrentPlacementPlan.Edges);

            if (!result.Succeeded)
            {
                LogWarning(
                    $"Door placement rejected: {result.Failure}. "
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
                    $"Placed '{result.DefinitionId}' across "
                    + $"{result.SegmentCount} wall segments.",
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


        private void HandleDefinitionSelectionChanged(
            DoorDefinitionId definitionId)
        {
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
                "DoorConstructionToolController could not resolve its "
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
                runPreviewView,
                "DoorRunPreviewView");
            isValid &= RequireReference(mapHost, "GridMapHost");
            isValid &= RequireReference(
                historyHost,
                "ConstructionHistoryHost");
            isValid &= RequireReference(
                definitionSelection,
                "DoorDefinitionSelectionHost");

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
                $"DoorConstructionToolController has no {label} assigned.",
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

            if (definitionSelection != null)
            {
                definitionSelection.SelectedDefinitionChanged -=
                    HandleDefinitionSelectionChanged;
            }

            DetachFromRuntimeStates();

            IsActive = false;
            ClearPlacementPreview();
        }
    }
}
