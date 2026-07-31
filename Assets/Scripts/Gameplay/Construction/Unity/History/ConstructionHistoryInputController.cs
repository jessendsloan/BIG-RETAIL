using BigRetail.Construction.Unity.Floors;
using BigRetail.Construction.Unity.Foundations;
using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Construction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.History
{
    /// <summary>
    /// Converts global construction Undo and Redo input into neutral
    /// history requests.
    ///
    /// The selected construction tool is preserved. History requests
    /// are blocked only while any tool is planning a live gesture.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)]
    public sealed class ConstructionHistoryInputController :
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
        private ConstructionHistoryHost historyHost;


        [Header("Construction Tools")]

        [SerializeField]
        private FoundationConstructionToolController
            foundationConstructionTool;

        [SerializeField]
        private WallConstructionToolController
            wallConstructionTool;

        [SerializeField]
        private WallDemolitionToolController
            wallDemolitionTool;

        [SerializeField]
        private FloorConstructionToolController
            floorConstructionTool;

        [SerializeField]
        private FloorDemolitionToolController
            floorDemolitionTool;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logHistoryResults = true;


        private InputAction undoAction;
        private InputAction redoAction;

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


        private void LateUpdate()
        {
            if (!isInitialized
                || IsAnyConstructionGestureActive())
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


        [ContextMenu("Undo Construction Action")]
        public void UndoFromContextMenu()
        {
            if (RequirePlayMode())
            {
                TryUndo();
            }
        }


        [ContextMenu("Redo Construction Action")]
        public void RedoFromContextMenu()
        {
            if (RequirePlayMode())
            {
                TryRedo();
            }
        }


        public bool TryUndo()
        {
            if (IsAnyConstructionGestureActive())
            {
                LogWarning(
                    "Undo was ignored because a construction " +
                    "gesture is currently being planned.");

                return false;
            }

            if (!TryGetHistory(
                    out ConstructionHistory history))
            {
                return false;
            }

            bool succeeded =
                history.TryUndo(
                    out ConstructionHistoryResult result);

            LogResult(
                "Undo",
                result,
                history);

            return succeeded;
        }


        public bool TryRedo()
        {
            if (IsAnyConstructionGestureActive())
            {
                LogWarning(
                    "Redo was ignored because a construction " +
                    "gesture is currently being planned.");

                return false;
            }

            if (!TryGetHistory(
                    out ConstructionHistory history))
            {
                return false;
            }

            bool succeeded =
                history.TryRedo(
                    out ConstructionHistoryResult result);

            LogResult(
                "Redo",
                result,
                history);

            return succeeded;
        }


        private bool IsAnyConstructionGestureActive()
        {
            return
                foundationConstructionTool.IsPlanningArea
                || wallConstructionTool.IsPlanningRun
                || wallDemolitionTool.IsPlanningRun
                || floorConstructionTool.IsPlanningArea
                || floorDemolitionTool.IsPlanningArea;
        }


        private bool TryGetHistory(
            out ConstructionHistory history)
        {
            history = null;

            if (!historyHost.TryInitialize()
                || historyHost.History == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController could not " +
                    "access an initialized ConstructionHistory.",
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
                    "ConstructionHistoryInputController could not " +
                    "find an Input Actions asset on PlayerInput.",
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
                    $"ConstructionHistoryInputController requires " +
                    $"actions named '{undoActionName}' and " +
                    $"'{redoActionName}' inside the " +
                    $"'{constructionActionMapName}' Action Map.",
                    this);

                return false;
            }

            return true;
        }


        private void LogResult(
            string operationName,
            ConstructionHistoryResult result,
            ConstructionHistory history)
        {
            if (!logHistoryResults)
            {
                return;
            }

            if (result.Succeeded)
            {
                Debug.Log(
                    $"{operationName} succeeded: " +
                    $"{result.Action.Description}. " +
                    $"Undo entries: {history.UndoCount}. " +
                    $"Redo entries: {history.RedoCount}.",
                    this);

                return;
            }

            if (result.Failure
                == ConstructionHistoryFailure.NothingToUndo
                || result.Failure
                    == ConstructionHistoryFailure.NothingToRedo)
            {
                Debug.Log(
                    $"{operationName}: {result.Failure}.",
                    this);

                return;
            }

            Debug.LogWarning(
                $"{operationName} failed. " +
                $"History failure: {result.Failure}. " +
                $"{result.ActionFailureReason}",
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

            if (foundationConstructionTool == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "FoundationConstructionToolController assigned.",
                    this);

                isValid = false;
            }

            if (playerInput == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "PlayerInput assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "ConstructionHistoryHost assigned.",
                    this);

                isValid = false;
            }

            if (wallConstructionTool == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "WallConstructionToolController assigned.",
                    this);

                isValid = false;
            }

            if (wallDemolitionTool == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "WallDemolitionToolController assigned.",
                    this);

                isValid = false;
            }

            if (floorConstructionTool == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "FloorConstructionToolController assigned.",
                    this);

                isValid = false;
            }

            if (floorDemolitionTool == null)
            {
                Debug.LogError(
                    "ConstructionHistoryInputController has no " +
                    "FloorDemolitionToolController assigned.",
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
                "Construction history actions can only run during " +
                "Play Mode.",
                this);

            return false;
        }
    }
}
