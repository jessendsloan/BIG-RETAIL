using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Connects the authored commercial catalog and runtime draft service to
    /// the product-first Purchasing workspace.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PurchasingWorkspaceDocumentHost))]
    public sealed class PurchasingWorkspacePresenter : MonoBehaviour
    {
        private const string AllCategoriesId = "";
        private const int MaximumDraftPackCount = 999;
        private static readonly CommercialTime LabCommercialTime =
            new CommercialTime(0, 9, 0);

        [SerializeField]
        private PurchasingWorkspaceDocumentHost documentHost;

        [SerializeField]
        private CommercialCatalogAsset commercialCatalog;

        private readonly Dictionary<ProductId, ProductDefinitionAsset>
            productAssets =
                new Dictionary<ProductId, ProductDefinitionAsset>();
        private readonly Dictionary<BrandId, BrandDefinitionAsset> brandAssets =
            new Dictionary<BrandId, BrandDefinitionAsset>();
        private readonly Dictionary<SupplierId, SupplierDefinitionAsset>
            supplierAssets =
                new Dictionary<SupplierId, SupplierDefinitionAsset>();
        private readonly Dictionary<ProductId, SupplierOfferId> selectedOffers =
            new Dictionary<ProductId, SupplierOfferId>();

        private CommercialCatalog catalog;
        private PurchasingService purchasing;
        private PurchasingWorkspaceView boundView;
        private string initializationError;
        private string searchText = string.Empty;
        private string selectedCategoryId = AllCategoriesId;
        private SupplierId? selectedSupplierId;
        private PurchasingReviewState reviewState;
        private IReadOnlyList<PlacedPurchaseOrder> lastPlacedBatch;
        private bool suppressDraftRefresh;


        private void Reset()
        {
            documentHost = GetComponent<PurchasingWorkspaceDocumentHost>();
        }

        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost = GetComponent<PurchasingWorkspaceDocumentHost>();
            }

            InitializeCatalog();
        }

        private void OnEnable()
        {
            if (documentHost == null)
            {
                return;
            }

            documentHost.ViewReady += HandleViewReady;

            if (purchasing != null)
            {
                purchasing.DraftsChanged += HandleDraftsChanged;
            }

            if (documentHost.HasView)
            {
                BindView(documentHost.View);
            }
        }

        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.ViewReady -= HandleViewReady;
            }

            if (purchasing != null)
            {
                purchasing.DraftsChanged -= HandleDraftsChanged;
            }

            UnbindView();
        }


        private void InitializeCatalog()
        {
            productAssets.Clear();
            brandAssets.Clear();
            supplierAssets.Clear();
            selectedOffers.Clear();
            catalog = null;
            purchasing = null;
            initializationError = string.Empty;
            reviewState = PurchasingReviewState.None;
            lastPlacedBatch = null;

            if (commercialCatalog == null)
            {
                initializationError =
                    "No commercial catalog is assigned to Purchasing.";
                Debug.LogError(initializationError, this);
                return;
            }

            if (!commercialCatalog.TryCreateCatalog(
                    out catalog,
                    out initializationError))
            {
                Debug.LogError(initializationError, commercialCatalog);
                return;
            }

            if (!BuildPresentationMaps(out initializationError))
            {
                Debug.LogError(initializationError, commercialCatalog);
                catalog = null;
                return;
            }

            purchasing = new PurchasingService(catalog);
        }

        private bool BuildPresentationMaps(out string error)
        {
            IReadOnlyList<BrandDefinitionAsset> authoredBrands =
                commercialCatalog.BrandCatalog.Brands;

            for (int index = 0; index < authoredBrands.Count; index++)
            {
                BrandDefinitionAsset asset = authoredBrands[index];
                string entryError = string.Empty;

                if (asset == null
                    || !asset.TryCreateDefinition(
                        out BrandDefinition definition,
                        out entryError))
                {
                    error = string.IsNullOrEmpty(entryError)
                        ? $"Brand presentation entry {index} is missing."
                        : entryError;
                    return false;
                }

                brandAssets.Add(definition.Id, asset);
            }

            IReadOnlyList<ProductDefinitionAsset> authoredProducts =
                commercialCatalog.ProductCatalog.Products;

            for (int index = 0; index < authoredProducts.Count; index++)
            {
                ProductDefinitionAsset asset = authoredProducts[index];
                string entryError = string.Empty;

                if (asset == null
                    || !asset.TryCreateDefinition(
                        out ProductDefinition definition,
                        out entryError))
                {
                    error = string.IsNullOrEmpty(entryError)
                        ? $"Product presentation entry {index} is missing."
                        : entryError;
                    return false;
                }

                if (asset.Brand == null)
                {
                    error =
                        $"{asset.name}: Opening products require an authored brand.";
                    return false;
                }

                productAssets.Add(definition.Id, asset);
            }

            IReadOnlyList<SupplierDefinitionAsset> authoredSuppliers =
                commercialCatalog.SupplierCatalog.Suppliers;

            for (int index = 0; index < authoredSuppliers.Count; index++)
            {
                SupplierDefinitionAsset asset = authoredSuppliers[index];
                string entryError = string.Empty;

                if (asset == null
                    || !asset.TryCreateDefinition(
                        out SupplierDefinition definition,
                        out entryError))
                {
                    error = string.IsNullOrEmpty(entryError)
                        ? $"Supplier presentation entry {index} is missing."
                        : entryError;
                    return false;
                }

                supplierAssets.Add(definition.Id, asset);
            }

            error = string.Empty;
            return true;
        }


        private void HandleViewReady(PurchasingWorkspaceView view)
        {
            BindView(view);
        }

        private void BindView(PurchasingWorkspaceView view)
        {
            if (boundView == view)
            {
                RefreshView();
                return;
            }

            UnbindView();
            boundView = view;
            boundView.SearchChanged += HandleSearchChanged;
            boundView.CategoryFilterRequested += HandleCategoryFilterRequested;
            boundView.SupplierFilterRequested += HandleSupplierFilterRequested;
            boundView.OfferRequested += HandleOfferRequested;
            boundView.QuantityDeltaRequested += HandleQuantityDeltaRequested;
            boundView.ReviewRequested += HandleReviewRequested;
            boundView.ReviewBackRequested += HandleReviewBackRequested;
            boundView.PlaceOrdersRequested += HandlePlaceOrdersRequested;
            boundView.ConfirmationCloseRequested +=
                HandleConfirmationCloseRequested;
            RefreshView();
        }

        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SearchChanged -= HandleSearchChanged;
            boundView.CategoryFilterRequested -= HandleCategoryFilterRequested;
            boundView.SupplierFilterRequested -= HandleSupplierFilterRequested;
            boundView.OfferRequested -= HandleOfferRequested;
            boundView.QuantityDeltaRequested -= HandleQuantityDeltaRequested;
            boundView.ReviewRequested -= HandleReviewRequested;
            boundView.ReviewBackRequested -= HandleReviewBackRequested;
            boundView.PlaceOrdersRequested -= HandlePlaceOrdersRequested;
            boundView.ConfirmationCloseRequested -=
                HandleConfirmationCloseRequested;
            boundView = null;
        }

        private void HandleSearchChanged(string value)
        {
            searchText = value == null
                ? string.Empty
                : value.Trim();
            RefreshView();
        }

        private void HandleCategoryFilterRequested(string categoryId)
        {
            selectedCategoryId = categoryId ?? AllCategoriesId;
            RefreshView();
        }

        private void HandleSupplierFilterRequested(SupplierId? supplierId)
        {
            selectedSupplierId = supplierId;
            RefreshView();
        }

        private void HandleOfferRequested(
            ProductId productId,
            SupplierOfferId offerId)
        {
            SupplierOfferDefinition offer = catalog.Offers.GetRequired(offerId);

            if (offer.ProductId != productId)
            {
                Debug.LogError(
                    $"Offer '{offerId}' does not belong to product '{productId}'.",
                    this);
                return;
            }

            selectedOffers[productId] = offerId;
            RefreshView();
        }

        private void HandleQuantityDeltaRequested(
            ProductId productId,
            SupplierOfferId offerId,
            int delta)
        {
            if (purchasing == null || delta == 0)
            {
                return;
            }

            SupplierOfferDefinition offer = catalog.Offers.GetRequired(offerId);

            if (offer.ProductId != productId)
            {
                Debug.LogError(
                    $"Offer '{offerId}' does not belong to product '{productId}'.",
                    this);
                return;
            }

            int currentCount = purchasing.GetPurchasePackCount(offerId);
            int nextCount = Mathf.Clamp(
                currentCount + delta,
                0,
                MaximumDraftPackCount);

            if (nextCount != currentCount)
            {
                purchasing.SetPurchasePackCount(offerId, nextCount);
            }
        }

        private void HandleDraftsChanged()
        {
            if (!suppressDraftRefresh)
            {
                RefreshView();
            }
        }

        private void HandleReviewRequested()
        {
            reviewState = PurchasingReviewState.Review;
            lastPlacedBatch = null;
            RefreshView();
        }

        private void HandleReviewBackRequested()
        {
            reviewState = PurchasingReviewState.None;
            RefreshView();
        }

        private void HandlePlaceOrdersRequested()
        {
            if (purchasing == null)
            {
                return;
            }

            try
            {
                suppressDraftRefresh = true;
                lastPlacedBatch =
                    purchasing.PlaceDrafts(LabCommercialTime);
                reviewState = PurchasingReviewState.Confirmation;
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(exception.Message, this);
                reviewState = PurchasingReviewState.Review;
            }
            finally
            {
                suppressDraftRefresh = false;
            }

            RefreshView();
        }

        private void HandleConfirmationCloseRequested()
        {
            reviewState = PurchasingReviewState.None;
            lastPlacedBatch = null;
            RefreshView();
        }

        private void RefreshView()
        {
            if (boundView == null)
            {
                return;
            }

            if (catalog == null || purchasing == null)
            {
                boundView.ShowError(initializationError);
                return;
            }

            boundView.SetModel(BuildModel());
        }

        private PurchasingWorkspaceModel BuildModel()
        {
            List<PurchasingProductItem> products = BuildProducts();
            List<PurchasingDraftItem> drafts = BuildDrafts(out long grandTotalCents);
            PurchasingReviewModel review =
                BuildReviewModel(drafts, grandTotalCents);

            return new PurchasingWorkspaceModel(
                BuildCategoryFilters(),
                BuildSupplierFilters(),
                products,
                drafts,
                selectedCategoryId,
                selectedSupplierId,
                grandTotalCents,
                FormatCommercialTime(LabCommercialTime),
                review);
        }

        private List<PurchasingFilterItem> BuildCategoryFilters()
        {
            List<string> categoryOrder = new List<string>();
            Dictionary<string, int> counts =
                new Dictionary<string, int>(StringComparer.Ordinal);
            int productCount = 0;

            foreach (ProductDefinition product in catalog.Products.EnumerateDefinitions())
            {
                string categoryId = product.CategoryId.Value;
                productCount++;

                if (!counts.ContainsKey(categoryId))
                {
                    counts.Add(categoryId, 0);
                    categoryOrder.Add(categoryId);
                }

                counts[categoryId]++;
            }

            List<PurchasingFilterItem> filters =
                new List<PurchasingFilterItem>(categoryOrder.Count + 1)
                {
                    new PurchasingFilterItem(
                        AllCategoriesId,
                        "All products",
                        productCount)
                };

            for (int index = 0; index < categoryOrder.Count; index++)
            {
                string categoryId = categoryOrder[index];
                filters.Add(
                    new PurchasingFilterItem(
                        categoryId,
                        GetCategoryDisplayName(categoryId),
                        counts[categoryId]));
            }

            return filters;
        }

        private List<PurchasingSupplierFilterItem> BuildSupplierFilters()
        {
            List<PurchasingSupplierFilterItem> filters =
                new List<PurchasingSupplierFilterItem>();

            foreach (
                SupplierDefinition supplier
                in catalog.Suppliers.EnumerateDefinitions())
            {
                filters.Add(
                    new PurchasingSupplierFilterItem(
                        supplier.Id,
                        supplier.DisplayName,
                        GetSupplierColor(supplier.Id)));
            }

            return filters;
        }

        private List<PurchasingProductItem> BuildProducts()
        {
            List<PurchasingProductItem> items =
                new List<PurchasingProductItem>();

            foreach (ProductDefinition product in catalog.Products.EnumerateDefinitions())
            {
                List<PurchasingOfferItem> offers = BuildOffers(product);

                if (offers.Count == 0
                    || !ProductMatchesFilters(product, offers))
                {
                    continue;
                }

                BrandDefinition brand = product.BrandId == BrandId.Unbranded
                    ? null
                    : catalog.Brands.GetRequired(product.BrandId);
                ProductDefinitionAsset productAsset = productAssets[product.Id];

                items.Add(
                    new PurchasingProductItem(
                        product.Id,
                        brand == null ? "Unbranded" : brand.DisplayName,
                        product.DisplayName,
                        product.ProductLine,
                        product.PackageForm,
                        GetCategoryDisplayName(product.CategoryId.Value),
                        product.MarketPosition.ToString(),
                        productAsset.CatalogImage,
                        GetBrandColor(product.BrandId),
                        offers));
            }

            return items;
        }

        private List<PurchasingOfferItem> BuildOffers(ProductDefinition product)
        {
            List<SupplierOfferDefinition> visibleOffers =
                new List<SupplierOfferDefinition>();

            foreach (
                SupplierOfferDefinition offer
                in catalog.Offers.EnumerateForProduct(product.Id))
            {
                if (!selectedSupplierId.HasValue
                    || offer.SupplierId == selectedSupplierId.Value)
                {
                    visibleOffers.Add(offer);
                }
            }

            if (visibleOffers.Count == 0)
            {
                return new List<PurchasingOfferItem>();
            }

            SupplierOfferId selectedOfferId;

            if (!selectedOffers.TryGetValue(product.Id, out selectedOfferId)
                || !ContainsOffer(visibleOffers, selectedOfferId))
            {
                selectedOfferId = visibleOffers[0].Id;
            }

            List<PurchasingOfferItem> items =
                new List<PurchasingOfferItem>(visibleOffers.Count);

            for (int index = 0; index < visibleOffers.Count; index++)
            {
                SupplierOfferDefinition offer = visibleOffers[index];
                SupplierDefinition supplier =
                    catalog.Suppliers.GetRequired(offer.SupplierId);

                items.Add(
                    new PurchasingOfferItem(
                        offer.Id,
                        supplier.Id,
                        supplier.DisplayName,
                        GetSupplierColor(supplier.Id),
                        offer.PurchasePackQuantity,
                        offer.PackPriceCents,
                        offer.UnitCostCents,
                        FormatDeliveryEstimate(
                            supplier.DeliveryRule.EstimateDelivery(
                                LabCommercialTime),
                            LabCommercialTime),
                        purchasing.GetPurchasePackCount(offer.Id),
                        offer.Id == selectedOfferId));
            }

            return items;
        }

        private bool ProductMatchesFilters(
            ProductDefinition product,
            IReadOnlyList<PurchasingOfferItem> visibleOffers)
        {
            if (!string.IsNullOrEmpty(selectedCategoryId)
                && !string.Equals(
                    product.CategoryId.Value,
                    selectedCategoryId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            BrandDefinition brand = product.BrandId == BrandId.Unbranded
                ? null
                : catalog.Brands.GetRequired(product.BrandId);
            string comparison = searchText.ToUpperInvariant();

            return ContainsIgnoreCase(product.DisplayName, comparison)
                || ContainsIgnoreCase(product.ProductLine, comparison)
                || ContainsIgnoreCase(product.PackageForm, comparison)
                || ContainsIgnoreCase(product.CategoryId.Value, comparison)
                || (brand != null
                    && ContainsIgnoreCase(brand.DisplayName, comparison))
                || OfferSupplierMatchesSearch(visibleOffers, comparison);
        }

        private bool OfferSupplierMatchesSearch(
            IReadOnlyList<PurchasingOfferItem> offers,
            string comparison)
        {
            for (int index = 0; index < offers.Count; index++)
            {
                if (ContainsIgnoreCase(offers[index].SupplierName, comparison))
                {
                    return true;
                }
            }

            return false;
        }

        private List<PurchasingDraftItem> BuildDrafts(out long grandTotalCents)
        {
            List<PurchasingDraftItem> items = new List<PurchasingDraftItem>();
            grandTotalCents = 0;

            foreach (
                SupplierDefinition supplier
                in catalog.Suppliers.EnumerateDefinitions())
            {
                if (!purchasing.TryGetDraft(
                        supplier.Id,
                        out DraftPurchaseOrder draft))
                {
                    continue;
                }

                List<PurchasingDraftLineItem> lines =
                    new List<PurchasingDraftLineItem>();
                foreach (PurchaseOrderLine line in draft.EnumerateLines())
                {
                    ProductDefinition product =
                        catalog.Products.GetRequired(line.Offer.ProductId);
                    lines.Add(
                        new PurchasingDraftLineItem(
                            product.DisplayName,
                            line.PurchasePackCount,
                            line.LineTotalCents));
                }

                if (lines.Count == 0)
                {
                    continue;
                }

                long totalCents = draft.TotalCents;
                grandTotalCents = checked(grandTotalCents + totalCents);
                items.Add(
                    new PurchasingDraftItem(
                        supplier.Id,
                        supplier.DisplayName,
                        GetSupplierColor(supplier.Id),
                        FormatDeliveryEstimate(
                            supplier.DeliveryRule.EstimateDelivery(
                                LabCommercialTime),
                            LabCommercialTime),
                        supplier.MinimumOrderCents,
                        totalCents,
                        draft.GetAmountRemainingForMinimum(supplier),
                        lines));
            }

            return items;
        }

        private PurchasingReviewModel BuildReviewModel(
            IReadOnlyList<PurchasingDraftItem> drafts,
            long grandTotalCents)
        {
            if (reviewState == PurchasingReviewState.None)
            {
                return null;
            }

            if (reviewState == PurchasingReviewState.Confirmation)
            {
                return BuildConfirmationModel();
            }

            List<PurchasingReviewOrderItem> orders =
                new List<PurchasingReviewOrderItem>(drafts.Count);
            string blockingMessage = string.Empty;

            for (int index = 0; index < drafts.Count; index++)
            {
                PurchasingDraftItem draft = drafts[index];
                string validationSummary;

                if (draft.MinimumOrderCents == 0)
                {
                    validationSummary = "READY · NO ORDER MINIMUM";
                }
                else if (draft.MeetsMinimum)
                {
                    validationSummary = "READY · MINIMUM MET";
                }
                else
                {
                    validationSummary =
                        $"BLOCKED · ADD "
                        + $"{FormatMoney(draft.RemainingForMinimumCents)}";

                    if (string.IsNullOrEmpty(blockingMessage))
                    {
                        blockingMessage =
                            $"{draft.SupplierName} needs "
                            + $"{FormatMoney(draft.RemainingForMinimumCents)} "
                            + "more to meet its minimum.";
                    }
                }

                orders.Add(
                    new PurchasingReviewOrderItem(
                        null,
                        draft.SupplierName,
                        draft.AccentColor,
                        draft.DeliverySummary,
                        draft.TotalCents,
                        validationSummary,
                        draft.MeetsMinimum,
                        draft.Lines));
            }

            return new PurchasingReviewModel(
                false,
                $"PLACING {FormatCommercialTime(LabCommercialTime)}",
                orders,
                grandTotalCents,
                blockingMessage);
        }

        private PurchasingReviewModel BuildConfirmationModel()
        {
            List<PurchasingReviewOrderItem> orders =
                new List<PurchasingReviewOrderItem>();
            long grandTotalCents = 0;

            if (lastPlacedBatch != null)
            {
                for (int index = 0;
                     index < lastPlacedBatch.Count;
                     index++)
                {
                    PlacedPurchaseOrder placed = lastPlacedBatch[index];
                    SupplierDefinition supplier =
                        catalog.Suppliers.GetRequired(placed.SupplierId);
                    List<PurchasingDraftLineItem> lines =
                        new List<PurchasingDraftLineItem>(placed.Lines.Count);

                    for (int lineIndex = 0;
                         lineIndex < placed.Lines.Count;
                         lineIndex++)
                    {
                        PlacedPurchaseOrderLine line =
                            placed.Lines[lineIndex];
                        ProductDefinition product =
                            catalog.Products.GetRequired(line.ProductId);
                        lines.Add(
                            new PurchasingDraftLineItem(
                                product.DisplayName,
                                line.PurchasePackCount,
                                line.LineTotalCents));
                    }

                    grandTotalCents = checked(
                        grandTotalCents + placed.TotalCents);
                    orders.Add(
                        new PurchasingReviewOrderItem(
                            placed.OrderNumber,
                            supplier.DisplayName,
                            GetSupplierColor(supplier.Id),
                            FormatDeliveryEstimate(
                                placed.DeliveryEstimate,
                                placed.PlacedAt),
                            placed.TotalCents,
                            "SCHEDULED",
                            true,
                            lines));
                }
            }

            return new PurchasingReviewModel(
                true,
                $"PLACED {FormatCommercialTime(LabCommercialTime)}",
                orders,
                grandTotalCents,
                string.Empty);
        }


        private Color GetBrandColor(BrandId brandId)
        {
            return brandAssets.TryGetValue(
                    brandId,
                    out BrandDefinitionAsset asset)
                ? asset.AccentColor
                : new Color(0.36f, 0.42f, 0.45f, 1f);
        }

        private Color GetSupplierColor(SupplierId supplierId)
        {
            return supplierAssets.TryGetValue(
                    supplierId,
                    out SupplierDefinitionAsset asset)
                ? asset.AccentColor
                : new Color(0.22f, 0.34f, 0.38f, 1f);
        }

        private static bool ContainsOffer(
            IReadOnlyList<SupplierOfferDefinition> offers,
            SupplierOfferId offerId)
        {
            for (int index = 0; index < offers.Count; index++)
            {
                if (offers[index].Id == offerId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsIgnoreCase(
            string value,
            string upperComparison)
        {
            return !string.IsNullOrEmpty(value)
                && value.ToUpperInvariant().Contains(upperComparison);
        }

        private static string FormatDeliveryEstimate(
            SupplierDeliveryEstimate estimate,
            CommercialTime orderedAt)
        {
            int dayOffset =
                estimate.EarliestArrival.DayIndex - orderedAt.DayIndex;
            string daySummary;

            if (dayOffset == 0)
            {
                daySummary = "TODAY";
            }
            else if (dayOffset == 1)
            {
                daySummary = "TOMORROW";
            }
            else
            {
                daySummary =
                    FormatWeekday(estimate.EarliestArrival.Weekday)
                        .ToUpperInvariant();
            }

            return estimate.HasExactArrivalTime
                ? $"{daySummary} · {FormatClockTime(estimate.EarliestArrival)}"
                : $"{daySummary} ROUTE";
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100m:0.00}";
        }

        private static string FormatCommercialTime(CommercialTime time)
        {
            return $"{FormatWeekday(time.Weekday).ToUpperInvariant()} "
                + $"· {FormatClockTime(time)}";
        }

        private static string FormatClockTime(CommercialTime time)
        {
            int twelveHour = time.Hour % 12;

            if (twelveHour == 0)
            {
                twelveHour = 12;
            }

            string suffix = time.Hour < 12 ? "AM" : "PM";
            return $"{twelveHour}:{time.Minute:00} {suffix}";
        }

        private static string FormatWeekday(SupplierWeekday weekday)
        {
            switch (weekday)
            {
                case SupplierWeekday.Monday:
                    return "Monday";
                case SupplierWeekday.Tuesday:
                    return "Tuesday";
                case SupplierWeekday.Wednesday:
                    return "Wednesday";
                case SupplierWeekday.Thursday:
                    return "Thursday";
                case SupplierWeekday.Friday:
                    return "Friday";
                case SupplierWeekday.Saturday:
                    return "Saturday";
                case SupplierWeekday.Sunday:
                    return "Sunday";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(weekday),
                        weekday,
                        "A single supported weekday is required.");
            }
        }

        private static string GetCategoryDisplayName(string categoryId)
        {
            switch (categoryId)
            {
                case "BEVERAGES":
                    return "Beverages";
                case "SNACKS":
                    return "Snacks & candy";
                case "GROCERY":
                    return "Grocery";
                case "HOUSEHOLD":
                    return "Household";
                case "PERSONAL-CARE":
                    return "Personal care";
                default:
                    return categoryId.Replace('-', ' ');
            }
        }


        private enum PurchasingReviewState
        {
            None = 0,
            Review = 1,
            Confirmation = 2
        }
    }
}
