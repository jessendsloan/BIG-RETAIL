using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Purchasing.Unity.UI
{
    public sealed class EquipmentCatalogWorkspaceModel
    {
        public IReadOnlyList<EquipmentCatalogFilterItem> Categories
        {
            get;
        }

        public IReadOnlyList<EquipmentCatalogItem> Equipment { get; }

        public IReadOnlyList<EquipmentDraftLineItem> DraftLines { get; }

        public string SelectedCategory { get; }

        public bool RequiredOnly { get; }

        public long DraftTotalCents { get; }

        public long AvailableCashCents { get; }

        public string CurrentTimeSummary { get; }

        public int ScheduledShipmentCount { get; }

        public int StagedShipmentCount { get; }

        public int WaitingForReceivingCount { get; }

        public string StatusMessage { get; }

        public bool HasRequiredEquipment { get; }

        public bool CanPlaceOrder =>
            DraftLines.Count > 0
            && DraftTotalCents > 0
            && DraftTotalCents <= AvailableCashCents;


        public EquipmentCatalogWorkspaceModel(
            IReadOnlyList<EquipmentCatalogFilterItem> categories,
            IReadOnlyList<EquipmentCatalogItem> equipment,
            IReadOnlyList<EquipmentDraftLineItem> draftLines,
            string selectedCategory,
            bool requiredOnly,
            long draftTotalCents,
            long availableCashCents,
            string currentTimeSummary,
            int scheduledShipmentCount,
            int stagedShipmentCount,
            int waitingForReceivingCount,
            bool hasRequiredEquipment,
            string statusMessage)
        {
            Categories = categories
                ?? Array.Empty<EquipmentCatalogFilterItem>();
            Equipment = equipment
                ?? Array.Empty<EquipmentCatalogItem>();
            DraftLines = draftLines
                ?? Array.Empty<EquipmentDraftLineItem>();
            SelectedCategory = selectedCategory ?? string.Empty;
            RequiredOnly = requiredOnly;
            DraftTotalCents = draftTotalCents;
            AvailableCashCents = availableCashCents;
            CurrentTimeSummary = currentTimeSummary ?? string.Empty;
            ScheduledShipmentCount = scheduledShipmentCount;
            StagedShipmentCount = stagedShipmentCount;
            WaitingForReceivingCount = waitingForReceivingCount;
            HasRequiredEquipment = hasRequiredEquipment;
            StatusMessage = statusMessage ?? string.Empty;
        }
    }


    public sealed class EquipmentCatalogFilterItem
    {
        public string Name { get; }

        public int ItemCount { get; }


        public EquipmentCatalogFilterItem(string name, int itemCount)
        {
            Name = name ?? string.Empty;
            ItemCount = itemCount;
        }
    }


    public sealed class EquipmentCatalogItem
    {
        public string DefinitionId { get; }

        public string DisplayName { get; }

        public string CategoryName { get; }

        public Sprite Icon { get; }

        public long UnitPriceCents { get; }

        public string DeliverySummary { get; }

        public int OwnedQuantity { get; }

        public int PlannedQuantity { get; }

        public int OutstandingQuantity { get; }

        public int RequiredQuantity { get; }

        public int DraftQuantity { get; }


        public EquipmentCatalogItem(
            string definitionId,
            string displayName,
            string categoryName,
            Sprite icon,
            long unitPriceCents,
            string deliverySummary,
            int ownedQuantity,
            int plannedQuantity,
            int outstandingQuantity,
            int requiredQuantity,
            int draftQuantity)
        {
            DefinitionId = definitionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CategoryName = categoryName ?? string.Empty;
            Icon = icon;
            UnitPriceCents = unitPriceCents;
            DeliverySummary = deliverySummary ?? string.Empty;
            OwnedQuantity = ownedQuantity;
            PlannedQuantity = plannedQuantity;
            OutstandingQuantity = outstandingQuantity;
            RequiredQuantity = requiredQuantity;
            DraftQuantity = draftQuantity;
        }
    }


    public sealed class EquipmentDraftLineItem
    {
        public string DefinitionId { get; }

        public string DisplayName { get; }

        public int Quantity { get; }

        public long LineTotalCents { get; }

        public string DeliverySummary { get; }


        public EquipmentDraftLineItem(
            string definitionId,
            string displayName,
            int quantity,
            long lineTotalCents,
            string deliverySummary)
        {
            DefinitionId = definitionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quantity = quantity;
            LineTotalCents = lineTotalCents;
            DeliverySummary = deliverySummary ?? string.Empty;
        }
    }
}
