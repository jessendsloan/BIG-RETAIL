using System;
using System.Collections.Generic;

namespace BigRetail.Map.Construction
{
    public enum LandRegionPurchaseFailure
    {
        None,
        UnknownRegion,
        AlreadyOwned,
        NotOffered,
        NotAdjacent
    }

    /// <summary>
    /// Replaceable commercial metadata for a region purchase. Payment and
    /// permit qualification remain outside the ownership transaction so the
    /// final economy/progression rules can be attached later.
    /// </summary>
    public sealed class LandRegionPurchaseOption
    {
        public LandRegionId RegionId { get; }

        public long PriceCents { get; }

        public string QualificationId { get; }

        public LandRegionPurchaseOption(
            LandRegionId regionId,
            long priceCents,
            string qualificationId)
        {
            if (priceCents < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priceCents));
            }

            RegionId = regionId;
            PriceCents = priceCents;
            QualificationId = qualificationId ?? string.Empty;
        }
    }

    public readonly struct LandRegionPurchaseResult
    {
        public bool Succeeded { get; }

        public LandRegionId RegionId { get; }

        public LandRegionPurchaseFailure Failure { get; }

        private LandRegionPurchaseResult(
            bool succeeded,
            LandRegionId regionId,
            LandRegionPurchaseFailure failure)
        {
            Succeeded = succeeded;
            RegionId = regionId;
            Failure = failure;
        }

        public static LandRegionPurchaseResult Success(LandRegionId regionId)
        {
            return new LandRegionPurchaseResult(
                true,
                regionId,
                LandRegionPurchaseFailure.None);
        }

        public static LandRegionPurchaseResult Rejected(
            LandRegionId regionId,
            LandRegionPurchaseFailure failure)
        {
            return new LandRegionPurchaseResult(false, regionId, failure);
        }
    }

    /// <summary>
    /// Transfers offered, adjacent Land Regions into ownership after an
    /// external caller has authorized any price and permit requirements.
    /// </summary>
    public sealed class LandRegionPurchaseService
    {
        private readonly LandRegionCatalog catalog;
        private readonly LandRegionOwnershipState ownership;
        private readonly Dictionary<LandRegionId, LandRegionPurchaseOption>
            options =
                new Dictionary<LandRegionId, LandRegionPurchaseOption>();

        public LandRegionPurchaseService(
            LandRegionCatalog catalog,
            LandRegionOwnershipState ownership,
            IEnumerable<LandRegionPurchaseOption> options)
        {
            this.catalog =
                catalog
                ?? throw new ArgumentNullException(nameof(catalog));
            this.ownership =
                ownership
                ?? throw new ArgumentNullException(nameof(ownership));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (LandRegionPurchaseOption option in options)
            {
                if (option == null)
                {
                    throw new ArgumentException(
                        "Land Region purchase options cannot contain null.",
                        nameof(options));
                }

                if (!catalog.Contains(option.RegionId))
                {
                    throw new ArgumentException(
                        $"Purchase option {option.RegionId} is outside the property.",
                        nameof(options));
                }

                this.options.Add(option.RegionId, option);
            }
        }

        public bool TryGetAvailableOption(
            LandRegionId id,
            out LandRegionPurchaseOption option)
        {
            if (!options.TryGetValue(id, out option)
                || ownership.IsOwned(id)
                || !IsAdjacentToOwnedRegion(id))
            {
                option = null;
                return false;
            }

            return true;
        }

        public IEnumerable<LandRegionPurchaseOption>
            EnumerateAvailableOptions()
        {
            foreach (LandRegionPurchaseOption option in options.Values)
            {
                if (TryGetAvailableOption(option.RegionId, out _))
                {
                    yield return option;
                }
            }
        }

        public LandRegionPurchaseResult TryCompletePurchase(LandRegionId id)
        {
            if (!catalog.Contains(id))
            {
                return LandRegionPurchaseResult.Rejected(
                    id,
                    LandRegionPurchaseFailure.UnknownRegion);
            }

            if (ownership.IsOwned(id))
            {
                return LandRegionPurchaseResult.Rejected(
                    id,
                    LandRegionPurchaseFailure.AlreadyOwned);
            }

            if (!options.ContainsKey(id))
            {
                return LandRegionPurchaseResult.Rejected(
                    id,
                    LandRegionPurchaseFailure.NotOffered);
            }

            if (!IsAdjacentToOwnedRegion(id))
            {
                return LandRegionPurchaseResult.Rejected(
                    id,
                    LandRegionPurchaseFailure.NotAdjacent);
            }

            ownership.Own(id);
            return LandRegionPurchaseResult.Success(id);
        }

        private bool IsAdjacentToOwnedRegion(LandRegionId id)
        {
            return IsOwned(id.Column - 1, id.Row)
                || IsOwned(id.Column + 1, id.Row)
                || IsOwned(id.Column, id.Row - 1)
                || IsOwned(id.Column, id.Row + 1);
        }

        private bool IsOwned(int column, int row)
        {
            LandRegionId neighbor = new LandRegionId(column, row);
            return catalog.Contains(neighbor)
                && ownership.IsOwned(neighbor);
        }
    }
}
