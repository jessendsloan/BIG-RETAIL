using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Owns the runtime lifecycle of the Purchasing UI Toolkit document.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class PurchasingWorkspaceDocumentHost : MonoBehaviour
    {
        [SerializeField]
        private PanelRenderer panelRenderer;

        private int loadedVersion = -1;


        public PurchasingWorkspaceView View { get; private set; }

        public bool HasView => View != null;


        public event Action<PurchasingWorkspaceView> ViewReady;


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
                    "PurchasingWorkspaceDocumentHost has no PanelRenderer assigned.",
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
                    "PurchasingWorkspaceDocumentHost received no root element.",
                    this);
                return;
            }

            if (View != null && loadedVersion == version)
            {
                return;
            }

            DisposeView();

            try
            {
                View = new PurchasingWorkspaceView(root);
                loadedVersion = version;
                ViewReady?.Invoke(View);
            }
            catch (Exception exception)
            {
                DisposeView();
                loadedVersion = -1;
                Debug.LogError(
                    $"Purchasing workspace could not create its view: {exception.Message}",
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
