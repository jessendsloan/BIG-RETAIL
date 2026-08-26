using System;
using System.Collections.Generic;
using BigRetail.Construction.Unity.Fixtures;
using BigRetail.Construction.Unity.Tools;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Purchasing.Unity;
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

        [SerializeField]
        private FixtureEquipmentRuntimeHost equipmentRuntimeHost;


        private FixtureDefinitionPickerView boundView;
        private bool referencesAreValid;
        private bool catalogIsBound;
        private string equipmentStatus =
            "Plan freely, then order the equipment your layout needs.";


        public event Action EquipmentCatalogRequested;


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
            equipmentRuntimeHost.Initialized +=
                HandleEquipmentRuntimeInitialized;
            equipmentRuntimeHost.StateChanged +=
                HandleEquipmentStateChanged;
            equipmentRuntimeHost.PlanModeChanged +=
                HandlePlanModeChanged;

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

            if (equipmentRuntimeHost != null)
            {
                equipmentRuntimeHost.Initialized -=
                    HandleEquipmentRuntimeInitialized;
                equipmentRuntimeHost.StateChanged -=
                    HandleEquipmentStateChanged;
                equipmentRuntimeHost.PlanModeChanged -=
                    HandlePlanModeChanged;
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

        private void HandlePlanModeRequested()
        {
            equipmentRuntimeHost.SetPlanMode(
                !equipmentRuntimeHost.IsPlanMode);
            equipmentStatus = equipmentRuntimeHost.IsPlanMode
                ? "Planning is free. Click the foundation to lay out translucent fixtures."
                : "Install mode uses equipment already received into storage.";
            RefreshEquipmentView();
        }

        private void HandleEquipmentCatalogRequested()
        {
            EquipmentCatalogRequested?.Invoke();
        }

        private void HandleInstallPlansRequested()
        {
            FixtureEquipmentBatchInstallationResult result =
                equipmentRuntimeHost.Installation.TryInstallReadyPlans();
            equipmentStatus =
                $"Installed {result.InstalledCount} planned fixture(s). "
                + $"Waiting for equipment: {result.WaitingForEquipmentCount}; "
                + $"blocked: {result.BlockedCount}.";
            RefreshEquipmentView();
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
            RefreshEquipmentView();
        }


        private void HandleOrientationChanged(FixtureOrientation orientation)
        {
            boundView?.SetOrientationTooltip(orientation.ToString());
        }

        private void HandleEquipmentRuntimeInitialized(
            FixtureEquipmentRuntimeHost initializedHost)
        {
            catalogIsBound = false;
            RefreshView();
        }

        private void HandleEquipmentStateChanged()
        {
            RefreshEquipmentView();
        }

        private void HandlePlanModeChanged(bool isPlanMode)
        {
            RefreshEquipmentView();
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
            boundView.PlanModeRequested += HandlePlanModeRequested;
            boundView.EquipmentCatalogRequested +=
                HandleEquipmentCatalogRequested;
            boundView.InstallPlansRequested += HandleInstallPlansRequested;
            catalogIsBound = false;
            RefreshView();
        }


        private void UnbindView()
        {
            if (boundView != null)
            {
                boundView.DefinitionRequested -= HandleDefinitionRequested;
                boundView.RotateRequested -= HandleRotateRequested;
                boundView.PlanModeRequested -= HandlePlanModeRequested;
                boundView.EquipmentCatalogRequested -=
                    HandleEquipmentCatalogRequested;
                boundView.InstallPlansRequested -=
                    HandleInstallPlansRequested;
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
            RefreshEquipmentView();
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
                        BuildEquipmentTooltip(definitionAsset),
                        definitionAsset.CatalogIcon));
            }

            boundView.SetItems(items);
            catalogIsBound = true;
            RefreshOwnedBadges();
        }

        private void RefreshEquipmentView()
        {
            if (boundView == null
                || equipmentRuntimeHost == null
                || !equipmentRuntimeHost.IsInitialized
                || definitionSelectionHost == null
                || !definitionSelectionHost.IsInitialized)
            {
                return;
            }

            FixtureDefinitionId selectedId =
                definitionSelectionHost.SelectedDefinitionId;
            FixtureEquipmentDefinition definition =
                equipmentRuntimeHost.Catalog.GetRequired(selectedId);
            int owned =
                equipmentRuntimeHost.Inventory.GetQuantity(selectedId);
            int planned =
                equipmentRuntimeHost.Plans.CountFor(selectedId);
            int outstanding =
                equipmentRuntimeHost.Orders.GetOutstandingQuantity(selectedId);

            RefreshOwnedBadges();
            boundView.SetEquipmentSummary(
                definition.DisplayName,
                owned,
                definition.UnitPriceCents,
                planned,
                outstanding,
                HasInstallablePlan(),
                equipmentRuntimeHost.IsPlanMode,
                ResolveEquipmentStatus(outstanding));
        }

        private void RefreshOwnedBadges()
        {
            if (boundView == null
                || equipmentRuntimeHost == null
                || !equipmentRuntimeHost.IsInitialized)
            {
                return;
            }

            foreach (FixtureEquipmentDefinition definition
                     in equipmentRuntimeHost.Catalog.EnumerateDefinitions())
            {
                boundView.SetOwnedQuantity(
                    definition.FixtureDefinitionId.Value,
                    equipmentRuntimeHost.Inventory.GetQuantity(
                        definition.FixtureDefinitionId));
            }
        }

        private bool HasInstallablePlan()
        {
            foreach (FixtureEquipmentDefinition definition
                     in equipmentRuntimeHost.Catalog.EnumerateDefinitions())
            {
                FixtureDefinitionId id = definition.FixtureDefinitionId;

                if (equipmentRuntimeHost.Plans.CountFor(id) > 0
                    && equipmentRuntimeHost.Inventory.GetQuantity(id) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private string BuildEquipmentTooltip(
            FixtureDefinitionAsset definitionAsset)
        {
            if (equipmentRuntimeHost == null
                || !equipmentRuntimeHost.IsInitialized
                || !equipmentRuntimeHost.Catalog.TryGet(
                    definitionAsset.Id,
                    out FixtureEquipmentDefinition equipment))
            {
                return definitionAsset.DisplayName;
            }

            long leadMinutes = equipment.DeliveryLeadTimeSeconds / 60;
            return $"{definitionAsset.DisplayName} · "
                + $"{FormatMoney(equipment.UnitPriceCents)} · "
                + $"delivery in {leadMinutes} game minutes";
        }

        private string ResolveEquipmentStatus(int selectedOutstandingQuantity)
        {
            int stagedCount = equipmentRuntimeHost.StagedReadyCount;
            int readyCount = equipmentRuntimeHost.ReadyToReceiveCount;

            if (stagedCount > 0)
            {
                return stagedCount == 1
                    ? "1 BIG Wholesale equipment shipment is staged. Open RCV to receive it."
                    : $"{stagedCount} BIG Wholesale equipment shipments are staged. Open RCV to receive them.";
            }

            int waitingCount = Math.Max(0, readyCount - stagedCount);

            if (waitingCount > 0)
            {
                return waitingCount == 1
                    ? "1 BIG Wholesale equipment shipment is waiting for open Receiving space."
                    : $"{waitingCount} BIG Wholesale equipment shipments are waiting for open Receiving space.";
            }

            if (selectedOutstandingQuantity > 0)
            {
                return "Selected equipment is on order from BIG Wholesale and will arrive through Receiving.";
            }

            return equipmentStatus;
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100:N0}.{Math.Abs(cents % 100):00}";
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

            if (equipmentRuntimeHost == null)
            {
                Debug.LogError(
                    "FixtureDefinitionPickerPresenter has no FixtureEquipmentRuntimeHost assigned.",
                    this);
                isValid = false;
            }

            return isValid;
        }
    }
}
