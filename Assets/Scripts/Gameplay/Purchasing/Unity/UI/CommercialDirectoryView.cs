using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Read-only presentation for the opening Brand and Supplier catalogs.
    /// </summary>
    public sealed class CommercialDirectoryView : IDisposable
    {
        private const string SelectedClassName = "is-selected";

        private readonly Button brandsButton;
        private readonly Button suppliersButton;
        private readonly Label sectionEyebrow;
        private readonly Label sectionTitle;
        private readonly Label sectionDescription;
        private readonly Label sectionCount;
        private readonly VisualElement content;
        private readonly Label errorState;

        private bool isDisposed;


        public CommercialDirectoryView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            brandsButton = Require<Button>(root, "directory-brands-button");
            suppliersButton = Require<Button>(root, "directory-suppliers-button");
            sectionEyebrow = Require<Label>(root, "directory-section-eyebrow");
            sectionTitle = Require<Label>(root, "directory-section-title");
            sectionDescription = Require<Label>(
                root,
                "directory-section-description");
            sectionCount = Require<Label>(root, "directory-section-count");
            content = Require<VisualElement>(root, "directory-content");
            errorState = Require<Label>(root, "directory-error-state");

            brandsButton.clicked += HandleBrandsClicked;
            suppliersButton.clicked += HandleSuppliersClicked;
        }


        public event Action<CommercialDirectorySection> SectionRequested;


        public void SetModel(CommercialDirectoryModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            errorState.style.display = DisplayStyle.None;
            content.Clear();

            brandsButton.text = $"BRANDS  {model.Brands.Count}";
            suppliersButton.text = $"SUPPLIERS  {model.Suppliers.Count}";
            brandsButton.EnableInClassList(
                SelectedClassName,
                model.SelectedSection == CommercialDirectorySection.Brands);
            suppliersButton.EnableInClassList(
                SelectedClassName,
                model.SelectedSection == CommercialDirectorySection.Suppliers);

            if (model.SelectedSection == CommercialDirectorySection.Brands)
            {
                BuildBrands(model.Brands);
            }
            else
            {
                BuildSuppliers(model.Suppliers);
            }
        }

        public void ShowError(string message)
        {
            content.Clear();
            sectionEyebrow.text = "CATALOG UNAVAILABLE";
            sectionTitle.text = "Commercial directory";
            sectionDescription.text = string.Empty;
            sectionCount.text = "—";
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
                brandsButton.clicked -= HandleBrandsClicked;
                suppliersButton.clicked -= HandleSuppliersClicked;
                content.Clear();
            }
            catch (InvalidOperationException)
            {
                // PanelRenderer may release the tree before its reload callback.
            }
            finally
            {
                isDisposed = true;
            }
        }


        private void BuildBrands(IReadOnlyList<CommercialBrandItem> brands)
        {
            sectionEyebrow.text = "CONSUMER WORLD";
            sectionTitle.text = "Opening brands";
            sectionDescription.text =
                "The names customers recognize on the shelf. Brands are separate from the companies that supply them.";
            sectionCount.text = brands.Count == 1
                ? "1 BRAND"
                : $"{brands.Count} BRANDS";
            content.AddToClassList("showing-brands");
            content.RemoveFromClassList("showing-suppliers");

            for (int index = 0; index < brands.Count; index++)
            {
                content.Add(BuildBrandCard(brands[index]));
            }
        }

        private void BuildSuppliers(
            IReadOnlyList<CommercialSupplierItem> suppliers)
        {
            sectionEyebrow.text = "OUTSIDE ECONOMY";
            sectionTitle.text = "Opening suppliers";
            sectionDescription.text =
                "The companies that sell merchandise to the store, each with its own assortment and operating terms.";
            sectionCount.text = suppliers.Count == 1
                ? "1 SUPPLIER"
                : $"{suppliers.Count} SUPPLIERS";
            content.AddToClassList("showing-suppliers");
            content.RemoveFromClassList("showing-brands");

            for (int index = 0; index < suppliers.Count; index++)
            {
                content.Add(BuildSupplierCard(suppliers[index]));
            }
        }

        private static VisualElement BuildBrandCard(CommercialBrandItem item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("directory-card");
            card.AddToClassList("brand-card");
            card.style.borderTopColor = item.AccentColor;

            VisualElement header = new VisualElement();
            header.AddToClassList("directory-card__header");
            card.Add(header);

            header.Add(BuildMark(
                item.DisplayName,
                item.Logo,
                item.AccentColor,
                "brand-card__mark"));

            VisualElement titleGroup = new VisualElement();
            titleGroup.AddToClassList("directory-card__title-group");
            header.Add(titleGroup);

            Label title = new Label(item.DisplayName);
            title.AddToClassList("directory-card__title");
            titleGroup.Add(title);

            Label count = new Label(
                item.ProductNames.Count == 1
                    ? "1 OPENING PRODUCT"
                    : $"{item.ProductNames.Count} OPENING PRODUCTS");
            count.AddToClassList("directory-card__count");
            titleGroup.Add(count);

            Label identity = new Label(item.Identity);
            identity.AddToClassList("brand-card__identity");
            card.Add(identity);

            card.Add(BuildProductList(item.ProductNames));
            return card;
        }

        private static VisualElement BuildSupplierCard(
            CommercialSupplierItem item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("directory-card");
            card.AddToClassList("supplier-card");
            card.style.borderTopColor = item.AccentColor;

            VisualElement header = new VisualElement();
            header.AddToClassList("directory-card__header");
            card.Add(header);

            header.Add(BuildMark(
                item.DisplayName,
                item.Logo,
                item.AccentColor,
                "supplier-card__mark"));

            VisualElement titleGroup = new VisualElement();
            titleGroup.AddToClassList("directory-card__title-group");
            header.Add(titleGroup);

            Label title = new Label(item.DisplayName);
            title.AddToClassList("directory-card__title");
            titleGroup.Add(title);

            Label specialty = new Label(item.Specialty.ToUpperInvariant());
            specialty.AddToClassList("supplier-card__specialty");
            titleGroup.Add(specialty);

            Label description = new Label(item.Description);
            description.AddToClassList("supplier-card__description");
            card.Add(description);

            VisualElement terms = new VisualElement();
            terms.AddToClassList("supplier-card__terms");
            terms.Add(BuildTerm("DELIVERY", item.DeliverySummary));
            terms.Add(BuildTerm(
                "ORDER MINIMUM",
                item.MinimumOrderCents == 0
                    ? "None"
                    : FormatMoney(item.MinimumOrderCents)));
            terms.Add(BuildTerm(
                "ASSORTMENT",
                $"{item.ProductNames.Count} of 12 products"));
            card.Add(terms);

            card.Add(BuildProductList(item.ProductNames));
            return card;
        }

        private static VisualElement BuildMark(
            string displayName,
            Sprite logo,
            Color accentColor,
            string additionalClass)
        {
            VisualElement mark = new VisualElement();
            mark.AddToClassList("directory-card__mark");
            mark.AddToClassList(additionalClass);
            mark.style.backgroundColor = accentColor;

            if (logo != null)
            {
                mark.style.backgroundImage = new StyleBackground(logo);
                mark.AddToClassList("has-image");
            }
            else
            {
                Label initials = new Label(GetInitials(displayName));
                initials.AddToClassList("directory-card__initials");
                initials.pickingMode = PickingMode.Ignore;
                mark.Add(initials);
            }

            return mark;
        }

        private static VisualElement BuildTerm(string heading, string value)
        {
            VisualElement term = new VisualElement();
            term.AddToClassList("supplier-term");

            Label headingLabel = new Label(heading);
            headingLabel.AddToClassList("supplier-term__heading");
            term.Add(headingLabel);

            Label valueLabel = new Label(value);
            valueLabel.AddToClassList("supplier-term__value");
            term.Add(valueLabel);
            return term;
        }

        private static VisualElement BuildProductList(
            IReadOnlyList<string> productNames)
        {
            VisualElement group = new VisualElement();
            group.AddToClassList("directory-product-group");

            Label heading = new Label("OPENING ASSORTMENT");
            heading.AddToClassList("directory-product-group__heading");
            group.Add(heading);

            VisualElement products = new VisualElement();
            products.AddToClassList("directory-product-list");
            group.Add(products);

            for (int index = 0; index < productNames.Count; index++)
            {
                Label product = new Label(productNames[index]);
                product.AddToClassList("directory-product-pill");
                products.Add(product);
            }

            return group;
        }

        private static string GetInitials(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "?";
            }

            string[] words = value.Split(
                new[] { ' ', '-', '/' },
                StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 1)
            {
                return words[0].Substring(
                    0,
                    Math.Min(2, words[0].Length)).ToUpperInvariant();
            }

            return string.Concat(words[0][0], words[1][0]).ToUpperInvariant();
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100m:0.00}";
        }

        private void HandleBrandsClicked()
        {
            SectionRequested?.Invoke(CommercialDirectorySection.Brands);
        }

        private void HandleSuppliersClicked()
        {
            SectionRequested?.Invoke(CommercialDirectorySection.Suppliers);
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
                $"Commercial directory is missing required element '{elementName}'.");
        }
    }
}
