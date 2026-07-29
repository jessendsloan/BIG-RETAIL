using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.Walls;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Connects the PC wall-finish picker to the authoritative player-facing
    /// wall-finish selection host.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(360)]
    public sealed class WallFinishPickerPresenter : MonoBehaviour
    {
        [Header("Toolbar")]

        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;


        [Header("Construction Services")]

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        [SerializeField]
        private WallFinishSelectionHost finishSelectionHost;


        private WallFinishPickerView boundView;
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

            documentHost.FinishPickerViewReady +=
                HandleViewReady;

            toolCoordinator.ModeChanged +=
                HandleModeChanged;

            finishSelectionHost.SelectedFinishChanged +=
                HandleSelectedFinishChanged;

            if (documentHost.HasFinishPickerView)
            {
                BindView(
                    documentHost.FinishPickerView);
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
                documentHost.FinishPickerViewReady -=
                    HandleViewReady;
            }

            if (toolCoordinator != null)
            {
                toolCoordinator.ModeChanged -=
                    HandleModeChanged;
            }

            if (finishSelectionHost != null)
            {
                finishSelectionHost.SelectedFinishChanged -=
                    HandleSelectedFinishChanged;
            }

            UnbindView();
        }


        private void HandleViewReady(
            WallFinishPickerView view)
        {
            BindView(
                view);
        }


        private void HandleFinishRequested(
            string finishId)
        {
            try
            {
                finishSelectionHost.SelectFinish(
                    new WallFinishId(
                        finishId));
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
            RefreshVisibility(
                mode);
        }


        private void HandleSelectedFinishChanged(
            WallFinishId finishId)
        {
            if (boundView == null)
            {
                return;
            }

            EnsureCatalogIsBound();
            boundView.SetSelectedFinish(
                finishId.Value);
        }


        private void BindView(
            WallFinishPickerView view)
        {
            UnbindView();

            boundView =
                view;

            if (boundView == null)
            {
                return;
            }

            boundView.FinishRequested +=
                HandleFinishRequested;

            catalogIsBound =
                false;

            RefreshView();
        }


        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.FinishRequested -=
                    HandleFinishRequested;
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
                || finishSelectionHost == null)
            {
                return;
            }

            RefreshVisibility(
                toolCoordinator.CurrentMode);

            if (!finishSelectionHost.IsInitialized)
            {
                return;
            }

            EnsureCatalogIsBound();

            boundView.SetSelectedFinish(
                finishSelectionHost.SelectedFinishId.Value);
        }


        private void RefreshVisibility(
            ConstructionToolMode mode)
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SetVisible(
                mode == ConstructionToolMode.BuildWalls);
        }


        private void EnsureCatalogIsBound()
        {
            if (catalogIsBound
                || boundView == null
                || !finishSelectionHost.IsInitialized)
            {
                return;
            }

            List<WallFinishPickerItem> items =
                new List<WallFinishPickerItem>();

            foreach (
                WallFinishAsset finishAsset
                in finishSelectionHost.EnumerateAvailableFinishes())
            {
                if (finishAsset == null)
                {
                    continue;
                }

                if (finishAsset.CatalogIcon == null)
                {
                    Debug.LogWarning(
                        $"Wall finish '{finishAsset.name}' has no catalog icon assigned.",
                        finishAsset);
                }

                items.Add(
                    new WallFinishPickerItem(
                        finishAsset.Id.Value,
                        finishAsset.name,
                        finishAsset.CatalogIcon));
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
                    "WallFinishPickerPresenter has no "
                    + "ConstructionToolbarDocumentHost assigned.",
                    this);

                isValid =
                    false;
            }

            if (toolCoordinator == null)
            {
                Debug.LogError(
                    "WallFinishPickerPresenter has no "
                    + "ConstructionToolCoordinator assigned.",
                    this);

                isValid =
                    false;
            }

            if (finishSelectionHost == null)
            {
                Debug.LogError(
                    "WallFinishPickerPresenter has no "
                    + "WallFinishSelectionHost assigned.",
                    this);

                isValid =
                    false;
            }

            return isValid;
        }
    }
}
