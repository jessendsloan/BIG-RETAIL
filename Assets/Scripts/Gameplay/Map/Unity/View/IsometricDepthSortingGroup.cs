using System;
using BigRetail.Map.Domain;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.View
{
    /// <summary>
    /// Places one moving world object into the same cell-depth contract used
    /// by fixtures and walls. Child renderer orders remain local to the
    /// SortingGroup, so a character rig and anything it carries move through
    /// the world as one coherent visual stack.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SortingGroup))]
    public sealed class IsometricDepthSortingGroup : MonoBehaviour
    {
        [SerializeField]
        private IsometricViewHost viewHost;

        [SerializeField]
        private Tilemap coordinateTilemap;

        [Tooltip(
            "Ground-contact transform used to choose the occupied map cell. "
            + "Defaults to this object.")]
        [SerializeField]
        private Transform groundAnchor;

        [Tooltip(
            "Optional offset within the cell-occupant band. Keep zero for "
            + "ordinary actors and carried objects.")]
        [SerializeField]
        private int sortingOrderOffset;

        private SortingGroup sortingGroup;


        public bool HasAppliedDepth { get; private set; }

        public GridPosition CurrentDisplayCell { get; private set; }

        public int CurrentSortingOrder =>
            sortingGroup != null
                ? sortingGroup.sortingOrder
                : 0;


        private void Awake()
        {
            EnsureSortingGroup();
        }


        private void LateUpdate()
        {
            RefreshSortingOrder();
        }


        /// <summary>
        /// Supplies the active map presentation used by this moving object.
        /// The ground anchor should remain at the object's contact point,
        /// normally the character root at its feet.
        /// </summary>
        public void Configure(
            IsometricViewHost newViewHost,
            Tilemap newCoordinateTilemap,
            Transform newGroundAnchor = null,
            int newSortingOrderOffset = 0)
        {
            viewHost = newViewHost
                ?? throw new ArgumentNullException(
                    nameof(newViewHost));
            coordinateTilemap = newCoordinateTilemap
                ?? throw new ArgumentNullException(
                    nameof(newCoordinateTilemap));
            groundAnchor = newGroundAnchor != null
                ? newGroundAnchor
                : transform;
            sortingOrderOffset = newSortingOrderOffset;

            EnsureSortingGroup();
            RefreshSortingOrder();
        }


        /// <summary>
        /// Recomputes the root group order from the current ground cell.
        /// Returns false while the shared view is not ready.
        /// </summary>
        public bool RefreshSortingOrder()
        {
            if (viewHost == null
                || coordinateTilemap == null
                || !viewHost.TryInitialize())
            {
                return false;
            }

            Transform anchor = groundAnchor != null
                ? groundAnchor
                : transform;

            GridPosition logicalCell =
                viewHost.WorldToLogicalCell(
                    anchor.position,
                    coordinateTilemap);

            GridPosition displayCell =
                viewHost.Projection.ToDisplayCell(
                    logicalCell);

            ApplyDisplayCell(displayCell);
            return true;
        }


        /// <summary>
        /// Applies a known display cell. Public for deterministic presentation
        /// tests and editor diagnostics; runtime callers normally use
        /// RefreshSortingOrder.
        /// </summary>
        public int ApplyDisplayCell(GridPosition displayCell)
        {
            EnsureSortingGroup();

            CurrentDisplayCell = displayCell;
            sortingGroup.sortingOrder =
                IsometricRenderOrderResolver.ResolveCell(displayCell)
                + sortingOrderOffset;
            HasAppliedDepth = true;
            return sortingGroup.sortingOrder;
        }


        private void EnsureSortingGroup()
        {
            sortingGroup ??= GetComponent<SortingGroup>();

            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<SortingGroup>();
            }
        }
    }
}
