using System;
using System.Collections.Generic;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Adapts the main Property's nine Land Regions to the location-policy
    /// boundary while preserving its existing ownership and purchase model.
    /// </summary>
    public sealed class PurchasableLandRegionPolicy :
        ILocationLandPolicy
    {
        public LocationLandPolicyKind Kind =>
            LocationLandPolicyKind.PurchasableLandRegions;

        public IConstructionCellEligibility ConstructionEligibility
        {
            get;
        }

        public bool SupportsLandPurchases => true;

        public LandRegionCatalog LandRegions { get; }

        public LandRegionOwnershipState LandRegionOwnership { get; }

        public LandRegionPurchaseService LandRegionPurchases { get; }


        public PurchasableLandRegionPolicy(
            ConstructionAreaDefinition constructionArea,
            bool ownAllRegions,
            IEnumerable<LandRegionPurchaseOption> purchaseOptions)
        {
            if (constructionArea == null)
            {
                throw new ArgumentNullException(
                    nameof(constructionArea));
            }

            LandRegions =
                LandRegionCatalog.CreateFor(
                    constructionArea);

            LandRegionOwnership =
                new LandRegionOwnershipState(
                    LandRegions);

            if (ownAllRegions)
            {
                LandRegionOwnership.OwnAll();
            }
            else
            {
                LandRegionOwnership.Own(
                    LandRegionCatalog.FrontCornerRegionId);
            }

            ConstructionEligibility =
                new LandRegionConstructionEligibility(
                    constructionArea,
                    LandRegions,
                    LandRegionOwnership);

            LandRegionPurchases =
                new LandRegionPurchaseService(
                    LandRegions,
                    LandRegionOwnership,
                    purchaseOptions
                    ?? Array.Empty<LandRegionPurchaseOption>());
        }


        public IEnumerable<string> EnumerateDefinedLandRegionIds()
        {
            foreach (LandRegionDefinition definition in
                     LandRegions.EnumerateDefinitions())
            {
                yield return definition.Id.ToStableId();
            }
        }

        public IEnumerable<string> EnumerateOwnedLandRegionIds()
        {
            foreach (LandRegionId id in
                     LandRegionOwnership.EnumerateOwnedRegions())
            {
                yield return id.ToStableId();
            }
        }
    }
}
