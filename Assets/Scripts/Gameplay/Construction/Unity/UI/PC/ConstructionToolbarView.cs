using System;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Presentation-only wrapper around the PC construction toolbar document.
    /// It exposes user intent and visual state without owning gameplay rules.
    /// </summary>
    public sealed class ConstructionToolbarView : IDisposable
    {
        public const string WallsButtonName = "walls-button";
        public const string FoundationsButtonName = "foundations-button";
        public const string FloorsButtonName = "floors-button";
        public const string DemolitionButtonName = "demolition-button";
        public const string DemolishFoundationsButtonName =
            "demolish-foundations-button";
        public const string DemolishFloorsButtonName =
            "demolish-floors-button";
        public const string DemolishWallsButtonName =
            "demolish-walls-button";
        public const string DemolitionPickerName = "demolition-picker";
        public const string UndoButtonName = "undo-button";
        public const string RedoButtonName = "redo-button";
        public const string SelectedClassName = "is-selected";

        private readonly Button wallsButton;
        private readonly Button foundationsButton;
        private readonly Button floorsButton;
        private readonly Button demolitionButton;
        private readonly Button demolishFoundationsButton;
        private readonly Button demolishFloorsButton;
        private readonly Button demolishWallsButton;
        private readonly VisualElement demolitionPicker;
        private readonly Button undoButton;
        private readonly Button redoButton;

        private bool isDisposed;

        public ConstructionToolbarView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            wallsButton = RequireButton(root, WallsButtonName);
            foundationsButton =
                RequireButton(root, FoundationsButtonName);
            floorsButton = RequireButton(root, FloorsButtonName);
            demolitionButton = RequireButton(root, DemolitionButtonName);
            demolishFoundationsButton =
                RequireButton(root, DemolishFoundationsButtonName);
            demolishFloorsButton =
                RequireButton(root, DemolishFloorsButtonName);
            demolishWallsButton =
                RequireButton(root, DemolishWallsButtonName);
            demolitionPicker = RequireElement(root, DemolitionPickerName);
            undoButton = RequireButton(root, UndoButtonName);
            redoButton = RequireButton(root, RedoButtonName);

            wallsButton.clicked += HandleWallsRequested;
            foundationsButton.clicked +=
                HandleFoundationsRequested;
            floorsButton.clicked += HandleFloorsRequested;
            demolitionButton.clicked += HandleDemolitionPickerRequested;
            demolishFoundationsButton.clicked +=
                HandleDemolishFoundationsRequested;
            demolishFloorsButton.clicked += HandleDemolishFloorsRequested;
            demolishWallsButton.clicked += HandleDemolishWallsRequested;
            undoButton.clicked += HandleUndoRequested;
            redoButton.clicked += HandleRedoRequested;
        }

        public event Action<ConstructionToolbarSection> SectionRequested;
        public event Action DemolitionPickerRequested;
        public event Action<ConstructionToolbarDemolitionTarget>
            DemolitionTargetRequested;
        public event Action UndoRequested;
        public event Action RedoRequested;

        public void SetSelectedSection(ConstructionToolbarSection section)
        {
            SetSelected(wallsButton, section == ConstructionToolbarSection.Walls);
            SetSelected(
                foundationsButton,
                section == ConstructionToolbarSection.Foundations);
            SetSelected(floorsButton, section == ConstructionToolbarSection.Floors);
            SetSelected(demolitionButton, section == ConstructionToolbarSection.Demolition);
        }

        public void SetUndoEnabled(bool isEnabled)
        {
            undoButton.SetEnabled(isEnabled);
        }

        public void SetDemolitionPickerVisible(bool isVisible)
        {
            demolitionPicker.style.display = isVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void SetSelectedDemolitionTarget(
            ConstructionToolbarDemolitionTarget? target)
        {
            SetSelected(
                demolishFoundationsButton,
                target == ConstructionToolbarDemolitionTarget.Foundations);
            SetSelected(
                demolishFloorsButton,
                target == ConstructionToolbarDemolitionTarget.Floors);
            SetSelected(
                demolishWallsButton,
                target == ConstructionToolbarDemolitionTarget.Walls);
        }

        public void SetRedoEnabled(bool isEnabled)
        {
            redoButton.SetEnabled(isEnabled);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            wallsButton.clicked -= HandleWallsRequested;
            foundationsButton.clicked -=
                HandleFoundationsRequested;
            floorsButton.clicked -= HandleFloorsRequested;
            demolitionButton.clicked -= HandleDemolitionPickerRequested;
            demolishFoundationsButton.clicked -=
                HandleDemolishFoundationsRequested;
            demolishFloorsButton.clicked -= HandleDemolishFloorsRequested;
            demolishWallsButton.clicked -= HandleDemolishWallsRequested;
            undoButton.clicked -= HandleUndoRequested;
            redoButton.clicked -= HandleRedoRequested;

            isDisposed = true;
        }

        private void HandleWallsRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Walls);
        }

        private void HandleFoundationsRequested()
        {
            SectionRequested?.Invoke(
                ConstructionToolbarSection.Foundations);
        }

        private void HandleFloorsRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Floors);
        }

        private void HandleDemolitionPickerRequested()
        {
            DemolitionPickerRequested?.Invoke();
        }

        private void HandleDemolishFoundationsRequested()
        {
            DemolitionTargetRequested?.Invoke(
                ConstructionToolbarDemolitionTarget.Foundations);
        }

        private void HandleDemolishFloorsRequested()
        {
            DemolitionTargetRequested?.Invoke(
                ConstructionToolbarDemolitionTarget.Floors);
        }

        private void HandleDemolishWallsRequested()
        {
            DemolitionTargetRequested?.Invoke(
                ConstructionToolbarDemolitionTarget.Walls);
        }

        private void HandleUndoRequested()
        {
            UndoRequested?.Invoke();
        }

        private void HandleRedoRequested()
        {
            RedoRequested?.Invoke();
        }

        private static Button RequireButton(VisualElement root, string buttonName)
        {
            Button button = root.Q<Button>(buttonName);

            if (button != null)
            {
                return button;
            }

            throw new InvalidOperationException(
                $"Construction toolbar is missing required button '{buttonName}'.");
        }

        private static VisualElement RequireElement(
            VisualElement root,
            string elementName)
        {
            VisualElement element = root.Q(elementName);

            if (element != null)
            {
                return element;
            }

            throw new InvalidOperationException(
                $"Construction toolbar is missing required element '{elementName}'.");
        }

        private static void SetSelected(Button button, bool isSelected)
        {
            button.EnableInClassList(SelectedClassName, isSelected);
        }
    }
}
