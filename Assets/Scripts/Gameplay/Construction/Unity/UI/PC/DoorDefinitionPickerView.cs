using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Immutable presentation data for one door-definition catalog card.
    /// </summary>
    public sealed class DoorDefinitionPickerItem
    {
        public DoorDefinitionPickerItem(
            string definitionId,
            string tooltip,
            Sprite icon)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new ArgumentException(
                    "A door-definition picker item requires a definition identifier.",
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
    /// Presentation-only wrapper around the door-definition catalog elements
    /// in the PC construction toolbar document.
    /// </summary>
    public sealed class DoorDefinitionPickerView : IDisposable
    {
        public const string PanelName =
            "door-definition-picker";

        public const string ItemsContainerName =
            "door-definition-picker-items";

        public const string SelectedClassName =
            "is-selected";

        private const string ButtonClassName =
            "finish-picker__button";

        private const string IconClassName =
            "finish-picker__icon";

        private readonly VisualElement panel;
        private readonly VisualElement itemsContainer;
        private readonly List<ButtonBinding> buttonBindings =
            new List<ButtonBinding>();

        private bool isDisposed;


        public DoorDefinitionPickerView(
            VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(
                    nameof(root));
            }

            panel =
                RequireElement(
                    root,
                    PanelName);

            itemsContainer =
                RequireElement(
                    root,
                    ItemsContainerName);
        }


        public event Action<string> DefinitionRequested;


        public void SetVisible(
            bool isVisible)
        {
            panel.style.display =
                isVisible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }


        public void SetItems(
            IReadOnlyList<DoorDefinitionPickerItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(
                    nameof(items));
            }

            ClearButtons();

            for (int index = 0;
                 index < items.Count;
                 index++)
            {
                DoorDefinitionPickerItem item =
                    items[index]
                    ?? throw new InvalidOperationException(
                        $"Door-definition picker item {index} is null.");

                AddButton(
                    item);
            }
        }


        public void SetSelectedDefinition(
            string definitionId)
        {
            for (int index = 0;
                 index < buttonBindings.Count;
                 index++)
            {
                ButtonBinding binding =
                    buttonBindings[index];

                binding.Button.EnableInClassList(
                    SelectedClassName,
                    string.Equals(
                        binding.DefinitionId,
                        definitionId,
                        StringComparison.Ordinal));
            }
        }


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            ClearButtons();
            isDisposed = true;
        }


        private void AddButton(
            DoorDefinitionPickerItem item)
        {
            Button button =
                new Button
                {
                    name =
                        $"door-definition-{item.DefinitionId}-button",
                    tooltip =
                        item.Tooltip
                };

            button.AddToClassList(
                ButtonClassName);

            VisualElement icon =
                new VisualElement
                {
                    pickingMode =
                        PickingMode.Ignore
                };

            icon.AddToClassList(
                IconClassName);

            if (item.Icon != null)
            {
                icon.style.backgroundImage =
                    new StyleBackground(
                        item.Icon);
            }

            button.Add(
                icon);

            Action clickHandler =
                () => DefinitionRequested?.Invoke(
                    item.DefinitionId);

            button.clicked +=
                clickHandler;

            buttonBindings.Add(
                new ButtonBinding(
                    item.DefinitionId,
                    button,
                    clickHandler));

            itemsContainer.Add(
                button);
        }


        private void ClearButtons()
        {
            for (int index = 0;
                 index < buttonBindings.Count;
                 index++)
            {
                ButtonBinding binding =
                    buttonBindings[index];

                binding.Button.clicked -=
                    binding.ClickHandler;

                binding.Button.RemoveFromHierarchy();
            }

            buttonBindings.Clear();
        }


        private static VisualElement RequireElement(
            VisualElement root,
            string elementName)
        {
            VisualElement element =
                root.Q<VisualElement>(
                    elementName);

            if (element != null)
            {
                return element;
            }

            throw new InvalidOperationException(
                $"Door-definition picker is missing required element '{elementName}'.");
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
