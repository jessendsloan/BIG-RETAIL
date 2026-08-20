using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Doors;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the PC door-definition picker to the authoritative
    /// player-facing door selection host.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(360)]
    public sealed class DoorDefinitionPickerPresenter : MonoBehaviour
    {
        [Header("Toolbar")]

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;


        [Header("Construction Services")]

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private DoorDefinitionSelectionHost definitionSelectionHost;


        private DoorDefinitionPickerView boundView;
        private bool referencesAreValid;
        private bool catalogIsBound;


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

            documentHost.DoorDefinitionPickerViewReady +=
                HandleViewReady;

            toolCoordinator.ModeChanged +=
                HandleModeChanged;

            definitionSelectionHost.SelectedDefinitionChanged +=
                HandleSelectedDefinitionChanged;

            if (documentHost.HasDoorDefinitionPickerView)
            {
                BindView(
                    documentHost.DoorDefinitionPickerView);
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
                documentHost.DoorDefinitionPickerViewReady -=
                    HandleViewReady;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -=
                    HandleModeChanged;
            }

            if (definitionSelectionHost != null)
            {
                definitionSelectionHost.SelectedDefinitionChanged -=
                    HandleSelectedDefinitionChanged;
            }

            UnbindView();
        }


        private void HandleViewReady(
            DoorDefinitionPickerView view)
        {
            BindView(
                view);
        }


        private void HandleDefinitionRequested(
            string definitionId)
        {
            try
            {
                if (!definitionSelectionHost.SelectDefinition(
                        new DoorDefinitionId(
                        definitionId)))
                {
                    Debug.LogWarning(
                        $"Door definition '{definitionId}' could not be selected.",
                        this);
                    return;
                }

                toolCoordinator.SetMode(
                    ConstructionToolMode.BuildDoors);
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    this);
            }
        }


        private void HandleModeChanged(
            ConstructionToolMode mode)
        {
            RefreshView();
        }


        private void HandleSelectedDefinitionChanged(
            DoorDefinitionId definitionId)
        {
            if (boundView == null)
            {
                return;
            }

            EnsureCatalogIsBound();
            boundView.SetSelectedDefinition(
                toolCoordinator.CurrentMode
                    == ConstructionToolMode.BuildWindows
                    ? null
                    : definitionId.Value);
        }


        private void BindView(
            DoorDefinitionPickerView view)
        {
            UnbindView();

            boundView =
                view;

            if (boundView == null)
            {
                return;
            }

            boundView.DefinitionRequested +=
                HandleDefinitionRequested;

            catalogIsBound =
                false;

            RefreshView();
        }


        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.DefinitionRequested -=
                    HandleDefinitionRequested;
            }

            boundView =
                null;

            catalogIsBound =
                false;
        }


        private void RefreshView()
        {
            if (boundView == null
                || toolCoordinator == null
                || definitionSelectionHost == null)
            {
                return;
            }

            RefreshVisibility(
                toolCoordinator.CurrentMode);

            if (!definitionSelectionHost.IsInitialized)
            {
                return;
            }

            EnsureCatalogIsBound();

            boundView.SetSelectedDefinition(
                toolCoordinator.CurrentMode
                    == ConstructionToolMode.BuildWindows
                    ? null
                    : definitionSelectionHost.SelectedDefinitionId.Value);
        }


        private void RefreshVisibility(
            ConstructionToolMode mode)
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetVisible(
                mode == ConstructionToolMode.BuildDoors
                || mode == ConstructionToolMode.BuildWindows);
        }


        private void EnsureCatalogIsBound()
        {
            if (catalogIsBound
                || boundView == null
                || !definitionSelectionHost.IsInitialized)
            {
                return;
            }

            List<DoorDefinitionPickerItem> items =
                new List<DoorDefinitionPickerItem>();

            foreach (
                DoorDefinitionAsset definitionAsset
                in definitionSelectionHost.EnumerateAvailableDefinitions())
            {
                if (definitionAsset == null)
                {
                    continue;
                }

                if (!definitionAsset.HasPassageSegments)
                {
                    continue;
                }

                if (definitionAsset.CatalogIcon == null)
                {
                    Debug.LogWarning(
                        $"Door definition '{definitionAsset.name}' has no catalog icon assigned.",
                        definitionAsset);
                }

                items.Add(
                    new DoorDefinitionPickerItem(
                        definitionAsset.Id.Value,
                        definitionAsset.DisplayName,
                        definitionAsset.CatalogIcon));
            }

            boundView.SetItems(
                items);

            catalogIsBound =
                true;
        }


        private bool ValidateReferences()
        {
            bool isValid =
                true;

            if (documentHost == null)
            {
                Debug.LogError(
                    "DoorDefinitionPickerPresenter has no "
                    + "ConstructionToolbarDocumentHost assigned.",
                    this);

                isValid =
                    false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "DoorDefinitionPickerPresenter has no "
                    + "ConstructionToolCoordinator assigned.",
                    this);

                isValid =
                    false;
            }

            if (definitionSelectionHost == null)
            {
                Debug.LogError(
                    "DoorDefinitionPickerPresenter has no "
                    + "DoorDefinitionSelectionHost assigned.",
                    this);

                isValid =
                    false;
            }

            return isValid;
        }
    }
}
