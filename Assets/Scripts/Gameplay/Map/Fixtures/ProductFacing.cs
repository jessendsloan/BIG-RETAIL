using System;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// One contiguous product allocation on a shelf run.
    /// </summary>
    public readonly struct ProductFacing : IEquatable<ProductFacing>
    {
        public ProductId ProductId { get; }

        public int StartFrontageUnit { get; }

        public int FrontageUnitCount { get; }

        public int EndFrontageUnitExclusive =>
            StartFrontageUnit + FrontageUnitCount;


        public ProductFacing(
            ProductId productId,
            int startFrontageUnit,
            int frontageUnitCount)
        {
            if (!productId.IsValid)
            {
                throw new ArgumentException(
                    "A product facing requires a valid product identifier.",
                    nameof(productId));
            }

            if (startFrontageUnit < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startFrontageUnit));
            }

            if (frontageUnitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frontageUnitCount));
            }

            ProductId = productId;
            StartFrontageUnit = startFrontageUnit;
            FrontageUnitCount = frontageUnitCount;
        }


        public bool ContainsFrontageUnit(int frontageUnitIndex)
        {
            return frontageUnitIndex >= StartFrontageUnit
                && frontageUnitIndex < EndFrontageUnitExclusive;
        }

        public bool Equals(ProductFacing other)
        {
            return ProductId == other.ProductId
                && StartFrontageUnit == other.StartFrontageUnit
                && FrontageUnitCount == other.FrontageUnitCount;
        }

        public override bool Equals(object obj)
        {
            return obj is ProductFacing other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ProductId.GetHashCode();
                hash = (hash * 397) ^ StartFrontageUnit;
                hash = (hash * 397) ^ FrontageUnitCount;
                return hash;
            }
        }
    }
}
