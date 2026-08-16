using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Owns merchandise after a shopper removes it from a display and before
    /// a checkout completes the transaction. Each line retains its source
    /// fixture so sales reporting still belongs to the display that earned it.
    /// </summary>
    public sealed class ShoppingBasket
    {
        private readonly List<ShoppingBasketLine> lines =
            new List<ShoppingBasketLine>();


        public IReadOnlyList<ShoppingBasketLine> Lines =>
            lines;

        public int TotalUnitCount { get; private set; }

        public bool IsEmpty =>
            TotalUnitCount == 0;


        public int GetQuantity(
            FixtureInstanceId sourceFixtureId,
            ProductId productId)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                ShoppingBasketLine line = lines[index];

                if (line.SourceFixtureId == sourceFixtureId
                    && line.ProductId == productId)
                {
                    return line.UnitCount;
                }
            }

            return 0;
        }


        internal bool CanAccept(
            FixtureInstanceId sourceFixtureId,
            ProductId productId,
            int unitCount)
        {
            if (!sourceFixtureId.IsValid
                || !productId.IsValid
                || unitCount <= 0
                || TotalUnitCount > int.MaxValue - unitCount)
            {
                return false;
            }

            int currentQuantity =
                GetQuantity(sourceFixtureId, productId);

            return currentQuantity <= int.MaxValue - unitCount;
        }

        internal void Add(
            FixtureInstanceId sourceFixtureId,
            ProductId productId,
            int unitCount)
        {
            if (!CanAccept(
                    sourceFixtureId,
                    productId,
                    unitCount))
            {
                throw new InvalidOperationException(
                    "The shopping basket cannot accept that merchandise line.");
            }

            for (int index = 0; index < lines.Count; index++)
            {
                ShoppingBasketLine line = lines[index];

                if (line.SourceFixtureId != sourceFixtureId
                    || line.ProductId != productId)
                {
                    continue;
                }

                lines[index] =
                    new ShoppingBasketLine(
                        sourceFixtureId,
                        productId,
                        line.UnitCount + unitCount);
                TotalUnitCount += unitCount;
                return;
            }

            lines.Add(
                new ShoppingBasketLine(
                    sourceFixtureId,
                    productId,
                    unitCount));
            TotalUnitCount += unitCount;
        }

        internal void Clear()
        {
            lines.Clear();
            TotalUnitCount = 0;
        }
    }


    public readonly struct ShoppingBasketLine
    {
        public FixtureInstanceId SourceFixtureId { get; }

        public ProductId ProductId { get; }

        public int UnitCount { get; }


        internal ShoppingBasketLine(
            FixtureInstanceId sourceFixtureId,
            ProductId productId,
            int unitCount)
        {
            SourceFixtureId = sourceFixtureId;
            ProductId = productId;
            UnitCount = unitCount;
        }
    }
}
