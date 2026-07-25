using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays one temporary segment belonging to a planned wall run.
    ///
    /// This component is presentation only.
    /// It does not evaluate or place walls.
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
            ValidatePresentation();

            Edge = edge;

            transform.SetPositionAndRotation(
                worldPose.Position,
                worldPose.Rotation);

            ApplySpriteScale(
                worldPose.Length,
                thickness);

            spriteRenderer.color = color;
            spriteRenderer.enabled = true;

            gameObject.name =
                $"Wall Run Preview — {edge}";
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


        private void ValidatePresentation()
        {
            if (spriteRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallRunPreviewSegmentView)} on " +
                    $"'{name}' requires a SpriteRenderer reference.");
            }

            if (spriteRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallRunPreviewSegmentView)} on " +
                    $"'{name}' requires a Sprite.");
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