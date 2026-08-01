using System;
using BigRetail.Map.View;
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
        public const string WallViewUpButtonName =
            "wall-view-up-button";
        public const string WallViewCutawayButtonName =
            "wall-view-cutaway-button";
        public const string WallViewDownButtonName =
            "wall-view-down-button";
        public const string UndoButtonName = "undo-button";
        public const string RedoButtonName = "redo-button";
        public const string SelectedClassName = "is-selected";

        private readonly Button wallsButton;
        private readonly Button departmentsButton;
        private readonly Button foundationsButton;
        private readonly Button floorsButton;
        private readonly Button demolitionButton;
        private readonly Button demolishFoundationsButton;
        private readonly Button demolishFloorsButton;
        private readonly Button demolishWallsButton;
        private readonly VisualElement demolitionPicker;
        private readonly Button wallViewUpButton;
        private readonly Button wallViewCutawayButton;
        private readonly Button wallViewDownButton;
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
            departmentsButton =
                RequireButton(root, DepartmentPickerView.DepartmentsButtonName);
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
            wallViewUpButton =
                RequireButton(root, WallViewUpButtonName);
            wallViewCutawayButton =
                RequireButton(root, WallViewCutawayButtonName);
            wallViewDownButton =
                RequireButton(root, WallViewDownButtonName);
            undoButton = RequireButton(root, UndoButtonName);
            redoButton = RequireButton(root, RedoButtonName);

            wallsButton.clicked += HandleWallsRequested;
            departmentsButton.clicked += HandleDepartmentsRequested;
            foundationsButton.clicked +=
                HandleFoundationsRequested;
            floorsButton.clicked += HandleFloorsRequested;
            demolitionButton.clicked += HandleDemolitionPickerRequested;
            demolishFoundationsButton.clicked +=
                HandleDemolishFoundationsRequested;
            demolishFloorsButton.clicked += HandleDemolishFloorsRequested;
            demolishWallsButton.clicked += HandleDemolishWallsRequested;
            wallViewUpButton.clicked += HandleWallViewUpRequested;
            wallViewCutawayButton.clicked +=
                HandleWallViewCutawayRequested;
            wallViewDownButton.clicked += HandleWallViewDownRequested;
            undoButton.clicked += HandleUndoRequested;
            redoButton.clicked += HandleRedoRequested;
        }

        public event Action<ConstructionToolbarSection> SectionRequested;
        public event Action DepartmentsRequested;
        public event Action DemolitionPickerRequested;
        public event Action<ConstructionToolbarDemolitionTarget>
            DemolitionTargetRequested;
        public event Action<WallDisplayMode> WallDisplayModeRequested;
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

        public void SetWallDisplayMode(
            WallDisplayMode displayMode)
        {
            if (displayMode != WallDisplayMode.WallsUp
                && displayMode != WallDisplayMode.Cutaway
                && displayMode != WallDisplayMode.WallsDown)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayMode),
                    displayMode,
                    "Unknown wall display mode.");
            }

            SetSelected(
                wallViewUpButton,
                displayMode == WallDisplayMode.WallsUp);

            SetSelected(
                wallViewCutawayButton,
                displayMode == WallDisplayMode.Cutaway);

            SetSelected(
                wallViewDownButton,
                displayMode == WallDisplayMode.WallsDown);
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
            departmentsButton.clicked -= HandleDepartmentsRequested;
            foundationsButton.clicked -=
                HandleFoundationsRequested;
            floorsButton.clicked -= HandleFloorsRequested;
            demolitionButton.clicked -= HandleDemolitionPickerRequested;
            demolishFoundationsButton.clicked -=
                HandleDemolishFoundationsRequested;
            demolishFloorsButton.clicked -= HandleDemolishFloorsRequested;
            demolishWallsButton.clicked -= HandleDemolishWallsRequested;
            wallViewUpButton.clicked -= HandleWallViewUpRequested;
            wallViewCutawayButton.clicked -=
                HandleWallViewCutawayRequested;
            wallViewDownButton.clicked -= HandleWallViewDownRequested;
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

        private void HandleDepartmentsRequested()
        {
            DepartmentsRequested?.Invoke();
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

        private void HandleWallViewUpRequested()
        {
            WallDisplayModeRequested?.Invoke(
                WallDisplayMode.WallsUp);
        }

        private void HandleWallViewCutawayRequested()
        {
            WallDisplayModeRequested?.Invoke(
                WallDisplayMode.Cutaway);
        }

        private void HandleWallViewDownRequested()
        {
            WallDisplayModeRequested?.Invoke(
                WallDisplayMode.WallsDown);
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
