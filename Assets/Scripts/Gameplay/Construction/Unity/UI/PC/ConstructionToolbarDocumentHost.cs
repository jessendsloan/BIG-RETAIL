using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Owns the runtime lifecycle of the PC construction toolbar document.
    /// Gameplay presenters can observe the created view without coupling the
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

        public bool HasView => View != null;

        public event Action<ConstructionToolbarView> ViewReady;

        private int loadedVersion = -1;

        private void Reset()
        {
            panelRenderer = GetComponent<PanelRenderer>();
        }

        private void Awake()
        {
            if (panelRenderer == null)
            {
                panelRenderer = GetComponent<PanelRenderer>();
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

            panelRenderer.RegisterUIReloadCallback(HandleUIReload);
        }

        private void OnDisable()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(HandleUIReload);
            }

            DisposeView();
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
                && loadedVersion == version)
            {
                return;
            }

            DisposeView();

            try
            {
                View = new ConstructionToolbarView(root);
                loadedVersion = version;
                ViewReady?.Invoke(View);
            }
            catch (Exception exception)
            {
                loadedVersion = -1;
                Debug.LogError(
                    $"ConstructionToolbarDocumentHost could not create its view: {exception.Message}",
                    this);
            }
        }

        private void DisposeView()
        {
            if (View == null)
            {
                return;
            }

            View.Dispose();
            View = null;
        }
    }
}
