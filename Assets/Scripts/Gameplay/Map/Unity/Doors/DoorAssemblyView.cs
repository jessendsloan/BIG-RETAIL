using System;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Layered presentation for one complete door assembly. The frame remains
    /// static while the two door transforms are deliberately independent so
    /// a later interaction pass can slide them without changing the artwork.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DoorAssemblyView : MonoBehaviour
    {
        public const int RequiredPanelCount = 4;

        // Door panels share the nearest supporting wall's depth slot. A
        // perpendicular wall closer to the viewer can then occlude the whole
        // assembly cleanly instead of landing between its glass and frame.
        public const int SortingOrderOffsetFromSupportingWall = 0;


        private SpriteRenderer frameRenderer;
        private SpriteRenderer leftGlassRenderer;
        private SpriteRenderer leftDoorRenderer;
        private SpriteRenderer rightDoorRenderer;
        private SpriteRenderer rightGlassRenderer;


        public DoorAssemblyId AssemblyId { get; private set; }

        public Transform LeftDoorTransform =>
            leftDoorRenderer != null
                ? leftDoorRenderer.transform
                : null;

        public Transform RightDoorTransform =>
            rightDoorRenderer != null
                ? rightDoorRenderer.transform
                : null;


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
                screenOrderedPanelPositions[1] - worldPosition,
                sortingLayerId,
                sortingOrder,
                rendererPriority + 1,
                sharedMaterial,
                tint);

            ApplyLayer(
                rightDoorRenderer,
                sprites.RightDoor,
                screenOrderedPanelPositions[2] - worldPosition,
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
