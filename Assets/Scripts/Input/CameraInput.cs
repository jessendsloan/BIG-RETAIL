using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Input
{
    /// <summary>
    /// Reads camera-navigation intent from the PlayerInput-owned actions.
    /// It does not move the camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class CameraInput : MonoBehaviour
    {
        private const string PanActionPath = "Camera/Pan";
        private const string ZoomActionPath = "Camera/Zoom";
        private const string RotateClockwiseActionPath =
            "Camera/RotateClockwise";
        private const string RotateCounterClockwiseActionPath =
            "Camera/RotateCounterClockwise";

        private InputAction panAction;
        private InputAction zoomAction;
        private InputAction rotateClockwiseAction;
        private InputAction rotateCounterClockwiseAction;

        public Vector2 Pan =>
            panAction != null
                ? panAction.ReadValue<Vector2>()
                : Vector2.zero;

        public float Zoom =>
            zoomAction != null
                ? zoomAction.ReadValue<float>()
                : 0f;

        public bool RotateClockwisePressedThisFrame =>
            rotateClockwiseAction != null
            && rotateClockwiseAction.WasPressedThisFrame();

        public bool RotateCounterClockwisePressedThisFrame =>
            rotateCounterClockwiseAction != null
            && rotateCounterClockwiseAction.WasPressedThisFrame();

        private void Awake()
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();

            if (playerInput.actions == null)
            {
                Debug.LogError(
                    "CameraInput could not find an Input Actions asset on PlayerInput.",
                    this);

                enabled = false;
                return;
            }

            panAction = playerInput.actions.FindAction(PanActionPath);
            zoomAction = playerInput.actions.FindAction(ZoomActionPath);
            rotateClockwiseAction =
                playerInput.actions.FindAction(
                    RotateClockwiseActionPath);

            rotateCounterClockwiseAction =
                playerInput.actions.FindAction(
                    RotateCounterClockwiseActionPath);

            if (panAction == null
                || zoomAction == null
                || rotateClockwiseAction == null
                || rotateCounterClockwiseAction == null)
            {
                Debug.LogError(
                    $"CameraInput requires actions named " +
                    $"'{PanActionPath}', '{ZoomActionPath}', " +
                    $"'{RotateClockwiseActionPath}', and " +
                    $"'{RotateCounterClockwiseActionPath}'.",
                    this);

                enabled = false;
            }
        }
    }
}
