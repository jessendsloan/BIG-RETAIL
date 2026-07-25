using UnityEngine;

namespace BigRetail.Construction.Unity.Input
{
    /// <summary>
    /// Displays the gamepad-controlled virtual construction cursor.
    ///
    /// This is presentation only. The pointer controller remains the
    /// authority for the current screen position and input mode.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ConstructionPointerView : MonoBehaviour
    {
        [Header("Pointer")]

        [SerializeField]
        private ConstructionPointerController pointerController;


        [Header("Canvas")]

        [SerializeField]
        private Canvas containingCanvas;

        [SerializeField]
        private RectTransform cursorRectTransform;


        private CanvasGroup canvasGroup;

        private RectTransform canvasRectTransform;


        private void Awake()
        {
            canvasGroup =
                GetComponent<CanvasGroup>();

            if (cursorRectTransform == null)
            {
                cursorRectTransform =
                    GetComponent<RectTransform>();
            }

            if (containingCanvas != null)
            {
                canvasRectTransform =
                    containingCanvas.transform
                        as RectTransform;
            }

            ConfigureCanvasGroup();
        }


        private void LateUpdate()
        {
            if (!ValidateRuntimeReferences())
            {
                SetVisible(false);
                return;
            }

            SetVisible(
                pointerController.IsUsingGamepad);

            if (!pointerController.IsUsingGamepad)
            {
                return;
            }

            MoveToScreenPosition(
                pointerController.ScreenPosition);
        }


        private void MoveToScreenPosition(
            Vector2 screenPosition)
        {
            Camera canvasCamera =
                containingCanvas.renderMode
                    == RenderMode.ScreenSpaceOverlay
                    ? null
                    : containingCanvas.worldCamera;

            bool foundWorldPosition =
                RectTransformUtility
                    .ScreenPointToWorldPointInRectangle(
                        canvasRectTransform,
                        screenPosition,
                        canvasCamera,
                        out Vector3 worldPosition);

            if (!foundWorldPosition)
            {
                return;
            }

            cursorRectTransform.position =
                worldPosition;
        }


        private void SetVisible(
            bool isVisible)
        {
            canvasGroup.alpha =
                isVisible ? 1f : 0f;
        }


        private bool ValidateRuntimeReferences()
        {
            return pointerController != null
                && containingCanvas != null
                && canvasRectTransform != null
                && cursorRectTransform != null;
        }


        private void ConfigureCanvasGroup()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = false;
        }


        private void Reset()
        {
            cursorRectTransform =
                GetComponent<RectTransform>();

            containingCanvas =
                GetComponentInParent<Canvas>();
        }


        private void OnValidate()
        {
            if (cursorRectTransform == null)
            {
                cursorRectTransform =
                    GetComponent<RectTransform>();
            }

            if (containingCanvas == null)
            {
                containingCanvas =
                    GetComponentInParent<Canvas>();
            }
        }
    }
}