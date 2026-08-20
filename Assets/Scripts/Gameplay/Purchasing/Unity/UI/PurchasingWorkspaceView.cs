using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Presentation-only wrapper for the product-first Purchasing workspace.
    /// </summary>
    public sealed class PurchasingWorkspaceView : IDisposable
    {
        private const string SearchFieldName = "purchasing-search";
        private const string CategoryFiltersName = "category-filters";
        private const string SupplierFiltersName = "supplier-filters";
        private const string ProductListName = "product-list";
        private const string ProductCountName = "product-count";
        private const string DraftListName = "draft-list";
        private const string DraftEmptyStateName = "draft-empty-state";
        private const string DraftGrandTotalName = "draft-grand-total";
        private const string ReviewButtonName = "review-orders-button";
        private const string CommercialTimeName = "commercial-time";
        private const string AvailableCashName = "available-cash";
        private const string CloseButtonName = "close-purchasing-button";
        private const string ReviewOverlayName = "order-review-overlay";
        private const string ReviewKickerName = "order-review-kicker";
        private const string ReviewTitleName = "order-review-title";
        private const string ReviewTimingName = "order-review-timing";
        private const string ReviewListName = "order-review-list";
        private const string ReviewBlockerName = "order-review-blocker";
        private const string ReviewGrandTotalName =
            "order-review-grand-total";
        private const string ReviewBackButtonName =
            "order-review-back-button";
        private const string PlaceOrdersButtonName = "place-orders-button";
        private const string ConfirmationCloseButtonName =
            "order-confirmation-close-button";
        private const string EmptyStateName = "product-empty-state";
        private const string ErrorStateName = "purchasing-error-state";

        private const string SelectedClassName = "is-selected";

        private readonly TextField searchField;
        private readonly VisualElement categoryFilters;
        private readonly VisualElement supplierFilters;
        private readonly VisualElement productList;
        private readonly Label productCount;
        private readonly VisualElement draftList;
        private readonly Label draftEmptyState;
        private readonly Label draftGrandTotal;
        private readonly Button reviewButton;
        private readonly Label commercialTime;
        private readonly Label availableCash;
        private readonly Button closeButton;
        private readonly VisualElement reviewOverlay;
        private readonly Label reviewKicker;
        private readonly Label reviewTitle;
        private readonly Label reviewTiming;
        private readonly VisualElement reviewList;
        private readonly Label reviewBlocker;
        private readonly Label reviewGrandTotal;
        private readonly Button reviewBackButton;
        private readonly Button placeOrdersButton;
        private readonly Button confirmationCloseButton;
        private readonly Label emptyState;
        private readonly Label errorState;
        private readonly List<ButtonBinding> buttonBindings =
            new List<ButtonBinding>();

        private bool isDisposed;


        public PurchasingWorkspaceView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            searchField = Require<TextField>(root, SearchFieldName);
            categoryFilters = Require<VisualElement>(root, CategoryFiltersName);
            supplierFilters = Require<VisualElement>(root, SupplierFiltersName);
            productList = Require<VisualElement>(root, ProductListName);
            productCount = Require<Label>(root, ProductCountName);
            draftList = Require<VisualElement>(root, DraftListName);
            draftEmptyState = Require<Label>(root, DraftEmptyStateName);
            draftGrandTotal = Require<Label>(root, DraftGrandTotalName);
            reviewButton = Require<Button>(root, ReviewButtonName);
            commercialTime = Require<Label>(root, CommercialTimeName);
            availableCash = Require<Label>(root, AvailableCashName);
            closeButton = Require<Button>(root, CloseButtonName);
            reviewOverlay = Require<VisualElement>(root, ReviewOverlayName);
            reviewKicker = Require<Label>(root, ReviewKickerName);
            reviewTitle = Require<Label>(root, ReviewTitleName);
            reviewTiming = Require<Label>(root, ReviewTimingName);
            reviewList = Require<VisualElement>(root, ReviewListName);
            reviewBlocker = Require<Label>(root, ReviewBlockerName);
            reviewGrandTotal = Require<Label>(root, ReviewGrandTotalName);
            reviewBackButton = Require<Button>(root, ReviewBackButtonName);
            placeOrdersButton = Require<Button>(root, PlaceOrdersButtonName);
            confirmationCloseButton =
                Require<Button>(root, ConfirmationCloseButtonName);
            emptyState = Require<Label>(root, EmptyStateName);
            errorState = Require<Label>(root, ErrorStateName);

            searchField.RegisterValueChangedCallback(HandleSearchChanged);
            reviewButton.clicked += HandleReviewRequested;
            reviewBackButton.clicked += HandleReviewBackRequested;
            placeOrdersButton.clicked += HandlePlaceOrdersRequested;
            confirmationCloseButton.clicked +=
                HandleConfirmationCloseRequested;
            closeButton.clicked += HandleCloseRequested;
            reviewButton.SetEnabled(false);
            reviewButton.tooltip = "Stage at least one case to review orders.";
        }


        public event Action<string> SearchChanged;
        public event Action<string> CategoryFilterRequested;
        public event Action<SupplierId?> SupplierFilterRequested;
        public event Action<ProductId, SupplierOfferId> OfferRequested;
        public event Action<ProductId, SupplierOfferId, int>
            QuantityDeltaRequested;
        public event Action ReviewRequested;
        public event Action ReviewBackRequested;
        public event Action PlaceOrdersRequested;
        public event Action ConfirmationCloseRequested;
        public event Action CloseRequested;


        public void SetModel(PurchasingWorkspaceModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            errorState.style.display = DisplayStyle.None;
            ClearDynamicContent();

            BuildCategoryFilters(model);
            BuildSupplierFilters(model);
            BuildProducts(model.Products);
            int renderedDraftCount = BuildDrafts(model.Drafts);
            BuildReview(model.Review);

            productCount.text =
                model.Products.Count == 1
                    ? "1 product"
                    : $"{model.Products.Count} products";
            draftGrandTotal.text = FormatMoney(model.GrandTotalCents);
            commercialTime.text = model.CurrentTimeSummary;
            availableCash.text = model.AvailableCashCents.HasValue
                ? $"AVAILABLE CASH · {FormatMoney(model.AvailableCashCents.Value)}"
                : "EXPECTED ARRIVALS USE STORE TIME";
            reviewButton.SetEnabled(renderedDraftCount > 0);
            reviewButton.tooltip = renderedDraftCount > 0
                ? "Review supplier orders and scheduled arrivals."
                : "Stage at least one case to review orders.";
            draftEmptyState.style.display = renderedDraftCount == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            emptyState.style.display = model.Products.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void ShowError(string message)
        {
            ClearDynamicContent();
            productCount.text = "Catalog unavailable";
            draftGrandTotal.text = "$0.00";
            reviewButton.SetEnabled(false);
            reviewOverlay.style.display = DisplayStyle.None;
            draftEmptyState.style.display = DisplayStyle.None;
            emptyState.style.display = DisplayStyle.None;
            errorState.text = string.IsNullOrWhiteSpace(message)
                ? "The commercial catalog could not be loaded."
                : message;
            errorState.style.display = DisplayStyle.Flex;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            try
            {
                searchField.UnregisterValueChangedCallback(HandleSearchChanged);
                reviewButton.clicked -= HandleReviewRequested;
                reviewBackButton.clicked -= HandleReviewBackRequested;
                placeOrdersButton.clicked -= HandlePlaceOrdersRequested;
                confirmationCloseButton.clicked -=
                    HandleConfirmationCloseRequested;
                closeButton.clicked -= HandleCloseRequested;
                ClearDynamicContent();
            }
            catch (InvalidOperationException)
            {
                // PanelRenderer can release a visual tree before announcing a
                // UI reload. Released elements no longer accept mutations, and
                // their callbacks disappear with the tree itself.
                buttonBindings.Clear();
            }
            finally
            {
                isDisposed = true;
            }
        }


        private void BuildCategoryFilters(PurchasingWorkspaceModel model)
        {
            for (int index = 0;
                 index < model.CategoryFilters.Count;
                 index++)
            {
                PurchasingFilterItem item = model.CategoryFilters[index];
                Button button =
                    new Button
                    {
                        text = item.DisplayName,
                        tooltip = $"Show {item.DisplayName.ToLowerInvariant()} products"
                    };
                button.AddToClassList("purchasing-filter-button");
                button.EnableInClassList(
                    SelectedClassName,
                    string.Equals(
                        item.Id,
                        model.SelectedCategoryId,
                        StringComparison.Ordinal));

                Label count = new Label(item.ItemCount.ToString());
                count.AddToClassList("purchasing-filter-button__count");
                count.pickingMode = PickingMode.Ignore;
                button.Add(count);

                string categoryId = item.Id;
                BindButton(
                    button,
                    () => CategoryFilterRequested?.Invoke(categoryId));
                categoryFilters.Add(button);
            }
        }

        private void BuildSupplierFilters(PurchasingWorkspaceModel model)
        {
            Button allButton = new Button { text = "All suppliers" };
            allButton.AddToClassList("supplier-filter-button");
            allButton.EnableInClassList(
                SelectedClassName,
                !model.SelectedSupplierId.HasValue);
            BindButton(
                allButton,
                () => SupplierFilterRequested?.Invoke(null));
            supplierFilters.Add(allButton);

            for (int index = 0;
                 index < model.SupplierFilters.Count;
                 index++)
            {
                PurchasingSupplierFilterItem item =
                    model.SupplierFilters[index];
                Button button = new Button { text = item.DisplayName };
                button.AddToClassList("supplier-filter-button");
                button.style.borderLeftColor = item.AccentColor;
                button.EnableInClassList(
                    SelectedClassName,
                    model.SelectedSupplierId.HasValue
                    && model.SelectedSupplierId.Value == item.Id);

                SupplierId supplierId = item.Id;
                BindButton(
                    button,
                    () => SupplierFilterRequested?.Invoke(supplierId));
                supplierFilters.Add(button);
            }
        }

        private void BuildProducts(
            IReadOnlyList<PurchasingProductItem> products)
        {
            for (int index = 0; index < products.Count; index++)
            {
                productList.Add(BuildProductCard(products[index]));
            }
        }

        private VisualElement BuildProductCard(PurchasingProductItem item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("product-card");

            VisualElement identity = new VisualElement();
            identity.AddToClassList("product-card__identity");
            card.Add(identity);

            VisualElement art = new VisualElement();
            art.AddToClassList("product-card__art");
            art.style.backgroundColor = item.AccentColor;
            art.pickingMode = PickingMode.Ignore;

            if (item.Image != null)
            {
                art.style.backgroundImage = new StyleBackground(item.Image);
                art.AddToClassList("has-image");
            }
            else
            {
                Label initials = new Label(GetInitials(item.DisplayName));
                initials.AddToClassList("product-card__art-initials");
                initials.pickingMode = PickingMode.Ignore;
                art.Add(initials);
            }

            identity.Add(art);

            VisualElement text = new VisualElement();
            text.AddToClassList("product-card__text");
            identity.Add(text);

            Label brand = new Label(item.BrandName.ToUpperInvariant());
            brand.AddToClassList("product-card__brand");
            text.Add(brand);

            Label name = new Label(item.DisplayName);
            name.AddToClassList("product-card__name");
            text.Add(name);

            Label package = new Label(item.PackageForm);
            package.AddToClassList("product-card__package");
            text.Add(package);

            VisualElement tags = new VisualElement();
            tags.AddToClassList("product-card__tags");
            tags.Add(BuildTag(item.CategoryName));
            tags.Add(BuildTag(item.MarketPosition));
            text.Add(tags);

            BuildOffers(card, item);
            return card;
        }

        private void BuildOffers(
            VisualElement card,
            PurchasingProductItem product)
        {
            if (product.Offers.Count == 0)
            {
                Label unavailable =
                    new Label("No available supplier offers for this filter.");
                unavailable.AddToClassList("product-card__unavailable");
                card.Add(unavailable);
                return;
            }

            VisualElement offerArea = new VisualElement();
            offerArea.AddToClassList("product-card__offer-area");
            card.Add(offerArea);

            if (product.Offers.Count > 1)
            {
                VisualElement choices = new VisualElement();
                choices.AddToClassList("offer-choices");
                offerArea.Add(choices);

                for (int index = 0;
                     index < product.Offers.Count;
                     index++)
                {
                    PurchasingOfferItem offer = product.Offers[index];
                    string buttonText = offer.DraftPackCount > 0
                        ? $"{offer.SupplierName} · {offer.DraftPackCount}"
                        : offer.SupplierName;
                    Button button = new Button { text = buttonText };
                    button.AddToClassList("offer-choice-button");
                    button.style.borderBottomColor = offer.SupplierColor;
                    button.EnableInClassList(
                        SelectedClassName,
                        offer.IsSelected);

                    ProductId productId = product.Id;
                    SupplierOfferId offerId = offer.Id;
                    BindButton(
                        button,
                        () => OfferRequested?.Invoke(productId, offerId));
                    choices.Add(button);
                }
            }

            PurchasingOfferItem selected = FindSelectedOffer(product.Offers);
            VisualElement selectedPanel = BuildSelectedOffer(product.Id, selected);
            offerArea.Add(selectedPanel);
        }

        private VisualElement BuildSelectedOffer(
            ProductId productId,
            PurchasingOfferItem offer)
        {
            VisualElement panel = new VisualElement();
            panel.AddToClassList("selected-offer");
            panel.style.borderLeftColor = offer.SupplierColor;

            VisualElement supplierColumn = new VisualElement();
            supplierColumn.AddToClassList("selected-offer__supplier");
            panel.Add(supplierColumn);

            Label supplier = new Label(offer.SupplierName);
            supplier.AddToClassList("selected-offer__supplier-name");
            supplierColumn.Add(supplier);

            Label delivery = new Label(offer.DeliverySummary);
            delivery.AddToClassList("selected-offer__delivery");
            supplierColumn.Add(delivery);

            VisualElement packColumn = new VisualElement();
            packColumn.AddToClassList("selected-offer__metric");
            packColumn.Add(MetricLabel("CASE PACK", $"Case × {offer.PackQuantity}"));
            panel.Add(packColumn);

            VisualElement unitColumn = new VisualElement();
            unitColumn.AddToClassList("selected-offer__metric");
            unitColumn.Add(
                MetricLabel(
                    "UNIT COST",
                    FormatUnitMoney(offer.UnitCostCents)));
            panel.Add(unitColumn);

            VisualElement priceColumn = new VisualElement();
            priceColumn.AddToClassList("selected-offer__price");
            priceColumn.Add(MetricLabel("CASE PRICE", FormatMoney(offer.PackPriceCents)));
            panel.Add(priceColumn);

            VisualElement quantity = new VisualElement();
            quantity.AddToClassList("quantity-control");
            panel.Add(quantity);

            Button remove = new Button { text = "−", tooltip = "Remove one case" };
            remove.AddToClassList("quantity-control__button");
            remove.SetEnabled(offer.DraftPackCount > 0);
            BindButton(
                remove,
                () => QuantityDeltaRequested?.Invoke(productId, offer.Id, -1));
            quantity.Add(remove);

            Label count = new Label(
                offer.DraftPackCount == 1
                    ? "1 case"
                    : $"{offer.DraftPackCount} cases");
            count.AddToClassList("quantity-control__count");
            quantity.Add(count);

            Button add = new Button { text = "+", tooltip = "Add one case" };
            add.AddToClassList("quantity-control__button");
            BindButton(
                add,
                () => QuantityDeltaRequested?.Invoke(productId, offer.Id, 1));
            quantity.Add(add);

            return panel;
        }

        private int BuildDrafts(IReadOnlyList<PurchasingDraftItem> drafts)
        {
            int renderedCount = 0;

            for (int index = 0; index < drafts.Count; index++)
            {
                PurchasingDraftItem draft = drafts[index];

                if (!draft.HasLines)
                {
                    continue;
                }

                draftList.Add(BuildDraftCard(draft));
                renderedCount++;
            }

            return renderedCount;
        }

        private VisualElement BuildDraftCard(PurchasingDraftItem item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("draft-card");
            card.style.borderTopColor = item.AccentColor;

            VisualElement header = new VisualElement();
            header.AddToClassList("draft-card__header");
            card.Add(header);

            VisualElement title = new VisualElement();
            title.AddToClassList("draft-card__title-group");
            header.Add(title);

            Label supplier = new Label(item.SupplierName);
            supplier.AddToClassList("draft-card__supplier");
            title.Add(supplier);

            Label delivery = new Label(item.DeliverySummary);
            delivery.AddToClassList("draft-card__delivery");
            title.Add(delivery);

            Label total = new Label(FormatMoney(item.TotalCents));
            total.AddToClassList("draft-card__total");
            header.Add(total);

            for (int index = 0; index < item.Lines.Count; index++)
            {
                PurchasingDraftLineItem line = item.Lines[index];
                VisualElement lineRow = new VisualElement();
                lineRow.AddToClassList("draft-line");

                Label lineName =
                    new Label($"{line.ProductName}  ×{line.PurchasePackCount}");
                lineName.AddToClassList("draft-line__name");
                lineRow.Add(lineName);

                Label lineTotal = new Label(FormatMoney(line.LineTotalCents));
                lineTotal.AddToClassList("draft-line__total");
                lineRow.Add(lineTotal);
                card.Add(lineRow);
            }

            Label minimum = BuildMinimumLabel(item);
            card.Add(minimum);
            return card;
        }

        private static Label BuildMinimumLabel(PurchasingDraftItem item)
        {
            string message;

            if (item.MinimumOrderCents == 0)
            {
                message = "NO ORDER MINIMUM";
            }
            else if (item.MeetsMinimum)
            {
                message = "MINIMUM MET";
            }
            else
            {
                message =
                    $"ADD {FormatMoney(item.RemainingForMinimumCents)} FOR MINIMUM";
            }

            Label label = new Label(message);
            label.AddToClassList("draft-card__minimum");
            label.EnableInClassList(
                "is-met",
                item.MeetsMinimum);
            return label;
        }

        private void BuildReview(PurchasingReviewModel review)
        {
            if (review == null)
            {
                reviewOverlay.style.display = DisplayStyle.None;
                reviewOverlay.EnableInClassList("is-confirmation", false);
                return;
            }

            reviewOverlay.style.display = DisplayStyle.Flex;
            reviewOverlay.EnableInClassList(
                "is-confirmation",
                review.IsConfirmation);
            reviewKicker.text = review.IsConfirmation
                ? "ORDERS SCHEDULED"
                : "FINAL CHECK";
            reviewTitle.text = review.IsConfirmation
                ? "Purchase orders placed"
                : "Review purchase orders";
            reviewTiming.text = review.TimingSummary;
            reviewGrandTotal.text = FormatMoney(review.GrandTotalCents);

            for (int index = 0; index < review.Orders.Count; index++)
            {
                reviewList.Add(BuildReviewOrderCard(review.Orders[index]));
            }

            reviewBlocker.text = review.BlockingMessage;
            reviewBlocker.style.display =
                string.IsNullOrEmpty(review.BlockingMessage)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;

            reviewBackButton.style.display = review.IsConfirmation
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            placeOrdersButton.style.display = review.IsConfirmation
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            confirmationCloseButton.style.display = review.IsConfirmation
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            placeOrdersButton.SetEnabled(review.CanPlace);

            if (!review.IsConfirmation)
            {
                string orderWord = review.Orders.Count == 1
                    ? "ORDER"
                    : "ORDERS";
                placeOrdersButton.text =
                    $"PLACE {review.Orders.Count} {orderWord} · "
                    + FormatMoney(review.GrandTotalCents);
            }
        }

        private static VisualElement BuildReviewOrderCard(
            PurchasingReviewOrderItem item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("order-review-card");
            card.style.borderTopColor = item.AccentColor;

            VisualElement header = new VisualElement();
            header.AddToClassList("order-review-card__header");
            card.Add(header);

            VisualElement identity = new VisualElement();
            identity.AddToClassList("order-review-card__identity");
            header.Add(identity);

            Label number = new Label(
                item.OrderNumber.HasValue
                    ? $"PO-{item.OrderNumber.Value:0000}"
                    : "DRAFT PURCHASE ORDER");
            number.AddToClassList("order-review-card__number");
            identity.Add(number);

            Label supplier = new Label(item.SupplierName);
            supplier.AddToClassList("order-review-card__supplier");
            identity.Add(supplier);

            VisualElement arrivalGroup = new VisualElement();
            arrivalGroup.AddToClassList("order-review-card__arrival-group");
            header.Add(arrivalGroup);

            Label arrivalLabel = new Label("EXPECTED ARRIVAL");
            arrivalLabel.AddToClassList("order-review-card__arrival-label");
            arrivalGroup.Add(arrivalLabel);

            Label arrival = new Label(item.ArrivalSummary);
            arrival.AddToClassList("order-review-card__arrival");
            arrivalGroup.Add(arrival);

            Label total = new Label(FormatMoney(item.TotalCents));
            total.AddToClassList("order-review-card__total");
            header.Add(total);

            VisualElement lines = new VisualElement();
            lines.AddToClassList("order-review-card__lines");
            card.Add(lines);

            for (int index = 0; index < item.Lines.Count; index++)
            {
                PurchasingDraftLineItem line = item.Lines[index];
                VisualElement row = new VisualElement();
                row.AddToClassList("order-review-card__line");

                string caseWord = line.PurchasePackCount == 1
                    ? "case"
                    : "cases";
                Label name =
                    new Label(
                        $"{line.ProductName} · "
                        + $"{line.PurchasePackCount} {caseWord}");
                name.AddToClassList("order-review-card__line-name");
                row.Add(name);

                Label lineTotal = new Label(FormatMoney(line.LineTotalCents));
                lineTotal.AddToClassList("order-review-card__line-total");
                row.Add(lineTotal);
                lines.Add(row);
            }

            Label validation = new Label(item.ValidationSummary);
            validation.AddToClassList("order-review-card__validation");
            validation.EnableInClassList("is-valid", item.IsValid);
            card.Add(validation);
            return card;
        }

        private static VisualElement MetricLabel(
            string heading,
            string value)
        {
            VisualElement group = new VisualElement();

            Label headingLabel = new Label(heading);
            headingLabel.AddToClassList("selected-offer__metric-heading");
            group.Add(headingLabel);

            Label valueLabel = new Label(value);
            valueLabel.AddToClassList("selected-offer__metric-value");
            group.Add(valueLabel);
            return group;
        }

        private static Label BuildTag(string value)
        {
            Label tag = new Label(value.ToUpperInvariant());
            tag.AddToClassList("product-tag");
            return tag;
        }

        private static PurchasingOfferItem FindSelectedOffer(
            IReadOnlyList<PurchasingOfferItem> offers)
        {
            for (int index = 0; index < offers.Count; index++)
            {
                if (offers[index].IsSelected)
                {
                    return offers[index];
                }
            }

            return offers[0];
        }

        private static string GetInitials(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "?";
            }

            string[] words =
                value.Split(
                    new[] { ' ', '-', '/' },
                    StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 1)
            {
                return words[0].Substring(
                    0,
                    Math.Min(2, words[0].Length)).ToUpperInvariant();
            }

            return string.Concat(
                words[0][0],
                words[1][0]).ToUpperInvariant();
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100m:0.00}";
        }

        private static string FormatUnitMoney(decimal cents)
        {
            return $"${cents / 100m:0.00} / unit";
        }

        private void HandleSearchChanged(ChangeEvent<string> changeEvent)
        {
            SearchChanged?.Invoke(changeEvent.newValue ?? string.Empty);
        }

        private void HandleReviewRequested()
        {
            ReviewRequested?.Invoke();
        }

        private void HandleReviewBackRequested()
        {
            ReviewBackRequested?.Invoke();
        }

        private void HandlePlaceOrdersRequested()
        {
            PlaceOrdersRequested?.Invoke();
        }

        private void HandleConfirmationCloseRequested()
        {
            ConfirmationCloseRequested?.Invoke();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void BindButton(Button button, Action clickHandler)
        {
            button.clicked += clickHandler;
            buttonBindings.Add(new ButtonBinding(button, clickHandler));
        }

        private void ClearDynamicContent()
        {
            for (int index = 0; index < buttonBindings.Count; index++)
            {
                ButtonBinding binding = buttonBindings[index];
                binding.Button.clicked -= binding.ClickHandler;
            }

            buttonBindings.Clear();
            categoryFilters.Clear();
            supplierFilters.Clear();
            productList.Clear();
            draftList.Clear();
            reviewList.Clear();
        }

        private static T Require<T>(VisualElement root, string elementName)
            where T : VisualElement
        {
            T element = root.Q<T>(elementName);

            if (element != null)
            {
                return element;
            }

            throw new InvalidOperationException(
                $"Purchasing workspace is missing required element "
                + $"'{elementName}'.");
        }


        private sealed class ButtonBinding
        {
            public ButtonBinding(Button button, Action clickHandler)
            {
                Button = button;
                ClickHandler = clickHandler;
            }

            public Button Button { get; }
            public Action ClickHandler { get; }
        }
    }
}
