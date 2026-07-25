using System;
using BigRetail.Map.Domain;
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
            "The visible thickness of the temporary wall bar " +
            "in Unity world units.")]
        [SerializeField, Min(0.001f)]
        private float wallThickness = 0.08f;

        [Tooltip(
            "Optional world-space adjustment applied after the wall " +
            "position has been calculated.")]
        [SerializeField]
        private Vector3 worldPositionOffset =
            Vector3.zero;


        public CellEdge Edge { get; private set; }

        public bool IsInitialized { get; private set; }


        /// <summary>
        /// Configures this view to represent one logical CellEdge.
        /// </summary>
        public void Initialize(
            CellEdge edge,
            Tilemap coordinateTilemap,
            int logicalLevel,
            int unityCellZ)
        {
            ValidatePresentation();

            CellEdgeWorldPose worldPose =
                CellEdgeWorldPose.Calculate(
                    edge,
                    coordinateTilemap,
                    logicalLevel,
                    unityCellZ);

            Edge = edge;

            ApplyWorldPose(
                worldPose);

            gameObject.name =
                $"Wall {Edge.AnchorCell.X}, " +
                $"{Edge.AnchorCell.Y}, " +
                $"Level {Edge.AnchorCell.Level} — " +
                $"{Edge.CanonicalDirection}";

            IsInitialized = true;
        }


        private void ApplyWorldPose(
            CellEdgeWorldPose worldPose)
        {
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
                    $"{nameof(WallSegmentView)} on '{name}' " +
                    "requires a SpriteRenderer reference.");
            }

            if (spriteRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallSegmentView)} on '{name}' " +
                    "requires a Sprite assigned to its SpriteRenderer.");
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