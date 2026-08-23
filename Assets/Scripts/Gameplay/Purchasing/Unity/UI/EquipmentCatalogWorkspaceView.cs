using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Presentation-only wrapper for the durable-equipment catalog.
    /// </summary>
    public sealed class EquipmentCatalogWorkspaceView : IDisposable
    {
        private const string SelectedClassName = "is-selected";

        private readonly VisualElement root;
        private readonly TextField searchField;
        private readonly VisualElement categoryFilters;
        private readonly Button requiredFilterButton;
        private readonly VisualElement equipmentList;
        private readonly Label equipmentCount;
        private readonly VisualElement draftList;
        private readonly Label draftEmptyState;
        private readonly Label draftTotal;
        private readonly Button addRequirementsButton;
        private readonly Button clearDraftButton;
        private readonly Button placeOrderButton;
        private readonly Label commercialTime;
        private readonly Label availableCash;
        private readonly Label scheduledShipments;
        private readonly Label stagedShipments;
        private readonly Label waitingShipments;
        private readonly Label statusMessage;
        private readonly Label emptyState;
        private readonly Label errorState;
        private readonly Button closeButton;
        private readonly List<ButtonBinding> dynamicBindings =
            new List<ButtonBinding>();

        private bool isDisposed;


        public EquipmentCatalogWorkspaceView(VisualElement root)
        {
            this.root = root
                ?? throw new ArgumentNullException(nameof(root));
            searchField = Require<TextField>(root, "equipment-search");
            categoryFilters =
                Require<VisualElement>(root, "equipment-category-filters");
            requiredFilterButton =
                Require<Button>(root, "equipment-required-filter");
            equipmentList =
                Require<VisualElement>(root, "equipment-list");
            equipmentCount = Require<Label>(root, "equipment-count");
            draftList =
                Require<VisualElement>(root, "equipment-draft-list");
            draftEmptyState =
                Require<Label>(root, "equipment-draft-empty");
            draftTotal = Require<Label>(root, "equipment-draft-total");
            addRequirementsButton =
                Require<Button>(root, "equipment-add-requirements");
            clearDraftButton =
                Require<Button>(root, "equipment-clear-draft");
            placeOrderButton =
                Require<Button>(root, "equipment-place-order");
            commercialTime = Require<Label>(root, "equipment-time");
            availableCash = Require<Label>(root, "equipment-cash");
            scheduledShipments =
                Require<Label>(root, "equipment-scheduled-shipments");
            stagedShipments =
                Require<Label>(root, "equipment-staged-shipments");
            waitingShipments =
                Require<Label>(root, "equipment-waiting-shipments");
            statusMessage = Require<Label>(root, "equipment-status");
            emptyState = Require<Label>(root, "equipment-empty-state");
            errorState = Require<Label>(root, "equipment-error-state");
            closeButton = Require<Button>(root, "close-equipment-button");

            searchField.RegisterValueChangedCallback(HandleSearchChanged);
            requiredFilterButton.clicked += HandleRequiredFilterRequested;
            addRequirementsButton.clicked += HandleAddRequirementsRequested;
            clearDraftButton.clicked += HandleClearDraftRequested;
            placeOrderButton.clicked += HandlePlaceOrderRequested;
            closeButton.clicked += HandleCloseRequested;
        }


        public event Action<string> SearchChanged;

        public event Action<string> CategoryRequested;

        public event Action RequiredFilterRequested;

        public event Action<string, int> QuantityDeltaRequested;

        public event Action<string> AddRequiredItemRequested;

        public event Action AddRequirementsRequested;

        public event Action ClearDraftRequested;

        public event Action PlaceOrderRequested;

        public event Action CloseRequested;


        public void SetVisible(bool isVisible)
        {
            if (!isVisible)
            {
                Focusable focused =
                    root.panel?.focusController?.focusedElement;

                if (focused is VisualElement focusedVisual
                    && root.Contains(focusedVisual))
                {
                    focused.Blur();
                }
            }

            root.style.display = isVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void SetModel(EquipmentCatalogWorkspaceModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            errorState.style.display = DisplayStyle.None;
            ClearDynamicContent();
            BuildCategoryFilters(model);
            BuildEquipmentCards(model.Equipment);
            BuildDraft(model.DraftLines);

            requiredFilterButton.EnableInClassList(
                SelectedClassName,
                model.RequiredOnly);
            equipmentCount.text = model.Equipment.Count == 1
                ? "1 fixture"
                : $"{model.Equipment.Count} fixtures";
            emptyState.style.display = model.Equipment.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            draftEmptyState.style.display = model.DraftLines.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            draftTotal.text = FormatMoney(model.DraftTotalCents);
            commercialTime.text = model.CurrentTimeSummary;
            availableCash.text =
                $"AVAILABLE CASH · {FormatMoney(model.AvailableCashCents)}";
            scheduledShipments.text =
                $"IN TRANSIT  {model.ScheduledShipmentCount}";
            stagedShipments.text =
                $"STAGED IN RECEIVING  {model.StagedShipmentCount}";
            waitingShipments.text =
                $"WAITING FOR SPACE  {model.WaitingForReceivingCount}";
            statusMessage.text = model.StatusMessage;
            addRequirementsButton.SetEnabled(model.HasRequiredEquipment);
            clearDraftButton.SetEnabled(model.DraftLines.Count > 0);
            placeOrderButton.SetEnabled(model.CanPlaceOrder);
            placeOrderButton.text = model.DraftLines.Count > 0
                ? $"PLACE BIG ORDER · "
                    + FormatMoney(model.DraftTotalCents)
                : "PLACE BIG ORDER";
            placeOrderButton.tooltip = model.DraftLines.Count == 0
                ? "Add at least one fixture module to the order."
                : model.DraftTotalCents > model.AvailableCashCents
                    ? "The store does not have enough cash for this order."
                    : "Pay now and schedule this equipment shipment.";
        }

        public void ShowError(string message)
        {
            ClearDynamicContent();
            equipmentCount.text = "Catalog unavailable";
            draftTotal.text = "$0.00";
            addRequirementsButton.SetEnabled(false);
            clearDraftButton.SetEnabled(false);
            placeOrderButton.SetEnabled(false);
            emptyState.style.display = DisplayStyle.None;
            draftEmptyState.style.display = DisplayStyle.None;
            errorState.text = string.IsNullOrWhiteSpace(message)
                ? "The Equipment Catalog could not be loaded."
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
                searchField.UnregisterValueChangedCallback(
                    HandleSearchChanged);
                requiredFilterButton.clicked -=
                    HandleRequiredFilterRequested;
                addRequirementsButton.clicked -=
                    HandleAddRequirementsRequested;
                clearDraftButton.clicked -= HandleClearDraftRequested;
                placeOrderButton.clicked -= HandlePlaceOrderRequested;
                closeButton.clicked -= HandleCloseRequested;
                ClearDynamicContent();
            }
            catch (InvalidOperationException)
            {
                dynamicBindings.Clear();
            }
            finally
            {
                isDisposed = true;
            }
        }


        private void BuildCategoryFilters(
            EquipmentCatalogWorkspaceModel model)
        {
            int allCount = 0;

            for (int index = 0; index < model.Categories.Count; index++)
            {
                allCount += model.Categories[index].ItemCount;
            }

            AddCategoryButton(
                "All equipment",
                string.Empty,
                allCount,
                string.IsNullOrEmpty(model.SelectedCategory));

            for (int index = 0; index < model.Categories.Count; index++)
            {
                EquipmentCatalogFilterItem item = model.Categories[index];
                AddCategoryButton(
                    item.Name,
                    item.Name,
                    item.ItemCount,
                    string.Equals(
                        item.Name,
                        model.SelectedCategory,
                        StringComparison.Ordinal));
            }
        }

        private void AddCategoryButton(
            string label,
            string category,
            int count,
            bool isSelected)
        {
            Button button = new Button { text = label };
            button.AddToClassList("equipment-filter-button");
            button.EnableInClassList(SelectedClassName, isSelected);

            Label countLabel = new Label(count.ToString())
            {
                pickingMode = PickingMode.Ignore
            };
            countLabel.AddToClassList("equipment-filter-button__count");
            button.Add(countLabel);
            BindButton(
                button,
                () => CategoryRequested?.Invoke(category));
            categoryFilters.Add(button);
        }

        private void BuildEquipmentCards(
            IReadOnlyList<EquipmentCatalogItem> items)
        {
            for (int index = 0; index < items.Count; index++)
            {
                equipmentList.Add(BuildEquipmentCard(items[index]));
            }
        }

        private VisualElement BuildEquipmentCard(EquipmentCatalogItem item)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("equipment-card");

            VisualElement identity = new VisualElement();
            identity.AddToClassList("equipment-card__identity");
            card.Add(identity);

            VisualElement art = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            art.AddToClassList("equipment-card__art");

            if (item.Icon != null)
            {
                art.style.backgroundImage = new StyleBackground(item.Icon);
                art.AddToClassList("has-image");
            }
            else
            {
                Label initials = new Label(GetInitials(item.DisplayName))
                {
                    pickingMode = PickingMode.Ignore
                };
                initials.AddToClassList("equipment-card__initials");
                art.Add(initials);
            }

            identity.Add(art);

            VisualElement copy = new VisualElement();
            copy.AddToClassList("equipment-card__copy");
            identity.Add(copy);

            Label category = new Label(item.CategoryName.ToUpperInvariant());
            category.AddToClassList("equipment-card__category");
            copy.Add(category);

            Label name = new Label(item.DisplayName);
            name.AddToClassList("equipment-card__name");
            copy.Add(name);

            Label price = new Label(
                $"{FixtureEquipmentOrder.ExclusiveSupplierDisplayName.ToUpperInvariant()}  ·  "
                + $"{FormatMoney(item.UnitPriceCents)} EACH  ·  "
                + item.DeliverySummary);
            price.AddToClassList("equipment-card__price");
            copy.Add(price);

            VisualElement stats = new VisualElement();
            stats.AddToClassList("equipment-card__stats");
            stats.Add(BuildStat("OWNED", item.OwnedQuantity));
            stats.Add(BuildStat("PLANNED", item.PlannedQuantity));
            stats.Add(BuildStat("ON ORDER", item.OutstandingQuantity));
            copy.Add(stats);

            if (item.RequiredQuantity > 0)
            {
                Label need = new Label(
                    $"PLAN NEEDS {item.RequiredQuantity} MORE");
                need.AddToClassList("equipment-card__need");
                copy.Add(need);
            }

            VisualElement order = new VisualElement();
            order.AddToClassList("equipment-card__order");
            card.Add(order);

            Button remove = BuildQuantityButton("−", item, -1);
            remove.SetEnabled(item.DraftQuantity > 0);
            order.Add(remove);

            Label quantity = new Label($"{item.DraftQuantity} TO ORDER");
            quantity.AddToClassList("equipment-card__quantity");
            order.Add(quantity);

            order.Add(BuildQuantityButton("+", item, 1));

            Button addNeeded = new Button
            {
                text = item.RequiredQuantity > 0
                    ? $"ADD {item.RequiredQuantity} NEEDED"
                    : "PLAN COVERED"
            };
            addNeeded.AddToClassList("equipment-card__needed-button");
            addNeeded.SetEnabled(
                item.RequiredQuantity > item.DraftQuantity);
            BindButton(
                addNeeded,
                () => AddRequiredItemRequested?.Invoke(item.DefinitionId));
            order.Add(addNeeded);
            return card;
        }

        private Button BuildQuantityButton(
            string text,
            EquipmentCatalogItem item,
            int delta)
        {
            Button button = new Button { text = text };
            button.AddToClassList("equipment-card__quantity-button");
            BindButton(
                button,
                () => QuantityDeltaRequested?.Invoke(
                    item.DefinitionId,
                    delta));
            return button;
        }

        private static VisualElement BuildStat(string label, int value)
        {
            VisualElement stat = new VisualElement();
            stat.AddToClassList("equipment-card__stat");
            Label heading = new Label(label);
            heading.AddToClassList("equipment-card__stat-heading");
            stat.Add(heading);
            Label count = new Label(value.ToString());
            count.AddToClassList("equipment-card__stat-value");
            stat.Add(count);
            return stat;
        }

        private void BuildDraft(
            IReadOnlyList<EquipmentDraftLineItem> lines)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                EquipmentDraftLineItem item = lines[index];
                VisualElement line = new VisualElement();
                line.AddToClassList("equipment-draft-line");

                VisualElement copy = new VisualElement();
                copy.AddToClassList("equipment-draft-line__copy");
                line.Add(copy);
                Label name = new Label(item.DisplayName);
                name.AddToClassList("equipment-draft-line__name");
                copy.Add(name);
                Label detail = new Label(
                    $"{item.Quantity} × {item.DeliverySummary}");
                detail.AddToClassList("equipment-draft-line__detail");
                copy.Add(detail);

                Label total = new Label(FormatMoney(item.LineTotalCents));
                total.AddToClassList("equipment-draft-line__total");
                line.Add(total);

                Button remove = new Button { text = "REMOVE" };
                remove.AddToClassList("equipment-draft-line__remove");
                BindButton(
                    remove,
                    () => QuantityDeltaRequested?.Invoke(
                        item.DefinitionId,
                        -item.Quantity));
                line.Add(remove);
                draftList.Add(line);
            }
        }

        private void ClearDynamicContent()
        {
            for (int index = 0; index < dynamicBindings.Count; index++)
            {
                ButtonBinding binding = dynamicBindings[index];
                binding.Button.clicked -= binding.Handler;
            }

            dynamicBindings.Clear();
            categoryFilters.Clear();
            equipmentList.Clear();
            draftList.Clear();
        }

        private void BindButton(Button button, Action handler)
        {
            button.clicked += handler;
            dynamicBindings.Add(new ButtonBinding(button, handler));
        }

        private void HandleSearchChanged(ChangeEvent<string> change)
        {
            SearchChanged?.Invoke(change.newValue ?? string.Empty);
        }

        private void HandleRequiredFilterRequested()
        {
            RequiredFilterRequested?.Invoke();
        }

        private void HandleAddRequirementsRequested()
        {
            AddRequirementsRequested?.Invoke();
        }

        private void HandleClearDraftRequested()
        {
            ClearDraftRequested?.Invoke();
        }

        private void HandlePlaceOrderRequested()
        {
            PlaceOrderRequested?.Invoke();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private static T Require<T>(
            VisualElement root,
            string elementName)
            where T : VisualElement
        {
            return root.Q<T>(elementName)
                ?? throw new InvalidOperationException(
                    $"Equipment Catalog is missing '{elementName}'.");
        }

        private static string GetInitials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "EQ";
            }

            string[] words = displayName.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            return words.Length == 1
                ? words[0].Substring(0, Math.Min(2, words[0].Length))
                    .ToUpperInvariant()
                : $"{words[0][0]}{words[1][0]}".ToUpperInvariant();
        }

        private static string FormatMoney(long cents)
        {
            return $"${cents / 100:N0}.{Math.Abs(cents % 100):00}";
        }


        private sealed class ButtonBinding
        {
            public Button Button { get; }

            public Action Handler { get; }


            public ButtonBinding(Button button, Action handler)
            {
                Button = button;
                Handler = handler;
            }
        }
    }
}
