using System;
using System.Collections.Generic;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Presentation-only fixture inspector and merchandising controls.
    /// </summary>
    public sealed class FixtureMerchandisingInspectorView : IDisposable
    {
        public const string PanelName = "fixture-merchandising-inspector";
        private const string ProductSelectedClassName = "is-selected";

        private readonly VisualElement panel;
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Label planogramValueLabel;
        private readonly Label productsValueLabel;
        private readonly Label shelfStockValueLabel;
        private readonly Label backstockValueLabel;
        private readonly Label restockStatusValueLabel;
        private readonly Label shelfLabel;
        private readonly Label widthLabel;
        private readonly VisualElement overviewControls;
        private readonly VisualElement editingControls;
        private readonly VisualElement frontageControls;
        private readonly VisualElement productContainer;
        private readonly Button editButton;
        private readonly Button restockButton;
        private readonly Button debugSaleButton;
        private readonly Button autoRestockButton;
        private readonly Button doneButton;
        private readonly Button closeButton;
        private readonly Button widthDecreaseButton;
        private readonly Button widthIncreaseButton;
        private readonly Button clearButton;
        private readonly List<ProductButtonBinding> productBindings =
            new List<ProductButtonBinding>();

        private bool isDisposed;


        public FixtureMerchandisingInspectorView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            panel = Require<VisualElement>(root, PanelName);
            titleLabel = Require<Label>(root, "fixture-merchandising-title");
            statusLabel = Require<Label>(root, "fixture-merchandising-status");
            planogramValueLabel = Require<Label>(
                root,
                "fixture-merchandising-planogram-value");
            productsValueLabel = Require<Label>(
                root,
                "fixture-merchandising-products-value");
            shelfStockValueLabel = Require<Label>(
                root,
                "fixture-merchandising-shelf-stock-value");
            backstockValueLabel = Require<Label>(
                root,
                "fixture-merchandising-backstock-value");
            restockStatusValueLabel = Require<Label>(
                root,
                "fixture-merchandising-restock-status-value");
            shelfLabel = Require<Label>(root, "fixture-merchandising-shelf");
            widthLabel = Require<Label>(root, "fixture-merchandising-width");
            overviewControls = Require<VisualElement>(
                root,
                "fixture-merchandising-overview");
            editingControls = Require<VisualElement>(root, "fixture-merchandising-editing");
            frontageControls = Require<VisualElement>(root, "fixture-merchandising-frontage-controls");
            productContainer = Require<VisualElement>(root, "fixture-merchandising-products");
            editButton = Require<Button>(root, "fixture-merchandising-edit-button");
            restockButton = Require<Button>(
                root,
                "fixture-merchandising-restock-button");
            debugSaleButton = Require<Button>(
                root,
                "fixture-merchandising-debug-sale-button");
            autoRestockButton = Require<Button>(
                root,
                "fixture-merchandising-auto-restock-button");
            doneButton = Require<Button>(root, "fixture-merchandising-done-button");
            closeButton = Require<Button>(root, "fixture-merchandising-close-button");
            widthDecreaseButton = Require<Button>(root, "fixture-merchandising-width-decrease");
            widthIncreaseButton = Require<Button>(root, "fixture-merchandising-width-increase");
            clearButton = Require<Button>(root, "fixture-merchandising-clear-button");

            // Automation still advertises a future worker-job hookup. Manual
            // restocking is connected to physical display inventory.
            autoRestockButton.SetEnabled(false);

            editButton.clicked += HandleEditRequested;
            restockButton.clicked += HandleRestockRequested;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugSaleButton.style.display = DisplayStyle.Flex;
            debugSaleButton.clicked += HandleDebugSaleRequested;
#else
            debugSaleButton.style.display = DisplayStyle.None;
#endif
            doneButton.clicked += HandleDoneRequested;
            closeButton.clicked += HandleCloseRequested;
            widthDecreaseButton.clicked += HandleWidthDecreaseRequested;
            widthIncreaseButton.clicked += HandleWidthIncreaseRequested;
            clearButton.clicked += HandleClearRequested;
        }


        public event Action EditRequested;

        public event Action RestockRequested;

        public event Action DebugSaleRequested;

        public event Action DoneRequested;

        public event Action CloseRequested;

        public event Action<int> WidthDeltaRequested;

        public event Action<ProductId> ProductRequested;

        public event Action ClearRequested;


        public void SetVisible(bool isVisible)
        {
            panel.style.display =
                isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetFixtureTitle(string fixtureName)
        {
            titleLabel.text = fixtureName ?? "Fixture";
        }

        public void SetPlanogramSummary(
            int assignedFrontageUnitCount,
            int totalFrontageUnitCount,
            int assignedProductCount)
        {
            planogramValueLabel.text =
                $"{assignedFrontageUnitCount} / {totalFrontageUnitCount} assigned";

            productsValueLabel.text =
                assignedProductCount == 1
                    ? "1 product"
                    : $"{assignedProductCount} products";
        }

        public void SetInventorySummary(
            int stockedUnitCount,
            int capacityUnitCount,
            int backstockUnitCount,
            bool canRestock)
        {
            shelfStockValueLabel.text =
                $"{stockedUnitCount} / {capacityUnitCount} units";

            backstockValueLabel.text =
                $"{backstockUnitCount} units available";

            restockButton.SetEnabled(canRestock);
        }

        public void SetRestockStatus(string status)
        {
            restockStatusValueLabel.text =
                string.IsNullOrWhiteSpace(status)
                    ? "Awaiting planogram"
                    : status;
        }

        public void SetEditing(bool isEditing)
        {
            overviewControls.style.display =
                isEditing ? DisplayStyle.None : DisplayStyle.Flex;

            statusLabel.style.display =
                isEditing ? DisplayStyle.Flex : DisplayStyle.None;

            editButton.style.display =
                isEditing ? DisplayStyle.None : DisplayStyle.Flex;

            doneButton.style.display =
                isEditing ? DisplayStyle.Flex : DisplayStyle.None;

            editingControls.style.display =
                isEditing ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetFrontageControlsVisible(bool isVisible)
        {
            frontageControls.style.display =
                isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetShelfLabel(string label)
        {
            shelfLabel.text = label ?? string.Empty;
        }

        public void SetStatus(string status, bool isError = false)
        {
            statusLabel.text = status ?? string.Empty;
            statusLabel.EnableInClassList(
                "fixture-merchandising-inspector__status--error",
                isError);
        }

        public void SetWidth(
            int frontageUnitCount,
            int maximumFrontageUnitCount)
        {
            widthLabel.text =
                $"Width: {frontageUnitCount} of {maximumFrontageUnitCount} slots";

            widthDecreaseButton.SetEnabled(frontageUnitCount > 1);
            widthIncreaseButton.SetEnabled(
                frontageUnitCount < maximumFrontageUnitCount);
        }

        public void SetProducts(
            IEnumerable<ProductDefinition> products)
        {
            ClearProductButtons();

            if (products == null)
            {
                return;
            }

            foreach (ProductDefinition product in products)
            {
                if (product == null)
                {
                    continue;
                }

                AddProductButton(product);
            }
        }

        public void SetSelectedProduct(ProductId productId)
        {
            for (int index = 0;
                 index < productBindings.Count;
                 index++)
            {
                ProductButtonBinding binding = productBindings[index];
                binding.Button.EnableInClassList(
                    ProductSelectedClassName,
                    productId.IsValid
                    && binding.ProductId == productId);
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            editButton.clicked -= HandleEditRequested;
            restockButton.clicked -= HandleRestockRequested;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugSaleButton.clicked -= HandleDebugSaleRequested;
#endif
            doneButton.clicked -= HandleDoneRequested;
            closeButton.clicked -= HandleCloseRequested;
            widthDecreaseButton.clicked -= HandleWidthDecreaseRequested;
            widthIncreaseButton.clicked -= HandleWidthIncreaseRequested;
            clearButton.clicked -= HandleClearRequested;
            ClearProductButtons();
            isDisposed = true;
        }


        private void AddProductButton(ProductDefinition product)
        {
            Button button =
                new Button
                {
                    text = product.DisplayName,
                    tooltip = $"Assign {product.DisplayName} to the selected shelf slot."
                };

            button.AddToClassList(
                "fixture-merchandising-inspector__product-button");

            button.style.borderLeftColor =
                FixtureMerchandisingGrayboxPalette
                    .ResolveProductColor(product.Id);

            Action clickHandler =
                () => ProductRequested?.Invoke(product.Id);

            button.clicked += clickHandler;
            productContainer.Add(button);
            productBindings.Add(
                new ProductButtonBinding(
                    product.Id,
                    button,
                    clickHandler));
        }

        private void ClearProductButtons()
        {
            for (int index = 0;
                 index < productBindings.Count;
                 index++)
            {
                ProductButtonBinding binding = productBindings[index];
                binding.Button.clicked -= binding.ClickHandler;
            }

            productBindings.Clear();
            productContainer.Clear();
        }

        private void HandleEditRequested()
        {
            EditRequested?.Invoke();
        }

        private void HandleRestockRequested()
        {
            RestockRequested?.Invoke();
        }

        private void HandleDebugSaleRequested()
        {
            DebugSaleRequested?.Invoke();
        }

        private void HandleDoneRequested()
        {
            DoneRequested?.Invoke();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleWidthDecreaseRequested()
        {
            WidthDeltaRequested?.Invoke(-1);
        }

        private void HandleWidthIncreaseRequested()
        {
            WidthDeltaRequested?.Invoke(1);
        }

        private void HandleClearRequested()
        {
            ClearRequested?.Invoke();
        }

        private static T Require<T>(
            VisualElement root,
            string elementName)
            where T : VisualElement
        {
            return root.Q<T>(elementName)
                ?? throw new InvalidOperationException(
                    $"Fixture merchandising inspector is missing '{elementName}'.");
        }


        private sealed class ProductButtonBinding
        {
            public ProductButtonBinding(
                ProductId productId,
                Button button,
                Action clickHandler)
            {
                ProductId = productId;
                Button = button;
                ClickHandler = clickHandler;
            }

            public ProductId ProductId { get; }

            public Button Button { get; }

            public Action ClickHandler { get; }
        }
    }
}
