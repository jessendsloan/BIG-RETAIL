using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Displays one model-owned wall edge in the Unity scene.
    ///
    /// This component controls presentation only.
    /// It does not create, remove, validate, or own wall finishes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WallSegmentView : MonoBehaviour
    {
        [Header("Visual")]

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [Tooltip(
            "Optional world-space adjustment applied after the wall "
            + "position has been calculated. This can be used while "
            + "validating authored sprite pivots and wall height.")]
        [SerializeField]
        private Vector3 worldPositionOffset =
            Vector3.zero;


        public CellEdge Edge { get; private set; }

        public bool IsInitialized { get; private set; }

        public WallFinishAsset CurrentFinish { get; private set; }

        public WallFinishId CurrentFinishId { get; private set; }

        public WallDisplaySlope CurrentDisplaySlope { get; private set; }

        public WallPresentationHeight CurrentHeight { get; private set; } =
            WallPresentationHeight.Full;


        private Tilemap coordinateTilemap;
        private int logicalLevel;
        private int unityCellZ;
        private WallFinishPresentationResolver finishResolver;


        /// <summary>
        /// Configures this view to represent one logical CellEdge.
        /// </summary>
        public void Initialize(
            CellEdge edge,
            Tilemap coordinateTilemap,
            int logicalLevel,
            int unityCellZ,
            IsometricViewProjection projection,
            WallFinishPresentationResolver finishResolver,
            WallPresentationHeight presentationHeight)
        {
            ValidatePresentation();

            this.coordinateTilemap =
                coordinateTilemap
                ?? throw new ArgumentNullException(
                    nameof(coordinateTilemap));

            this.finishResolver =
                finishResolver
                ?? throw new ArgumentNullException(
                    nameof(finishResolver));

            Edge = edge;
            this.logicalLevel = logicalLevel;
            this.unityCellZ = unityCellZ;

            ApplyProjection(
                projection,
                presentationHeight);

            gameObject.name =
                $"Wall {Edge.AnchorCell.X}, "
                + $"{Edge.AnchorCell.Y}, "
                + $"Level {Edge.AnchorCell.Level} — "
                + $"{Edge.CanonicalDirection}";

            IsInitialized = true;
        }


        public void ApplyProjection(
            IsometricViewProjection projection)
        {
            ApplyProjection(
                projection,
                CurrentHeight);
        }


        public void ApplyProjection(
            IsometricViewProjection projection,
            WallPresentationHeight presentationHeight)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            CellEdgeWorldPose worldPose =
                CellEdgeWorldPose.Calculate(
                    Edge,
                    coordinateTilemap,
                    logicalLevel,
                    unityCellZ,
                    projection);

            ApplyWorldPose(
                worldPose,
                presentationHeight);
        }


        private void ApplyWorldPose(
            CellEdgeWorldPose worldPose,
            WallPresentationHeight presentationHeight)
        {
            ApplyDirectionalFinish(
                worldPose,
                presentationHeight);

            spriteRenderer.sortingOrder =
                WallRenderOrderResolver.ResolveWall(
                    worldPose.DisplayEdge);

            spriteRenderer.rendererPriority =
                WallRenderOrderResolver.ResolveWallPriority(
                    worldPose.DisplayEdge);
        }


        private void ApplyDirectionalFinish(
            CellEdgeWorldPose worldPose,
            WallPresentationHeight presentationHeight)
        {
            WallFinishAsset visibleFinish =
                finishResolver.ResolveAsset(
                    Edge,
                    worldPose.ViewerFacingCell);

            CurrentFinish =
                visibleFinish;

            CurrentFinishId =
                visibleFinish.Id;

            CurrentDisplaySlope =
                worldPose.DisplaySlope;

            CurrentHeight =
                presentationHeight;

            spriteRenderer.sprite =
                visibleFinish.GetSprite(
                    worldPose.DisplaySlope,
                    presentationHeight);

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;
        }


        private void ValidatePresentation()
        {
            if (spriteRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallSegmentView)} on '{name}' "
                    + "requires a SpriteRenderer reference.");
            }
        }


        private void Reset()
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }


        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }
        }
    }
}
