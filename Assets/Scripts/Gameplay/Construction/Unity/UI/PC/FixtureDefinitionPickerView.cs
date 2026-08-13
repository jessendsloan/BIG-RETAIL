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

        private const string ButtonClassName = "finish-picker__button";
        private const string IconClassName = "finish-picker__icon";

        private readonly VisualElement panel;
        private readonly VisualElement itemsContainer;
        private readonly Button rotateButton;
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

            rotateButton.clicked += HandleRotateRequested;
        }


        public event Action<string> DefinitionRequested;

        public event Action RotateRequested;


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


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            rotateButton.clicked -= HandleRotateRequested;
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

            Action clickHandler =
                () => DefinitionRequested?.Invoke(item.DefinitionId);

            button.clicked += clickHandler;
            buttonBindings.Add(
                new ButtonBinding(
                    item.DefinitionId,
                    button,
                    clickHandler));
            itemsContainer.Add(button);
        }


        private void HandleRotateRequested()
        {
            RotateRequested?.Invoke();
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


        private sealed class ButtonBinding
        {
            public ButtonBinding(
                string definitionId,
                Button button,
                Action clickHandler)
            {
                DefinitionId = definitionId;
                Button = button;
                ClickHandler = clickHandler;
            }

            public string DefinitionId { get; }

            public Button Button { get; }

            public Action ClickHandler { get; }
        }
    }
}
