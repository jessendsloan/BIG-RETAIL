using System.Collections.Generic;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Runtime construction entitlement selected by one location. Consumers
    /// depend on this policy instead of assuming every map is the nine-Lot
    /// main Property.
    /// </summary>
    public interface ILocationLandPolicy
    {
        LocationLandPolicyKind Kind { get; }

        IConstructionCellEligibility ConstructionEligibility { get; }

        bool SupportsLandPurchases { get; }

        LandRegionCatalog LandRegions { get; }

        LandRegionOwnershipState LandRegionOwnership { get; }

        LandRegionPurchaseService LandRegionPurchases { get; }

        IEnumerable<string> EnumerateDefinedLandRegionIds();

        IEnumerable<string> EnumerateOwnedLandRegionIds();
    }
}
