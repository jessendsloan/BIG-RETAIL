using BigRetail.Construction.Unity.Tools;
using BigRetail.Purchasing.Unity.UI;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Opens the Equipment Catalog from the gameplay toolbar or fixture drawer
    /// while temporarily yielding the construction map pointer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [RequireComponent(typeof(FixtureDefinitionPickerPresenter))]
    public sealed class EquipmentCatalogGameplayOverlayController : MonoBehaviour
    {
        private ConstructionToolbarDocumentHost toolbarDocumentHost;

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private FixtureDefinitionPickerPresenter fixturePickerPresenter;

        [SerializeField]
        private GameObject equipmentWorkspace;

        [SerializeField]
        private EquipmentCatalogWorkspacePresenter equipmentPresenter;

        private ConstructionToolMode previousToolMode;
        private ConstructionToolbarView boundToolbarView;
        private bool isOpen;


        public bool IsOpen => isOpen;


        private void Reset()
        {
            toolbarDocumentHost =
                GetComponent<ConstructionToolbarDocumentHost>();
            fixturePickerPresenter =
                GetComponent<FixtureDefinitionPickerPresenter>();
        }

        private void OnEnable()
        {
            if (toolbarDocumentHost == null)
            {
                toolbarDocumentHost =
                    GetComponent<ConstructionToolbarDocumentHost>();
            }

            if (fixturePickerPresenter == null)
            {
                fixturePickerPresenter =
                    GetComponent<FixtureDefinitionPickerPresenter>();
            }

            if (fixturePickerPresenter != null)
            {
                fixturePickerPresenter.EquipmentCatalogRequested +=
                    OpenForPlanRequirements;
            }

            if (toolbarDocumentHost != null)
            {
                toolbarDocumentHost.ViewReady += HandleToolbarViewReady;

                if (toolbarDocumentHost.HasView)
                {
                    BindToolbarView(toolbarDocumentHost.View);
                }
            }

            if (equipmentPresenter != null)
            {
                equipmentPresenter.CloseRequested += Close;
            }
        }

        private void OnDisable()
        {
            if (fixturePickerPresenter != null)
            {
                fixturePickerPresenter.EquipmentCatalogRequested -=
                    OpenForPlanRequirements;
            }

            if (toolbarDocumentHost != null)
            {
                toolbarDocumentHost.ViewReady -= HandleToolbarViewReady;
            }

            if (equipmentPresenter != null)
            {
                equipmentPresenter.CloseRequested -= Close;
                equipmentPresenter.SetWorkspaceVisible(false);
            }

            isOpen = false;
            UnbindToolbarView();
        }


        public void Open()
        {
            OpenInternal(showPlanRequirements: false);
        }

        private void OpenForPlanRequirements()
        {
            OpenInternal(showPlanRequirements: true);
        }

        private void OpenInternal(bool showPlanRequirements)
        {
            if (isOpen
                || equipmentWorkspace == null
                || equipmentPresenter == null)
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
            equipmentPresenter.SetWorkspaceVisible(
                true,
                showPlanRequirements);

            if (!equipmentWorkspace.activeSelf)
            {
                equipmentWorkspace.SetActive(true);
            }
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            equipmentPresenter?.SetWorkspaceVisible(false);

            if (toolCoordinator != null)
            {
                toolCoordinator.SetMode(previousToolMode);
            }
        }


        private void HandleToolbarViewReady(ConstructionToolbarView view)
        {
            BindToolbarView(view);
        }

        private void BindToolbarView(ConstructionToolbarView view)
        {
            if (boundToolbarView == view)
            {
                return;
            }

            UnbindToolbarView();
            boundToolbarView = view;

            if (boundToolbarView != null)
            {
                boundToolbarView.EquipmentCatalogRequested += Open;
            }
        }

        private void UnbindToolbarView()
        {
            if (boundToolbarView == null)
            {
                return;
            }

            boundToolbarView.EquipmentCatalogRequested -= Open;
            boundToolbarView = null;
        }
    }
}
