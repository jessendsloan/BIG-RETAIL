using System;
using System.Collections.Generic;
using BigRetail.Departments.Unity;
using BigRetail.Departments.Unity.UI;
using BigRetail.Construction.Unity.Tools;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the Departments rail entry to the shared document and player
    /// selection. Department painting remains a later, separate concern.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [RequireComponent(typeof(ConstructionUiInputGate))]
    [DefaultExecutionOrder(370)]
    public sealed class DepartmentPickerPresenter : MonoBehaviour
    {
        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private DepartmentDefinitionSelectionHost selectionHost;

        private DepartmentPickerView boundView;
        private ConstructionUiInputGate uiInputGate;
        private bool referencesAreValid;
        private bool catalogIsBound;
        private bool isPickerRequested;


        private void Reset()
        {
            documentHost = GetComponent<ConstructionToolbarDocumentHost>();
        }


        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost = GetComponent<ConstructionToolbarDocumentHost>();
            }

            uiInputGate = GetComponent<ConstructionUiInputGate>();

            referencesAreValid = ValidateReferences();
        }


        private void OnEnable()
        {
            if (!referencesAreValid)
            {
                return;
            }

            documentHost.DepartmentPickerViewReady += HandleViewReady;
            uiInputGate.CancelRequested += HandleCancelRequested;
            toolCoordinator.ModeChanged += HandleModeChanged;
            selectionHost.SelectedDefinitionChanged +=
                HandleSelectedDefinitionChanged;

            if (documentHost.HasDepartmentPickerView)
            {
                BindView(documentHost.DepartmentPickerView);
            }
        }


        private void Start()
        {
            RefreshView();
        }


        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.DepartmentPickerViewReady -= HandleViewReady;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -= HandleModeChanged;
            }

            if (uiInputGate != null)
            {
                uiInputGate.CancelRequested -= HandleCancelRequested;
            }

            if (selectionHost != null)
            {
                selectionHost.SelectedDefinitionChanged -=
                    HandleSelectedDefinitionChanged;
            }

            UnbindView();
        }


        private void HandleViewReady(DepartmentPickerView view)
        {
            BindView(view);
        }


        private void HandleDepartmentsRequested()
        {
            toolCoordinator.SetMode(ConstructionToolMode.None);
            isPickerRequested = true;
            RefreshView();
        }


        private void HandleDefinitionRequested(
            DepartmentDefinitionAsset definition)
        {
            try
            {
                selectionHost.SelectDefinition(definition);
                isPickerRequested = true;
                RefreshView();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }


        private void HandleModeChanged(ConstructionToolMode mode)
        {
            if (mode != ConstructionToolMode.None)
            {
                isPickerRequested = false;
            }

            RefreshView();
        }


        private void HandleCancelRequested()
        {
            if (!isPickerRequested)
            {
                return;
            }

            isPickerRequested = false;
            RefreshView();
        }


        private void HandleSelectedDefinitionChanged(
            DepartmentDefinitionAsset definition)
        {
            boundView?.SetSelectedDefinition(definition);
        }


        private void BindView(DepartmentPickerView view)
        {
            UnbindView();
            boundView = view;
            if (boundView == null)
            {
                return;
            }

            boundView.DepartmentsRequested += HandleDepartmentsRequested;
            boundView.DefinitionRequested += HandleDefinitionRequested;
            catalogIsBound = false;
            RefreshView();
        }


        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.DepartmentsRequested -= HandleDepartmentsRequested;
                boundView.DefinitionRequested -= HandleDefinitionRequested;
            }

            boundView = null;
            catalogIsBound = false;
        }


        private void RefreshView()
        {
            if (boundView == null)
            {
                return;
            }

            EnsureCatalogIsBound();
            boundView.SetVisible(isPickerRequested);
            boundView.SetSelectedDefinition(selectionHost.SelectedDefinition);
        }


        private void EnsureCatalogIsBound()
        {
            if (catalogIsBound || boundView == null)
            {
                return;
            }

            List<DepartmentPickerItem> items =
                new List<DepartmentPickerItem>();

            foreach (DepartmentDefinitionAsset definition
                     in selectionHost.EnumerateAvailableDefinitions())
            {
                items.Add(new DepartmentPickerItem(
                    definition,
                    definition.DisplayName,
                    definition.CatalogIcon));
            }

            boundView.SetItems(items);
            catalogIsBound = true;
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "DepartmentPickerPresenter has no document host assigned.",
                    this);
                isValid = false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "DepartmentPickerPresenter has no construction "
                    + "tool coordinator assigned.", this);
                isValid = false;
            }

            if (uiInputGate == null)
            {
                Debug.LogError(
                    "DepartmentPickerPresenter has no construction "
                    + "UI input gate assigned.", this);
                isValid = false;
            }

            if (selectionHost == null)
            {
                Debug.LogError(
                    "DepartmentPickerPresenter has no department selection "
                    + "host assigned.", this);
                isValid = false;
            }

            return isValid;
        }
    }
}
