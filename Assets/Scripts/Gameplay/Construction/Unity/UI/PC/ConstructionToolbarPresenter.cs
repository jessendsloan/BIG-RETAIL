using BigRetail.Construction.Unity.Tools;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects PC toolbar intent to authoritative construction services.
    /// This first vertical slice activates wall construction and mirrors
    /// tool-mode changes back into the toolbar selection state.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(350)]
    public sealed class ConstructionToolbarPresenter : MonoBehaviour
    {
        [Header("Toolbar")]

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;


        [Header("Construction Services")]

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;


        private ConstructionToolbarView boundView;
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

            toolCoordinator.ModeChanged +=
                HandleModeChanged;

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

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -=
                    HandleModeChanged;
            }

            UnbindView();
        }


        private void HandleViewReady(
            ConstructionToolbarView view)
        {
            BindView(
                view);
        }


        private void HandleSectionRequested(
            ConstructionToolbarSection section)
        {
            if (section
                != ConstructionToolbarSection.Walls)
            {
                return;
            }

            toolCoordinator.SetMode(
                ConstructionToolMode.BuildWalls);
        }


        private void HandleModeChanged(
            ConstructionToolMode mode)
        {
            RefreshSelection(
                mode);
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

            boundView.SectionRequested +=
                HandleSectionRequested;

            RefreshSelection(
                toolCoordinator.CurrentMode);
        }


        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SectionRequested -=
                HandleSectionRequested;

            boundView = null;
        }


        private void RefreshSelection(
            ConstructionToolMode mode)
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetSelectedSection(
                ConstructionToolbarModeMapper.ToSection(
                    mode));
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "ConstructionToolbarDocumentHost assigned.",
                    this);

                isValid = false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "ConstructionToolbarPresenter has no "
                    + "ConstructionToolCoordinator assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }
    }
}
