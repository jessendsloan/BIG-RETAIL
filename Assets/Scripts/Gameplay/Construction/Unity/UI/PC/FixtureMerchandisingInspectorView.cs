using System;
using System.Collections.Generic;
using System.Globalization;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Merchandise.Domain;
using UnityEngine;
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
        private const float PreferredStoragePanelHeight = 680f;
        private const float StoragePanelVerticalMargin = 36f;

        private readonly VisualElement root;
        private readonly VisualElement panel;
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Label planogramValueLabel;
        private readonly Label productsValueLabel;
        private readonly Label shelfStockValueLabel;
        private readonly Label backstockValueLabel;
        private readonly Label salesTodayValueLabel;
        private readonly Label restockStatusValueLabel;
        private readonly Label shelfLabel;
        private readonly Label widthLabel;
        private readonly VisualElement overviewControls;
        private readonly VisualElement storageOverview;
        private readonly VisualElement storageContents;
        private readonly VisualElement editingControls;
        private readonly VisualElement frontageControls;
        private readonly VisualElement productContainer;
        private readonly Button editButton;
        private readonly Button restockButton;
        private readonly Button autoRestockButton;
        private readonly Button doneButton;
        private readonly Button closeButton;
        private readonly Button widthDecreaseButton;
        private readonly Button widthIncreaseButton;
        private readonly Button clearButton;
        private readonly Label storageRackCapacityValueLabel;
        private readonly Label storageTotalCapacityValueLabel;
        private readonly Label storageStoredValueLabel;
        private readonly Label storageUnallocatedValueLabel;
        private readonly Label storageFreeSpaceValueLabel;
        private readonly Label storageStatusValueLabel;
        private readonly VisualElement purchasingProductContainer;
        private readonly Label purchasingPendingValueLabel;
        private readonly Label purchasingCashValueLabel;
        private readonly Button receiveDeliveryButton;
        private readonly Label purchasingStatusLabel;
        private readonly List<ProductButtonBinding> productBindings =
            new List<ProductButtonBinding>();
        private readonly List<PurchaseButtonBinding> purchaseBindings =
            new List<PurchaseButtonBinding>();

        private bool isDisposed;
        private bool isStorageMode;
        private float rootHeight;


        public FixtureMerchandisingInspectorView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            this.root = root;
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
            salesTodayValueLabel = Require<Label>(
                root,
                "fixture-merchandising-sales-today-value");
            restockStatusValueLabel = Require<Label>(
                root,
                "fixture-merchandising-restock-status-value");
            shelfLabel = Require<Label>(root, "fixture-merchandising-shelf");
            widthLabel = Require<Label>(root, "fixture-merchandising-width");
            overviewControls = Require<VisualElement>(
                root,
                "fixture-merchandising-overview");
            storageOverview = Require<VisualElement>(
                root,
                "fixture-storage-overview");
            storageContents = Require<VisualElement>(
                root,
                "fixture-storage-contents");
            editingControls = Require<VisualElement>(root, "fixture-merchandising-editing");
            frontageControls = Require<VisualElement>(root, "fixture-merchandising-frontage-controls");
            productContainer = Require<VisualElement>(root, "fixture-merchandising-products");
            editButton = Require<Button>(root, "fixture-merchandising-edit-button");
            restockButton = Require<Button>(
                root,
                "fixture-merchandising-restock-button");
            autoRestockButton = Require<Button>(
                root,
                "fixture-merchandising-auto-restock-button");
            doneButton = Require<Button>(root, "fixture-merchandising-done-button");
            closeButton = Require<Button>(root, "fixture-merchandising-close-button");
            widthDecreaseButton = Require<Button>(root, "fixture-merchandising-width-decrease");
            widthIncreaseButton = Require<Button>(root, "fixture-merchandising-width-increase");
            clearButton = Require<Button>(root, "fixture-merchandising-clear-button");
            storageRackCapacityValueLabel = Require<Label>(
                root,
                "fixture-storage-rack-capacity-value");
            storageTotalCapacityValueLabel = Require<Label>(
                root,
                "fixture-storage-total-capacity-value");
            storageStoredValueLabel = Require<Label>(
                root,
                "fixture-storage-stored-value");
            storageUnallocatedValueLabel = Require<Label>(
                root,
                "fixture-storage-unallocated-value");
            storageFreeSpaceValueLabel = Require<Label>(
                root,
                "fixture-storage-free-space-value");
            storageStatusValueLabel = Require<Label>(
                root,
                "fixture-storage-status-value");
            purchasingProductContainer = Require<VisualElement>(
                root,
                "fixture-purchasing-products");
            purchasingPendingValueLabel = Require<Label>(
                root,
                "fixture-purchasing-pending-value");
            purchasingCashValueLabel = Require<Label>(
                root,
                "fixture-purchasing-cash-value");
            receiveDeliveryButton = Require<Button>(
                root,
                "fixture-purchasing-receive-button");
            purchasingStatusLabel = Require<Label>(
                root,
                "fixture-purchasing-status");

            // Automation still advertises a future worker-job hookup. Manual
            // restocking is connected to physical display inventory.
            autoRestockButton.SetEnabled(false);

            editButton.clicked += HandleEditRequested;
            restockButton.clicked += HandleRestockRequested;
            doneButton.clicked += HandleDoneRequested;
            closeButton.clicked += HandleCloseRequested;
            widthDecreaseButton.clicked += HandleWidthDecreaseRequested;
            widthIncreaseButton.clicked += HandleWidthIncreaseRequested;
            clearButton.clicked += HandleClearRequested;
            receiveDeliveryButton.clicked += HandleReceiveDeliveryRequested;
            root.RegisterCallback<GeometryChangedEvent>(
                HandleRootGeometryChanged);
        }


        public event Action EditRequested;

        public event Action RestockRequested;

        public event Action DoneRequested;

        public event Action CloseRequested;

        public event Action<int> WidthDeltaRequested;

        public event Action<ProductId> ProductRequested;

        public event Action<ProductId> PurchaseCaseRequested;

        public event Action ReceiveDeliveryRequested;

        public event Action ClearRequested;


        public void SetVisible(bool isVisible)
        {
            panel.style.display =
                isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (isVisible)
            {
                RefreshPanelHeight();
            }
        }

        public void SetFixtureTitle(string fixtureName)
        {
            titleLabel.text = fixtureName ?? "Fixture";
        }

        public void SetStorageMode(bool isStorageFixture)
        {
            isStorageMode = isStorageFixture;
            storageOverview.style.display =
                isStorageFixture
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            RefreshPanelHeight();

            if (!isStorageFixture)
            {
                return;
            }

            overviewControls.style.display = DisplayStyle.None;
            editingControls.style.display = DisplayStyle.None;
            frontageControls.style.display = DisplayStyle.None;
            statusLabel.style.display = DisplayStyle.None;
        }

        public void SetStorageSummary(
            int rackCapacityUnitCount,
            int rackStoredUnitCount,
            int totalCapacityUnitCount,
            int storedUnitCount,
            int unallocatedUnitCount,
            int freeSpaceUnitCount,
            string status,
            bool isWarning)
        {
            storageRackCapacityValueLabel.text =
                $"{rackStoredUnitCount} / {rackCapacityUnitCount} units";

            storageTotalCapacityValueLabel.text =
                $"{totalCapacityUnitCount} units";

            storageStoredValueLabel.text =
                $"{storedUnitCount} / {totalCapacityUnitCount} units";

            storageUnallocatedValueLabel.text =
                $"{unallocatedUnitCount} units";

            storageFreeSpaceValueLabel.text =
                $"{freeSpaceUnitCount} units";

            storageStatusValueLabel.text =
                string.IsNullOrWhiteSpace(status)
                    ? "Unavailable"
                    : status;

            storageStatusValueLabel.EnableInClassList(
                "fixture-merchandising-inspector__storage-status--warning",
                isWarning);
        }

        public void SetStorageContents(
            IEnumerable<StorageContentRow> contents)
        {
            storageContents.Clear();

            bool hasContents = false;

            if (contents != null)
            {
                foreach (StorageContentRow content in contents)
                {
                    hasContents = true;

                    VisualElement row = new VisualElement();
                    row.AddToClassList(
                        "fixture-merchandising-inspector__storage-content-row");
                    row.style.borderLeftColor = content.Color;

                    Label name = new Label(content.ProductName);
                    name.AddToClassList(
                        "fixture-merchandising-inspector__storage-content-name");
                    row.Add(name);

                    Label quantity =
                        new Label($"{content.Quantity} units");
                    quantity.AddToClassList(
                        "fixture-merchandising-inspector__storage-content-quantity");
                    row.Add(quantity);

                    storageContents.Add(row);
                }
            }

            if (hasContents)
            {
                return;
            }

            Label empty = new Label("Empty");
            empty.AddToClassList(
                "fixture-merchandising-inspector__storage-empty");
            storageContents.Add(empty);
        }

        public void SetPurchasingProducts(
            IEnumerable<PurchaseProductRow> products)
        {
            ClearPurchaseButtons();

            if (products == null)
            {
                return;
            }

            foreach (PurchaseProductRow product in products)
            {
                Button button =
                    new Button
                    {
                        text = product.PendingUnitCount > 0
                            ? $"{product.ProductName} case · {FormatMoney(product.CaseCostCents)} ({product.PendingUnitCount} pending)"
                            : $"{product.ProductName} case · {FormatMoney(product.CaseCostCents)}",
                        tooltip =
                            $"Buy one {product.CaseUnitCount}-unit case of {product.ProductName} for {FormatMoney(product.CaseCostCents)}."
                    };

                button.AddToClassList(
                    "fixture-merchandising-inspector__purchase-button");
                button.style.borderLeftColor = product.Color;
                button.SetEnabled(product.CanAfford);

                ProductId productId = product.ProductId;
                Action clickHandler =
                    () => PurchaseCaseRequested?.Invoke(productId);

                button.clicked += clickHandler;
                purchasingProductContainer.Add(button);
                purchaseBindings.Add(
                    new PurchaseButtonBinding(button, clickHandler));
            }
        }

        public void SetPurchasingSummary(
            long cashBalanceCents,
            int pendingUnitCount,
            bool canReceive)
        {
            purchasingCashValueLabel.text =
                FormatMoney(cashBalanceCents);
            purchasingPendingValueLabel.text =
                pendingUnitCount > 0
                    ? $"Ready to receive: {pendingUnitCount} units"
                    : "Nothing ready to receive";
            receiveDeliveryButton.SetEnabled(canReceive);
        }

        public void SetPurchasingStatus(string status)
        {
            purchasingStatusLabel.text =
                string.IsNullOrWhiteSpace(status)
                    ? "Place purchase orders, then receive arrived deliveries here."
                    : status;
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

        public void SetSalesToday(long amountCents)
        {
            salesTodayValueLabel.text =
                FormatMoney(amountCents);
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
            doneButton.clicked -= HandleDoneRequested;
            closeButton.clicked -= HandleCloseRequested;
            widthDecreaseButton.clicked -= HandleWidthDecreaseRequested;
            widthIncreaseButton.clicked -= HandleWidthIncreaseRequested;
            clearButton.clicked -= HandleClearRequested;
            receiveDeliveryButton.clicked -= HandleReceiveDeliveryRequested;
            root.UnregisterCallback<GeometryChangedEvent>(
                HandleRootGeometryChanged);
            ClearProductButtons();
            ClearPurchaseButtons();
            isDisposed = true;
        }


        private void HandleRootGeometryChanged(
            GeometryChangedEvent geometryChangedEvent)
        {
            rootHeight = geometryChangedEvent.newRect.height;
            RefreshPanelHeight();
        }


        private void RefreshPanelHeight()
        {
            if (!isStorageMode)
            {
                panel.style.height = new StyleLength(StyleKeyword.Auto);
                return;
            }

            float availableRootHeight =
                rootHeight > 0f
                    ? rootHeight
                    : root.resolvedStyle.height;

            if (float.IsNaN(availableRootHeight)
                || availableRootHeight <= 0f)
            {
                panel.style.height = PreferredStoragePanelHeight;
                return;
            }

            float availablePanelHeight =
                Mathf.Max(
                    0f,
                    availableRootHeight - StoragePanelVerticalMargin);

            panel.style.height =
                Mathf.Min(
                    PreferredStoragePanelHeight,
                    availablePanelHeight);
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

        private void ClearPurchaseButtons()
        {
            for (int index = 0;
                 index < purchaseBindings.Count;
                 index++)
            {
                PurchaseButtonBinding binding = purchaseBindings[index];
                binding.Button.clicked -= binding.ClickHandler;
            }

            purchaseBindings.Clear();
            purchasingProductContainer.Clear();
        }

        private void HandleEditRequested()
        {
            EditRequested?.Invoke();
        }

        private void HandleRestockRequested()
        {
            RestockRequested?.Invoke();
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

        private void HandleReceiveDeliveryRequested()
        {
            ReceiveDeliveryRequested?.Invoke();
        }

        private static string FormatMoney(long amountCents)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "${0:N2}",
                amountCents / 100m);
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


        private sealed class PurchaseButtonBinding
        {
            public PurchaseButtonBinding(
                Button button,
                Action clickHandler)
            {
                Button = button;
                ClickHandler = clickHandler;
            }


            public Button Button { get; }

            public Action ClickHandler { get; }
        }
    }


    public readonly struct StorageContentRow
    {
        public StorageContentRow(
            string productName,
            int quantity,
            UnityEngine.Color color)
        {
            ProductName = productName;
            Quantity = quantity;
            Color = color;
        }


        public string ProductName { get; }

        public int Quantity { get; }

        public UnityEngine.Color Color { get; }
    }


    public readonly struct PurchaseProductRow
    {
        public PurchaseProductRow(
            ProductId productId,
            string productName,
            int caseUnitCount,
            long caseCostCents,
            int pendingUnitCount,
            bool canAfford,
            UnityEngine.Color color)
        {
            ProductId = productId;
            ProductName = productName;
            CaseUnitCount = caseUnitCount;
            CaseCostCents = caseCostCents;
            PendingUnitCount = pendingUnitCount;
            CanAfford = canAfford;
            Color = color;
        }


        public ProductId ProductId { get; }

        public string ProductName { get; }

        public int CaseUnitCount { get; }

        public long CaseCostCents { get; }

        public int PendingUnitCount { get; }

        public bool CanAfford { get; }

        public UnityEngine.Color Color { get; }
    }
}
