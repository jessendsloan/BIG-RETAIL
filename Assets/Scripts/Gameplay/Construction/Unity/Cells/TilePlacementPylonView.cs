using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using UnityEngine;

namespace BigRetail.Construction.Unity.Cells
{
    /// <summary>
    /// Displays one temporary pylon at the center of a planned tile placement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TilePlacementPylonView :
        MonoBehaviour
    {
        private const int PylonBaseSortingOrder = 300;


        [SerializeField]
        private SpriteRenderer spriteRenderer;


        public GridPosition Cell { get; private set; }

        public bool IsVisible =>
            spriteRenderer != null
            && spriteRenderer.enabled;

        public Material SharedMaterial =>
            spriteRenderer != null
                ? spriteRenderer.sharedMaterial
                : null;


        public void Show(
            GridPosition cell,
            Vector3 worldPosition,
            int displayDepth,
            Color color)
        {
            ValidatePresentation();

            Cell = cell;

            transform.SetPositionAndRotation(
                worldPosition,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            spriteRenderer.sortingOrder =
                PylonBaseSortingOrder
                - displayDepth;

            spriteRenderer.color = color;
            spriteRenderer.enabled = true;

            gameObject.name =
                $"Tile Placement Pylon — {cell}";
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
                    $"{nameof(TilePlacementPylonView)} on "
                    + $"'{name}' requires a SpriteRenderer reference.");
            }

            if (spriteRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(TilePlacementPylonView)} on "
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


    /// <summary>
    /// Reuses tile-pylon views while a dragged construction area changes size.
    /// </summary>
    internal sealed class TilePlacementPylonPool
    {
        private readonly TilePlacementPylonView prefab;
        private readonly Transform parent;

        private readonly List<TilePlacementPylonView>
            views =
                new List<TilePlacementPylonView>();


        public int VisibleCount { get; private set; }


        public TilePlacementPylonPool(
            TilePlacementPylonView prefab,
            Transform parent)
        {
            this.prefab =
                prefab
                ?? throw new ArgumentNullException(
                    nameof(prefab));

            this.parent =
                parent
                ?? throw new ArgumentNullException(
                    nameof(parent));
        }


        public void Show(
            int index,
            GridPosition cell,
            Vector3 worldPosition,
            int displayDepth,
            Color color)
        {
            EnsureCapacity(
                index + 1);

            views[index].Show(
                cell,
                worldPosition,
                displayDepth,
                color);

            VisibleCount =
                Mathf.Max(
                    VisibleCount,
                    index + 1);
        }


        public void HideUnused(
            int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex;
                 index < views.Count;
                 index++)
            {
                views[index].Hide();
            }

            VisibleCount =
                Mathf.Min(
                    VisibleCount,
                    firstUnusedIndex);
        }


        public void HideAll()
        {
            HideUnused(0);
        }


        private void EnsureCapacity(
            int requiredCount)
        {
            while (views.Count < requiredCount)
            {
                TilePlacementPylonView view =
                    UnityEngine.Object.Instantiate(
                        prefab,
                        parent);

                view.Hide();
                views.Add(view);
            }
        }
    }
}
