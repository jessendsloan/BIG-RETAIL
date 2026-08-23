using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Simulation.Time.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Builds a scalable Equipment Catalog from authoritative equipment,
    /// planning, ownership, order, cash, and simulation-time state.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquipmentCatalogWorkspaceDocumentHost))]
    public sealed class EquipmentCatalogWorkspacePresenter : MonoBehaviour
    {
        [SerializeField]
        private EquipmentCatalogWorkspaceDocumentHost documentHost;

        [SerializeField]
        private FixtureEquipmentRuntimeHost equipmentRuntimeHost;

        private readonly Dictionary<FixtureDefinitionId, int> draft =
            new Dictionary<FixtureDefinitionId, int>();

        private EquipmentCatalogWorkspaceView boundView;
        private string searchText = string.Empty;
        private string selectedCategory = string.Empty;
        private string statusMessage = string.Empty;
        private bool requiredOnly;
        private bool isWorkspaceVisible;
        private bool suppressStateRefresh;


        public event Action CloseRequested;


        private void Reset()
        {
            documentHost =
                GetComponent<EquipmentCatalogWorkspaceDocumentHost>();
        }

        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost =
                    GetComponent<EquipmentCatalogWorkspaceDocumentHost>();
            }
        }

        private void OnEnable()
        {
            if (documentHost == null || equipmentRuntimeHost == null)
            {
                Debug.LogError(
                    "EquipmentCatalogWorkspacePresenter requires its document and equipment runtime hosts.",
                    this);
                enabled = false;
                return;
            }

            documentHost.ViewReady += HandleViewReady;
            equipmentRuntimeHost.Initialized +=
                HandleEquipmentRuntimeInitialized;
            equipmentRuntimeHost.StateChanged +=
                HandleEquipmentStateChanged;

            if (documentHost.HasView)
            {
                BindView(documentHost.View);
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
                documentHost.ViewReady -= HandleViewReady;
            }

            if (equipmentRuntimeHost != null)
            {
                equipmentRuntimeHost.Initialized -=
                    HandleEquipmentRuntimeInitialized;
                equipmentRuntimeHost.StateChanged -=
                    HandleEquipmentStateChanged;
            }

            UnbindView();
        }


        public void SetWorkspaceVisible(
            bool isVisible,
            bool showPlanRequirements = false)
        {
            isWorkspaceVisible = isVisible;

            if (isVisible
                && showPlanRequirements
                && HasRequiredEquipment())
            {
                requiredOnly = true;
            }
            else if (isVisible && !showPlanRequirements)
            {
                requiredOnly = false;
            }

            boundView?.SetVisible(isVisible);

            if (isVisible)
            {
                statusMessage = string.Empty;
                RefreshView();
            }
        }


        private void HandleViewReady(EquipmentCatalogWorkspaceView view)
        {
            BindView(view);
        }

        private void BindView(EquipmentCatalogWorkspaceView view)
        {
            UnbindView();
            boundView = view;

            if (boundView == null)
            {
                return;
            }

            boundView.SearchChanged += HandleSearchChanged;
            boundView.CategoryRequested += HandleCategoryRequested;
            boundView.RequiredFilterRequested +=
                HandleRequiredFilterRequested;
            boundView.QuantityDeltaRequested +=
                HandleQuantityDeltaRequested;
            boundView.AddRequiredItemRequested +=
                HandleAddRequiredItemRequested;
            boundView.AddRequirementsRequested +=
                HandleAddRequirementsRequested;
            boundView.ClearDraftRequested += HandleClearDraftRequested;
            boundView.PlaceOrderRequested += HandlePlaceOrderRequested;
            boundView.CloseRequested += HandleCloseRequested;
            boundView.SetVisible(isWorkspaceVisible);
            RefreshView();
        }

        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SearchChanged -= HandleSearchChanged;
            boundView.CategoryRequested -= HandleCategoryRequested;
            boundView.RequiredFilterRequested -=
                HandleRequiredFilterRequested;
            boundView.QuantityDeltaRequested -=
                HandleQuantityDeltaRequested;
            boundView.AddRequiredItemRequested -=
                HandleAddRequiredItemRequested;
            boundView.AddRequirementsRequested -=
                HandleAddRequirementsRequested;
            boundView.ClearDraftRequested -= HandleClearDraftRequested;
            boundView.PlaceOrderRequested -= HandlePlaceOrderRequested;
            boundView.CloseRequested -= HandleCloseRequested;
            boundView = null;
        }

        private void HandleSearchChanged(string value)
        {
            searchText = value?.Trim() ?? string.Empty;
            RefreshView();
        }

        private void HandleCategoryRequested(string category)
        {
            selectedCategory = category ?? string.Empty;
            RefreshView();
        }

        private void HandleRequiredFilterRequested()
        {
            requiredOnly = !requiredOnly;
            RefreshView();
        }

        private void HandleQuantityDeltaRequested(
            string definitionId,
            int delta)
        {
            if (!TryResolveDefinition(
                    definitionId,
                    out FixtureDefinitionId id,
                    out _))
            {
                return;
            }

            int current = GetDraftQuantity(id);
            int next = Math.Max(0, Math.Min(999, current + delta));
            SetDraftQuantity(id, next);
            statusMessage = string.Empty;
            RefreshView();
        }

        private void HandleAddRequiredItemRequested(string definitionId)
        {
            if (!TryResolveDefinition(
                    definitionId,
                    out FixtureDefinitionId id,
                    out _))
            {
                return;
            }

            int needed = GetRequiredQuantity(id);
            SetDraftQuantity(
                id,
                Math.Max(GetDraftQuantity(id), needed));
            statusMessage = needed > 0
                ? "Added the selected plan requirement to the BIG Wholesale order."
                : "This fixture's planned need is already covered.";
            RefreshView();
        }

        private void HandleAddRequirementsRequested()
        {
            int added = 0;

            foreach (FixtureEquipmentDefinition definition
                     in equipmentRuntimeHost.Catalog.EnumerateDefinitions())
            {
                FixtureDefinitionId id = definition.FixtureDefinitionId;
                int needed = GetRequiredQuantity(id);
                int previous = GetDraftQuantity(id);

                if (needed > previous)
                {
                    SetDraftQuantity(id, needed);
                    added += needed - previous;
                }
            }

            statusMessage = added > 0
                ? $"Added {added} fixture module(s) required by the current plan."
                : "Every planned fixture is already owned, ordered, or in this draft.";
            RefreshView();
        }

        private void HandleClearDraftRequested()
        {
            draft.Clear();
            statusMessage = "BIG Wholesale equipment order cleared.";
            RefreshView();
        }

        private void HandlePlaceOrderRequested()
        {
            if (!equipmentRuntimeHost.TryInitialize() || draft.Count == 0)
            {
                statusMessage =
                    "Add equipment before placing the BIG Wholesale order.";
                RefreshView();
                return;
            }

            Dictionary<FixtureDefinitionId, int> request =
                new Dictionary<FixtureDefinitionId, int>();

            foreach (KeyValuePair<FixtureDefinitionId, int> line in draft)
            {
                if (line.Value > 0)
                {
                    request.Add(line.Key, line.Value);
                }
            }

            suppressStateRefresh = true;
            FixtureEquipmentOrderResult result;

            try
            {
                result = equipmentRuntimeHost.Orders.TryPlaceOrders(
                    request,
                    equipmentRuntimeHost.CurrentTime.TotalGameSeconds);
            }
            finally
            {
                suppressStateRefresh = false;
            }

            if (result.Succeeded)
            {
                int units = 0;

                for (int index = 0; index < result.Orders.Count; index++)
                {
                    units += result.Orders[index].Quantity;
                }

                draft.Clear();
                statusMessage =
                    $"BIG Wholesale equipment order placed: "
                    + $"{units} module(s) for "
                    + $"{FormatMoney(result.TotalCostCents)}. Payment made now.";
            }
            else
            {
                statusMessage = FormatOrderFailure(result.Failure);
            }

            RefreshView();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleEquipmentRuntimeInitialized(
            FixtureEquipmentRuntimeHost initializedHost)
        {
            statusMessage = string.Empty;
            RefreshView();
        }

        private void HandleEquipmentStateChanged()
        {
            if (suppressStateRefresh)
            {
                return;
            }

            statusMessage = string.Empty;
            RefreshView();
        }


        private void RefreshView()
        {
            if (boundView == null)
            {
                return;
            }

            if (!equipmentRuntimeHost.TryInitialize()
                || equipmentRuntimeHost.CatalogAsset == null)
            {
                boundView.ShowError(
                    equipmentRuntimeHost.InitializationError);
                return;
            }

            try
            {
                boundView.SetModel(BuildModel());
            }
            catch (Exception exception)
            {
                boundView.ShowError(exception.Message);
                Debug.LogException(exception, this);
            }
        }

        private EquipmentCatalogWorkspaceModel BuildModel()
        {
            List<FixtureEquipmentCatalogEntryAsset> entries =
                new List<FixtureEquipmentCatalogEntryAsset>();

            foreach (FixtureEquipmentCatalogEntryAsset entry
                     in equipmentRuntimeHost.CatalogAsset.EnumerateEntries())
            {
                entries.Add(entry);
            }

            entries.Sort(
                (left, right) =>
                {
                    int category = string.CompareOrdinal(
                        left.CategoryName,
                        right.CategoryName);
                    return category != 0
                        ? category
                        : string.CompareOrdinal(
                            left.FixtureDefinition.DisplayName,
                            right.FixtureDefinition.DisplayName);
                });

            Dictionary<string, int> categoryCounts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            List<EquipmentCatalogItem> equipment =
                new List<EquipmentCatalogItem>();
            List<EquipmentDraftLineItem> draftLines =
                new List<EquipmentDraftLineItem>();
            long draftTotal = 0;
            bool hasRequired = false;

            for (int index = 0; index < entries.Count; index++)
            {
                FixtureEquipmentCatalogEntryAsset entry = entries[index];
                FixtureDefinitionId id = entry.FixtureDefinition.Id;
                FixtureEquipmentDefinition definition =
                    equipmentRuntimeHost.Catalog.GetRequired(id);
                int required = GetRequiredQuantity(id);
                int draftQuantity = GetDraftQuantity(id);
                hasRequired |= required > draftQuantity;

                if (!categoryCounts.TryAdd(entry.CategoryName, 1))
                {
                    categoryCounts[entry.CategoryName]++;
                }

                if (draftQuantity > 0)
                {
                    long lineTotal = checked(
                        definition.UnitPriceCents * draftQuantity);
                    draftTotal = checked(draftTotal + lineTotal);
                    draftLines.Add(
                        new EquipmentDraftLineItem(
                            id.Value,
                            definition.DisplayName,
                            draftQuantity,
                            lineTotal,
                            FormatDelivery(definition)));
                }

                if (!MatchesFilter(
                        definition.DisplayName,
                        entry.CategoryName,
                        required))
                {
                    continue;
                }

                equipment.Add(
                    new EquipmentCatalogItem(
                        id.Value,
                        definition.DisplayName,
                        entry.CategoryName,
                        entry.FixtureDefinition.CatalogIcon,
                        definition.UnitPriceCents,
                        FormatDelivery(definition),
                        equipmentRuntimeHost.Inventory.GetQuantity(id),
                        equipmentRuntimeHost.Plans.CountFor(id),
                        equipmentRuntimeHost.Orders
                            .GetOutstandingQuantity(id),
                        required,
                        draftQuantity));
            }

            List<EquipmentCatalogFilterItem> categories =
                new List<EquipmentCatalogFilterItem>(categoryCounts.Count);

            foreach (KeyValuePair<string, int> category in categoryCounts)
            {
                categories.Add(
                    new EquipmentCatalogFilterItem(
                        category.Key,
                        category.Value));
            }

            categories.Sort(
                (left, right) => string.CompareOrdinal(
                    left.Name,
                    right.Name));
            draftLines.Sort(
                (left, right) => string.CompareOrdinal(
                    left.DisplayName,
                    right.DisplayName));

            CountShipmentStates(
                out int scheduled,
                out int ready);
            int staged = equipmentRuntimeHost.StagedReadyCount;
            int waiting = Math.Max(0, ready - staged);

            return new EquipmentCatalogWorkspaceModel(
                categories,
                equipment,
                draftLines,
                selectedCategory,
                requiredOnly,
                draftTotal,
                equipmentRuntimeHost.Cash.BalanceCents,
                FormatCurrentTime(equipmentRuntimeHost.CurrentTime),
                scheduled,
                staged,
                waiting,
                hasRequired,
                BuildStatusMessage(scheduled, staged, waiting));
        }

        private bool MatchesFilter(
            string displayName,
            string category,
            int requiredQuantity)
        {
            if (!string.IsNullOrEmpty(selectedCategory)
                && !string.Equals(
                    selectedCategory,
                    category,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (requiredOnly && requiredQuantity <= 0)
            {
                return false;
            }

            return string.IsNullOrEmpty(searchText)
                || displayName.IndexOf(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) >= 0
                || category.IndexOf(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryResolveDefinition(
            string definitionId,
            out FixtureDefinitionId id,
            out FixtureEquipmentDefinition definition)
        {
            try
            {
                id = new FixtureDefinitionId(definitionId);
            }
            catch (ArgumentException)
            {
                id = default;
                definition = null;
                return false;
            }

            return equipmentRuntimeHost.Catalog.TryGet(id, out definition);
        }

        private int GetRequiredQuantity(FixtureDefinitionId id)
        {
            return Math.Max(
                0,
                equipmentRuntimeHost.Plans.CountFor(id)
                - equipmentRuntimeHost.Inventory.GetQuantity(id)
                - equipmentRuntimeHost.Orders.GetOutstandingQuantity(id));
        }

        private bool HasRequiredEquipment()
        {
            if (!equipmentRuntimeHost.TryInitialize())
            {
                return false;
            }

            foreach (FixtureEquipmentDefinition definition
                     in equipmentRuntimeHost.Catalog.EnumerateDefinitions())
            {
                if (GetRequiredQuantity(
                        definition.FixtureDefinitionId) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetDraftQuantity(FixtureDefinitionId id)
        {
            return draft.TryGetValue(id, out int quantity)
                ? quantity
                : 0;
        }

        private void SetDraftQuantity(FixtureDefinitionId id, int quantity)
        {
            if (quantity <= 0)
            {
                draft.Remove(id);
                return;
            }

            draft[id] = quantity;
        }

        private void CountShipmentStates(out int scheduled, out int ready)
        {
            scheduled = 0;
            ready = 0;

            foreach (FixtureEquipmentOrder order
                     in equipmentRuntimeHost.Orders.EnumerateOrders())
            {
                if (order.Status == FixtureEquipmentOrderStatus.Scheduled)
                {
                    scheduled++;
                }
                else if (order.Status
                    == FixtureEquipmentOrderStatus.ReadyToReceive)
                {
                    ready++;
                }
            }
        }

        private string BuildStatusMessage(
            int scheduled,
            int staged,
            int waiting)
        {
            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                return statusMessage;
            }

            if (staged > 0)
            {
                return staged == 1
                    ? "1 BIG Wholesale equipment shipment is staged. Open RCV to receive it."
                    : $"{staged} BIG Wholesale equipment shipments are staged. Open RCV to receive them.";
            }

            if (waiting > 0)
            {
                return waiting == 1
                    ? "1 BIG Wholesale equipment shipment is waiting for Receiving space."
                    : $"{waiting} BIG Wholesale equipment shipments are waiting for Receiving space.";
            }

            if (scheduled > 0)
            {
                return scheduled == 1
                    ? "1 BIG Wholesale equipment shipment is in transit."
                    : $"{scheduled} BIG Wholesale equipment shipments are in transit.";
            }

            return "Plan freely, then order the physical modules from BIG Wholesale.";
        }

        private static string FormatDelivery(
            FixtureEquipmentDefinition definition)
        {
            long minutes = definition.DeliveryLeadTimeSeconds / 60;

            if (minutes == 0)
            {
                return "AVAILABLE NOW";
            }

            if (minutes % 60 == 0)
            {
                long hours = minutes / 60;
                return $"ARRIVES IN {hours} GAME HOUR"
                    + (hours == 1 ? string.Empty : "S");
            }

            return $"ARRIVES IN {minutes} GAME MINUTES";
        }

        private static string FormatCurrentTime(SimulationDateTime time)
        {
            int hour = time.Hour % 12;

            if (hour == 0)
            {
                hour = 12;
            }

            string period = time.Hour < 12 ? "AM" : "PM";
            return $"{time.DayOfWeek.ToString().ToUpperInvariant()} · "
                + $"{hour}:{time.Minute:00} {period}";
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100:N0}.{Math.Abs(cents % 100):00}";
        }

        private static string FormatOrderFailure(
            FixtureEquipmentOrderFailure failure)
        {
            return failure switch
            {
                FixtureEquipmentOrderFailure.InsufficientFunds =>
                    "The store does not have enough cash for this BIG Wholesale equipment order.",
                FixtureEquipmentOrderFailure.EmptyOrder =>
                    "Add at least one fixture module before ordering.",
                FixtureEquipmentOrderFailure.AccountingLimitReached =>
                    "This BIG Wholesale equipment order is too large to price safely.",
                _ => $"BIG Wholesale equipment order rejected: {failure}."
            };
        }
    }
}
