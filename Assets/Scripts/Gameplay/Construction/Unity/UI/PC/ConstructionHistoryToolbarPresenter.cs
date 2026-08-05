using BigRetail.Construction.Unity.History;
using BigRetail.Map.Construction;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the construction rail's Undo and Redo buttons to the existing
    /// neutral history input controller. Button availability follows the
    /// authoritative construction history rather than local UI assumptions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(355)]
    public sealed class ConstructionHistoryToolbarPresenter : MonoBehaviour
    {
        [Header("Toolbar")]

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;


        [Header("History")]

        [SerializeField]
        private ConstructionHistoryHost historyHost;

        [SerializeField]
        private ConstructionHistoryInputController historyInputController;


        private ConstructionToolbarView boundView;
        private ConstructionHistory subscribedHistory;
        private bool referencesAreValid;


        private void Reset()
        {
            documentHost =
                GetComponent<ConstructionToolbarDocumentHost>();
        }


        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost =
                    GetComponent<ConstructionToolbarDocumentHost>();
            }

            referencesAreValid =
                ValidateReferences();
        }


        private void OnEnable()
        {
            if (!referencesAreValid)
            {
                return;
            }

            documentHost.ViewReady +=
                HandleViewReady;

            historyHost.Initialized +=
                HandleHistoryInitialized;

            SubscribeToHistory();

            if (documentHost.HasView)
            {
                BindView(
                    documentHost.View);
            }
        }


        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.ViewReady -=
                    HandleViewReady;
            }

            if (historyHost != null)
            {
                historyHost.Initialized -=
                    HandleHistoryInitialized;
            }

            UnsubscribeFromHistory();
            UnbindView();
        }


        private void HandleViewReady(
            ConstructionToolbarView view)
        {
            BindView(view);
        }


        private void HandleHistoryInitialized()
        {
            SubscribeToHistory();
            RefreshAvailability();
        }


        private void HandleHistoryChanged()
        {
            RefreshAvailability();
        }


        private void HandleUndoRequested()
        {
            historyInputController.TryUndo();
            RefreshAvailability();
        }


        private void HandleRedoRequested()
        {
            historyInputController.TryRedo();
            RefreshAvailability();
        }


        private void BindView(
            ConstructionToolbarView view)
        {
            UnbindView();

            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.UndoRequested +=
                HandleUndoRequested;
            boundView.RedoRequested +=
                HandleRedoRequested;

            RefreshAvailability();
        }


        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.UndoRequested -=
                HandleUndoRequested;
            boundView.RedoRequested -=
                HandleRedoRequested;

            boundView = null;
        }


        private void SubscribeToHistory()
        {
            ConstructionHistory history =
                historyHost.IsInitialized
                    ? historyHost.History
                    : null;

            if (ReferenceEquals(history, subscribedHistory))
            {
                return;
            }

            UnsubscribeFromHistory();

            subscribedHistory = history;

            if (subscribedHistory != null)
            {
                subscribedHistory.HistoryChanged +=
                    HandleHistoryChanged;
            }
        }


        private void UnsubscribeFromHistory()
        {
            if (subscribedHistory != null)
            {
                subscribedHistory.HistoryChanged -=
                    HandleHistoryChanged;
            }

            subscribedHistory = null;
        }


        private void RefreshAvailability()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetUndoEnabled(
                subscribedHistory != null
                && subscribedHistory.CanUndo);
            boundView.SetRedoEnabled(
                subscribedHistory != null
                && subscribedHistory.CanRedo);
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "ConstructionHistoryToolbarPresenter has no "
                    + "ConstructionToolbarDocumentHost assigned.",
                    this);

                isValid = false;
            }

            if (historyHost == null)
            {
                Debug.LogError(
                    "ConstructionHistoryToolbarPresenter has no "
                    + "ConstructionHistoryHost assigned.",
                    this);

                isValid = false;
            }

            if (historyInputController == null)
            {
                Debug.LogError(
                    "ConstructionHistoryToolbarPresenter has no "
                    + "ConstructionHistoryInputController assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
