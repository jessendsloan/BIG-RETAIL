using System;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.History;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Fixtures
{
    /// <summary>
    /// Places one selected fixture at the targeted grid cell with one click.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class FixtureConstructionToolController : MonoBehaviour
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

        [SerializeField]
        private string rotateActionName = "RotateFixture";

        [Header("Fixture Tool")]

        [SerializeField]
        private GridCellTargetResolver targetResolver;

        [SerializeField]
        private FixturePlacementPreviewView previewView;

        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;

        [SerializeField]
        private FixtureDefinitionSelectionHost definitionSelection;

        [Header("Starting State")]

        [SerializeField]
        private bool startActive;

        [Header("Diagnostics")]

        [SerializeField]
        private bool logPlacementResults = true;


        public bool IsActive { get; private set; }

        public bool HasPlacementPreview { get; private set; }


        public event Action<bool> ToolActiveChanged;


        private InputAction confirmAction;
        private InputAction cancelAction;
        private InputAction rotateAction;
        private FixtureState subscribedState;
        private GridPosition currentCell;
        private FixtureDefinitionId currentDefinitionId;
        private FixtureOrientation currentOrientation;
        private bool previewDirty = true;
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
            runtimeHost.Initialized += HandleRuntimeInitialized;
            definitionSelection.SelectedDefinitionChanged +=
                HandleDefinitionChanged;
            definitionSelection.OrientationChanged +=
                HandleOrientationChanged;
        }


        private void Start()
        {
            AttachToState();
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
                SetToolActive(false);
                return;
            }

            if (rotateAction.WasPressedThisFrame())
            {
                definitionSelection.RotateClockwise();
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


        public void ClearPlacementPreview()
        {
            HasPlacementPreview = false;
            currentCell = default;
            currentDefinitionId = default;
            previewDirty = true;
            previewView.Hide();
        }


        private void RefreshPlacementPreview(bool forceRefresh = false)
        {
            if (!runtimeHost.TryInitialize()
                || !definitionSelection.IsInitialized
                || !targetResolver.HasTarget)
            {
                ClearPlacementPreview();
                return;
            }

            GridPosition targetedCell = targetResolver.CurrentCell;
            FixtureDefinitionId definitionId =
                definitionSelection.SelectedDefinitionId;
            FixtureOrientation orientation =
                definitionSelection.Orientation;

            if (!forceRefresh
                && !previewDirty
                && HasPlacementPreview
                && targetedCell == currentCell
                && definitionId == currentDefinitionId
                && orientation == currentOrientation)
            {
                return;
            }

            currentCell = targetedCell;
            currentDefinitionId = definitionId;
            currentOrientation = orientation;
            HasPlacementPreview = true;
            previewDirty = false;

            previewView.ShowPlacement(
                currentCell,
                currentDefinitionId,
                currentOrientation);
        }


        private bool TryCommitCurrentPlacement()
        {
            if (!HasPlacementPreview || !previewView.IsPlacementValid)
            {
                LogWarning(
                    $"Fixture placement rejected: {previewView.CurrentFailure}.");
                return false;
            }

            if (!runtimeHost.TryInitialize()
                || runtimeHost.FixturePlacement == null
                || !historyHost.TryInitialize())
            {
                Debug.LogError(
                    "FixtureConstructionToolController could not access initialized runtime services.",
                    this);
                return false;
            }

            FixtureInstanceId instanceId =
                new FixtureInstanceId(Guid.NewGuid().ToString("N"));

            FixturePlacementResult result =
                runtimeHost.FixturePlacement.TryPlaceFixture(
                    instanceId,
                    currentDefinitionId,
                    currentCell,
                    currentOrientation);

            if (!result.Succeeded)
            {
                LogWarning(
                    $"Fixture placement rejected: {result.Failure}. Cell: {result.FailedCell}.");
                previewDirty = true;
                RefreshPlacementPreview(forceRefresh: true);
                return false;
            }

            historyHost.History.Record(
                new ReversibleFixtureEditAction(
                    runtimeHost.FixturePlacement,
                    result.Edit));

            if (logPlacementResults)
            {
                Debug.Log(
                    $"Placed '{result.DefinitionId}' across {result.OccupiedCellCount} cell(s).",
                    this);
            }

            ClearPlacementPreview();
            return true;
        }


        private void SetToolActive(bool isActive)
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

            if (isActive)
            {
                previewDirty = true;
            }
            else
            {
                ClearPlacementPreview();
            }

            ToolActiveChanged?.Invoke(isActive);
        }


        private void HandleRuntimeInitialized(FixtureRuntimeHost initializedHost)
        {
            AttachToState();
            previewDirty = true;
        }


        private void HandleDefinitionChanged(FixtureDefinitionId definitionId)
        {
            previewDirty = true;
        }


        private void HandleOrientationChanged(FixtureOrientation orientation)
        {
            previewDirty = true;
        }


        private void HandleFixtureChanged(FixtureInstance fixture)
        {
            previewDirty = true;
        }


        private void AttachToState()
        {
            if (!runtimeHost.IsInitialized
                || runtimeHost.FixtureState == null
                || subscribedState == runtimeHost.FixtureState)
            {
                return;
            }

            DetachFromState();
            subscribedState = runtimeHost.FixtureState;
            subscribedState.FixtureAdded += HandleFixtureChanged;
            subscribedState.FixtureRemoved += HandleFixtureChanged;
        }


        private void DetachFromState()
        {
            if (subscribedState == null)
            {
                return;
            }

            subscribedState.FixtureAdded -= HandleFixtureChanged;
            subscribedState.FixtureRemoved -= HandleFixtureChanged;
            subscribedState = null;
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

            confirmAction = actionMap.FindAction(confirmActionName, false);
            cancelAction = actionMap.FindAction(cancelActionName, false);
            rotateAction = actionMap.FindAction(rotateActionName, false);

            if (confirmAction != null
                && cancelAction != null
                && rotateAction != null)
            {
                return true;
            }

            Debug.LogError(
                "FixtureConstructionToolController could not resolve its Confirm, Cancel, and RotateFixture actions.",
                this);
            return false;
        }


        private bool ValidateReferences()
        {
            bool isValid = true;
            isValid &= RequireReference(playerInput, "PlayerInput");
            isValid &= RequireReference(targetResolver, "GridCellTargetResolver");
            isValid &= RequireReference(previewView, "FixturePlacementPreviewView");
            isValid &= RequireReference(runtimeHost, "FixtureRuntimeHost");
            isValid &= RequireReference(historyHost, "ConstructionHistoryHost");
            isValid &= RequireReference(definitionSelection, "FixtureDefinitionSelectionHost");
            return isValid;
        }


        private bool RequireReference(UnityEngine.Object reference, string label)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"FixtureConstructionToolController has no {label} assigned.",
                this);
            return false;
        }


        private void LogWarning(string message)
        {
            if (logPlacementResults)
            {
                Debug.LogWarning(message, this);
            }
        }


        private void OnDisable()
        {
            if (runtimeHost != null)
            {
                runtimeHost.Initialized -= HandleRuntimeInitialized;
            }

            if (definitionSelection != null)
            {
                definitionSelection.SelectedDefinitionChanged -=
                    HandleDefinitionChanged;
                definitionSelection.OrientationChanged -=
                    HandleOrientationChanged;
            }

            DetachFromState();
            IsActive = false;
            ClearPlacementPreview();
        }
    }
}
