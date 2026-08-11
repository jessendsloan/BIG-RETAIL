using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Validates and applies fixture planogram edits.
    /// This service describes what the fixture wants; it never changes stock.
    /// </summary>
    public sealed class FixturePlanogramService : IDisposable
    {
        private readonly FixtureState fixtureState;
        private readonly ProductCatalog productCatalog;
        private bool isDisposed;


        public FixturePlanogramState State { get; }


        public FixturePlanogramService(
            FixtureState fixtureState,
            ProductCatalog productCatalog,
            FixturePlanogramState state = null)
        {
            this.fixtureState =
                fixtureState
                ?? throw new ArgumentNullException(nameof(fixtureState));

            this.productCatalog =
                productCatalog
                ?? throw new ArgumentNullException(nameof(productCatalog));

            State = state ?? new FixturePlanogramState();

            fixtureState.FixtureRemoved += HandleFixtureRemoved;
        }


        public bool TryAssignFrontage(
            FixtureShelfRunKey shelfRun,
            int startFrontageUnit,
            int frontageUnitCount,
            ProductId productId,
            out FixturePlanogramFailure failure)
        {
            if (!TryResolveShelfRun(
                    shelfRun,
                    out FixtureDisplayFaceDefinition displayFace,
                    out failure)
                || !TryValidateProduct(productId, out failure)
                || !TryValidateRange(
                    displayFace,
                    startFrontageUnit,
                    frontageUnitCount,
                    out failure))
            {
                return false;
            }

            int endExclusive = startFrontageUnit + frontageUnitCount;

            for (int index = startFrontageUnit;
                 index < endExclusive;
                 index++)
            {
                if (State.TryGetProductAt(
                        shelfRun,
                        index,
                        out ProductId existingProductId)
                    && existingProductId != productId)
                {
                    failure = FixturePlanogramFailure.FrontageOccupied;
                    return false;
                }
            }

            if (!State.TryAssignRange(
                    shelfRun,
                    displayFace.FrontageUnitsPerRun,
                    startFrontageUnit,
                    frontageUnitCount,
                    productId))
            {
                failure = FixturePlanogramFailure.Busy;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        public int GetMaximumFrontageUnitCount(
            FixtureShelfRunKey shelfRun,
            int startFrontageUnit,
            ProductId compatibleProductId = default)
        {
            if (!TryResolveShelfRun(
                    shelfRun,
                    out FixtureDisplayFaceDefinition displayFace,
                    out _)
                || startFrontageUnit < 0
                || startFrontageUnit >= displayFace.FrontageUnitsPerRun)
            {
                return 0;
            }

            int availableUnitCount = 0;

            for (int index = startFrontageUnit;
                 index < displayFace.FrontageUnitsPerRun;
                 index++)
            {
                if (State.TryGetProductAt(
                        shelfRun,
                        index,
                        out ProductId existingProductId)
                    && (!compatibleProductId.IsValid
                        || existingProductId != compatibleProductId))
                {
                    break;
                }

                availableUnitCount++;
            }

            return availableUnitCount;
        }

        public bool TryReplaceFacingProduct(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            ProductId replacementProductId,
            out FixturePlanogramFailure failure)
        {
            if (!TryResolveShelfRun(
                    shelfRun,
                    out FixtureDisplayFaceDefinition displayFace,
                    out failure)
                || !TryValidateProduct(replacementProductId, out failure)
                || !TryValidateRange(
                    displayFace,
                    frontageUnitIndex,
                    1,
                    out failure))
            {
                return false;
            }

            if (!State.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex,
                    out ProductFacing existingFacing))
            {
                failure = FixturePlanogramFailure.NoFacing;
                return false;
            }

            if (!State.TryReplaceFacingProduct(
                    shelfRun,
                    existingFacing,
                    replacementProductId))
            {
                failure = FixturePlanogramFailure.Busy;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        public bool TryResizeFacing(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            int newFrontageUnitCount,
            out FixturePlanogramFailure failure)
        {
            if (!TryResolveShelfRun(
                    shelfRun,
                    out FixtureDisplayFaceDefinition displayFace,
                    out failure)
                || !TryValidateRange(
                    displayFace,
                    frontageUnitIndex,
                    1,
                    out failure))
            {
                return false;
            }

            if (!State.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex,
                    out ProductFacing existingFacing))
            {
                failure = FixturePlanogramFailure.NoFacing;
                return false;
            }

            if (!TryValidateRange(
                    displayFace,
                    existingFacing.StartFrontageUnit,
                    newFrontageUnitCount,
                    out failure))
            {
                return false;
            }

            if (!State.TryResizeFacing(
                    shelfRun,
                    existingFacing,
                    newFrontageUnitCount))
            {
                failure = FixturePlanogramFailure.FrontageOccupied;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        public bool TryClearFacing(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex,
            out FixturePlanogramFailure failure)
        {
            if (!TryResolveShelfRun(
                    shelfRun,
                    out FixtureDisplayFaceDefinition displayFace,
                    out failure)
                || !TryValidateRange(
                    displayFace,
                    frontageUnitIndex,
                    1,
                    out failure))
            {
                return false;
            }

            if (!State.TryGetFacingAt(
                    shelfRun,
                    frontageUnitIndex,
                    out ProductFacing facing))
            {
                failure = FixturePlanogramFailure.NoFacing;
                return false;
            }

            if (!State.TryClearFacing(shelfRun, facing))
            {
                failure = FixturePlanogramFailure.Busy;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        public bool TryClearShelfRun(
            FixtureShelfRunKey shelfRun,
            out FixturePlanogramFailure failure)
        {
            if (!TryResolveShelfRun(
                    shelfRun,
                    out _,
                    out failure))
            {
                return false;
            }

            if (!State.TryClearShelfRun(shelfRun))
            {
                failure = FixturePlanogramFailure.Busy;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            fixtureState.FixtureRemoved -= HandleFixtureRemoved;
            isDisposed = true;
        }


        private bool TryResolveShelfRun(
            FixtureShelfRunKey shelfRun,
            out FixtureDisplayFaceDefinition displayFace,
            out FixturePlanogramFailure failure)
        {
            if (!fixtureState.TryGetFixture(
                    shelfRun.FixtureId,
                    out FixtureInstance fixture))
            {
                displayFace = null;
                failure = FixturePlanogramFailure.UnknownFixture;
                return false;
            }

            if (!fixture.Definition.MerchandisingProfile.TryGetDisplayFace(
                    shelfRun.LocalDisplaySide,
                    out displayFace))
            {
                failure = FixturePlanogramFailure.InvalidDisplayFace;
                return false;
            }

            if (shelfRun.ShelfRunIndex >= displayFace.ShelfRunCount)
            {
                failure = FixturePlanogramFailure.InvalidShelfRun;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        private bool TryValidateProduct(
            ProductId productId,
            out FixturePlanogramFailure failure)
        {
            if (!productId.IsValid)
            {
                failure = FixturePlanogramFailure.InvalidProduct;
                return false;
            }

            if (!productCatalog.Contains(productId))
            {
                failure = FixturePlanogramFailure.UnknownProduct;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        private static bool TryValidateRange(
            FixtureDisplayFaceDefinition displayFace,
            int startFrontageUnit,
            int frontageUnitCount,
            out FixturePlanogramFailure failure)
        {
            if (startFrontageUnit < 0
                || frontageUnitCount <= 0
                || startFrontageUnit + frontageUnitCount
                    > displayFace.FrontageUnitsPerRun)
            {
                failure = FixturePlanogramFailure.InvalidFrontageRange;
                return false;
            }

            failure = FixturePlanogramFailure.None;
            return true;
        }

        private void HandleFixtureRemoved(FixtureInstance fixture)
        {
            State.ClearFixture(fixture.Id);
        }
    }
}
