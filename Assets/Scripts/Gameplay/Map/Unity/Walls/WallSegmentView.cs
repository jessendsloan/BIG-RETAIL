using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.Rendering;
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
    [RequireComponent(
        typeof(SpriteRenderer),
        typeof(SortingGroup))]
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

        [Tooltip(
            "Temporary tint used when a placed door has no authored panel "
            + "sprite for the visible wall finish yet.")]
        [SerializeField]
        private Color doorPlaceholderColor =
            new Color(0.58f, 0.82f, 0.92f, 1f);


        public CellEdge Edge { get; private set; }

        public bool IsInitialized { get; private set; }

        public WallFinishAsset CurrentFinish { get; private set; }

        public WallFinishId CurrentFinishId { get; private set; }

        public WallDisplaySlope CurrentDisplaySlope { get; private set; }

        public WallPresentationHeight CurrentHeight { get; private set; } =
            WallPresentationHeight.Full;

        public bool IsDoorPanel { get; private set; }

        public DoorAssemblyId? CurrentDoorAssemblyId { get; private set; }

        public int CurrentDoorPanelIndex { get; private set; } = -1;

        public int SortingLayerId =>
            sortingGroup != null
                ? sortingGroup.sortingLayerID
                : spriteRenderer.sortingLayerID;

        public int SortingOrder =>
            sortingGroup != null
                ? sortingGroup.sortingOrder
                : spriteRenderer.sortingOrder;

        public int RendererPriority =>
            spriteRenderer.rendererPriority;

        public Material SharedMaterial =>
            spriteRenderer.sharedMaterial;


        private Tilemap coordinateTilemap;
        private int logicalLevel;
        private int unityCellZ;
        private WallFinishPresentationResolver finishResolver;
        private DoorPresentationResolver doorResolver;
        private SpriteMask apertureMask;
        private SortingGroup sortingGroup;


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
            DoorPresentationResolver doorResolver,
            WallPresentationHeight presentationHeight)
        {
            ValidatePresentation();
            EnsureSortingGroup();

            this.coordinateTilemap =
                coordinateTilemap
                ?? throw new ArgumentNullException(
                    nameof(coordinateTilemap));

            this.finishResolver =
                finishResolver
                ?? throw new ArgumentNullException(
                    nameof(finishResolver));

            this.doorResolver =
                doorResolver
                ?? throw new ArgumentNullException(
                    nameof(doorResolver));

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
            spriteRenderer.sortingOrder =
                WallRenderOrderResolver.ResolveWall(
                    worldPose.DisplayEdge);

            sortingGroup.sortingLayerID =
                spriteRenderer.sortingLayerID;

            sortingGroup.sortingOrder =
                spriteRenderer.sortingOrder;

            spriteRenderer.rendererPriority =
                WallRenderOrderResolver.ResolveWallPriority(
                    worldPose.DisplayEdge);

            // Aperture masks target the wall renderer's exact sorting range,
            // so depth must be current before directional art refreshes them.
            ApplyDirectionalFinish(
                worldPose,
                presentationHeight);
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

            IsDoorPanel =
                doorResolver.TryResolvePanel(
                    Edge,
                    out DoorAssembly assembly,
                    out DoorDefinitionAsset doorDefinitionAsset,
                    out int panelIndex);

            CurrentDoorAssemblyId =
                IsDoorPanel
                    ? assembly.Id
                    : null;

            CurrentDoorPanelIndex =
                IsDoorPanel
                    ? panelIndex
                    : -1;

            bool usesLayeredDoorArt =
                presentationHeight == WallPresentationHeight.Full
                && IsDoorPanel
                && doorDefinitionAsset.HasCompleteAssemblyVisuals;

            Sprite apertureSprite =
                ResolveDoorApertureSprite(
                    usesLayeredDoorArt,
                    doorDefinitionAsset,
                    worldPose.DisplaySlope);

            spriteRenderer.sprite =
                visibleFinish.GetSprite(
                    worldPose.DisplaySlope,
                    presentationHeight);

            spriteRenderer.color =
                IsDoorPanel
                && presentationHeight == WallPresentationHeight.Full
                && !usesLayeredDoorArt
                    ? doorPlaceholderColor
                    : Color.white;

            transform.SetPositionAndRotation(
                worldPose.Position
                    + worldPositionOffset,
                Quaternion.identity);

            transform.localScale =
                Vector3.one;

            ApplyDoorAperture(
                usesLayeredDoorArt,
                apertureSprite);
        }


        private static Sprite ResolveDoorApertureSprite(
            bool usesLayeredDoorArt,
            DoorDefinitionAsset definitionAsset,
            WallDisplaySlope displaySlope)
        {
            if (!usesLayeredDoorArt
                || !definitionAsset.TryGetAssemblySprites(
                    displaySlope,
                    out DoorAssemblySprites sprites))
            {
                return null;
            }

            // The authored sliding panel already has the exact isometric
            // slope, pivot, and opening silhouette needed by one wall panel.
            // Scaling a full wall sprite vertically changes its slope and
            // creates triangular over-cutting at door corners.
            return sprites.LeftDoor;
        }


        private void ApplyDoorAperture(
            bool isEnabled,
            Sprite apertureSprite)
        {
            if (!isEnabled
                || apertureSprite == null)
            {
                spriteRenderer.maskInteraction =
                    SpriteMaskInteraction.None;

                if (apertureMask != null)
                {
                    apertureMask.enabled =
                        false;
                }

                return;
            }

            EnsureApertureMask();

            apertureMask.sprite =
                apertureSprite;

            apertureMask.alphaCutoff =
                0.01f;

            apertureMask.isCustomRangeActive =
                true;

            apertureMask.frontSortingLayerID =
                spriteRenderer.sortingLayerID;

            apertureMask.backSortingLayerID =
                spriteRenderer.sortingLayerID;

            apertureMask.frontSortingOrder =
                spriteRenderer.sortingOrder;

            apertureMask.backSortingOrder =
                spriteRenderer.sortingOrder - 1;

            apertureMask.transform.localPosition =
                Vector3.zero;

            apertureMask.transform.localRotation =
                Quaternion.identity;

            apertureMask.transform.localScale =
                Vector3.one;

            apertureMask.enabled =
                true;

            spriteRenderer.maskInteraction =
                SpriteMaskInteraction.VisibleOutsideMask;
        }


        private void EnsureApertureMask()
        {
            if (apertureMask != null)
            {
                return;
            }

            GameObject maskObject =
                new GameObject("Door Aperture Mask");

            maskObject.transform.SetParent(
                transform,
                false);

            apertureMask =
                maskObject.AddComponent<SpriteMask>();
        }


        private void EnsureSortingGroup()
        {
            if (sortingGroup != null)
            {
                return;
            }

            sortingGroup =
                GetComponent<SortingGroup>();

            if (sortingGroup == null)
            {
                sortingGroup =
                    gameObject.AddComponent<SortingGroup>();
            }
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

            sortingGroup =
                GetComponent<SortingGroup>();
        }


        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }

            if (sortingGroup == null)
            {
                sortingGroup =
                    GetComponent<SortingGroup>();
            }
        }
    }
}
