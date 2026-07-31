using BigRetail.Construction.Unity.Tools;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.CameraControl
{
    /// <summary>
    /// Executes one complete isometric view turn:
    /// cancel construction intent, preserve the logical camera landmark,
    /// rotate the map presentation, rebuild bounds, and restore focus.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IsometricViewRotationController :
        MonoBehaviour
    {
        [Header("Input")]

        [SerializeField]
        private BigRetail.Input.CameraInput cameraInput;


        [Header("View Rotation")]

        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private ConstructionToolCoordinator
            constructionToolCoordinator;


        [Header("Camera")]

        [SerializeField]
        private CameraController cameraController;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [Tooltip(
            "World-space breathing room added beyond the projected map " +
            "envelope before camera clamping.")]
        [SerializeField]
        private Vector2 boundsPadding =
            new Vector2(
                4f,
                4f);


        [Header("Diagnostics")]

        [SerializeField]
        private bool logRotations = true;


        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
            }
        }


        private void Update()
        {
            bool rotateClockwise =
                cameraInput.RotateClockwisePressedThisFrame;

            bool rotateCounterClockwise =
                cameraInput.RotateCounterClockwisePressedThisFrame;

            if (rotateClockwise == rotateCounterClockwise)
            {
                return;
            }

            Rotate(
                rotateClockwise);
        }


        [ContextMenu("Rotate View Clockwise")]
        public void RotateViewClockwise()
        {
            if (RequirePlayMode())
            {
                Rotate(
                    clockwise: true);
            }
        }


        [ContextMenu("Rotate View Counterclockwise")]
        public void RotateViewCounterClockwise()
        {
            if (RequirePlayMode())
            {
                Rotate(
                    clockwise: false);
            }
        }


        private void Rotate(
            bool clockwise)
        {
            if (!viewHost.TryInitialize())
            {
                Debug.LogError(
                    "The isometric view could not initialize before " +
                    "rotation.",
                    this);

                return;
            }

            GridPosition logicalFocus =
                viewHost.WorldToLogicalCell(
                    cameraController.WorldCenter,
                    coordinateTilemap);

            constructionToolCoordinator
                .CancelActiveGesture();

            bool changed =
                clockwise
                    ? viewHost.RotateClockwise()
                    : viewHost.RotateCounterClockwise();

            if (!changed)
            {
                return;
            }

            Bounds projectedBounds =
                viewHost.CalculateProjectedWorldBounds(
                    coordinateTilemap);

            projectedBounds.Expand(
                new Vector3(
                    boundsPadding.x * 2f,
                    boundsPadding.y * 2f,
                    0f));

            cameraController.SetWorldBounds(
                projectedBounds);

            cameraController.SetWorldCenter(
                viewHost.GetLogicalCellCenterWorld(
                    logicalFocus,
                    coordinateTilemap));

            cameraController.ClampCurrentView();

            if (logRotations)
            {
                Debug.Log(
                    $"Isometric view rotated to " +
                    $"{viewHost.Orientation}. " +
                    $"Logical camera focus: {logicalFocus}.",
                    this);
            }
        }


        private bool ValidateReferences()
        {
            bool isValid = true;

            if (cameraInput == null)
            {
                Debug.LogError(
                    "IsometricViewRotationController has no " +
                    "CameraInput assigned.",
                    this);

                isValid = false;
            }

            if (viewHost == null)
            {
                Debug.LogError(
                    "IsometricViewRotationController has no " +
                    "IsometricViewHost assigned.",
                    this);

                isValid = false;
            }

            if (constructionToolCoordinator == null)
            {
                Debug.LogError(
                    "IsometricViewRotationController has no " +
                    "ConstructionToolCoordinator assigned.",
                    this);

                isValid = false;
            }

            if (cameraController == null)
            {
                Debug.LogError(
                    "IsometricViewRotationController has no " +
                    "CameraController assigned.",
                    this);

                isValid = false;
            }

            if (coordinateTilemap == null)
            {
                Debug.LogError(
                    "IsometricViewRotationController has no coordinate " +
                    "Tilemap assigned.",
                    this);

                isValid = false;
            }

            return isValid;
        }


        private bool RequirePlayMode()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning(
                "The isometric view can only rotate during Play Mode.",
                this);

            return false;
        }


        private void OnValidate()
        {
            boundsPadding.x =
                Mathf.Max(
                    0f,
                    boundsPadding.x);

            boundsPadding.y =
                Mathf.Max(
                    0f,
                    boundsPadding.y);
        }
    }
}
