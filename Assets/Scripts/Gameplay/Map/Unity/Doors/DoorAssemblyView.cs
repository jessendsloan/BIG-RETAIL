using System;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Layered presentation for one complete door assembly. The frame remains
    /// static while the two center panels slide outward over a short,
    /// presentation-only open or close transition.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorAssemblyView : MonoBehaviour
    {
        public const int RequiredPanelCount = 4;

        private const float OpeningDurationSeconds = 1.5f;
        private const float ClosingDurationSeconds = 2.25f;
        private const float MaximumAnimationDeltaTimeSeconds =
            1f / 30f;

        // Door panels share the nearest supporting wall's depth slot. A
        // perpendicular wall closer to the viewer can then occlude the whole
        // assembly cleanly instead of landing between its glass and frame.
        public const int SortingOrderOffsetFromSupportingWall = 0;


        private SpriteRenderer frameRenderer;
        private SpriteRenderer leftGlassRenderer;
        private SpriteRenderer leftDoorRenderer;
        private SpriteRenderer rightDoorRenderer;
        private SpriteRenderer rightGlassRenderer;

        private Vector3 leftDoorClosedLocalPosition;
        private Vector3 leftDoorOpenLocalPosition;
        private Vector3 rightDoorClosedLocalPosition;
        private Vector3 rightDoorOpenLocalPosition;

        private bool hasPresentation;
        private float openProgress;
        private float targetOpenProgress;


        public DoorAssemblyId AssemblyId { get; private set; }

        public Transform LeftDoorTransform =>
            leftDoorRenderer != null
                ? leftDoorRenderer.transform
                : null;

        public Transform RightDoorTransform =>
            rightDoorRenderer != null
                ? rightDoorRenderer.transform
                : null;

        public float OpenProgress =>
            openProgress;

        public float TargetOpenProgress =>
            targetOpenProgress;

        public bool IsAnimating =>
            !Mathf.Approximately(
                openProgress,
                targetOpenProgress);


        public void Initialize(
            DoorAssemblyId assemblyId)
        {
            if (!assemblyId.IsValid)
            {
                throw new ArgumentException(
                    "A door assembly view requires a valid assembly ID.",
                    nameof(assemblyId));
            }

            AssemblyId = assemblyId;

            leftGlassRenderer =
                CreateLayer("Left Fixed Glass");

            leftDoorRenderer =
                CreateLayer("Left Sliding Door");

            rightDoorRenderer =
                CreateLayer("Right Sliding Door");

            rightGlassRenderer =
                CreateLayer("Right Fixed Glass");

            frameRenderer =
                CreateLayer("Static Door Frame");

            gameObject.name =
                $"Door Assembly {assemblyId}";

            // Idle doors do not need a per-frame callback. Open and Close
            // re-enable the component only while a transition is active.
            enabled = false;
        }


        [ContextMenu("Open Door")]
        public void Open()
        {
            SetOpen(
                true);
        }


        [ContextMenu("Close Door")]
        public void Close()
        {
            SetOpen(
                false);
        }


        [ContextMenu("Toggle Door")]
        public void Toggle()
        {
            SetOpen(
                targetOpenProgress < 0.5f);
        }


        public void SetOpen(
            bool shouldOpen)
        {
            targetOpenProgress =
                shouldOpen
                    ? 1f
                    : 0f;

            enabled =
                hasPresentation
                && IsAnimating;
        }


        public void SetOpenProgressImmediately(
            float progress)
        {
            openProgress =
                Mathf.Clamp01(
                    progress);

            targetOpenProgress =
                openProgress;

            ApplySlidingDoorPositions();
            enabled = false;
        }


        public void ApplyPresentation(
            DoorAssemblySprites sprites,
            Vector3[] screenOrderedPanelPositions,
            Vector3 worldPosition,
            int sortingLayerId,
            int sortingOrder,
            int rendererPriority,
            Material sharedMaterial,
            Color tint)
        {
            ValidateInitialization();

            if (screenOrderedPanelPositions == null
                || screenOrderedPanelPositions.Length
                    != RequiredPanelCount)
            {
                throw new ArgumentException(
                    $"A layered door view requires exactly "
                    + $"{RequiredPanelCount} screen-ordered panel positions.",
                    nameof(screenOrderedPanelPositions));
            }

            transform.SetPositionAndRotation(
                worldPosition,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            leftDoorClosedLocalPosition =
                screenOrderedPanelPositions[1]
                - worldPosition;

            leftDoorOpenLocalPosition =
                screenOrderedPanelPositions[0]
                - worldPosition;

            rightDoorClosedLocalPosition =
                screenOrderedPanelPositions[2]
                - worldPosition;

            rightDoorOpenLocalPosition =
                screenOrderedPanelPositions[3]
                - worldPosition;

            hasPresentation = true;

            ApplyLayer(
                leftGlassRenderer,
                sprites.LeftGlass,
                screenOrderedPanelPositions[0] - worldPosition,
                sortingLayerId,
                sortingOrder,
                rendererPriority,
                sharedMaterial,
                tint);

            ApplyLayer(
                leftDoorRenderer,
                sprites.LeftDoor,
                ResolveLeftDoorLocalPosition(),
                sortingLayerId,
                sortingOrder,
                rendererPriority + 1,
                sharedMaterial,
                tint);

            ApplyLayer(
                rightDoorRenderer,
                sprites.RightDoor,
                ResolveRightDoorLocalPosition(),
                sortingLayerId,
                sortingOrder,
                rendererPriority + 2,
                sharedMaterial,
                tint);

            ApplyLayer(
                rightGlassRenderer,
                sprites.RightGlass,
                screenOrderedPanelPositions[3] - worldPosition,
                sortingLayerId,
                sortingOrder,
                rendererPriority + 3,
                sharedMaterial,
                tint);

            ApplyLayer(
                frameRenderer,
                sprites.Frame,
                Vector3.zero,
                sortingLayerId,
                sortingOrder,
                rendererPriority + 4,
                sharedMaterial,
                tint);

            enabled =
                IsAnimating;
        }


        private void Update()
        {
            if (!hasPresentation
                || !IsAnimating)
            {
                enabled = false;
                return;
            }

            openProgress =
                Mathf.MoveTowards(
                    openProgress,
                    targetOpenProgress,
                    Mathf.Min(
                        Time.deltaTime,
                        MaximumAnimationDeltaTimeSeconds)
                    / ResolveTransitionDuration());

            ApplySlidingDoorPositions();

            if (!IsAnimating)
            {
                enabled = false;
            }
        }


        private void ApplySlidingDoorPositions()
        {
            if (!hasPresentation)
            {
                return;
            }

            leftDoorRenderer.transform.localPosition =
                ResolveLeftDoorLocalPosition();

            rightDoorRenderer.transform.localPosition =
                ResolveRightDoorLocalPosition();
        }


        private Vector3 ResolveLeftDoorLocalPosition()
        {
            return Vector3.Lerp(
                leftDoorClosedLocalPosition,
                leftDoorOpenLocalPosition,
                ResolveEasedOpenProgress());
        }


        private Vector3 ResolveRightDoorLocalPosition()
        {
            return Vector3.Lerp(
                rightDoorClosedLocalPosition,
                rightDoorOpenLocalPosition,
                ResolveEasedOpenProgress());
        }


        private float ResolveTransitionDuration()
        {
            return targetOpenProgress
                    > openProgress
                ? OpeningDurationSeconds
                : ClosingDurationSeconds;
        }


        private float ResolveEasedOpenProgress()
        {
            return Mathf.SmoothStep(
                0f,
                1f,
                openProgress);
        }


        private SpriteRenderer CreateLayer(
            string layerName)
        {
            GameObject layer =
                new GameObject(layerName);

            layer.transform.SetParent(
                transform,
                false);

            SpriteRenderer renderer =
                layer.AddComponent<SpriteRenderer>();

            renderer.color =
                Color.white;

            return renderer;
        }


        private static void ApplyLayer(
            SpriteRenderer renderer,
            Sprite sprite,
            Vector3 localPosition,
            int sortingLayerId,
            int sortingOrder,
            int rendererPriority,
            Material sharedMaterial,
            Color tint)
        {
            renderer.sprite =
                sprite;

            renderer.sortingLayerID =
                sortingLayerId;

            renderer.sortingOrder =
                sortingOrder;

            renderer.rendererPriority =
                rendererPriority;

            renderer.color =
                tint;

            if (sharedMaterial != null)
            {
                renderer.sharedMaterial =
                    sharedMaterial;
            }

            renderer.transform.localPosition =
                localPosition;

            renderer.transform.localRotation =
                Quaternion.identity;

            renderer.transform.localScale =
                Vector3.one;
        }


        private void ValidateInitialization()
        {
            if (frameRenderer == null
                || leftGlassRenderer == null
                || leftDoorRenderer == null
                || rightDoorRenderer == null
                || rightGlassRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DoorAssemblyView)} on '{name}' has not been "
                    + "initialized.");
            }
        }
    }
}
