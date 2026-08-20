using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity.UI
{
    public sealed class PurchasingWorkspaceModel
    {
        public PurchasingWorkspaceModel(
            IReadOnlyList<PurchasingFilterItem> categoryFilters,
            IReadOnlyList<PurchasingSupplierFilterItem> supplierFilters,
            IReadOnlyList<PurchasingProductItem> products,
            IReadOnlyList<PurchasingDraftItem> drafts,
            string selectedCategoryId,
            SupplierId? selectedSupplierId,
            long grandTotalCents,
            string currentTimeSummary,
            PurchasingReviewModel review,
            long? availableCashCents = null)
        {
            CategoryFilters = categoryFilters
                ?? throw new ArgumentNullException(nameof(categoryFilters));
            SupplierFilters = supplierFilters
                ?? throw new ArgumentNullException(nameof(supplierFilters));
            Products = products
                ?? throw new ArgumentNullException(nameof(products));
            Drafts = drafts
                ?? throw new ArgumentNullException(nameof(drafts));
            SelectedCategoryId = selectedCategoryId ?? string.Empty;
            SelectedSupplierId = selectedSupplierId;
            GrandTotalCents = grandTotalCents;
            CurrentTimeSummary = currentTimeSummary ?? string.Empty;
            Review = review;
            AvailableCashCents = availableCashCents;
        }

        public IReadOnlyList<PurchasingFilterItem> CategoryFilters { get; }
        public IReadOnlyList<PurchasingSupplierFilterItem> SupplierFilters { get; }
        public IReadOnlyList<PurchasingProductItem> Products { get; }
        public IReadOnlyList<PurchasingDraftItem> Drafts { get; }
        public string SelectedCategoryId { get; }
        public SupplierId? SelectedSupplierId { get; }
        public long GrandTotalCents { get; }
        public string CurrentTimeSummary { get; }
        public PurchasingReviewModel Review { get; }
        public long? AvailableCashCents { get; }
    }


    public sealed class PurchasingFilterItem
    {
        public PurchasingFilterItem(string id, string displayName, int itemCount)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ItemCount = itemCount;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int ItemCount { get; }
    }


    public sealed class PurchasingSupplierFilterItem
    {
        public PurchasingSupplierFilterItem(
            SupplierId id,
            string displayName,
            Color accentColor)
        {
            Id = id;
            DisplayName = displayName ?? string.Empty;
            AccentColor = accentColor;
        }

        public SupplierId Id { get; }
        public string DisplayName { get; }
        public Color AccentColor { get; }
    }


    public sealed class PurchasingProductItem
    {
        public PurchasingProductItem(
            ProductId id,
            string brandName,
            string displayName,
            string productLine,
            string packageForm,
            string categoryName,
            string marketPosition,
            Sprite image,
            Color accentColor,
            IReadOnlyList<PurchasingOfferItem> offers)
        {
            Id = id;
            BrandName = brandName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ProductLine = productLine ?? string.Empty;
            PackageForm = packageForm ?? string.Empty;
            CategoryName = categoryName ?? string.Empty;
            MarketPosition = marketPosition ?? string.Empty;
            Image = image;
            AccentColor = accentColor;
            Offers = offers ?? throw new ArgumentNullException(nameof(offers));
        }

        public ProductId Id { get; }
        public string BrandName { get; }
        public string DisplayName { get; }
        public string ProductLine { get; }
        public string PackageForm { get; }
        public string CategoryName { get; }
        public string MarketPosition { get; }
        public Sprite Image { get; }
        public Color AccentColor { get; }
        public IReadOnlyList<PurchasingOfferItem> Offers { get; }
    }


    public sealed class PurchasingOfferItem
    {
        public PurchasingOfferItem(
            SupplierOfferId id,
            SupplierId supplierId,
            string supplierName,
            Color supplierColor,
            int packQuantity,
            long packPriceCents,
            decimal unitCostCents,
            string deliverySummary,
            int draftPackCount,
            bool isSelected)
        {
            Id = id;
            SupplierId = supplierId;
            SupplierName = supplierName ?? string.Empty;
            SupplierColor = supplierColor;
            PackQuantity = packQuantity;
            PackPriceCents = packPriceCents;
            UnitCostCents = unitCostCents;
            DeliverySummary = deliverySummary ?? string.Empty;
            DraftPackCount = draftPackCount;
            IsSelected = isSelected;
        }

        public SupplierOfferId Id { get; }
        public SupplierId SupplierId { get; }
        public string SupplierName { get; }
        public Color SupplierColor { get; }
        public int PackQuantity { get; }
        public long PackPriceCents { get; }
        public decimal UnitCostCents { get; }
        public string DeliverySummary { get; }
        public int DraftPackCount { get; }
        public bool IsSelected { get; }
    }


    public sealed class PurchasingDraftItem
    {
        public PurchasingDraftItem(
            SupplierId supplierId,
            string supplierName,
            Color accentColor,
            string deliverySummary,
            long minimumOrderCents,
            long totalCents,
            long remainingForMinimumCents,
            IReadOnlyList<PurchasingDraftLineItem> lines)
        {
            SupplierId = supplierId;
            SupplierName = supplierName ?? string.Empty;
            AccentColor = accentColor;
            DeliverySummary = deliverySummary ?? string.Empty;
            MinimumOrderCents = minimumOrderCents;
            TotalCents = totalCents;
            RemainingForMinimumCents = remainingForMinimumCents;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }

        public SupplierId SupplierId { get; }
        public string SupplierName { get; }
        public Color AccentColor { get; }
        public string DeliverySummary { get; }
        public long MinimumOrderCents { get; }
        public long TotalCents { get; }
        public long RemainingForMinimumCents { get; }
        public IReadOnlyList<PurchasingDraftLineItem> Lines { get; }
        public bool HasLines => Lines.Count > 0;
        public bool MeetsMinimum => RemainingForMinimumCents == 0;
    }


    public sealed class PurchasingDraftLineItem
    {
        public PurchasingDraftLineItem(
            string productName,
            int purchasePackCount,
            long lineTotalCents)
        {
            ProductName = productName ?? string.Empty;
            PurchasePackCount = purchasePackCount;
            LineTotalCents = lineTotalCents;
        }

        public string ProductName { get; }
        public int PurchasePackCount { get; }
        public long LineTotalCents { get; }
    }


    public sealed class PurchasingReviewModel
    {
        public PurchasingReviewModel(
            bool isConfirmation,
            string timingSummary,
            IReadOnlyList<PurchasingReviewOrderItem> orders,
            long grandTotalCents,
            string blockingMessage)
        {
            IsConfirmation = isConfirmation;
            TimingSummary = timingSummary ?? string.Empty;
            Orders = orders ?? throw new ArgumentNullException(nameof(orders));
            GrandTotalCents = grandTotalCents;
            BlockingMessage = blockingMessage ?? string.Empty;
        }

        public bool IsConfirmation { get; }
        public string TimingSummary { get; }
        public IReadOnlyList<PurchasingReviewOrderItem> Orders { get; }
        public long GrandTotalCents { get; }
        public string BlockingMessage { get; }
        public bool CanPlace =>
            !IsConfirmation
            && Orders.Count > 0
            && string.IsNullOrEmpty(BlockingMessage);
    }


    public sealed class PurchasingReviewOrderItem
    {
        public PurchasingReviewOrderItem(
            long? orderNumber,
            string supplierName,
            Color accentColor,
            string arrivalSummary,
            long totalCents,
            string validationSummary,
            bool isValid,
            IReadOnlyList<PurchasingDraftLineItem> lines)
        {
            OrderNumber = orderNumber;
            SupplierName = supplierName ?? string.Empty;
            AccentColor = accentColor;
            ArrivalSummary = arrivalSummary ?? string.Empty;
            TotalCents = totalCents;
            ValidationSummary = validationSummary ?? string.Empty;
            IsValid = isValid;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }

        public long? OrderNumber { get; }
        public string SupplierName { get; }
        public Color AccentColor { get; }
        public string ArrivalSummary { get; }
        public long TotalCents { get; }
        public string ValidationSummary { get; }
        public bool IsValid { get; }
        public IReadOnlyList<PurchasingDraftLineItem> Lines { get; }
    }
}
