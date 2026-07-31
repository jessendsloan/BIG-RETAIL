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
        public const string FloorsButtonName = "floors-button";
        public const string DemolitionButtonName = "demolition-button";
        public const string UndoButtonName = "undo-button";
        public const string RedoButtonName = "redo-button";
        public const string SelectedClassName = "is-selected";

        private readonly Button wallsButton;
        private readonly Button floorsButton;
        private readonly Button demolitionButton;
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
            floorsButton = RequireButton(root, FloorsButtonName);
            demolitionButton = RequireButton(root, DemolitionButtonName);
            undoButton = RequireButton(root, UndoButtonName);
            redoButton = RequireButton(root, RedoButtonName);

            wallsButton.clicked += HandleWallsRequested;
            floorsButton.clicked += HandleFloorsRequested;
            demolitionButton.clicked += HandleDemolitionRequested;
            undoButton.clicked += HandleUndoRequested;
            redoButton.clicked += HandleRedoRequested;
        }

        public event Action<ConstructionToolbarSection> SectionRequested;
        public event Action UndoRequested;
        public event Action RedoRequested;

        public void SetSelectedSection(ConstructionToolbarSection section)
        {
            SetSelected(wallsButton, section == ConstructionToolbarSection.Walls);
            SetSelected(floorsButton, section == ConstructionToolbarSection.Floors);
            SetSelected(demolitionButton, section == ConstructionToolbarSection.Demolition);
        }

        public void SetUndoEnabled(bool isEnabled)
        {
            undoButton.SetEnabled(isEnabled);
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
            floorsButton.clicked -= HandleFloorsRequested;
            demolitionButton.clicked -= HandleDemolitionRequested;
            undoButton.clicked -= HandleUndoRequested;
            redoButton.clicked -= HandleRedoRequested;

            isDisposed = true;
        }

        private void HandleWallsRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Walls);
        }

        private void HandleFloorsRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Floors);
        }

        private void HandleDemolitionRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Demolition);
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

        private static void SetSelected(Button button, bool isSelected)
        {
            button.EnableInClassList(SelectedClassName, isSelected);
        }
    }
}
