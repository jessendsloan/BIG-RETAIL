using System;
using BigRetail.Construction.Unity.Input;
using BigRetail.Construction.Unity.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Action Names")]

        [SerializeField]
        private string constructionActionMapName =
            "Construction";

        [SerializeField]
        private string cancelActionName =
            "Cancel";

        public bool IsPointerOverConstructionUi { get; private set; }

        public event Action CancelRequested;

        private InputAction cancelAction;

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
                return;
            }

            if (!TryResolveCancelAction())
            {
                enabled = false;
            }
        }

        private void Update()
        {
            if (cancelAction.WasPressedThisFrame())
            {
                CancelRequested?.Invoke();
            }

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

        private bool TryResolveCancelAction()
        {
            PlayerInput playerInput =
                pointerController.GetComponent<PlayerInput>();

            if (playerInput == null
                || playerInput.actions == null)
            {
                Debug.LogError(
                    "ConstructionUiInputGate could not access the "
                    + "construction Input Actions asset.",
                    this);

                return false;
            }

            InputActionMap constructionActionMap =
                playerInput.actions.FindActionMap(
                    constructionActionMapName,
                    throwIfNotFound: false);

            if (constructionActionMap == null)
            {
                Debug.LogError(
                    $"ConstructionUiInputGate could not find the "
                    + $"'{constructionActionMapName}' action map.",
                    this);

                return false;
            }

            cancelAction =
                constructionActionMap.FindAction(
                    cancelActionName,
                    throwIfNotFound: false);

            if (cancelAction != null)
            {
                return true;
            }

            Debug.LogError(
                $"ConstructionUiInputGate could not find the "
                + $"'{cancelActionName}' action in the "
                + $"'{constructionActionMapName}' action map.",
                this);

            return false;
        }
    }
}
