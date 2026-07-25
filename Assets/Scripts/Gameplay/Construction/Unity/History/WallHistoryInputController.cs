using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.History
{
    /// <summary>
    /// Converts player Undo and Redo input into wall-history requests.
    ///
    /// History input is ignored while a wall run is being planned.
    /// Undo and Redo preserve the currently selected construction tool.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)]
    public sealed class WallHistoryInputController :
        MonoBehaviour
    {
        [Header("Input")]

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private string constructionActionMapName =
            "Construction";

        [SerializeField]
        private string undoActionName =
            "Undo";

        [SerializeField]
        private string redoActionName =
            "Redo";


        [Header("History")]

        [SerializeField]
        private WallEditHistoryHost historyHost;


        [Header("Construction Tools")]

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private WallConstructionToolController
            wallConstructionTool;

        [SerializeField]
        private WallDemolitionToolController
            wallDemolitionTool;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logHistoryResults = true;


        private InputAction undoAction;
        private InputAction redoAction;

        private bool isInitialized;

        private ConstructionToolMode lastNonNoneMode =
            ConstructionToolMode.None;

        private ConstructionToolMode modeBeforeFrameDeactivation =
            ConstructionToolMode.None;

        private int deactivationFrame =
            -1;


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

            if (toolCoordinator.CurrentMode
                != ConstructionToolMode.None)
            {
                lastNonNoneMode =
                    toolCoordinator.CurrentMode;
            }

            isInitialized = true;
        }


        private void OnEnable()
        {
            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged +=
                    HandleToolModeChanged;
            }
        }


        private void LateUpdate()
        {
            if (!isInitialized)
            {
                return;
            }

            if (IsAnyRunBeingPlanned())
            {
                return;
            }

            if (undoAction.WasPressedThisFrame())
            {
                TryUndo();
                return;
            }

            if (redoAction.WasPressedThisFrame())
            {
                TryRedo();
            }
        }


        [ContextMenu("Undo Wall Edit")]
        public void UndoFromContextMenu()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            TryUndo();
        }


        [ContextMenu("Redo Wall Edit")]
        public void RedoFromContextMenu()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            TryRedo();
        }


        public bool TryUndo()
        {
            if (IsAnyRunBeingPlanned())
            {
                LogWarning(
                    "Undo was ignored because a wall run " +
                    "is currently being planned.");

                return false;
            }

            if (!TryGetHistory(
                    out WallEditHistory history))
            {
                return false;
            }

            ConstructionToolMode modeToPreserve =
                ResolveModeToPreserve();

            bool succeeded =
                history.TryUndo(
                    out WallHistoryResult result);

            RestoreToolMode(
                modeToPreserve);

            LogResult(
                "Undo",
                result,
                history);

            return succeeded;
        }


        public bool TryRedo()
        {
            if (IsAnyRunBeingPlanned())
            {
                LogWarning(
                    "Redo was ignored because a wall run " +
                    "is currently being planned.");

                return false;
            }

            if (!TryGetHistory(
                    out WallEditHistory history))
            {
                return false;
            }

            ConstructionToolMode modeToPreserve =
                ResolveModeToPreserve();

            bool succeeded =
                history.TryRedo(
                    out WallHistoryResult result);

            RestoreToolMode(
                modeToPreserve);

            LogResult(
                "Redo",
                result,
                history);

            return succeeded;
        }


        /// <summary>
        /// Returns the tool that was active when the history input
        /// began.
        ///
        /// If another input reaction deactivated the tool earlier in
        /// this same frame, the previous active mode is preserved.
        /// </summary>
        private ConstructionToolMode ResolveModeToPreserve()
        {
            if (toolCoordinator.CurrentMode
                != ConstructionToolMode.None)
            {
                return toolCoordinator.CurrentMode;
            }

            if (deactivationFrame == Time.frameCount)
            {
                return modeBeforeFrameDeactivation;
            }

            return ConstructionToolMode.None;
        }


        private void RestoreToolMode(
            ConstructionToolMode mode)
        {
            if (mode == ConstructionToolMode.None)
            {
                return;
            }

            if (toolCoordinator.CurrentMode == mode)
            {
                return;
            }

            toolCoordinator.SetMode(mode);

            if (logHistoryResults)
            {
                Debug.Log(
                    $"Restored construction tool mode to {mode} " +
                    $"after history operation.",
                    this);
            }
        }


        private void HandleToolModeChanged(
            ConstructionToolMode newMode)
        {
            if (newMode == ConstructionToolMode.None)
            {
                modeBeforeFrameDeactivation =
                    lastNonNoneMode;

                deactivationFrame =
                    Time.frameCount;

                return;
            }

            lastNonNoneMode =
                newMode;
        }


        private bool IsAnyRunBeingPlanned()
        {
            return
                wallConstructionTool.IsPlanningRun
                || wallDemolitionTool.IsPlanningRun;
        }


        private bool TryGetHistory(
            out WallEditHistory history)
        {
            history = null;

            if (!historyHost.TryInitialize()
                || historyHost.History == null)
            {
                Debug.LogError(
                    "WallHistoryInputController could not access " +
                    "an initialized WallEditHistory.",
                    this);

                return false;
            }

            history =
                historyHost.History;

            return true;
        }


        private bool TryResolveActions()
        {
            if (playerInput.actions == null)
            {
                Debug.LogError(
                    "WallHistoryInputController could not find an " +
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

            undoAction =
                actionMap.FindAction(
                    undoActionName,
                    throwIfNotFound: false);

            redoAction =
                actionMap.FindAction(
                    redoActionName,
                    throwIfNotFound: false);

            if (undoAction == null
                || redoAction == null)
            {
                Debug.LogError(
                    $"WallHistoryInputController requires actions " +
                    $"named '{undoActionName}' and " +
                    $"'{redoActionName}' inside the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void LogResult(
            string operationName,
            WallHistoryResult result,
            WallEditHistory history)
        {
            if (!logHistoryResults)
            {
                return;
            }

            if (result.Succeeded)
            {
                Debug.Log(
                    $"{operationName} succeeded. " +
                    $"Applied {result.AppliedEdit.Count} wall " +
                    $"change(s). Undo entries: " +
                    $"{history.UndoCount}. Redo entries: " +
                    $"{history.RedoCount}.",
                    this);

                return;
            }

            if (result.Failure
                == WallHistoryFailure.NothingToUndo
                || result.Failure
                == WallHistoryFailure.NothingToRedo)
            {
                Debug.Log(
                    $"{operationName}: {result.Failure}.",
                    this);

                return;
            }

            Debug.LogWarning(
                $"{operationName} failed. " +
                $"History failure: {result.Failure}. " +
                $"Apply failure: {result.ApplyFailure}. " +
                $"Failed edge: {result.FailedEdge}.",
                this);
        }


        private void LogWarning(
            string message)
        {
            if (logHistoryResults)
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
                    "WallHistoryInputController has no " +
                    "PlayerInput assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "WallHistoryInputController has no " +
                    "WallEditHistoryHost assigned.",
                    this);

                isValid = false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "WallHistoryInputController has no " +
                    "ConstructionToolCoordinator assigned.",
                    this);

                isValid = false;
            }

            if (wallConstructionTool == null)
            {
                Debug.LogError(
                    "WallHistoryInputController has no " +
                    "WallConstructionToolController assigned.",
                    this);

                isValid = false;
            }

            if (wallDemolitionTool == null)
            {
                Debug.LogError(
                    "WallHistoryInputController has no " +
                    "WallDemolitionToolController assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private bool RequirePlayMode()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning(
                "Wall history actions can only run during Play Mode.",
                this);

            return false;
        }


        private void OnDisable()
        {
            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -=
                    HandleToolModeChanged;
            }
        }
    }
}