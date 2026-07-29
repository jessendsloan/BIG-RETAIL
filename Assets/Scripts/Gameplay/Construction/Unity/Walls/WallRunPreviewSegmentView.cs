using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays one temporary segment belonging to a planned wall run.
    ///
    /// This component is presentation only. It can display the legacy thin
    /// edge marker or a full directional wall-finish sprite.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WallRunPreviewSegmentView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;


        public CellEdge Edge { get; private set; }

        public bool IsVisible =>
            spriteRenderer != null
            && spriteRenderer.enabled;


        public void Show(
            CellEdge edge,
            CellEdgeWorldPose worldPose,
            float thickness,
            Color color)
        {
            ValidateRenderer();

            if (spriteRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallRunPreviewSegmentView)} on "
                    + $"'{name}' requires a Sprite for thin-segment preview.");
            }

            Edge = edge;

            transform.SetPositionAndRotation(
                worldPose.Position,
                worldPose.Rotation);

            ApplySpriteScale(
                worldPose.Length,
                thickness);

            spriteRenderer.sortingOrder =
                WallRenderOrderResolver.ResolveWall(
                    worldPose.DisplayEdge);

            spriteRenderer.rendererPriority = 0;
            spriteRenderer.color = color;
            spriteRenderer.enabled = true;

            gameObject.name =
                $"Wall Run Preview — {edge}";
        }


        /// <summary>
        /// Displays the selected directional finish at the exact pose used by
        /// the runtime wall view. This produces a ghost wall on empty edges and
        /// a finish overlay on existing walls without mutating model state.
        /// </summary>
        public void ShowAppearance(
            CellEdge edge,
            CellEdgeWorldPose worldPose,
            Sprite finishSprite,
            Vector3 worldPositionOffset,
            Color color)
        {
            ValidateRenderer();

            if (finishSprite == null)
            {
                throw new ArgumentNullException(
                    nameof(finishSprite));
            }

            Edge = edge;
            spriteRenderer.sprite = finishSprite;

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            // Stay in the same logical depth slot as the runtime wall. A small
            // renderer priority draws the translucent preview after that exact
            // wall without letting it jump in front of other depth slots.
            spriteRenderer.sortingOrder =
                WallRenderOrderResolver.ResolveWall(
                    worldPose.DisplayEdge);

            spriteRenderer.rendererPriority = 1;
            spriteRenderer.color = color;
            spriteRenderer.enabled = true;

            gameObject.name =
                $"Wall Appearance Preview — {edge}";
        }


        public void Hide()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }


        private void ApplySpriteScale(
            float edgeLength,
            float thickness)
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
                    thickness / safeSpriteHeight,
                    1f);
        }


        private void ValidateRenderer()
        {
            if (spriteRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallRunPreviewSegmentView)} on "
                    + $"'{name}' requires a SpriteRenderer reference.");
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
