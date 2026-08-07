using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Foundations;
using BigRetail.Map.View;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Describes whether the camera-facing side of a door's supporting wall
    /// belongs to the constructed building footprint.
    /// </summary>
    public enum DoorViewerSide
    {
        Outside = 0,
        Inside = 1
    }


    /// <summary>
    /// Resolves the physical side of a wall currently facing the camera.
    /// The foundation footprint is authoritative for inside versus outside.
    /// </summary>
    public static class DoorViewerSideResolver
    {
        public static DoorViewerSide Resolve(
            CellEdge supportingEdge,
            IsometricViewProjection projection,
            FoundationState foundationState)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            if (foundationState == null)
            {
                throw new ArgumentNullException(
                    nameof(foundationState));
            }

            GridPosition viewerFacingCell =
                projection.GetViewerFacingCell(
                    supportingEdge);

            return foundationState.HasFoundation(
                    viewerFacingCell)
                ? DoorViewerSide.Inside
                : DoorViewerSide.Outside;
        }
    }
}
