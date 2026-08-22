using BigRetail.Construction.Unity.Tools;
using BigRetail.Purchasing.Unity.UI;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Opens Purchasing as a full-screen gameplay workspace while temporarily
    /// yielding the map pointer owned by the construction tools.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    public sealed class PurchasingGameplayOverlayController : MonoBehaviour
    {
        [SerializeField]
        private ConstructionToolbarDocumentHost toolbarDocumentHost;

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private GameObject purchasingWorkspace;

        [SerializeField]
        private PurchasingWorkspacePresenter purchasingPresenter;

        private ConstructionToolbarView boundToolbarView;
        private ConstructionToolMode previousToolMode;
        private bool isOpen;


        public bool IsOpen => isOpen;


        private void Reset()
        {
            toolbarDocumentHost =
                GetComponent<ConstructionToolbarDocumentHost>();
        }

        private void OnEnable()
        {
            if (toolbarDocumentHost == null)
            {
                toolbarDocumentHost =
                    GetComponent<ConstructionToolbarDocumentHost>();
            }

            if (toolbarDocumentHost != null)
            {
                toolbarDocumentHost.ViewReady += HandleViewReady;

                if (toolbarDocumentHost.HasView)
                {
                    BindToolbarView(toolbarDocumentHost.View);
                }
            }

            if (purchasingPresenter != null)
            {
                purchasingPresenter.CloseRequested += Close;
            }
        }

        private void OnDisable()
        {
            if (toolbarDocumentHost != null)
            {
                toolbarDocumentHost.ViewReady -= HandleViewReady;
            }

            if (purchasingPresenter != null)
            {
                purchasingPresenter.CloseRequested -= Close;
                purchasingPresenter.SetWorkspaceVisible(false);
            }

            isOpen = false;
            UnbindToolbarView();
        }


        public void Open()
        {
            if (IsOpen || purchasingWorkspace == null)
            {
                return;
            }

            if (toolCoordinator != null)
            {
                previousToolMode = toolCoordinator.CurrentMode;
                toolCoordinator.CancelActiveGesture();
                toolCoordinator.SetMode(ConstructionToolMode.None);
            }

            isOpen = true;
            purchasingPresenter?.SetWorkspaceVisible(true);

            if (!purchasingWorkspace.activeSelf)
            {
                purchasingWorkspace.SetActive(true);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            isOpen = false;
            purchasingPresenter?.SetWorkspaceVisible(false);

            if (toolCoordinator != null)
            {
                toolCoordinator.SetMode(previousToolMode);
            }
        }


        private void HandleViewReady(
            ConstructionToolbarView view)
        {
            BindToolbarView(view);
        }

        private void BindToolbarView(
            ConstructionToolbarView view)
        {
            if (boundToolbarView == view)
            {
                return;
            }

            UnbindToolbarView();
            boundToolbarView = view;

            if (boundToolbarView != null)
            {
                boundToolbarView.PurchasingRequested += Open;
            }
        }

        private void UnbindToolbarView()
        {
            if (boundToolbarView == null)
            {
                return;
            }

            boundToolbarView.PurchasingRequested -= Open;
            boundToolbarView = null;
        }
    }
}
