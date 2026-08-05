using System;
using BigRetail.Map.Domain;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Applies a subtle, world-stable value difference between the two wall
    /// axes. The result depends only on the logical edge, so camera rotation
    /// cannot move the shade from one physical wall to another.
    /// </summary>
    public static class WallWorldOrientationShading
    {
        // Measured between the original cool result and the over-corrected
        // warm result so the rendered wall lands near a neutral gray.
        private static readonly Color ShadedTint =
            new Color32(
                247,
                235,
                235,
                255);


        public static Color Resolve(
            CellEdge logicalEdge)
        {
            switch (logicalEdge.CanonicalDirection)
            {
                case CellEdgeDirection.NorthWest:
                    return Color.white;

                case CellEdgeDirection.NorthEast:
                    return ShadedTint;

                default:
                    throw new InvalidOperationException(
                        "A normalized logical CellEdge must use "
                        + "NorthEast or NorthWest.");
            }
        }
    }
}
