using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Walls;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Converts a structural wall run into the physical wall faces currently
    /// toward the viewer.
    ///
    /// The resulting WallFaceKeys remain logical identities. Rotating after the
    /// gesture is complete does not change which faces were edited.
    /// </summary>
    public static class WallFaceRunResolver
    {
        public static WallFaceKey[] ResolveViewerFacingFaces(
            IReadOnlyList<CellEdge> edges,
            IsometricViewProjection projection)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (projection == null)
            {
                throw new ArgumentNullException(
                    nameof(projection));
            }

            WallFaceKey[] faces =
                new WallFaceKey[edges.Count];

            for (int index = 0;
                 index < edges.Count;
                 index++)
            {
                CellEdge edge =
                    edges[index];

                GridPosition viewerFacingCell =
                    WallPresentationSelector.Select(
                        edge,
                        projection)
                    .ViewerFacingCell;

                faces[index] =
                    new WallFaceKey(
                        edge,
                        viewerFacingCell);
            }

            return faces;
        }
    }
}
