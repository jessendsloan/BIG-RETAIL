using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Immutable location facts used to reject an authored layout before any
    /// runtime state is changed.
    /// </summary>
    public sealed class StoreLocationValidationContext
    {
        private readonly HashSet<StoreCellData> validCells;
        private readonly HashSet<string> landRegionIds;


        public string MapId { get; }

        public string MapFingerprint { get; }

        public IStoreDefinitionCatalog Definitions { get; }


        public StoreLocationValidationContext(
            string mapId,
            string mapFingerprint,
            IEnumerable<StoreCellData> validCells,
            IEnumerable<string> landRegionIds,
            IStoreDefinitionCatalog definitions)
        {
            MapId =
                StoreDataIdentity.NormalizeRequired(
                    mapId,
                    nameof(mapId));

            if (string.IsNullOrWhiteSpace(mapFingerprint))
            {
                throw new ArgumentException(
                    "A location validation context requires a map fingerprint.",
                    nameof(mapFingerprint));
            }

            MapFingerprint = mapFingerprint.Trim();

            if (validCells == null)
            {
                throw new ArgumentNullException(
                    nameof(validCells));
            }

            this.validCells =
                new HashSet<StoreCellData>(validCells);

            if (this.validCells.Count == 0)
            {
                throw new ArgumentException(
                    "A location validation context requires valid map cells.",
                    nameof(validCells));
            }

            this.landRegionIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            if (landRegionIds != null)
            {
                foreach (string landRegionId in landRegionIds)
                {
                    this.landRegionIds.Add(
                        StoreDataIdentity.NormalizeRequired(
                            landRegionId,
                            nameof(landRegionIds)));
                }
            }

            Definitions =
                definitions
                ?? throw new ArgumentNullException(
                    nameof(definitions));
        }


        public bool ContainsCell(
            StoreCellData cell)
        {
            return validCells.Contains(cell);
        }

        public bool ContainsEdge(
            StoreEdgeData edge)
        {
            return edge.HasSupportedDirection()
                && (ContainsCell(edge.FirstCell)
                    || ContainsCell(edge.SecondCell));
        }

        public bool ContainsLandRegion(
            string landRegionId)
        {
            return StoreDataIdentity.TryNormalize(
                       landRegionId,
                       out string normalizedId)
                && landRegionIds.Contains(normalizedId);
        }
    }
}
