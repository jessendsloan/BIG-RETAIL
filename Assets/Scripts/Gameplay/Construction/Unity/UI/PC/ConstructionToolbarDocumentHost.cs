using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Owns the runtime lifecycle of the PC construction toolbar document.
    /// Gameplay presenters can observe the created views without coupling the
    /// PanelRenderer to construction rules or services.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class ConstructionToolbarDocumentHost : MonoBehaviour
    {
        [SerializeField]
        private PanelRenderer panelRenderer;

        public ConstructionToolbarView View
        {
            get;
            private set;
        }

        public WallFinishPickerView FinishPickerView
        {
            get;
            private set;
        }

        public FloorFinishPickerView FloorFinishPickerView
        {
            get;
            private set;
        }

        public DoorDefinitionPickerView DoorDefinitionPickerView
        {
            get;
            private set;
        }

        public FixtureDefinitionPickerView FixtureDefinitionPickerView
        {
            get;
            private set;
        }

        public DepartmentPickerView DepartmentPickerView
        {
            get;
            private set;
        }

        public FixtureMerchandisingInspectorView
            FixtureMerchandisingInspectorView
        {
            get;
            private set;
        }

        public SimulationClockView SimulationClockView
        {
            get;
            private set;
        }

        public bool HasView =>
            View != null;

        public bool HasFinishPickerView =>
            FinishPickerView != null;

        public bool HasFloorFinishPickerView =>
            FloorFinishPickerView != null;

        public bool HasDoorDefinitionPickerView =>
            DoorDefinitionPickerView != null;

        public bool HasFixtureDefinitionPickerView =>
            FixtureDefinitionPickerView != null;

        public bool HasDepartmentPickerView =>
            DepartmentPickerView != null;

        public bool HasFixtureMerchandisingInspectorView =>
            FixtureMerchandisingInspectorView != null;

        public bool HasSimulationClockView =>
            SimulationClockView != null;

        /// <summary>
        /// Returns true when a screen position is currently over a pickable
        /// element in this construction UI document. The document root itself
        /// is intentionally ignored, so empty game space remains available to
        /// construction tools.
        /// </summary>
        public bool IsPointerOverInteractiveElement(
            Vector2 screenPosition)
        {
            if (rootElement == null
                || rootElement.panel == null)
            {
                return false;
            }

            Vector2 uiToolkitScreenPosition =
                ToUiToolkitScreenPosition(
                    screenPosition,
                    Screen.height);

            Vector2 panelPosition =
                RuntimePanelUtils.ScreenToPanel(
                    rootElement.panel,
                    uiToolkitScreenPosition);

            VisualElement pickedElement =
                rootElement.panel.Pick(panelPosition);

            // The panel root spans the full screen, but it is not a visible
            // construction control. Only an actual descendant should block
            // construction targeting.
            return pickedElement != null
                && pickedElement != rootElement;
        }

        internal static Vector2 ToUiToolkitScreenPosition(
            Vector2 screenPosition,
            float screenHeight)
        {
            screenPosition.y =
                screenHeight - screenPosition.y;

            return screenPosition;
        }

        public event Action<ConstructionToolbarView> ViewReady;

        public event Action<WallFinishPickerView> FinishPickerViewReady;

        public event Action<FloorFinishPickerView> FloorFinishPickerViewReady;

        public event Action<DoorDefinitionPickerView>
            DoorDefinitionPickerViewReady;

        public event Action<FixtureDefinitionPickerView>
            FixtureDefinitionPickerViewReady;

        public event Action<DepartmentPickerView> DepartmentPickerViewReady;

        public event Action<FixtureMerchandisingInspectorView>
            FixtureMerchandisingInspectorViewReady;

        public event Action<SimulationClockView> SimulationClockViewReady;

        private int loadedVersion = -1;

        private VisualElement rootElement;


        private void Reset()
        {
            panelRenderer =
                GetComponent<PanelRenderer>();
        }


        private void Awake()
        {
            if (panelRenderer == null)
            {
                panelRenderer =
                    GetComponent<PanelRenderer>();
            }
        }


        private void OnEnable()
        {
            if (panelRenderer == null)
            {
                Debug.LogError(
                    "ConstructionToolbarDocumentHost has no PanelRenderer assigned.",
                    this);
                return;
            }

            panelRenderer.RegisterUIReloadCallback(
                HandleUIReload);
        }


        private void OnDisable()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(
                    HandleUIReload);
            }

            DisposeViews();
            loadedVersion = -1;
        }


        private void HandleUIReload(
            PanelRenderer source,
            VisualElement root,
            int version)
        {
            if (root == null)
            {
                Debug.LogError(
                    "ConstructionToolbarDocumentHost received no root VisualElement.",
                    this);
                return;
            }

            if (View != null
                && FinishPickerView != null
                && FloorFinishPickerView != null
                && DoorDefinitionPickerView != null
                && FixtureDefinitionPickerView != null
                && DepartmentPickerView != null
                && FixtureMerchandisingInspectorView != null
                && SimulationClockView != null
                && loadedVersion == version)
            {
                return;
            }

            DisposeViews();

            rootElement = root;

            try
            {
                View =
                    new ConstructionToolbarView(
                        root);

                FinishPickerView =
                    new WallFinishPickerView(
                        root);

                FloorFinishPickerView =
                    new FloorFinishPickerView(
                        root);

                DoorDefinitionPickerView =
                    new DoorDefinitionPickerView(
                        root);

                FixtureDefinitionPickerView =
                    new FixtureDefinitionPickerView(
                        root);

                DepartmentPickerView =
                    new DepartmentPickerView(
                        root);

                FixtureMerchandisingInspectorView =
                    new FixtureMerchandisingInspectorView(
                        root);

                SimulationClockView =
                    new SimulationClockView(
                        root);

                loadedVersion =
                    version;

                ViewReady?.Invoke(
                    View);

                FinishPickerViewReady?.Invoke(
                    FinishPickerView);

                FloorFinishPickerViewReady?.Invoke(
                    FloorFinishPickerView);

                DoorDefinitionPickerViewReady?.Invoke(
                    DoorDefinitionPickerView);

                FixtureDefinitionPickerViewReady?.Invoke(
                    FixtureDefinitionPickerView);

                DepartmentPickerViewReady?.Invoke(
                    DepartmentPickerView);

                FixtureMerchandisingInspectorViewReady?.Invoke(
                    FixtureMerchandisingInspectorView);

                SimulationClockViewReady?.Invoke(
                    SimulationClockView);
            }
            catch (Exception exception)
            {
                DisposeViews();
                loadedVersion = -1;

                Debug.LogError(
                    $"ConstructionToolbarDocumentHost could not create its views: {exception.Message}",
                    this);
            }
        }


        private void DisposeViews()
        {
            rootElement = null;

            if (View != null)
            {
                View.Dispose();
                View = null;
            }

            if (FinishPickerView != null)
            {
                FinishPickerView.Dispose();
                FinishPickerView = null;
            }

            if (FloorFinishPickerView != null)
            {
                FloorFinishPickerView.Dispose();
                FloorFinishPickerView = null;
            }

            if (DoorDefinitionPickerView != null)
            {
                DoorDefinitionPickerView.Dispose();
                DoorDefinitionPickerView = null;
            }

            if (FixtureDefinitionPickerView != null)
            {
                FixtureDefinitionPickerView.Dispose();
                FixtureDefinitionPickerView = null;
            }

            if (DepartmentPickerView != null)
            {
                DepartmentPickerView.Dispose();
                DepartmentPickerView = null;
            }

            if (FixtureMerchandisingInspectorView != null)
            {
                FixtureMerchandisingInspectorView.Dispose();
                FixtureMerchandisingInspectorView = null;
            }

            if (SimulationClockView != null)
            {
                SimulationClockView.Dispose();
                SimulationClockView = null;
            }
        }
    }
}
