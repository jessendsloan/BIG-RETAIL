using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Combines the permanent authored construction mask with current Land
    /// Region ownership. It does not decide cost, permit, or tool conflicts.
    /// </summary>
    public sealed class LandRegionConstructionEligibility :
        IConstructionCellEligibility
    {
        private readonly ConstructionAreaDefinition physicalArea;
        private readonly LandRegionCatalog regions;
        private readonly LandRegionOwnershipState ownership;

        public LandRegionConstructionEligibility(
            ConstructionAreaDefinition physicalArea,
            LandRegionCatalog regions,
            LandRegionOwnershipState ownership)
        {
            this.physicalArea =
                physicalArea
                ?? throw new ArgumentNullException(nameof(physicalArea));
            this.regions =
                regions
                ?? throw new ArgumentNullException(nameof(regions));
            this.ownership =
                ownership
                ?? throw new ArgumentNullException(nameof(ownership));
        }

        public bool IsEligible(GridPosition position)
        {
            return physicalArea.IsEligible(position)
                && regions.TryGetRegion(
                    position,
                    out LandRegionDefinition region)
                && ownership.IsOwned(region.Id);
        }
    }
}
