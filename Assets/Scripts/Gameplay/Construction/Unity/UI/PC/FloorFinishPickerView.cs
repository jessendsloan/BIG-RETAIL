using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Immutable presentation data for one Floor-finish catalog card.
    /// </summary>
    public sealed class FloorFinishPickerItem
    {
        public FloorFinishPickerItem(
            string finishId,
            string tooltip,
            Sprite icon)
        {
            if (string.IsNullOrWhiteSpace(finishId))
            {
                throw new ArgumentException(
                    "A Floor-finish picker item requires a finish identifier.",
                    nameof(finishId));
            }

            FinishId = finishId;
            Tooltip = tooltip ?? string.Empty;
            Icon = icon;
        }

        public string FinishId { get; }

        public string Tooltip { get; }

        public Sprite Icon { get; }
    }


    /// <summary>
    /// Presentation-only wrapper around the Floor-finish catalog elements in
    /// the PC construction toolbar document.
    /// </summary>
    public sealed class FloorFinishPickerView : IDisposable
    {
        public const string PanelName =
            "floor-finish-picker";

        public const string ItemsContainerName =
            "floor-finish-picker-items";

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


        public FloorFinishPickerView(
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


        public event Action<string> FinishRequested;


        public void SetVisible(
            bool isVisible)
        {
            panel.style.display =
                isVisible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }


        public void SetItems(
            IReadOnlyList<FloorFinishPickerItem> items)
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
                FloorFinishPickerItem item =
                    items[index]
                    ?? throw new InvalidOperationException(
                        $"Floor-finish picker item {index} is null.");

                AddButton(
                    item);
            }
        }


        public void SetSelectedFinish(
            string finishId)
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
                        binding.FinishId,
                        finishId,
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
            FloorFinishPickerItem item)
        {
            Button button =
                new Button
                {
                    name =
                        $"floor-finish-{item.FinishId}-button",
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
                () => FinishRequested?.Invoke(
                    item.FinishId);

            button.clicked +=
                clickHandler;

            buttonBindings.Add(
                new ButtonBinding(
                    item.FinishId,
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
            }

            buttonBindings.Clear();
            itemsContainer.Clear();
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
                $"Floor-finish picker is missing required element '{elementName}'.");
        }


        private sealed class ButtonBinding
        {
            public ButtonBinding(
                string finishId,
                Button button,
                Action clickHandler)
            {
                FinishId = finishId;
                Button = button;
                ClickHandler = clickHandler;
            }

            public string FinishId { get; }

            public Button Button { get; }

            public Action ClickHandler { get; }
        }
    }
}
