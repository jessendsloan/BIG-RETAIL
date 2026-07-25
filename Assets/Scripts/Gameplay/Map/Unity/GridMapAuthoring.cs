using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity
{
    /// <summary>
    /// Holds the Unity-authored inputs needed to create the logical map.
    ///
    /// This component translates semantic Tilemaps into plain C#
    /// definitions. Runtime gameplay uses those definitions rather
    /// than repeatedly querying the Tilemaps.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridMapAuthoring : MonoBehaviour
    {
        [Header("Map Identity")]

        [Tooltip(
            "Stable internal identifier for this authored map. " +
            "This is not a player-facing display name.")]
        [SerializeField]
        private string mapId =
            "bigretail.map.main_property";


        [Header("Logical Coordinates")]

        [Tooltip(
            "The Big Retail building level represented by these masks. " +
            "Ground level is currently 0.")]
        [SerializeField]
        private int logicalLevel = 0;

        [Tooltip(
            "The Unity Tilemap cell Z layer on which semantic marker " +
            "tiles are expected to be painted.")]
        [SerializeField]
        private int expectedUnityCellZ = 0;


        [Header("Semantic Masks")]

        [Tooltip(
            "Marks every cell belonging to the authored map, including " +
            "the property, sidewalks, and roads.")]
        [SerializeField]
        private Tilemap mapAreaMaskTilemap;

        [Tooltip(
            "Marks cells physically eligible for construction. " +
            "For the current map, this is the green property only.")]
        [SerializeField]
        private Tilemap constructionAreaMaskTilemap;


        [Header("Runtime Presentation")]

        [Tooltip(
            "Hide the semantic mask renderers after successful runtime " +
            "map initialization. The Tilemap data remains intact.")]
        [SerializeField]
        private bool hideMaskRenderersAtRuntime = true;


        public string MapId => mapId;
        public int LogicalLevel => logicalLevel;


        /// <summary>
        /// Creates the authored map geometry from the Map Area Mask.
        /// </summary>
        public GridMapDefinition CreateMapDefinition()
        {
            ValidateAuthoring();

            HashSet<GridPosition> validCells =
                TilemapCellMaskReader.ReadOccupiedCells(
                    mapAreaMaskTilemap,
                    logicalLevel,
                    expectedUnityCellZ);

            return new GridMapDefinition(
                mapId,
                validCells);
        }

        /// <summary>
        /// Creates the construction-area definition from the
        /// Construction Area Mask.
        ///
        /// The supplied map definition is used to verify that every
        /// construction-eligible cell belongs to the authored map.
        /// </summary>
        public ConstructionAreaDefinition
            CreateConstructionAreaDefinition(
                GridMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(mapDefinition));
            }

            ValidateAuthoring();

            HashSet<GridPosition> eligibleCells =
                TilemapCellMaskReader.ReadOccupiedCells(
                    constructionAreaMaskTilemap,
                    logicalLevel,
                    expectedUnityCellZ);

            return new ConstructionAreaDefinition(
                mapDefinition,
                eligibleCells);
        }

        /// <summary>
        /// Applies the intended runtime visibility after the map has
        /// successfully been created.
        ///
        /// Only rendering is disabled. The semantic Tilemaps and their
        /// cell data remain available.
        /// </summary>
        public void ApplyRuntimeVisibility()
        {
            if (!hideMaskRenderersAtRuntime)
            {
                return;
            }

            SetRendererEnabled(
                mapAreaMaskTilemap,
                false);

            SetRendererEnabled(
                constructionAreaMaskTilemap,
                false);
        }

        private void ValidateAuthoring()
        {
            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new InvalidOperationException(
                    $"{nameof(GridMapAuthoring)} on '{name}' " +
                    "requires a stable Map ID.");
            }

            if (mapAreaMaskTilemap == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GridMapAuthoring)} on '{name}' " +
                    "requires a Map Area Mask Tilemap.");
            }

            if (constructionAreaMaskTilemap == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(GridMapAuthoring)} on '{name}' " +
                    "requires a Construction Area Mask Tilemap.");
            }

            if (mapAreaMaskTilemap
                == constructionAreaMaskTilemap)
            {
                throw new InvalidOperationException(
                    "The Map Area Mask and Construction Area Mask " +
                    "must be separate Tilemaps.");
            }

            Grid mapAreaGrid =
                mapAreaMaskTilemap.layoutGrid;

            Grid constructionAreaGrid =
                constructionAreaMaskTilemap.layoutGrid;

            if (mapAreaGrid == null
                || constructionAreaGrid == null)
            {
                throw new InvalidOperationException(
                    "Both semantic masks must belong to a Unity Grid.");
            }

            if (mapAreaGrid != constructionAreaGrid)
            {
                throw new InvalidOperationException(
                    "The Map Area Mask and Construction Area Mask " +
                    "must belong to the same Unity Grid.");
            }
        }

        private static void SetRendererEnabled(
            Tilemap tilemap,
            bool isEnabled)
        {
            if (tilemap == null)
            {
                return;
            }

            TilemapRenderer tilemapRenderer =
                tilemap.GetComponent<TilemapRenderer>();

            if (tilemapRenderer != null)
            {
                tilemapRenderer.enabled =
                    isEnabled;
            }
        }

        private void OnValidate()
        {
            if (mapId != null)
            {
                mapId = mapId.Trim();
            }
        }
    }
}