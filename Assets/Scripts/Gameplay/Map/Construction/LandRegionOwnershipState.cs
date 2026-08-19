using System;
using System.Collections.Generic;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Mutable ownership state for the nine immutable Land Regions.
    /// </summary>
    public sealed class LandRegionOwnershipState
    {
        private readonly LandRegionCatalog catalog;
        private readonly HashSet<LandRegionId> ownedRegions =
            new HashSet<LandRegionId>();

        public int OwnedRegionCount => ownedRegions.Count;

        public LandRegionOwnershipState(LandRegionCatalog catalog)
        {
            this.catalog =
                catalog
                ?? throw new ArgumentNullException(nameof(catalog));
        }

        public event Action<LandRegionId> RegionOwned;

        public bool IsOwned(LandRegionId id)
        {
            return ownedRegions.Contains(id);
        }

        public bool Own(LandRegionId id)
        {
            if (!catalog.Contains(id))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    id,
                    "Cannot own a Land Region outside this property.");
            }

            if (!ownedRegions.Add(id))
            {
                return false;
            }

            RegionOwned?.Invoke(id);
            return true;
        }

        public void OwnAll()
        {
            foreach (LandRegionDefinition definition in
                     catalog.EnumerateDefinitions())
            {
                Own(definition.Id);
            }
        }

        public IEnumerable<LandRegionId> EnumerateOwnedRegions()
        {
            foreach (LandRegionId id in ownedRegions)
            {
                yield return id;
            }
        }
    }
}
