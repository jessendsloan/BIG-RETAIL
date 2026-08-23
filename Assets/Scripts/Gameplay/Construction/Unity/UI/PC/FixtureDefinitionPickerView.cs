using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    public sealed class FixtureDefinitionPickerItem
    {
        public FixtureDefinitionPickerItem(
            string definitionId,
            string tooltip,
            Sprite icon)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new ArgumentException(
                    "A fixture picker item requires a definition identifier.",
                    nameof(definitionId));
            }

            DefinitionId = definitionId;
            Tooltip = tooltip ?? string.Empty;
            Icon = icon;
        }

        public string DefinitionId { get; }

        public string Tooltip { get; }

        public Sprite Icon { get; }
    }


    /// <summary>
    /// Presentation-only wrapper around the fixture catalog and rotate
    /// control in the PC construction toolbar.
    /// </summary>
    public sealed class FixtureDefinitionPickerView : IDisposable
    {
        public const string PanelName = "fixture-definition-picker";
        public const string ItemsContainerName =
            "fixture-definition-picker-items";
        public const string RotateButtonName = "fixture-rotate-button";
        public const string SelectedClassName = "is-selected";

        public const string EquipmentSelectedName =
            "fixture-equipment-selected";
        public const string EquipmentSummaryName =
            "fixture-equipment-summary";
        public const string PlanModeButtonName =
            "fixture-plan-mode-button";
        public const string OrderPlansButtonName =
            "fixture-order-plans-button";
        public const string InstallPlansButtonName =
            "fixture-install-plans-button";
        public const string EquipmentStatusName =
            "fixture-equipment-status";

        private const string ButtonClassName = "finish-picker__button";
        private const string IconClassName = "finish-picker__icon";
        private const string OwnedBadgeClassName =
            "fixture-equipment__owned-badge";

        private readonly VisualElement panel;
        private readonly VisualElement itemsContainer;
        private readonly Button rotateButton;
        private readonly Label equipmentSelected;
        private readonly Label equipmentSummary;
        private readonly Label equipmentStatus;
        private readonly Button planModeButton;
        private readonly Button orderPlansButton;
        private readonly Button installPlansButton;
        private readonly List<ButtonBinding> buttonBindings =
            new List<ButtonBinding>();

        private bool isDisposed;


        public FixtureDefinitionPickerView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            panel = RequireElement(root, PanelName);
            itemsContainer = RequireElement(root, ItemsContainerName);
            rotateButton =
                root.Q<Button>(RotateButtonName)
                ?? throw new InvalidOperationException(
                    $"Fixture picker is missing required button '{RotateButtonName}'.");

            equipmentSelected = Require<Label>(root, EquipmentSelectedName);
            equipmentSummary = Require<Label>(root, EquipmentSummaryName);
            equipmentStatus = Require<Label>(root, EquipmentStatusName);
            planModeButton = Require<Button>(root, PlanModeButtonName);
            orderPlansButton = Require<Button>(root, OrderPlansButtonName);
            installPlansButton = Require<Button>(root, InstallPlansButtonName);

            rotateButton.clicked += HandleRotateRequested;
            planModeButton.clicked += HandlePlanModeRequested;
            orderPlansButton.clicked += HandleEquipmentCatalogRequested;
            installPlansButton.clicked += HandleInstallPlansRequested;
        }


        public event Action<string> DefinitionRequested;

        public event Action RotateRequested;

        public event Action PlanModeRequested;

        public event Action EquipmentCatalogRequested;

        public event Action InstallPlansRequested;


        public void SetVisible(bool isVisible)
        {
            panel.style.display =
                isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }


        public void SetItems(IReadOnlyList<FixtureDefinitionPickerItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            ClearButtons();

            for (int index = 0; index < items.Count; index++)
            {
                AddButton(
                    items[index]
                    ?? throw new InvalidOperationException(
                        $"Fixture picker item {index} is null."));
            }
        }


        public void SetSelectedDefinition(string definitionId)
        {
            for (int index = 0; index < buttonBindings.Count; index++)
            {
                ButtonBinding binding = buttonBindings[index];
                binding.Button.EnableInClassList(
                    SelectedClassName,
                    string.Equals(
                        binding.DefinitionId,
                        definitionId,
                        StringComparison.Ordinal));
            }
        }


        public void SetOrientationTooltip(string orientationName)
        {
            rotateButton.tooltip =
                $"Rotate fixture clockwise (R). Current orientation: {orientationName}.";
        }

        public void SetOwnedQuantity(
            string definitionId,
            int quantity)
        {
            for (int index = 0; index < buttonBindings.Count; index++)
            {
                ButtonBinding binding = buttonBindings[index];

                if (string.Equals(
                        binding.DefinitionId,
                        definitionId,
                        StringComparison.Ordinal))
                {
                    binding.OwnedBadge.text = quantity.ToString();
                    return;
                }
            }
        }

        public void SetEquipmentSummary(
            string displayName,
            int ownedQuantity,
            long unitPriceCents,
            int plannedQuantity,
            int outstandingQuantity,
            bool canInstallPlannedEquipment,
            bool isPlanMode,
            string status)
        {
            equipmentSelected.text =
                string.IsNullOrWhiteSpace(displayName)
                    ? "Selected fixture"
                    : displayName;
            equipmentSummary.text =
                $"OWNED {ownedQuantity}  ·  PLANNED {plannedQuantity}  ·  "
                + $"ORDERED {outstandingQuantity}  ·  "
                + FormatMoney(unitPriceCents);
            planModeButton.text = isPlanMode
                ? "PLANNING: ON"
                : "PLAN LAYOUT";
            orderPlansButton.SetEnabled(true);
            installPlansButton.SetEnabled(
                canInstallPlannedEquipment);
            equipmentStatus.text = status ?? string.Empty;
        }


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            rotateButton.clicked -= HandleRotateRequested;
            planModeButton.clicked -= HandlePlanModeRequested;
            orderPlansButton.clicked -= HandleEquipmentCatalogRequested;
            installPlansButton.clicked -= HandleInstallPlansRequested;
            ClearButtons();
            isDisposed = true;
        }


        private void AddButton(FixtureDefinitionPickerItem item)
        {
            Button button =
                new Button
                {
                    name = $"fixture-definition-{item.DefinitionId}-button",
                    tooltip = item.Tooltip
                };

            button.AddToClassList(ButtonClassName);

            VisualElement icon =
                new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(IconClassName);

            if (item.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(item.Icon);
            }

            button.Add(icon);

            Label ownedBadge =
                new Label("0")
                {
                    pickingMode = PickingMode.Ignore
                };
            ownedBadge.AddToClassList(OwnedBadgeClassName);
            button.Add(ownedBadge);

            Action clickHandler =
                () => DefinitionRequested?.Invoke(item.DefinitionId);

            button.clicked += clickHandler;
            buttonBindings.Add(
                new ButtonBinding(
                    item.DefinitionId,
                    button,
                    ownedBadge,
                    clickHandler));
            itemsContainer.Add(button);
        }


        private void HandleRotateRequested()
        {
            RotateRequested?.Invoke();
        }

        private void HandlePlanModeRequested()
        {
            PlanModeRequested?.Invoke();
        }

        private void HandleEquipmentCatalogRequested()
        {
            EquipmentCatalogRequested?.Invoke();
        }

        private void HandleInstallPlansRequested()
        {
            InstallPlansRequested?.Invoke();
        }

        private void ClearButtons()
        {
            for (int index = 0; index < buttonBindings.Count; index++)
            {
                ButtonBinding binding = buttonBindings[index];
                binding.Button.clicked -= binding.ClickHandler;
            }

            buttonBindings.Clear();
            itemsContainer.Clear();
        }


        private static VisualElement RequireElement(
            VisualElement root,
            string elementName)
        {
            return root.Q<VisualElement>(elementName)
                ?? throw new InvalidOperationException(
                    $"Fixture picker is missing required element '{elementName}'.");
        }

        private static T Require<T>(
            VisualElement root,
            string elementName)
            where T : VisualElement
        {
            return root.Q<T>(elementName)
                ?? throw new InvalidOperationException(
                    $"Fixture picker is missing required element '{elementName}'.");
        }

        private static string FormatMoney(long cents)
        {
            long dollars = cents / 100;
            long remainder = Math.Abs(cents % 100);
            return $"${dollars:N0}.{remainder:00} EACH";
        }


        private sealed class ButtonBinding
        {
            public ButtonBinding(
                string definitionId,
                Button button,
                Label ownedBadge,
                Action clickHandler)
            {
                DefinitionId = definitionId;
                Button = button;
                OwnedBadge = ownedBadge;
                ClickHandler = clickHandler;
            }

            public string DefinitionId { get; }

            public Button Button { get; }

            public Label OwnedBadge { get; }

            public Action ClickHandler { get; }
        }
    }
}
