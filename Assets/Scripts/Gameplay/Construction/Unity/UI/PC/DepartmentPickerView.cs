using System;
using System.Collections.Generic;
using BigRetail.Departments.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Immutable presentation data for one department catalog card.
    /// </summary>
    public sealed class DepartmentPickerItem
    {
        public DepartmentPickerItem(
            DepartmentDefinitionAsset definition,
            string displayName,
            Sprite icon)
        {
            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? definition.name
                : displayName;
            Icon = icon;
        }

        public DepartmentDefinitionAsset Definition { get; }

        public string DisplayName { get; }

        public Sprite Icon { get; }
    }


    /// <summary>
    /// Presentation-only wrapper around the Departments rail entry and its
    /// contextual picker in the shared player UI document.
    /// </summary>
    public sealed class DepartmentPickerView : IDisposable
    {
        public const string DepartmentsButtonName = "departments-button";
        public const string PanelName = "department-picker";
        public const string ItemsContainerName = "department-picker-items";
        public const string SelectedClassName = "is-selected";

        private const string ButtonClassName = "department-picker__button";
        private const string IconClassName = "department-picker__icon";
        private const string LabelClassName = "department-picker__label";

        private readonly Button departmentsButton;
        private readonly VisualElement panel;
        private readonly VisualElement itemsContainer;
        private readonly List<ButtonBinding> buttonBindings =
            new List<ButtonBinding>();
        private bool isDisposed;


        public DepartmentPickerView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            departmentsButton = RequireButton(root, DepartmentsButtonName);
            panel = RequireElement(root, PanelName);
            itemsContainer = RequireElement(root, ItemsContainerName);
            departmentsButton.clicked += HandleDepartmentsRequested;
        }


        public event Action DepartmentsRequested;

        public event Action<DepartmentDefinitionAsset> DefinitionRequested;


        public void SetVisible(bool isVisible)
        {
            panel.style.display = isVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            departmentsButton.EnableInClassList(
                SelectedClassName,
                isVisible);
        }


        public void SetItems(IReadOnlyList<DepartmentPickerItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            ClearButtons();

            for (int index = 0; index < items.Count; index++)
            {
                AddButton(items[index]
                    ?? throw new InvalidOperationException(
                        $"Department picker item {index} is null."));
            }
        }


        public void SetSelectedDefinition(
            DepartmentDefinitionAsset selectedDefinition)
        {
            for (int index = 0; index < buttonBindings.Count; index++)
            {
                ButtonBinding binding = buttonBindings[index];
                binding.Button.EnableInClassList(
                    SelectedClassName,
                    binding.Definition == selectedDefinition);
            }
        }


        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            departmentsButton.clicked -= HandleDepartmentsRequested;
            ClearButtons();
            isDisposed = true;
        }


        private void AddButton(DepartmentPickerItem item)
        {
            Button button = new Button
            {
                name = $"department-{item.Definition.name}-button",
                tooltip = item.DisplayName
            };
            button.AddToClassList(ButtonClassName);

            VisualElement icon = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            icon.AddToClassList(IconClassName);

            if (item.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(item.Icon);
            }

            Label label = new Label(item.DisplayName)
            {
                pickingMode = PickingMode.Ignore
            };
            label.AddToClassList(LabelClassName);
            button.Add(icon);
            button.Add(label);

            Action clickHandler =
                () => DefinitionRequested?.Invoke(item.Definition);
            button.clicked += clickHandler;
            buttonBindings.Add(new ButtonBinding(
                item.Definition,
                button,
                clickHandler));
            itemsContainer.Add(button);
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


        private void HandleDepartmentsRequested()
        {
            DepartmentsRequested?.Invoke();
        }


        private static Button RequireButton(
            VisualElement root,
            string elementName)
        {
            Button button = root.Q<Button>(elementName);
            if (button != null)
            {
                return button;
            }

            throw new InvalidOperationException(
                $"Department picker is missing required button '{elementName}'.");
        }


        private static VisualElement RequireElement(
            VisualElement root,
            string elementName)
        {
            VisualElement element = root.Q<VisualElement>(elementName);
            if (element != null)
            {
                return element;
            }

            throw new InvalidOperationException(
                $"Department picker is missing required element '{elementName}'.");
        }


        private sealed class ButtonBinding
        {
            public ButtonBinding(
                DepartmentDefinitionAsset definition,
                Button button,
                Action clickHandler)
            {
                Definition = definition;
                Button = button;
                ClickHandler = clickHandler;
            }

            public DepartmentDefinitionAsset Definition { get; }
            public Button Button { get; }
            public Action ClickHandler { get; }
        }
    }
}
