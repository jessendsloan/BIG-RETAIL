using System;
using System.Collections.Generic;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Grants the complete authored construction mask without creating Lot
    /// ownership or purchase state. Used by locations such as Frank Roadside.
    /// </summary>
    public sealed class FixedFootprintLandPolicy : ILocationLandPolicy
    {
        public LocationLandPolicyKind Kind =>
            LocationLandPolicyKind.FixedFootprint;

        public IConstructionCellEligibility ConstructionEligibility
        {
            get;
        }

        public bool SupportsLandPurchases => false;

        public LandRegionCatalog LandRegions => null;

        public LandRegionOwnershipState LandRegionOwnership => null;

        public LandRegionPurchaseService LandRegionPurchases => null;


        public FixedFootprintLandPolicy(
            ConstructionAreaDefinition constructionArea)
        {
            ConstructionEligibility =
                constructionArea
                ?? throw new ArgumentNullException(
                    nameof(constructionArea));
        }


        public IEnumerable<string> EnumerateDefinedLandRegionIds()
        {
            yield break;
        }

        public IEnumerable<string> EnumerateOwnedLandRegionIds()
        {
            yield break;
        }
    }
}
