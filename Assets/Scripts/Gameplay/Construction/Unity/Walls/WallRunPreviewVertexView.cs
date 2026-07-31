using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Displays one temporary pylon marker at a planned wall-run vertex.
    ///
    /// This component is presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WallRunPreviewVertexView : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer spriteRenderer;


        public GridVertex Vertex { get; private set; }

        public bool IsVisible =>
            spriteRenderer != null
            && spriteRenderer.enabled;


        public void Show(
            GridVertex vertex,
            GridVertexWorldPose worldPose,
            Vector3 worldPositionOffset,
            Color color)
        {
            ValidatePresentation();

            Vertex = vertex;

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            spriteRenderer.sortingOrder =
                WallRenderOrderResolver.ResolvePylon(
                    worldPose.DisplayDepth);

            spriteRenderer.color = color;
            spriteRenderer.enabled = true;

            gameObject.name =
                $"Wall Run Vertex Preview — {vertex}";
        }


        public void Hide()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }


        private void ValidatePresentation()
        {
            if (spriteRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallRunPreviewVertexView)} on "
                    + $"'{name}' requires a SpriteRenderer reference.");
            }

            if (spriteRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallRunPreviewVertexView)} on "
                    + $"'{name}' requires a pylon Sprite.");
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
