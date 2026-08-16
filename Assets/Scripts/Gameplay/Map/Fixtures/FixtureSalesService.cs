using System;
using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Settles shopper-owned merchandise as one atomic retail transaction.
    /// Display inventory has already transferred into the basket before this
    /// boundary; this service validates prices, credits cash, records sales,
    /// and clears the paid basket.
    /// </summary>
    public sealed class FixtureSalesService
    {
        private readonly ProductCatalog products;
        private readonly StoreCashState cash;
        private readonly Dictionary<FixtureInstanceId, long>
            fixtureSalesTodayCents =
                new Dictionary<FixtureInstanceId, long>();
        private readonly Dictionary<FixtureInstanceId, int>
            fixtureUnitsSoldToday =
                new Dictionary<FixtureInstanceId, int>();


        public long SalesTodayCents { get; private set; }

        public int UnitsSoldToday { get; private set; }


        public FixtureSalesService(
            ProductCatalog products,
            StoreCashState cash)
        {
            this.products =
                products
                ?? throw new ArgumentNullException(nameof(products));

            this.cash =
                cash
                ?? throw new ArgumentNullException(nameof(cash));
        }


        public event Action<FixtureInstanceId> SalesChanged;


        public long GetFixtureSalesTodayCents(
            FixtureInstanceId fixtureId)
        {
            return fixtureSalesTodayCents.TryGetValue(
                fixtureId,
                out long amountCents)
                    ? amountCents
                    : 0;
        }

        public int GetFixtureUnitsSoldToday(
            FixtureInstanceId fixtureId)
        {
            return fixtureUnitsSoldToday.TryGetValue(
                fixtureId,
                out int unitCount)
                    ? unitCount
                    : 0;
        }

        public FixtureSaleResult TryCompleteBasketSale(
            ShoppingBasket basket)
        {
            if (basket == null)
            {
                return FixtureSaleResult.Failed(
                    FixtureSaleOutcome.BasketUnavailable);
            }

            if (basket.IsEmpty)
            {
                return FixtureSaleResult.Failed(
                    FixtureSaleOutcome.BasketEmpty);
            }

            Dictionary<FixtureInstanceId, SaleAccumulator>
                pendingFixtureSales =
                    new Dictionary<FixtureInstanceId, SaleAccumulator>();

            long transactionRevenueCents = 0;
            int transactionUnitCount = 0;

            for (int index = 0; index < basket.Lines.Count; index++)
            {
                ShoppingBasketLine line = basket.Lines[index];

                if (!products.TryGet(
                        line.ProductId,
                        out ProductDefinition product))
                {
                    return FixtureSaleResult.Failed(
                        FixtureSaleOutcome.UnknownProduct);
                }

                long unitPriceCents = product.RetailUnitPriceCents;

                if (unitPriceCents <= 0)
                {
                    return FixtureSaleResult.Failed(
                        FixtureSaleOutcome.ProductNotPriced);
                }

                if (line.UnitCount <= 0
                    || line.UnitCount > long.MaxValue / unitPriceCents)
                {
                    return FixtureSaleResult.Failed(
                        FixtureSaleOutcome.AccountingLimitReached);
                }

                long lineRevenueCents =
                    unitPriceCents * line.UnitCount;

                if (transactionRevenueCents
                        > long.MaxValue - lineRevenueCents
                    || transactionUnitCount
                        > int.MaxValue - line.UnitCount)
                {
                    return FixtureSaleResult.Failed(
                        FixtureSaleOutcome.AccountingLimitReached);
                }

                transactionRevenueCents += lineRevenueCents;
                transactionUnitCount += line.UnitCount;

                pendingFixtureSales.TryGetValue(
                    line.SourceFixtureId,
                    out SaleAccumulator fixtureSale);

                if (fixtureSale.RevenueCents
                        > long.MaxValue - lineRevenueCents
                    || fixtureSale.UnitCount
                        > int.MaxValue - line.UnitCount)
                {
                    return FixtureSaleResult.Failed(
                        FixtureSaleOutcome.AccountingLimitReached);
                }

                pendingFixtureSales[line.SourceFixtureId] =
                    new SaleAccumulator(
                        fixtureSale.RevenueCents + lineRevenueCents,
                        fixtureSale.UnitCount + line.UnitCount);
            }

            if (cash.BalanceCents
                    > long.MaxValue - transactionRevenueCents
                || SalesTodayCents
                    > long.MaxValue - transactionRevenueCents
                || UnitsSoldToday
                    > int.MaxValue - transactionUnitCount)
            {
                return FixtureSaleResult.Failed(
                    FixtureSaleOutcome.AccountingLimitReached);
            }

            foreach (
                KeyValuePair<FixtureInstanceId, SaleAccumulator> entry
                in pendingFixtureSales)
            {
                if (GetFixtureSalesTodayCents(entry.Key)
                        > long.MaxValue - entry.Value.RevenueCents
                    || GetFixtureUnitsSoldToday(entry.Key)
                        > int.MaxValue - entry.Value.UnitCount)
                {
                    return FixtureSaleResult.Failed(
                        FixtureSaleOutcome.AccountingLimitReached);
                }
            }

            cash.Credit(transactionRevenueCents);
            SalesTodayCents += transactionRevenueCents;
            UnitsSoldToday += transactionUnitCount;

            foreach (
                KeyValuePair<FixtureInstanceId, SaleAccumulator> entry
                in pendingFixtureSales)
            {
                fixtureSalesTodayCents[entry.Key] =
                    GetFixtureSalesTodayCents(entry.Key)
                    + entry.Value.RevenueCents;
                fixtureUnitsSoldToday[entry.Key] =
                    GetFixtureUnitsSoldToday(entry.Key)
                    + entry.Value.UnitCount;
            }

            basket.Clear();

            foreach (FixtureInstanceId fixtureId
                     in pendingFixtureSales.Keys)
            {
                SalesChanged?.Invoke(fixtureId);
            }

            return FixtureSaleResult.Sold(
                transactionUnitCount,
                transactionRevenueCents);
        }


        private readonly struct SaleAccumulator
        {
            public long RevenueCents { get; }

            public int UnitCount { get; }


            public SaleAccumulator(
                long revenueCents,
                int unitCount)
            {
                RevenueCents = revenueCents;
                UnitCount = unitCount;
            }
        }
    }


    public enum FixtureSaleOutcome
    {
        None = 0,
        Sold = 1,
        BasketEmpty = 2,
        BasketUnavailable = 3,
        UnknownProduct = 4,
        ProductNotPriced = 5,
        AccountingLimitReached = 6,
        CheckoutUnavailable = 7
    }


    public readonly struct FixtureSaleResult
    {
        public FixtureSaleOutcome Outcome { get; }

        public int UnitsSold { get; }

        public long RevenueCents { get; }

        public bool Succeeded =>
            Outcome == FixtureSaleOutcome.Sold;


        private FixtureSaleResult(
            FixtureSaleOutcome outcome,
            int unitsSold,
            long revenueCents)
        {
            Outcome = outcome;
            UnitsSold = unitsSold;
            RevenueCents = revenueCents;
        }


        internal static FixtureSaleResult Sold(
            int unitsSold,
            long revenueCents)
        {
            return new FixtureSaleResult(
                FixtureSaleOutcome.Sold,
                unitsSold,
                revenueCents);
        }

        internal static FixtureSaleResult Failed(
            FixtureSaleOutcome outcome)
        {
            return new FixtureSaleResult(
                outcome,
                0,
                0);
        }
    }
}
