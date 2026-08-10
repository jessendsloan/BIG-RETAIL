using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Fixtures;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the fixture picker to the authoritative selection host.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(360)]
    public sealed class FixtureDefinitionPickerPresenter : MonoBehaviour
    {
        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private FixtureDefinitionSelectionHost definitionSelectionHost;


        private FixtureDefinitionPickerView boundView;
        private bool referencesAreValid;
        private bool catalogIsBound;


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

            referencesAreValid = ValidateReferences();
        }


        private void OnEnable()
        {
            if (!referencesAreValid)
            {
                return;
            }

            documentHost.FixtureDefinitionPickerViewReady += HandleViewReady;
            toolCoordinator.ModeChanged += HandleModeChanged;
            definitionSelectionHost.SelectedDefinitionChanged +=
                HandleSelectedDefinitionChanged;
            definitionSelectionHost.OrientationChanged +=
                HandleOrientationChanged;

            if (documentHost.HasFixtureDefinitionPickerView)
            {
                BindView(documentHost.FixtureDefinitionPickerView);
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
                documentHost.FixtureDefinitionPickerViewReady -= HandleViewReady;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -= HandleModeChanged;
            }

            if (definitionSelectionHost != null)
            {
                definitionSelectionHost.SelectedDefinitionChanged -=
                    HandleSelectedDefinitionChanged;
                definitionSelectionHost.OrientationChanged -=
                    HandleOrientationChanged;
            }

            UnbindView();
        }


        private void HandleViewReady(FixtureDefinitionPickerView view)
        {
            BindView(view);
        }


        private void HandleDefinitionRequested(string definitionId)
        {
            try
            {
                if (!definitionSelectionHost.SelectDefinition(
                    new FixtureDefinitionId(definitionId)))
                {
                    Debug.LogWarning(
                        $"Fixture definition '{definitionId}' could not be selected.",
                        this);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }


        private void HandleRotateRequested()
        {
            definitionSelectionHost.RotateClockwise();
        }


        private void HandleModeChanged(ConstructionToolMode mode)
        {
            RefreshVisibility(mode);
        }


        private void HandleSelectedDefinitionChanged(FixtureDefinitionId definitionId)
        {
            if (boundView == null)
            {
                return;
            }

            EnsureCatalogIsBound();
            boundView.SetSelectedDefinition(definitionId.Value);
        }


        private void HandleOrientationChanged(FixtureOrientation orientation)
        {
            boundView?.SetOrientationTooltip(orientation.ToString());
        }


        private void BindView(FixtureDefinitionPickerView view)
        {
            UnbindView();
            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.DefinitionRequested += HandleDefinitionRequested;
            boundView.RotateRequested += HandleRotateRequested;
            catalogIsBound = false;
            RefreshView();
        }


        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.DefinitionRequested -= HandleDefinitionRequested;
                boundView.RotateRequested -= HandleRotateRequested;
            }

            boundView = null;
            catalogIsBound = false;
        }


        private void RefreshView()
        {
            if (boundView == null
                || toolCoordinator == null
                || definitionSelectionHost == null)
            {
                return;
            }

            RefreshVisibility(toolCoordinator.CurrentMode);

            if (!definitionSelectionHost.IsInitialized)
            {
                return;
            }

            EnsureCatalogIsBound();
            boundView.SetSelectedDefinition(
                definitionSelectionHost.SelectedDefinitionId.Value);
            boundView.SetOrientationTooltip(
                definitionSelectionHost.Orientation.ToString());
        }


        private void RefreshVisibility(ConstructionToolMode mode)
        {
            boundView?.SetVisible(mode == ConstructionToolMode.BuildFixtures);
        }


        private void EnsureCatalogIsBound()
        {
            if (catalogIsBound
                || boundView == null
                || !definitionSelectionHost.IsInitialized)
            {
                return;
            }

            List<FixtureDefinitionPickerItem> items =
                new List<FixtureDefinitionPickerItem>();

            foreach (
                FixtureDefinitionAsset definitionAsset
                in definitionSelectionHost.EnumerateAvailableDefinitions())
            {
                if (definitionAsset == null)
                {
                    continue;
                }

                items.Add(
                    new FixtureDefinitionPickerItem(
                        definitionAsset.Id.Value,
                        definitionAsset.DisplayName,
                        definitionAsset.CatalogIcon));
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
                    "FixtureDefinitionPickerPresenter has no ConstructionToolbarDocumentHost assigned.",
                    this);
                isValid = false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "FixtureDefinitionPickerPresenter has no ConstructionToolCoordinator assigned.",
                    this);
                isValid = false;
            }

            if (definitionSelectionHost == null)
            {
                Debug.LogError(
                    "FixtureDefinitionPickerPresenter has no FixtureDefinitionSelectionHost assigned.",
                    this);
                isValid = false;
            }

            return isValid;
        }
    }
}
