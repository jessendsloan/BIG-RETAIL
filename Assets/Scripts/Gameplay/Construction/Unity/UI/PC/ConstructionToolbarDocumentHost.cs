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

        public bool HasView =>
            View != null;

        public bool HasFinishPickerView =>
            FinishPickerView != null;

        public bool HasFloorFinishPickerView =>
            FloorFinishPickerView != null;

        public event Action<ConstructionToolbarView> ViewReady;

        public event Action<WallFinishPickerView> FinishPickerViewReady;

        public event Action<FloorFinishPickerView> FloorFinishPickerViewReady;

        private int loadedVersion = -1;


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
                && loadedVersion == version)
            {
                return;
            }

            DisposeViews();

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

                loadedVersion =
                    version;

                ViewReady?.Invoke(
                    View);

                FinishPickerViewReady?.Invoke(
                    FinishPickerView);

                FloorFinishPickerViewReady?.Invoke(
                    FloorFinishPickerView);
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
        }
    }
}
