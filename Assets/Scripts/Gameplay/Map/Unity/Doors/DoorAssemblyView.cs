using System;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Rendering;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Presentation for one complete door assembly. Automatic doors slide
    /// their center panels outward; single doors switch their moving panel
    /// between closed and perpendicular open display edges; open doorways
    /// render only a permanently static frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorAssemblyView : MonoBehaviour
    {
        public const int RequiredPanelCount = 4;

        public const int RequiredHingedPanelCount = 1;

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
        private SortingGroup slidingSortingGroup;
        private bool ownsSlidingSortingGroup;

        private SpriteRenderer hingedFrameRenderer;
        private SpriteRenderer hingedDoorRenderer;

        private SpriteRenderer doorwayFrameRenderer;

        private Vector3 leftDoorClosedLocalPosition;
        private Vector3 leftDoorOpenLocalPosition;
        private Vector3 rightDoorClosedLocalPosition;
        private Vector3 rightDoorOpenLocalPosition;

        private Sprite hingedDoorClosedSprite;
        private Sprite hingedDoorOpenSprite;
        private Vector3 hingedDoorClosedLocalPosition;
        private Vector3 hingedDoorOpenLocalPosition;
        private int hingedDoorClosedSortingOrder;
        private int hingedDoorOpenSortingOrder;
        private int hingedDoorClosedRendererPriority;
        private int hingedDoorOpenRendererPriority;

        private DoorPresentationStyle presentationStyle;

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

        public Transform HingedDoorTransform =>
            hingedDoorRenderer != null
                ? hingedDoorRenderer.transform
                : null;

        public Transform DoorwayFrameTransform =>
            doorwayFrameRenderer != null
                ? doorwayFrameRenderer.transform
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
            if (hasPresentation
                && presentationStyle
                    == DoorPresentationStyle.StaticDoorway)
            {
                SetOpenProgressImmediately(
                    1f);
                return;
            }

            if (hasPresentation
                && presentationStyle
                    == DoorPresentationStyle.HingedSinglePanel)
            {
                SetOpenProgressImmediately(
                    shouldOpen
                        ? 1f
                        : 0f);
                return;
            }

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

            ApplyDoorMotion();
            enabled = false;
        }


        public void ApplyPresentation(
            DoorAssemblySprites sprites,
            WallDisplaySlope displaySlope,
            DoorViewerSide viewerSide,
            Vector3[] screenOrderedPanelPositions,
            Vector3 worldPosition,
            int sortingLayerId,
            int sortingOrder,
            int rendererPriority,
            Material sharedMaterial,
            Color tint)
        {
            EnsureSlidingLayers();

            if (ownsSlidingSortingGroup)
            {
                slidingSortingGroup.sortingLayerID =
                    sortingLayerId;

                slidingSortingGroup.sortingOrder =
                    sortingOrder;
            }

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

            presentationStyle =
                DoorPresentationStyle.SlidingFourPanel;

            SetSlidingLayersActive(true);
            SetHingedLayersActive(false);
            SetDoorwayLayerActive(false);

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
                sortingOrder
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 0),
                rendererPriority
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 0),
                sharedMaterial,
                tint);

            ApplyLayer(
                leftDoorRenderer,
                sprites.LeftDoor,
                ResolveLeftDoorLocalPosition(),
                sortingLayerId,
                sortingOrder
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 1),
                rendererPriority
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 1),
                sharedMaterial,
                tint);

            ApplyLayer(
                rightDoorRenderer,
                sprites.RightDoor,
                ResolveRightDoorLocalPosition(),
                sortingLayerId,
                sortingOrder
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 2),
                rendererPriority
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 2),
                sharedMaterial,
                tint);

            ApplyLayer(
                rightGlassRenderer,
                sprites.RightGlass,
                screenOrderedPanelPositions[3] - worldPosition,
                sortingLayerId,
                sortingOrder
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 3),
                rendererPriority
                    + ResolveSlidingPanelDepthOffset(
                        displaySlope,
                        viewerSide,
                        screenPanelIndex: 3),
                sharedMaterial,
                tint);

            ApplyLayer(
                frameRenderer,
                sprites.Frame,
                Vector3.zero,
                sortingLayerId,
                sortingOrder + 4,
                rendererPriority + 4,
                sharedMaterial,
                tint);

            enabled =
                IsAnimating;
        }


        private static int ResolveSlidingPanelDepthOffset(
            WallDisplaySlope displaySlope,
            DoorViewerSide viewerSide,
            int screenPanelIndex)
        {
            if (screenPanelIndex < 0
                || screenPanelIndex >= RequiredPanelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(screenPanelIndex));
            }

            if (viewerSide != DoorViewerSide.Outside
                && viewerSide != DoorViewerSide.Inside)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(viewerSide),
                    viewerSide,
                    "Unsupported door viewer side.");
            }

            bool isLeftPanel =
                screenPanelIndex <= 1;

            bool isSlidingDoor =
                screenPanelIndex == 1
                || screenPanelIndex == 2;

            int sideOffset =
                displaySlope switch
                {
                    WallDisplaySlope.RisingLeft =>
                        isLeftPanel
                            ? 0
                            : 1,

                    WallDisplaySlope.RisingRight =>
                        isLeftPanel
                            ? 1
                            : 0,

                    _ =>
                        throw new ArgumentOutOfRangeException(
                            nameof(displaySlope),
                            displaySlope,
                            "Unsupported wall display slope.")
                };

            // Outside the building, the moving doors sit in front of the
            // fixed glass. Inside, that physical stack is viewed from the
            // reverse side, so the moving doors sit behind the fixed glass.
            bool movingPanelsAreHigher =
                viewerSide == DoorViewerSide.Outside;

            bool belongsToHigherGroup =
                isSlidingDoor == movingPanelsAreHigher;

            int depthOffset =
                (belongsToHigherGroup ? 2 : 0)
                + sideOffset;

            return depthOffset;
        }


        public void ApplyHingedPresentation(
            HingedDoorSprites sprites,
            Sprite openDoorSprite,
            Vector3 closedPanelWorldPosition,
            Vector3 openPanelWorldPosition,
            int sortingLayerId,
            int closedSortingOrder,
            int openSortingOrder,
            int closedRendererPriority,
            int openRendererPriority,
            Material sharedMaterial,
            Color tint)
        {
            EnsureHingedLayers();

            if (openDoorSprite == null)
            {
                throw new ArgumentNullException(
                    nameof(openDoorSprite));
            }

            transform.SetPositionAndRotation(
                closedPanelWorldPosition,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            presentationStyle =
                DoorPresentationStyle.HingedSinglePanel;

            hingedDoorClosedLocalPosition =
                Vector3.zero;

            hingedDoorOpenLocalPosition =
                openPanelWorldPosition
                - closedPanelWorldPosition;

            hingedDoorClosedSprite =
                sprites.Door;

            hingedDoorOpenSprite =
                openDoorSprite;

            hingedDoorClosedSortingOrder =
                closedSortingOrder;

            hingedDoorOpenSortingOrder =
                openSortingOrder;

            hingedDoorClosedRendererPriority =
                closedRendererPriority;

            hingedDoorOpenRendererPriority =
                openRendererPriority;

            hasPresentation = true;

            SetSlidingLayersActive(false);
            SetHingedLayersActive(true);
            SetDoorwayLayerActive(false);

            ApplyLayer(
                hingedDoorRenderer,
                ResolveHingedDoorSprite(),
                ResolveHingedDoorLocalPosition(),
                sortingLayerId,
                ResolveHingedDoorSortingOrder(),
                ResolveHingedDoorRendererPriority(),
                sharedMaterial,
                tint);

            ApplyLayer(
                hingedFrameRenderer,
                sprites.Frame,
                Vector3.zero,
                sortingLayerId,
                closedSortingOrder,
                closedRendererPriority + 1,
                sharedMaterial,
                tint);

            ApplyHingedDoorState();

            enabled =
                IsAnimating;
        }


        public void ApplyDoorwayPresentation(
            Sprite frameSprite,
            Vector3 worldPosition,
            int sortingLayerId,
            int sortingOrder,
            int rendererPriority,
            Material sharedMaterial,
            Color tint)
        {
            if (frameSprite == null)
            {
                throw new ArgumentNullException(
                    nameof(frameSprite));
            }

            EnsureDoorwayLayer();

            transform.SetPositionAndRotation(
                worldPosition,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            presentationStyle =
                DoorPresentationStyle.StaticDoorway;

            hasPresentation = true;
            openProgress = 1f;
            targetOpenProgress = 1f;

            SetSlidingLayersActive(false);
            SetHingedLayersActive(false);
            SetDoorwayLayerActive(true);

            ApplyLayer(
                doorwayFrameRenderer,
                frameSprite,
                Vector3.zero,
                sortingLayerId,
                sortingOrder,
                rendererPriority,
                sharedMaterial,
                tint);

            enabled = false;
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

            ApplyDoorMotion();

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


        private void ApplyDoorMotion()
        {
            if (!hasPresentation)
            {
                return;
            }

            switch (presentationStyle)
            {
                case DoorPresentationStyle.HingedSinglePanel:
                    ApplyHingedDoorState();
                    return;

                case DoorPresentationStyle.SlidingFourPanel:
                    ApplySlidingDoorPositions();
                    return;

                case DoorPresentationStyle.StaticDoorway:
                    return;
            }
        }


        private void ApplyHingedDoorState()
        {
            if (hingedDoorRenderer == null)
            {
                return;
            }

            hingedDoorRenderer.sprite =
                ResolveHingedDoorSprite();

            hingedDoorRenderer.transform.localPosition =
                ResolveHingedDoorLocalPosition();

            hingedDoorRenderer.transform.localRotation =
                Quaternion.identity;

            hingedDoorRenderer.transform.localScale =
                Vector3.one;

            hingedDoorRenderer.sortingOrder =
                ResolveHingedDoorSortingOrder();

            hingedDoorRenderer.rendererPriority =
                ResolveHingedDoorRendererPriority();
        }


        private Sprite ResolveHingedDoorSprite()
        {
            return IsHingedDoorOpen()
                ? hingedDoorOpenSprite
                : hingedDoorClosedSprite;
        }


        private Vector3 ResolveHingedDoorLocalPosition()
        {
            return IsHingedDoorOpen()
                ? hingedDoorOpenLocalPosition
                : hingedDoorClosedLocalPosition;
        }


        private int ResolveHingedDoorSortingOrder()
        {
            return IsHingedDoorOpen()
                ? hingedDoorOpenSortingOrder
                : hingedDoorClosedSortingOrder;
        }


        private int ResolveHingedDoorRendererPriority()
        {
            return IsHingedDoorOpen()
                ? hingedDoorOpenRendererPriority
                : hingedDoorClosedRendererPriority;
        }


        private bool IsHingedDoorOpen()
        {
            return openProgress >= 0.5f;
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


        private void EnsureSlidingLayers()
        {
            if (leftGlassRenderer != null)
            {
                return;
            }

            slidingSortingGroup =
                GetComponent<SortingGroup>();

            if (slidingSortingGroup == null)
            {
                slidingSortingGroup =
                    gameObject.AddComponent<SortingGroup>();

                ownsSlidingSortingGroup =
                    true;
            }

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
        }


        private void EnsureHingedLayers()
        {
            if (hingedDoorRenderer != null)
            {
                return;
            }

            hingedDoorRenderer =
                CreateLayer("Moving Hinged Door");

            hingedFrameRenderer =
                CreateLayer("Static Hinged Door Frame");
        }


        private void EnsureDoorwayLayer()
        {
            if (doorwayFrameRenderer != null)
            {
                return;
            }

            doorwayFrameRenderer =
                CreateLayer("Static Open Doorway Frame");
        }


        private void SetSlidingLayersActive(
            bool isActive)
        {
            SetLayerActive(
                leftGlassRenderer,
                isActive);

            SetLayerActive(
                leftDoorRenderer,
                isActive);

            SetLayerActive(
                rightDoorRenderer,
                isActive);

            SetLayerActive(
                rightGlassRenderer,
                isActive);

            SetLayerActive(
                frameRenderer,
                isActive);
        }


        private void SetHingedLayersActive(
            bool isActive)
        {
            SetLayerActive(
                hingedDoorRenderer,
                isActive);

            SetLayerActive(
                hingedFrameRenderer,
                isActive);
        }


        private void SetDoorwayLayerActive(
            bool isActive)
        {
            SetLayerActive(
                doorwayFrameRenderer,
                isActive);
        }


        private static void SetLayerActive(
            SpriteRenderer renderer,
            bool isActive)
        {
            if (renderer != null)
            {
                renderer.gameObject.SetActive(
                    isActive);
            }
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


    }
}
