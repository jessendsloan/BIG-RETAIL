using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace BigRetail.Construction.Unity.Cells
{
    /// <summary>
    /// Reuses thin line renderers to outline a tile-placement footprint.
    /// </summary>
    internal sealed class TilePlacementBoundaryPool
    {
        private const int BorderBaseSortingOrder = 290;


        private readonly Transform parent;
        private readonly Material material;

        private readonly List<LineRenderer>
            lines =
                new List<LineRenderer>();


        public int VisibleCount { get; private set; }


        public TilePlacementBoundaryPool(
            Transform parent,
            Material material)
        {
            this.parent =
                parent
                ?? throw new ArgumentNullException(
                    nameof(parent));

            this.material =
                material
                ?? throw new ArgumentNullException(
                    nameof(material));
        }


        public void Show(
            IReadOnlyList<CellEdge> boundaryEdges,
            Tilemap coordinateTilemap,
            int logicalLevel,
            int unityCellZ,
            IsometricViewProjection projection,
            Color color,
            float width)
        {
            if (boundaryEdges == null)
            {
                throw new ArgumentNullException(
                    nameof(boundaryEdges));
            }

            EnsureCapacity(
                boundaryEdges.Count);

            for (int index = 0;
                 index < boundaryEdges.Count;
                 index++)
            {
                CellEdge edge =
                    boundaryEdges[index];

                CellEdgeWorldPose worldPose =
                    CellEdgeWorldPose.Calculate(
                        edge,
                        coordinateTilemap,
                        logicalLevel,
                        unityCellZ,
                        projection);

                ShowLine(
                    lines[index],
                    edge,
                    worldPose,
                    color,
                    width);
            }

            HideUnused(
                boundaryEdges.Count);

            VisibleCount =
                boundaryEdges.Count;
        }


        public void HideAll()
        {
            HideUnused(0);
            VisibleCount = 0;
        }


        private void EnsureCapacity(
            int requiredCount)
        {
            while (lines.Count < requiredCount)
            {
                GameObject lineObject =
                    new GameObject(
                        "Tile Placement Border Segment");

                lineObject.transform.SetParent(
                    parent,
                    false);

                LineRenderer line =
                    lineObject.AddComponent<LineRenderer>();

                line.useWorldSpace = true;
                line.loop = false;
                line.positionCount = 2;
                line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Stretch;
                // Slightly rounded caps overlap cleanly where two separate
                // boundary-edge renderers meet at a grid vertex.
                line.numCapVertices = 2;
                line.numCornerVertices = 0;
                line.sharedMaterial = material;
                line.shadowCastingMode =
                    ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.lightProbeUsage =
                    LightProbeUsage.Off;
                line.reflectionProbeUsage =
                    ReflectionProbeUsage.Off;
                line.enabled = false;

                lines.Add(line);
            }
        }


        private static void ShowLine(
            LineRenderer line,
            CellEdge edge,
            CellEdgeWorldPose worldPose,
            Color color,
            float width)
        {
            Vector3 halfEdge =
                worldPose.Rotation
                * Vector3.right
                * (worldPose.Length * 0.5f);

            line.SetPosition(
                0,
                worldPose.Position - halfEdge);

            line.SetPosition(
                1,
                worldPose.Position + halfEdge);

            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;

            GridPosition displayAnchor =
                worldPose.DisplayEdge.AnchorCell;

            line.sortingOrder =
                BorderBaseSortingOrder
                - displayAnchor.X
                - displayAnchor.Y;

            line.enabled = true;
            line.gameObject.name =
                $"Tile Placement Border — {edge}";
        }


        private void HideUnused(
            int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex;
                 index < lines.Count;
                 index++)
            {
                lines[index].enabled = false;
            }
        }
    }
}
