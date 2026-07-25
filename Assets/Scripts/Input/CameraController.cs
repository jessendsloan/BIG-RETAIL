using BigRetail.Construction.Unity.Input;
using BigRetail.Input;
using UnityEngine;

namespace BigRetail.CameraControl
{
    /// <summary>
    /// Moves and zooms an orthographic camera while keeping its visible
    /// frame inside an authored world boundary.
    ///
    /// Camera movement can come from:
    /// - Direct keyboard camera input.
    /// - Gamepad virtual-cursor edge-pan intent.
    ///
    /// Both sources pass through the same movement, zoom-scaling,
    /// and boundary-clamping pipeline.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraController : MonoBehaviour
    {
        [Header("Input References")]

        [Tooltip(
            "Reads direct camera navigation input, such as keyboard pan " +
            "and mouse/gamepad zoom.")]
        [SerializeField]
        private CameraInput cameraInput;

        [Tooltip(
            "Provides camera-pan intent when the gamepad virtual cursor " +
            "is pressed against a screen edge.")]
        [SerializeField]
        private ConstructionPointerController constructionPointer;


        [Header("Camera References")]

        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private BoxCollider2D cameraBounds;


        [Header("Starting View")]

        [Tooltip(
            "The position where the camera begins when Gameplay loads.")]
        [SerializeField]
        private Transform cameraStartMarker;

        [Tooltip(
            "The camera's starting orthographic size.")]
        [SerializeField, Min(0.1f)]
        private float startingZoom = 10f;


        [Header("Panning")]

        [SerializeField, Min(0f)]
        private float panSpeed = 20f;

        [Tooltip(
            "Makes the camera move faster while zoomed farther out.")]
        [SerializeField]
        private bool scalePanSpeedWithZoom = true;

        [SerializeField, Min(0.1f)]
        private float referenceZoom = 10f;


        [Header("Zooming")]

        [SerializeField, Min(0f)]
        private float zoomSpeed = 12f;

        [SerializeField, Min(0.1f)]
        private float minimumZoom = 3f;

        [SerializeField, Min(0.1f)]
        private float maximumZoom = 35f;


        private void Awake()
        {
            if (!ValidateCriticalReferences())
            {
                enabled = false;
                return;
            }

            ValidateOptionalReferences();
        }


        private void Start()
        {
            ApplyStartingView();

            // Ensure the authored starting view is legal before
            // the player receives control.
            ClampZoomToBounds();
            ClampPositionToBounds();
        }


        /// <summary>
        /// Camera movement runs after ordinary Update methods.
        ///
        /// This allows ConstructionPointerController.Update() to calculate
        /// the current frame's EdgePanIntent before the camera consumes it.
        /// </summary>
        private void LateUpdate()
        {
            ApplyZoom();
            ApplyPan();

            ClampPositionToBounds();
        }


        /// <summary>
        /// Places the camera at the Camera Start Marker and applies
        /// the configured starting zoom.
        /// </summary>
        private void ApplyStartingView()
        {
            if (cameraStartMarker != null)
            {
                Vector3 startingPosition =
                    transform.position;

                startingPosition.x =
                    cameraStartMarker.position.x;

                startingPosition.y =
                    cameraStartMarker.position.y;

                // Preserve the CameraRig's existing Z position.
                transform.position =
                    startingPosition;
            }

            targetCamera.orthographicSize =
                startingZoom;
        }


        private void ApplyPan()
        {
            Vector2 panInput =
                GetCombinedPanInput();

            // Prevent diagonal movement from becoming faster than
            // horizontal or vertical movement.
            if (panInput.sqrMagnitude > 1f)
            {
                panInput.Normalize();
            }

            float speedMultiplier =
                CalculatePanSpeedMultiplier();

            Vector3 movement =
                new Vector3(
                    panInput.x,
                    panInput.y,
                    0f);

            transform.position +=
                movement
                * panSpeed
                * speedMultiplier
                * Time.unscaledDeltaTime;
        }


        /// <summary>
        /// Combines every currently valid source of camera-pan intent.
        ///
        /// Keyboard input and virtual-cursor edge panning do not move
        /// the camera independently. They feed this one shared result.
        /// </summary>
        private Vector2 GetCombinedPanInput()
        {
            Vector2 directPanInput =
                cameraInput.Pan;

            Vector2 edgePanInput =
                constructionPointer != null
                    ? constructionPointer.EdgePanIntent
                    : Vector2.zero;

            return directPanInput
                + edgePanInput;
        }


        private float CalculatePanSpeedMultiplier()
        {
            if (!scalePanSpeedWithZoom)
            {
                return 1f;
            }

            return
                targetCamera.orthographicSize
                / referenceZoom;
        }


        private void ApplyZoom()
        {
            float nextZoom =
                targetCamera.orthographicSize
                + cameraInput.Zoom
                * zoomSpeed
                * Time.unscaledDeltaTime;

            targetCamera.orthographicSize =
                nextZoom;

            ClampZoomToBounds();
        }


        /// <summary>
        /// Keeps the camera zoom inside both our configured limits
        /// and the largest view that can fit inside CameraBounds.
        /// </summary>
        private void ClampZoomToBounds()
        {
            float maximumZoomThatFits =
                CalculateMaximumZoomThatFitsBounds();

            float effectiveMaximumZoom =
                Mathf.Min(
                    maximumZoom,
                    maximumZoomThatFits);

            // On an unusually small map, the normal minimum zoom
            // might be larger than the entire boundary.
            float effectiveMinimumZoom =
                Mathf.Min(
                    minimumZoom,
                    effectiveMaximumZoom);

            targetCamera.orthographicSize =
                Mathf.Clamp(
                    targetCamera.orthographicSize,
                    effectiveMinimumZoom,
                    effectiveMaximumZoom);
        }


        private float CalculateMaximumZoomThatFitsBounds()
        {
            Bounds bounds =
                cameraBounds.bounds;

            float safeAspect =
                Mathf.Max(
                    targetCamera.aspect,
                    0.01f);

            float maximumFromHeight =
                bounds.extents.y;

            float maximumFromWidth =
                bounds.extents.x
                / safeAspect;

            return Mathf.Max(
                0.01f,
                Mathf.Min(
                    maximumFromHeight,
                    maximumFromWidth));
        }


        /// <summary>
        /// Keeps every edge of the visible camera frame inside
        /// the CameraBounds rectangle.
        /// </summary>
        private void ClampPositionToBounds()
        {
            Bounds bounds =
                cameraBounds.bounds;

            float cameraHalfHeight =
                targetCamera.orthographicSize;

            float cameraHalfWidth =
                cameraHalfHeight
                * targetCamera.aspect;

            float minimumX =
                bounds.min.x
                + cameraHalfWidth;

            float maximumX =
                bounds.max.x
                - cameraHalfWidth;

            float minimumY =
                bounds.min.y
                + cameraHalfHeight;

            float maximumY =
                bounds.max.y
                - cameraHalfHeight;

            Vector3 position =
                transform.position;

            position.x =
                ClampAxis(
                    position.x,
                    minimumX,
                    maximumX,
                    bounds.center.x);

            position.y =
                ClampAxis(
                    position.y,
                    minimumY,
                    maximumY,
                    bounds.center.y);

            transform.position =
                position;
        }


        private static float ClampAxis(
            float value,
            float minimum,
            float maximum,
            float fallbackCenter)
        {
            // This occurs when the visible camera frame is larger
            // than the boundary along this axis.
            if (minimum > maximum)
            {
                return fallbackCenter;
            }

            return Mathf.Clamp(
                value,
                minimum,
                maximum);
        }


        private bool ValidateCriticalReferences()
        {
            bool isValid = true;

            if (cameraInput == null)
            {
                Debug.LogError(
                    "CameraController has no CameraInput assigned.",
                    this);

                isValid = false;
            }

            if (targetCamera == null)
            {
                Debug.LogError(
                    "CameraController has no target Camera assigned.",
                    this);

                isValid = false;
            }

            if (cameraBounds == null)
            {
                Debug.LogError(
                    "CameraController has no Camera Bounds assigned.",
                    this);

                isValid = false;
            }

            if (!isValid)
            {
                return false;
            }

            if (!targetCamera.orthographic)
            {
                Debug.LogError(
                    "CameraController requires an orthographic camera.",
                    targetCamera);

                isValid = false;
            }

            if (!cameraBounds.enabled
                || !cameraBounds.gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    "Camera Bounds must remain active and enabled.",
                    cameraBounds);

                isValid = false;
            }

            return isValid;
        }


        private void ValidateOptionalReferences()
        {
            if (constructionPointer != null)
            {
                return;
            }

            Debug.LogWarning(
                "CameraController has no ConstructionPointerController " +
                "assigned. Keyboard camera movement will still work, " +
                "but virtual-cursor edge panning will be unavailable.",
                this);
        }


        private void OnValidate()
        {
            minimumZoom =
                Mathf.Max(
                    minimumZoom,
                    0.1f);

            maximumZoom =
                Mathf.Max(
                    maximumZoom,
                    minimumZoom);

            startingZoom =
                Mathf.Max(
                    startingZoom,
                    0.1f);

            referenceZoom =
                Mathf.Max(
                    referenceZoom,
                    0.1f);

            panSpeed =
                Mathf.Max(
                    panSpeed,
                    0f);

            zoomSpeed =
                Mathf.Max(
                    zoomSpeed,
                    0f);
        }
    }
}