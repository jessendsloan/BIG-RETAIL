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
    /// Removes the complete fixture occupying the targeted cell with one
    /// click and records that removal in shared construction history.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class FixtureDemolitionToolController : MonoBehaviour
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

        [Header("Fixture Demolition Tool")]

        [SerializeField]
        private GridCellTargetResolver targetResolver;

        [SerializeField]
        private FixtureDemolitionPreviewView previewView;

        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private ConstructionHistoryHost historyHost;

        [Header("Starting State")]

        [SerializeField]
        private bool startActive;

        [Header("Diagnostics")]

        [SerializeField]
        private bool logDemolitionResults = true;


        public bool IsActive { get; private set; }

        public bool HasDemolitionPreview { get; private set; }


        public event Action<bool> ToolActiveChanged;


        private InputAction confirmAction;
        private InputAction cancelAction;
        private FixtureState subscribedState;
        private GridPosition currentCell;
        private FixtureInstance currentFixture;
        private bool hasCurrentCell;
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

            RefreshDemolitionPreview();

            if (confirmAction.WasPressedThisFrame())
            {
                TryCommitCurrentDemolition();
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


        public void CancelCurrentGesture()
        {
            ClearDemolitionPreview();
        }


        public void ClearDemolitionPreview()
        {
            HasDemolitionPreview = false;
            hasCurrentCell = false;
            currentFixture = null;
            previewDirty = true;
            previewView.Hide();
        }


        private void RefreshDemolitionPreview(
            bool forceRefresh = false)
        {
            if (!runtimeHost.TryInitialize()
                || runtimeHost.FixtureState == null
                || !targetResolver.HasTarget)
            {
                ClearDemolitionPreview();
                return;
            }

            GridPosition targetedCell = targetResolver.CurrentCell;

            if (!forceRefresh
                && !previewDirty
                && hasCurrentCell
                && targetedCell == currentCell)
            {
                return;
            }

            currentCell = targetedCell;
            hasCurrentCell = true;
            previewDirty = false;

            if (!runtimeHost.FixtureState.TryGetFixtureAtCell(
                    currentCell,
                    out currentFixture))
            {
                HasDemolitionPreview = false;
                previewView.Hide();
                return;
            }

            HasDemolitionPreview = true;
            previewView.ShowFixture(currentFixture);
        }


        private bool TryCommitCurrentDemolition()
        {
            if (!HasDemolitionPreview || currentFixture == null)
            {
                LogWarning(
                    "Fixture demolition rejected because no fixture is targeted.");
                return false;
            }

            if (!runtimeHost.TryInitialize()
                || runtimeHost.FixturePlacement == null
                || !historyHost.TryInitialize())
            {
                Debug.LogError(
                    "FixtureDemolitionToolController could not access initialized runtime services.",
                    this);
                return false;
            }

            FixturePlacementResult result =
                runtimeHost.FixturePlacement
                    .TryRemoveFixtureAtCell(currentCell);

            if (!result.Succeeded)
            {
                LogWarning(
                    $"Fixture demolition rejected: {result.Failure}.");
                previewDirty = true;
                RefreshDemolitionPreview(forceRefresh: true);
                return false;
            }

            historyHost.History.Record(
                new ReversibleFixtureEditAction(
                    runtimeHost.FixturePlacement,
                    result.Edit));

            if (logDemolitionResults)
            {
                Debug.Log(
                    $"Removed '{result.DefinitionId}' across {result.OccupiedCellCount} cell(s).",
                    this);
            }

            ClearDemolitionPreview();
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
                ClearDemolitionPreview();
            }

            ToolActiveChanged?.Invoke(isActive);
        }


        private void HandleRuntimeInitialized(
            FixtureRuntimeHost initializedHost)
        {
            AttachToState();
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

            confirmAction = actionMap.FindAction(
                confirmActionName,
                throwIfNotFound: false);

            cancelAction = actionMap.FindAction(
                cancelActionName,
                throwIfNotFound: false);

            if (confirmAction != null && cancelAction != null)
            {
                return true;
            }

            Debug.LogError(
                "FixtureDemolitionToolController could not resolve its Confirm and Cancel actions.",
                this);
            return false;
        }


        private bool ValidateReferences()
        {
            bool isValid = true;
            isValid &= RequireReference(playerInput, "PlayerInput");
            isValid &= RequireReference(
                targetResolver,
                "GridCellTargetResolver");
            isValid &= RequireReference(
                previewView,
                "FixtureDemolitionPreviewView");
            isValid &= RequireReference(runtimeHost, "FixtureRuntimeHost");
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
                $"FixtureDemolitionToolController has no {label} assigned.",
                this);
            return false;
        }


        private void LogWarning(string message)
        {
            if (logDemolitionResults)
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

            DetachFromState();
            IsActive = false;
            ClearDemolitionPreview();
        }
    }
}
