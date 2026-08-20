using System;
using BigRetail.Construction.Unity.Cells;
using BigRetail.Construction.Unity.Input;
using BigRetail.Map.Domain;
using BigRetail.Receiving.Domain;
using BigRetail.Receiving.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Receiving
{
    /// <summary>
    /// Paints and erases rectangular Receiving Area designations with the
    /// shared construction pointer. Beginning on an existing Receiving cell
    /// erases; beginning on other valid floor adds space.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class ReceivingAreaToolController : MonoBehaviour
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


        [Header("Receiving Area")]

        [SerializeField]
        private GridCellTargetResolver cellTargetResolver;

        [SerializeField]
        private ReceivingAreaRuntimeHost runtimeHost;

        [SerializeField]
        private ReceivingAreaViewSystem viewSystem;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logResults = true;


        public bool IsActive { get; private set; }

        public bool IsPlanningArea { get; private set; }

        public bool CurrentGestureRemovesCells { get; private set; }

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
            SetToolActive(false);
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


        [ContextMenu("Activate Receiving Area Tool")]
        public void ActivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The Receiving Area tool can only be activated during "
                    + "Play Mode.",
                    this);
                return;
            }

            SetToolActive(true);
        }

        [ContextMenu("Deactivate Receiving Area Tool")]
        public void DeactivateTool()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "The Receiving Area tool can only be deactivated during "
                    + "Play Mode.",
                    this);
                return;
            }

            SetToolActive(false);
        }

        public void CancelCurrentGesture()
        {
            if (IsPlanningArea)
            {
                FinishArea();
            }

            hasCurrentIdleCell = false;
            viewSystem.ClearPreview();
        }


        private void BeginArea()
        {
            if (!cellTargetResolver.HasTarget)
            {
                LogWarning(
                    "Receiving Area planning could not begin because no "
                    + "grid cell is targeted.");
                return;
            }

            if (!runtimeHost.TryInitialize()
                || runtimeHost.State == null
                || runtimeHost.Designations == null)
            {
                Debug.LogError(
                    "ReceivingAreaToolController could not access the "
                    + "Receiving Area runtime.",
                    this);
                return;
            }

            StartCell = cellTargetResolver.CurrentCell;
            CurrentGestureRemovesCells =
                runtimeHost.State.Contains(StartCell);
            areaStartedWithGamepad = pointerController.IsUsingGamepad;
            IsPlanningArea = true;
            hasCurrentEndCell = false;
            RefreshAreaPlan(forceRefresh: true);
            AreaPlanningChanged?.Invoke(true);
        }

        private void RefreshIdlePreview()
        {
            if (!cellTargetResolver.HasTarget)
            {
                hasCurrentIdleCell = false;
                viewSystem.ClearPreview();
                return;
            }

            GridPosition targetedCell = cellTargetResolver.CurrentCell;

            if (hasCurrentIdleCell && targetedCell == currentIdleCell)
            {
                return;
            }

            currentIdleCell = targetedCell;
            hasCurrentIdleCell = true;
            bool removesCell = runtimeHost.State != null
                && runtimeHost.State.Contains(currentIdleCell);
            viewSystem.ShowPreview(
                new[] { currentIdleCell },
                removesCell);
        }

        private void RefreshAreaPlan(
            bool forceRefresh = false)
        {
            if (!IsPlanningArea || !cellTargetResolver.HasTarget)
            {
                return;
            }

            GridPosition endCell = cellTargetResolver.CurrentCell;

            if (!forceRefresh
                && hasCurrentEndCell
                && endCell == currentEndCell)
            {
                return;
            }

            currentEndCell = endCell;
            hasCurrentEndCell = true;
            CurrentAreaPlan = RectangularCellAreaPlanner.Plan(
                StartCell,
                currentEndCell);

            if (CurrentAreaPlan.Succeeded)
            {
                viewSystem.ShowPreview(
                    CurrentAreaPlan.Cells,
                    CurrentGestureRemovesCells);
            }
            else
            {
                viewSystem.ClearPreview();
            }
        }

        private bool TryCommitCurrentArea()
        {
            if (!CurrentAreaPlan.Succeeded)
            {
                LogWarning(
                    "The current Receiving Area gesture has no valid "
                    + "geometry.");
                return false;
            }

            ReceivingAreaChangeResult result =
                CurrentGestureRemovesCells
                    ? runtimeHost.Designations.TryRemoveArea(
                        CurrentAreaPlan.Cells)
                    : runtimeHost.Designations.TryAddArea(
                        CurrentAreaPlan.Cells);

            if (!result.Succeeded)
            {
                LogWarning(
                    $"Receiving Area could not be changed. Reason: "
                    + $"{result.Failure}. Cell: {result.FailedCell}.");

                if (!areaStartedWithGamepad)
                {
                    FinishArea();
                }

                return false;
            }

            if (logResults)
            {
                Debug.Log(
                    CurrentGestureRemovesCells
                        ? $"Removed {result.ChangedCellCount} Receiving "
                            + "cell(s)."
                        : $"Added {result.ChangedCellCount} Receiving "
                            + "cell(s).",
                    this);
            }

            FinishArea();
            return result.ChangedCellCount > 0;
        }

        private void HandleCancel()
        {
            if (IsPlanningArea)
            {
                FinishArea();
                return;
            }

            SetToolActive(false);
        }

        private void FinishArea()
        {
            IsPlanningArea = false;
            CurrentAreaPlan = default;
            hasCurrentEndCell = false;
            hasCurrentIdleCell = false;
            viewSystem.ClearPreview();
            AreaPlanningChanged?.Invoke(false);
        }

        private void SetToolActive(
            bool isActive)
        {
            if (IsActive == isActive)
            {
                if (IsActive && !IsPlanningArea)
                {
                    hasCurrentIdleCell = false;
                    RefreshIdlePreview();
                }

                return;
            }

            if (!isActive && IsPlanningArea)
            {
                FinishArea();
            }

            IsActive = isActive;
            hasCurrentIdleCell = false;
            viewSystem.SetManagementVisible(IsActive);

            if (IsActive)
            {
                RefreshIdlePreview();
            }
            else
            {
                viewSystem.ClearPreview();
            }

            ToolActiveChanged?.Invoke(IsActive);
        }

        private void HandlePointerModeChanged(bool isUsingGamepad)
        {
            if (IsPlanningArea)
            {
                FinishArea();
            }

            hasCurrentIdleCell = false;
        }

        private bool TryResolveActions()
        {
            if (playerInput.actions == null)
            {
                Debug.LogError(
                    "ReceivingAreaToolController could not find an Input "
                    + "Actions asset on PlayerInput.",
                    this);
                return false;
            }

            InputActionMap actionMap = playerInput.actions.FindActionMap(
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

            confirmAction = actionMap.FindAction(
                confirmActionName,
                throwIfNotFound: false);
            cancelAction = actionMap.FindAction(
                cancelActionName,
                throwIfNotFound: false);

            if (confirmAction == null || cancelAction == null)
            {
                Debug.LogError(
                    "ReceivingAreaToolController requires Confirm and "
                    + "Cancel actions in the Construction action map.",
                    this);
                return false;
            }

            return true;
        }

        private bool ValidateReferences()
        {
            bool valid = playerInput != null
                && pointerController != null
                && cellTargetResolver != null
                && runtimeHost != null
                && viewSystem != null;

            if (!valid)
            {
                Debug.LogError(
                    "ReceivingAreaToolController requires PlayerInput, the "
                    + "shared construction pointer/cell target, and its "
                    + "Receiving runtime/view.",
                    this);
            }

            return valid;
        }

        private void LogWarning(string message)
        {
            if (logResults)
            {
                Debug.LogWarning(message, this);
            }
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

            if (viewSystem != null)
            {
                viewSystem.SetManagementVisible(false);
            }
        }
    }
}
