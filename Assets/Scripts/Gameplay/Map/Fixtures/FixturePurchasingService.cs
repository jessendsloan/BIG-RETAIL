using System;
using System.Collections.Generic;
using BigRetail.Economy.Domain;
using BigRetail.Inventory.Domain;
using BigRetail.Merchandise.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Owns the first playable purchasing contract: cases become pending
    /// orders, then a receiving action turns those orders into real stock.
    /// Orders spend store cash immediately. Suppliers, lead times, and
    /// delivery labor remain later systems.
    /// </summary>
    public sealed class FixturePurchasingService
    {
        private readonly ProductCatalog productCatalog;
        private readonly FixtureBackstockService backstock;
        private readonly StoreCashState cash;
        private readonly Dictionary<ProductId, int> pendingUnitCounts =
            new Dictionary<ProductId, int>();


        public int CaseUnitCount { get; }

        public int PendingUnitCount
        {
            get
            {
                long pendingUnitCount = 0;

                foreach (int quantity in pendingUnitCounts.Values)
                {
                    pendingUnitCount += quantity;

                    if (pendingUnitCount >= int.MaxValue)
                    {
                        return int.MaxValue;
                    }
                }

                return (int)pendingUnitCount;
            }
        }

        public bool HasPendingDelivery =>
            pendingUnitCounts.Count > 0;

        public long CashBalanceCents =>
            cash.BalanceCents;


        public FixturePurchasingService(
            ProductCatalog productCatalog,
            FixtureBackstockService backstock,
            StoreCashState cash,
            int caseUnitCount)
        {
            this.productCatalog =
                productCatalog
                ?? throw new ArgumentNullException(nameof(productCatalog));

            this.backstock =
                backstock
                ?? throw new ArgumentNullException(nameof(backstock));

            this.cash =
                cash
                ?? throw new ArgumentNullException(nameof(cash));

            if (caseUnitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(caseUnitCount),
                    caseUnitCount,
                    "A purchasing case must contain at least one unit.");
            }

            CaseUnitCount = caseUnitCount;
        }


        public event Action OrdersChanged;


        public int GetPendingUnitCount(ProductId productId)
        {
            return pendingUnitCounts.TryGetValue(
                    productId,
                    out int quantity)
                ? quantity
                : 0;
        }

        public bool TryPlaceCaseOrder(ProductId productId)
        {
            return TryPlaceCaseOrder(
                productId,
                out FixturePurchaseFailure _);
        }

        public bool TryPlaceCaseOrder(
            ProductId productId,
            out FixturePurchaseFailure failure)
        {
            if (!productCatalog.TryGet(
                    productId,
                    out ProductDefinition product))
            {
                failure = FixturePurchaseFailure.UnknownProduct;
                return false;
            }

            if (product.WholesaleCaseCostCents <= 0)
            {
                failure = FixturePurchaseFailure.InvalidCaseCost;
                return false;
            }

            int currentUnitCount = GetPendingUnitCount(productId);

            if (currentUnitCount > int.MaxValue - CaseUnitCount)
            {
                failure = FixturePurchaseFailure.PendingCapacityExceeded;
                return false;
            }

            if (!cash.TrySpend(product.WholesaleCaseCostCents))
            {
                failure = FixturePurchaseFailure.InsufficientFunds;
                return false;
            }

            pendingUnitCounts[productId] =
                currentUnitCount + CaseUnitCount;
            failure = FixturePurchaseFailure.None;
            OrdersChanged?.Invoke();
            return true;
        }

        public FixtureDeliveryReceipt ReceivePendingDelivery()
        {
            if (!HasPendingDelivery)
            {
                return new FixtureDeliveryReceipt(0, 0);
            }

            int receivedUnitCount = 0;
            int failedUnitCount = 0;

            foreach (
                ProductDefinition product
                in productCatalog.EnumerateDefinitions())
            {
                int pendingUnitCount =
                    GetPendingUnitCount(product.Id);

                if (pendingUnitCount <= 0)
                {
                    continue;
                }

                StockAdditionResult result =
                    backstock.ReceiveInbound(
                        product.Id,
                        pendingUnitCount);

                if (result.Succeeded)
                {
                    pendingUnitCounts.Remove(product.Id);
                    receivedUnitCount += result.QuantityAdded;
                }
                else
                {
                    failedUnitCount += pendingUnitCount;
                }
            }

            OrdersChanged?.Invoke();

            return new FixtureDeliveryReceipt(
                receivedUnitCount,
                failedUnitCount);
        }
    }


    public enum FixturePurchaseFailure
    {
        None = 0,
        UnknownProduct = 1,
        InvalidCaseCost = 2,
        InsufficientFunds = 3,
        PendingCapacityExceeded = 4
    }


    public readonly struct FixtureDeliveryReceipt
    {
        public FixtureDeliveryReceipt(
            int receivedUnitCount,
            int failedUnitCount)
        {
            ReceivedUnitCount = receivedUnitCount;
            FailedUnitCount = failedUnitCount;
        }


        public int ReceivedUnitCount { get; }

        public int FailedUnitCount { get; }

        public bool Succeeded =>
            ReceivedUnitCount > 0
            && FailedUnitCount == 0;
    }
}
