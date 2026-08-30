using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Runtime planogram state for placed fixtures.
    ///
    /// Each shelf run is internally quantized into invisible frontage units.
    /// Empty units contain an invalid ProductId. Physical display inventory is
    /// deliberately not stored here.
    /// </summary>
    public sealed class FixturePlanogramState
    {
        private readonly Dictionary<FixtureShelfRunKey, ProductId[]>
            frontageByShelfRun =
                new Dictionary<FixtureShelfRunKey, ProductId[]>();

        private bool isPublishingChanges;


        public int AssignedShelfRunCount =>
            frontageByShelfRun.Count;

        public event Action<FixtureShelfRunKey> ShelfRunChanged;


        public bool TryGetProductAt(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            out ProductId productId)
        {
            if (!frontageByShelfRun.TryGetValue(
                    shelfRun,
                    out ProductId[] frontage)
                || frontageUnitIndex < 0
                || frontageUnitIndex >= frontage.Length)
            {
                productId = default;
                return false;
            }

            productId = frontage[frontageUnitIndex];
            return productId.IsValid;
        }

        public bool TryGetSingleAssignedFixture(
            out FixtureInstanceId fixtureId)
        {
            fixtureId = default;
            bool foundFixture = false;

            foreach (
                FixtureShelfRunKey shelfRun
                in frontageByShelfRun.Keys)
            {
                if (!foundFixture)
                {
                    fixtureId = shelfRun.FixtureId;
                    foundFixture = true;
                    continue;
                }

                if (shelfRun.FixtureId != fixtureId)
                {
                    fixtureId = default;
                    return false;
                }
            }

            return foundFixture;
        }

        public bool TryGetFacingAt(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            out ProductFacing facing)
        {
            if (!frontageByShelfRun.TryGetValue(
                    shelfRun,
                    out ProductId[] frontage)
                || frontageUnitIndex < 0
                || frontageUnitIndex >= frontage.Length
                || !frontage[frontageUnitIndex].IsValid)
            {
                facing = default;
                return false;
            }

            ProductId productId = frontage[frontageUnitIndex];
            int start = frontageUnitIndex;
            int endExclusive = frontageUnitIndex + 1;

            while (start > 0
                   && frontage[start - 1] == productId)
            {
                start--;
            }

            while (endExclusive < frontage.Length
                   && frontage[endExclusive] == productId)
            {
                endExclusive++;
            }

            facing =
                new ProductFacing(
                    productId,
                    start,
                    endExclusive - start);

            return true;
        }

        public IReadOnlyList<ProductFacing> GetFacings(
            FixtureShelfRunKey shelfRun)
        {
            if (!frontageByShelfRun.TryGetValue(
                    shelfRun,
                    out ProductId[] frontage))
            {
                return Array.Empty<ProductFacing>();
            }

            List<ProductFacing> facings =
                new List<ProductFacing>();

            int unitIndex = 0;

            while (unitIndex < frontage.Length)
            {
                ProductId productId = frontage[unitIndex];

                if (!productId.IsValid)
                {
                    unitIndex++;
                    continue;
                }

                int start = unitIndex;

                while (unitIndex < frontage.Length
                       && frontage[unitIndex] == productId)
                {
                    unitIndex++;
                }

                facings.Add(
                    new ProductFacing(
                        productId,
                        start,
                        unitIndex - start));
            }

            return facings;
        }


        internal bool TryAssignRange(
            FixtureShelfRunKey shelfRun,
            int frontageUnitCount,
            int startFrontageUnit,
            int assignedUnitCount,
            ProductId productId)
        {
            if (isPublishingChanges)
            {
                return false;
            }

            ProductId[] frontage =
                GetOrCreateFrontage(
                    shelfRun,
                    frontageUnitCount);

            bool changed = false;
            int endExclusive = startFrontageUnit + assignedUnitCount;

            for (int index = startFrontageUnit;
                 index < endExclusive;
                 index++)
            {
                if (frontage[index] == productId)
                {
                    continue;
                }

                frontage[index] = productId;
                changed = true;
            }

            if (changed)
            {
                PublishShelfRunChanged(shelfRun);
            }

            return true;
        }

        internal bool TryReplaceFacingProduct(
            FixtureShelfRunKey shelfRun,
            ProductFacing existingFacing,
            ProductId replacementProductId)
        {
            return TryAssignRange(
                shelfRun,
                GetRequiredFrontage(shelfRun).Length,
                existingFacing.StartFrontageUnit,
                existingFacing.FrontageUnitCount,
                replacementProductId);
        }

        internal bool TryResizeFacing(
            FixtureShelfRunKey shelfRun,
            ProductFacing existingFacing,
            int newFrontageUnitCount)
        {
            if (isPublishingChanges)
            {
                return false;
            }

            ProductId[] frontage =
                GetRequiredFrontage(shelfRun);

            int newEndExclusive =
                existingFacing.StartFrontageUnit
                + newFrontageUnitCount;

            for (int index = existingFacing.EndFrontageUnitExclusive;
                 index < newEndExclusive;
                 index++)
            {
                if (frontage[index].IsValid
                    && frontage[index] != existingFacing.ProductId)
                {
                    return false;
                }
            }

            bool changed = false;
            int oldEndExclusive = existingFacing.EndFrontageUnitExclusive;

            for (int index = existingFacing.StartFrontageUnit;
                 index < Math.Max(oldEndExclusive, newEndExclusive);
                 index++)
            {
                ProductId desiredProduct =
                    index < newEndExclusive
                        ? existingFacing.ProductId
                        : default;

                if (frontage[index] == desiredProduct)
                {
                    continue;
                }

                frontage[index] = desiredProduct;
                changed = true;
            }

            RemoveRunIfEmpty(shelfRun, frontage);

            if (changed)
            {
                PublishShelfRunChanged(shelfRun);
            }

            return true;
        }

        internal bool TryClearFacing(
            FixtureShelfRunKey shelfRun,
            ProductFacing facing)
        {
            if (isPublishingChanges)
            {
                return false;
            }

            ProductId[] frontage =
                GetRequiredFrontage(shelfRun);

            for (int index = facing.StartFrontageUnit;
                 index < facing.EndFrontageUnitExclusive;
                 index++)
            {
                frontage[index] = default;
            }

            RemoveRunIfEmpty(shelfRun, frontage);
            PublishShelfRunChanged(shelfRun);
            return true;
        }

        internal bool TryClearShelfRun(
            FixtureShelfRunKey shelfRun)
        {
            if (isPublishingChanges)
            {
                return false;
            }

            if (!frontageByShelfRun.Remove(shelfRun))
            {
                return true;
            }

            PublishShelfRunChanged(shelfRun);
            return true;
        }

        internal void ClearFixture(FixtureInstanceId fixtureId)
        {
            if (isPublishingChanges)
            {
                return;
            }

            List<FixtureShelfRunKey> removedRuns =
                new List<FixtureShelfRunKey>();

            foreach (
                KeyValuePair<FixtureShelfRunKey, ProductId[]> pair
                in frontageByShelfRun)
            {
                if (pair.Key.FixtureId == fixtureId)
                {
                    removedRuns.Add(pair.Key);
                }
            }

            for (int index = 0;
                 index < removedRuns.Count;
                 index++)
            {
                FixtureShelfRunKey shelfRun = removedRuns[index];
                frontageByShelfRun.Remove(shelfRun);
                PublishShelfRunChanged(shelfRun);
            }
        }


        private ProductId[] GetOrCreateFrontage(
            FixtureShelfRunKey shelfRun,
            int frontageUnitCount)
        {
            if (frontageByShelfRun.TryGetValue(
                    shelfRun,
                    out ProductId[] frontage))
            {
                if (frontage.Length != frontageUnitCount)
                {
                    throw new InvalidOperationException(
                        $"Shelf run '{shelfRun}' changed frontage-unit count while assignments existed.");
                }

                return frontage;
            }

            frontage = new ProductId[frontageUnitCount];
            frontageByShelfRun.Add(shelfRun, frontage);
            return frontage;
        }

        private ProductId[] GetRequiredFrontage(
            FixtureShelfRunKey shelfRun)
        {
            if (frontageByShelfRun.TryGetValue(
                    shelfRun,
                    out ProductId[] frontage))
            {
                return frontage;
            }

            throw new InvalidOperationException(
                $"Shelf run '{shelfRun}' has no product facing.");
        }

        private void RemoveRunIfEmpty(
            FixtureShelfRunKey shelfRun,
            ProductId[] frontage)
        {
            for (int index = 0;
                 index < frontage.Length;
                 index++)
            {
                if (frontage[index].IsValid)
                {
                    return;
                }
            }

            frontageByShelfRun.Remove(shelfRun);
        }

        private void PublishShelfRunChanged(
            FixtureShelfRunKey shelfRun)
        {
            isPublishingChanges = true;

            try
            {
                ShelfRunChanged?.Invoke(shelfRun);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}
