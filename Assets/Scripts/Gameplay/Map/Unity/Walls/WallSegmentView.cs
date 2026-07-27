using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Displays one model-owned wall edge in the Unity scene.
    ///
    /// This component controls presentation only.
    /// It does not create, remove, or validate walls.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WallSegmentView : MonoBehaviour
    {
        [Header("Visual")]

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [Tooltip(
            "Finish shown when the logical edge's FirstCell faces the viewer. "
            + "When both face finishes are empty, the legacy temporary bar "
            + "presentation remains active.")]
        [SerializeField]
        private WallFinishAsset firstCellFinish;

        [Tooltip(
            "Finish shown when the logical edge's SecondCell faces the viewer. "
            + "When both face finishes are empty, the legacy temporary bar "
            + "presentation remains active.")]
        [SerializeField]
        private WallFinishAsset secondCellFinish;

        [Tooltip(
            "The visible thickness of the temporary fallback wall bar "
            + "in Unity world units. Directional finish sprites keep their "
            + "authored size instead.")]
        [SerializeField, Min(0.001f)]
        private float wallThickness = 0.08f;

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

        public WallDisplaySlope CurrentDisplaySlope { get; private set; }


        private Tilemap coordinateTilemap;
        private int logicalLevel;
        private int unityCellZ;

        private bool UsesDirectionalFinishes =>
            firstCellFinish != null
            && secondCellFinish != null;


        /// <summary>
        /// Configures this view to represent one logical CellEdge.
        /// </summary>
        public void Initialize(
            CellEdge edge,
            Tilemap coordinateTilemap,
            int logicalLevel,
            int unityCellZ,
            IsometricViewProjection projection)
        {
            ValidatePresentation();

            Edge = edge;
            this.coordinateTilemap =
                coordinateTilemap;
            this.logicalLevel =
                logicalLevel;
            this.unityCellZ =
                unityCellZ;

            ApplyProjection(
                projection);

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
                worldPose);
        }


        private void ApplyWorldPose(
            CellEdgeWorldPose worldPose)
        {
            if (UsesDirectionalFinishes)
            {
                ApplyDirectionalFinish(
                    worldPose);
            }
            else
            {
                ApplyLegacyTemporaryBar(
                    worldPose);
            }

            spriteRenderer.sortingOrder =
                200
                - worldPose.DisplayEdge.AnchorCell.X
                - worldPose.DisplayEdge.AnchorCell.Y;
        }


        private void ApplyDirectionalFinish(
            CellEdgeWorldPose worldPose)
        {
            WallFinishAsset visibleFinish;

            if (worldPose.ViewerFacingCell
                == Edge.FirstCell)
            {
                visibleFinish =
                    firstCellFinish;
            }
            else if (worldPose.ViewerFacingCell
                == Edge.SecondCell)
            {
                visibleFinish =
                    secondCellFinish;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Viewer-facing cell {worldPose.ViewerFacingCell} "
                    + $"does not touch wall edge {Edge}.");
            }

            CurrentFinish =
                visibleFinish;

            CurrentDisplaySlope =
                worldPose.DisplaySlope;

            spriteRenderer.sprite =
                visibleFinish.GetSprite(
                    worldPose.DisplaySlope);

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;
        }


        private void ApplyLegacyTemporaryBar(
            CellEdgeWorldPose worldPose)
        {
            CurrentFinish =
                null;

            CurrentDisplaySlope =
                worldPose.DisplaySlope;

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                worldPose.Rotation);

            ApplySpriteScale(
                worldPose.Length);
        }


        private void ApplySpriteScale(
            float edgeLength)
        {
            Vector3 spriteSize =
                spriteRenderer.sprite.bounds.size;

            float safeSpriteWidth =
                Mathf.Max(
                    spriteSize.x,
                    0.0001f);

            float safeSpriteHeight =
                Mathf.Max(
                    spriteSize.y,
                    0.0001f);

            transform.localScale =
                new Vector3(
                    edgeLength / safeSpriteWidth,
                    wallThickness / safeSpriteHeight,
                    1f);
        }


        private void ValidatePresentation()
        {
            if (spriteRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallSegmentView)} on '{name}' "
                    + "requires a SpriteRenderer reference.");
            }

            bool hasFirstFinish =
                firstCellFinish != null;

            bool hasSecondFinish =
                secondCellFinish != null;

            if (hasFirstFinish != hasSecondFinish)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallSegmentView)} on '{name}' must assign "
                    + "both cell-face finishes or neither of them.");
            }

            if (hasFirstFinish)
            {
                firstCellFinish.ValidateConfiguration();
                secondCellFinish.ValidateConfiguration();
                return;
            }

            if (spriteRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallSegmentView)} on '{name}' requires "
                    + "a fallback Sprite when directional finishes "
                    + "are not assigned.");
            }
        }


        private void Reset()
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }


        private void OnValidate()
        {
            wallThickness =
                Mathf.Max(
                    wallThickness,
                    0.001f);

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }
        }
    }
}
