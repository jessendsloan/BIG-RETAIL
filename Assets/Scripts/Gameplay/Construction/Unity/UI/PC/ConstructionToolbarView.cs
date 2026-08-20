using System;
using System.Globalization;
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
        public const string WindowsButtonName = "windows-button";
        public const string DoorsButtonName = "doors-button";
        public const string FixturesButtonName = "fixtures-button";
        public const string FoundationsButtonName = "foundations-button";
        public const string SidewalksButtonName = "sidewalks-button";
        public const string FoundationPickerName = "foundation-picker";
        public const string FoundationDefaultButtonName =
            "foundation-default-button";
        public const string FloorsButtonName = "floors-button";
        public const string DemolitionButtonName = "demolition-button";
        public const string DemolishFoundationsButtonName =
            "demolish-foundations-button";
        public const string DemolishSidewalksButtonName =
            "demolish-sidewalks-button";
        public const string DemolishFloorsButtonName =
            "demolish-floors-button";
        public const string DemolishWallsButtonName =
            "demolish-walls-button";
        public const string DemolishFixturesButtonName =
            "demolish-fixtures-button";
        public const string DemolitionPickerName = "demolition-picker";
        public const string WallViewUpButtonName =
            "wall-view-up-button";
        public const string WallViewCutawayButtonName =
            "wall-view-cutaway-button";
        public const string WallViewDownButtonName =
            "wall-view-down-button";
        public const string CameraViewNorthButtonName =
            "camera-view-north-button";
        public const string CameraViewEastButtonName =
            "camera-view-east-button";
        public const string CameraViewSouthButtonName =
            "camera-view-south-button";
        public const string CameraViewWestButtonName =
            "camera-view-west-button";
        public const string UndoButtonName = "undo-button";
        public const string RedoButtonName = "redo-button";
        public const string MerchandiseToolButtonName =
            "merchandise-tool-button";
        public const string PurchasingButtonName =
            "purchasing-button";
        public const string ReceivingAreaButtonName =
            "receiving-area-button";
        public const string ReceivingAreaPanelName =
            "receiving-area-panel";
        public const string ReceivingAreaCapacityName =
            "receiving-area-capacity";
        public const string ReceivingAreaInstructionName =
            "receiving-area-instruction";
        public const string StoreCashValueName =
            "store-cash-value";
        public const string SelectedClassName = "is-selected";

        private readonly Button wallsButton;
        private readonly Button windowsButton;
        private readonly Button doorsButton;
        private readonly Button fixturesButton;
        private readonly Button departmentsButton;
        private readonly Button merchandiseToolButton;
        private readonly Button purchasingButton;
        private readonly Button receivingAreaButton;
        private readonly VisualElement receivingAreaPanel;
        private readonly Label receivingAreaCapacityLabel;
        private readonly Label receivingAreaInstructionLabel;
        private readonly Button foundationsButton;
        private readonly Button sidewalksButton;
        private readonly VisualElement foundationPicker;
        private readonly Button foundationDefaultButton;
        private readonly Button floorsButton;
        private readonly Button demolitionButton;
        private readonly Button demolishFoundationsButton;
        private readonly Button demolishSidewalksButton;
        private readonly Button demolishFloorsButton;
        private readonly Button demolishWallsButton;
        private readonly Button demolishFixturesButton;
        private readonly VisualElement demolitionPicker;
        private readonly Button wallViewUpButton;
        private readonly Button wallViewCutawayButton;
        private readonly Button wallViewDownButton;
        private readonly Button cameraViewNorthButton;
        private readonly Button cameraViewEastButton;
        private readonly Button cameraViewSouthButton;
        private readonly Button cameraViewWestButton;
        private readonly Button undoButton;
        private readonly Button redoButton;
        private readonly Label storeCashValueLabel;

        private bool isDisposed;

        public ConstructionToolbarView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            wallsButton = RequireButton(root, WallsButtonName);
            windowsButton = RequireButton(root, WindowsButtonName);
            doorsButton = RequireButton(root, DoorsButtonName);
            fixturesButton = RequireButton(root, FixturesButtonName);
            departmentsButton =
                RequireButton(root, DepartmentPickerView.DepartmentsButtonName);
            merchandiseToolButton =
                RequireButton(root, MerchandiseToolButtonName);
            purchasingButton =
                RequireButton(root, PurchasingButtonName);
            receivingAreaButton =
                RequireButton(root, ReceivingAreaButtonName);
            receivingAreaPanel =
                RequireElement(root, ReceivingAreaPanelName);
            receivingAreaCapacityLabel =
                RequireLabel(root, ReceivingAreaCapacityName);
            receivingAreaInstructionLabel =
                RequireLabel(root, ReceivingAreaInstructionName);
            foundationsButton =
                RequireButton(root, FoundationsButtonName);
            sidewalksButton = RequireButton(root, SidewalksButtonName);
            foundationPicker =
                RequireElement(root, FoundationPickerName);
            foundationDefaultButton =
                RequireButton(root, FoundationDefaultButtonName);
            floorsButton = RequireButton(root, FloorsButtonName);
            demolitionButton = RequireButton(root, DemolitionButtonName);
            demolishFoundationsButton =
                RequireButton(root, DemolishFoundationsButtonName);
            demolishSidewalksButton =
                RequireButton(root, DemolishSidewalksButtonName);
            demolishFloorsButton =
                RequireButton(root, DemolishFloorsButtonName);
            demolishWallsButton =
                RequireButton(root, DemolishWallsButtonName);
            demolishFixturesButton =
                RequireButton(root, DemolishFixturesButtonName);
            demolitionPicker = RequireElement(root, DemolitionPickerName);
            wallViewUpButton =
                RequireButton(root, WallViewUpButtonName);
            wallViewCutawayButton =
                RequireButton(root, WallViewCutawayButtonName);
            wallViewDownButton =
                RequireButton(root, WallViewDownButtonName);
            cameraViewNorthButton =
                RequireButton(root, CameraViewNorthButtonName);
            cameraViewEastButton =
                RequireButton(root, CameraViewEastButtonName);
            cameraViewSouthButton =
                RequireButton(root, CameraViewSouthButtonName);
            cameraViewWestButton =
                RequireButton(root, CameraViewWestButtonName);
            undoButton = RequireButton(root, UndoButtonName);
            redoButton = RequireButton(root, RedoButtonName);
            storeCashValueLabel =
                RequireLabel(root, StoreCashValueName);

            wallsButton.clicked += HandleWallsRequested;
            windowsButton.clicked += HandleWindowsRequested;
            doorsButton.clicked += HandleDoorsRequested;
            fixturesButton.clicked += HandleFixturesRequested;
            departmentsButton.clicked += HandleDepartmentsRequested;
            merchandiseToolButton.clicked +=
                HandleMerchandiseToolRequested;
            purchasingButton.clicked += HandlePurchasingRequested;
            receivingAreaButton.clicked +=
                HandleReceivingAreaRequested;
            foundationsButton.clicked +=
                HandleFoundationsRequested;
            sidewalksButton.clicked += HandleSidewalksRequested;
            foundationDefaultButton.clicked +=
                HandleFoundationsRequested;
            floorsButton.clicked += HandleFloorsRequested;
            demolitionButton.clicked += HandleDemolitionPickerRequested;
            demolishFoundationsButton.clicked +=
                HandleDemolishFoundationsRequested;
            demolishSidewalksButton.clicked +=
                HandleDemolishSidewalksRequested;
            demolishFloorsButton.clicked += HandleDemolishFloorsRequested;
            demolishWallsButton.clicked += HandleDemolishWallsRequested;
            demolishFixturesButton.clicked +=
                HandleDemolishFixturesRequested;
            wallViewUpButton.clicked += HandleWallViewUpRequested;
            wallViewCutawayButton.clicked +=
                HandleWallViewCutawayRequested;
            wallViewDownButton.clicked += HandleWallViewDownRequested;
            cameraViewNorthButton.clicked += HandleCameraViewNorthRequested;
            cameraViewEastButton.clicked += HandleCameraViewEastRequested;
            cameraViewSouthButton.clicked += HandleCameraViewSouthRequested;
            cameraViewWestButton.clicked += HandleCameraViewWestRequested;
            undoButton.clicked += HandleUndoRequested;
            redoButton.clicked += HandleRedoRequested;
        }

        public event Action<ConstructionToolbarSection> SectionRequested;
        public event Action DepartmentsRequested;
        public event Action MerchandiseToolRequested;
        public event Action PurchasingRequested;
        public event Action ReceivingAreaRequested;
        public event Action DemolitionPickerRequested;
        public event Action<ConstructionToolbarDemolitionTarget>
            DemolitionTargetRequested;
        public event Action<WallDisplayMode> WallDisplayModeRequested;
        public event Action<IsometricViewOrientation>
            CameraViewOrientationRequested;
        public event Action UndoRequested;
        public event Action RedoRequested;

        public void SetSelectedSection(ConstructionToolbarSection section)
        {
            bool isFoundationSelected =
                section == ConstructionToolbarSection.Foundations
                || section == ConstructionToolbarSection.Sidewalks;

            bool isDoorSelected =
                section == ConstructionToolbarSection.Doors
                || section == ConstructionToolbarSection.Windows;

            SetSelected(wallsButton, section == ConstructionToolbarSection.Walls);
            SetSelected(
                windowsButton,
                section == ConstructionToolbarSection.Windows);
            SetSelected(doorsButton, isDoorSelected);
            SetSelected(
                fixturesButton,
                section == ConstructionToolbarSection.Fixtures);
            SetSelected(
                foundationsButton,
                isFoundationSelected);
            SetSelected(
                sidewalksButton,
                section == ConstructionToolbarSection.Sidewalks);
            SetSelected(
                foundationDefaultButton,
                section == ConstructionToolbarSection.Foundations);
            foundationPicker.style.display =
                isFoundationSelected
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            SetSelected(floorsButton, section == ConstructionToolbarSection.Floors);
            SetSelected(demolitionButton, section == ConstructionToolbarSection.Demolition);
        }

        public void SetUndoEnabled(bool isEnabled)
        {
            undoButton.SetEnabled(isEnabled);
        }

        public void SetMerchandiseToolActive(bool isActive)
        {
            SetSelected(merchandiseToolButton, isActive);
        }

        public void SetReceivingAreaActive(bool isActive)
        {
            SetSelected(receivingAreaButton, isActive);
            receivingAreaPanel.style.display = isActive
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void SetReceivingAreaStatus(
            int operationalCellCount,
            int occupiedCellCount,
            int waitingDeliveryCount)
        {
            receivingAreaCapacityLabel.text = operationalCellCount == 0
                ? "NO RECEIVING SPACE"
                : $"{operationalCellCount} PALLET SPACE"
                    + (operationalCellCount == 1 ? string.Empty : "S")
                    + $" · {occupiedCellCount} OCCUPIED";

            receivingAreaInstructionLabel.text = waitingDeliveryCount > 0
                ? waitingDeliveryCount == 1
                    ? "1 supplier order is waiting for an open space."
                    : $"{waitingDeliveryCount} supplier orders are waiting "
                        + "for open spaces."
                : "Drag on finished floor to add. Start on Receiving "
                    + "space to erase.";
        }

        public void SetCashBalance(long balanceCents)
        {
            storeCashValueLabel.text =
                string.Format(
                    CultureInfo.InvariantCulture,
                    "${0:N2}",
                    balanceCents / 100m);
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
                demolishSidewalksButton,
                target == ConstructionToolbarDemolitionTarget.Sidewalks);
            SetSelected(
                demolishFloorsButton,
                target == ConstructionToolbarDemolitionTarget.Floors);
            SetSelected(
                demolishWallsButton,
                target == ConstructionToolbarDemolitionTarget.Walls);
            SetSelected(
                demolishFixturesButton,
                target == ConstructionToolbarDemolitionTarget.Fixtures);
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

        public void SetCameraViewOrientation(
            IsometricViewOrientation orientation)
        {
            if (orientation != IsometricViewOrientation.North
                && orientation != IsometricViewOrientation.East
                && orientation != IsometricViewOrientation.South
                && orientation != IsometricViewOrientation.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation),
                    orientation,
                    "Unknown isometric-view orientation.");
            }

            SetSelected(
                cameraViewNorthButton,
                orientation == IsometricViewOrientation.North);

            SetSelected(
                cameraViewEastButton,
                orientation == IsometricViewOrientation.East);

            SetSelected(
                cameraViewSouthButton,
                orientation == IsometricViewOrientation.South);

            SetSelected(
                cameraViewWestButton,
                orientation == IsometricViewOrientation.West);
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
            windowsButton.clicked -= HandleWindowsRequested;
            doorsButton.clicked -= HandleDoorsRequested;
            fixturesButton.clicked -= HandleFixturesRequested;
            departmentsButton.clicked -= HandleDepartmentsRequested;
            merchandiseToolButton.clicked -=
                HandleMerchandiseToolRequested;
            purchasingButton.clicked -= HandlePurchasingRequested;
            receivingAreaButton.clicked -=
                HandleReceivingAreaRequested;
            foundationsButton.clicked -=
                HandleFoundationsRequested;
            sidewalksButton.clicked -= HandleSidewalksRequested;
            foundationDefaultButton.clicked -=
                HandleFoundationsRequested;
            floorsButton.clicked -= HandleFloorsRequested;
            demolitionButton.clicked -= HandleDemolitionPickerRequested;
            demolishFoundationsButton.clicked -=
                HandleDemolishFoundationsRequested;
            demolishSidewalksButton.clicked -=
                HandleDemolishSidewalksRequested;
            demolishFloorsButton.clicked -= HandleDemolishFloorsRequested;
            demolishWallsButton.clicked -= HandleDemolishWallsRequested;
            demolishFixturesButton.clicked -=
                HandleDemolishFixturesRequested;
            wallViewUpButton.clicked -= HandleWallViewUpRequested;
            wallViewCutawayButton.clicked -=
                HandleWallViewCutawayRequested;
            wallViewDownButton.clicked -= HandleWallViewDownRequested;
            cameraViewNorthButton.clicked -= HandleCameraViewNorthRequested;
            cameraViewEastButton.clicked -= HandleCameraViewEastRequested;
            cameraViewSouthButton.clicked -= HandleCameraViewSouthRequested;
            cameraViewWestButton.clicked -= HandleCameraViewWestRequested;
            undoButton.clicked -= HandleUndoRequested;
            redoButton.clicked -= HandleRedoRequested;

            isDisposed = true;
        }

        private void HandleWallsRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Walls);
        }

        private void HandleWindowsRequested()
        {
            SectionRequested?.Invoke(
                ConstructionToolbarSection.Windows);
        }

        private void HandleDoorsRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Doors);
        }

        private void HandleFixturesRequested()
        {
            SectionRequested?.Invoke(ConstructionToolbarSection.Fixtures);
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

        private void HandleSidewalksRequested()
        {
            SectionRequested?.Invoke(
                ConstructionToolbarSection.Sidewalks);
        }

        private void HandleMerchandiseToolRequested()
        {
            MerchandiseToolRequested?.Invoke();
        }

        private void HandlePurchasingRequested()
        {
            PurchasingRequested?.Invoke();
        }

        private void HandleReceivingAreaRequested()
        {
            ReceivingAreaRequested?.Invoke();
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

        private void HandleDemolishFixturesRequested()
        {
            DemolitionTargetRequested?.Invoke(
                ConstructionToolbarDemolitionTarget.Fixtures);
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

        private void HandleCameraViewNorthRequested()
        {
            RequestCameraViewOrientation(
                IsometricViewOrientation.North);
        }

        private void HandleCameraViewEastRequested()
        {
            RequestCameraViewOrientation(
                IsometricViewOrientation.East);
        }

        private void HandleCameraViewSouthRequested()
        {
            RequestCameraViewOrientation(
                IsometricViewOrientation.South);
        }

        private void HandleCameraViewWestRequested()
        {
            RequestCameraViewOrientation(
                IsometricViewOrientation.West);
        }

        private void RequestCameraViewOrientation(
            IsometricViewOrientation orientation)
        {
            CameraViewOrientationRequested?.Invoke(
                orientation);
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

        private void HandleDemolishSidewalksRequested()
        {
            DemolitionTargetRequested?.Invoke(
                ConstructionToolbarDemolitionTarget.Sidewalks);
        }

        private static Label RequireLabel(
            VisualElement root,
            string labelName)
        {
            Label label = root.Q<Label>(labelName);

            if (label != null)
            {
                return label;
            }

            throw new InvalidOperationException(
                $"Construction toolbar is missing required label '{labelName}'.");
        }

        private static void SetSelected(Button button, bool isSelected)
        {
            button.EnableInClassList(SelectedClassName, isSelected);
        }
    }
}
