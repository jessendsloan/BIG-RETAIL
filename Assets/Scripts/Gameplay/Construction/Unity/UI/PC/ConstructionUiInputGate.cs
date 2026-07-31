using BigRetail.Construction.Unity.Input;
using BigRetail.Construction.Unity.Tools;
using UnityEngine;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Gives the construction UI first claim on the shared construction
    /// pointer. Entering a real UI control cancels a live map gesture, while
    /// empty screen space remains available to construction targeting.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ConstructionToolbarDocumentHost))]
    [DefaultExecutionOrder(125)]
    public sealed class ConstructionUiInputGate : MonoBehaviour
    {
        [SerializeField]
        private ConstructionToolbarDocumentHost documentHost;

        [SerializeField]
        private ConstructionPointerController pointerController;

        [SerializeField]
        private ConstructionToolCoordinator toolCoordinator;

        public bool IsPointerOverConstructionUi { get; private set; }

        private void Reset()
        {
            documentHost =
                GetComponent<ConstructionToolbarDocumentHost>();
        }

        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost =
                    GetComponent<ConstructionToolbarDocumentHost>();
            }

            if (documentHost == null
                || pointerController == null
                || toolCoordinator == null)
            {
                Debug.LogError(
                    "ConstructionUiInputGate requires a document host, "
                    + "construction pointer, and tool coordinator.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            bool wasOverConstructionUi =
                IsPointerOverConstructionUi;

            IsPointerOverConstructionUi =
                documentHost.IsPointerOverInteractiveElement(
                    pointerController.ScreenPosition);

            if (!wasOverConstructionUi
                && IsPointerOverConstructionUi)
            {
                toolCoordinator.CancelActiveGesture();
            }
        }

        private void OnDisable()
        {
            IsPointerOverConstructionUi = false;
        }
    }
}
