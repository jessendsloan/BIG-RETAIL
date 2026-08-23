using System;
using System.Collections.Generic;
using BigRetail.Economy.Domain;

namespace BigRetail.Map.Fixtures
{
    public enum FixtureEquipmentOrderFailure
    {
        None = 0,
        EmptyOrder = 1,
        UnknownEquipment = 2,
        InvalidQuantity = 3,
        AccountingLimitReached = 4,
        InsufficientFunds = 5,
        OrderNotFound = 6,
        OrderNotReady = 7
    }


    public readonly struct FixtureEquipmentOrderResult
    {
        public bool Succeeded { get; }

        public FixtureEquipmentOrderFailure Failure { get; }

        public IReadOnlyList<FixtureEquipmentOrder> Orders { get; }

        public long TotalCostCents { get; }


        private FixtureEquipmentOrderResult(
            bool succeeded,
            FixtureEquipmentOrderFailure failure,
            IReadOnlyList<FixtureEquipmentOrder> orders,
            long totalCostCents)
        {
            Succeeded = succeeded;
            Failure = failure;
            Orders = orders ?? Array.Empty<FixtureEquipmentOrder>();
            TotalCostCents = totalCostCents;
        }

        internal static FixtureEquipmentOrderResult Success(
            IReadOnlyList<FixtureEquipmentOrder> orders,
            long totalCostCents)
        {
            return new FixtureEquipmentOrderResult(
                true,
                FixtureEquipmentOrderFailure.None,
                orders,
                totalCostCents);
        }

        public static FixtureEquipmentOrderResult Rejected(
            FixtureEquipmentOrderFailure failure)
        {
            return new FixtureEquipmentOrderResult(
                false,
                failure,
                Array.Empty<FixtureEquipmentOrder>(),
                0);
        }
    }


    /// <summary>
    /// Pays for, schedules, and receives BIG Wholesale fixture-equipment
    /// shipments. Equipment has one exclusive supplier, so supplier selection
    /// is deliberately outside this service.
    /// </summary>
    public sealed class FixtureEquipmentOrderService
    {
        private readonly FixtureEquipmentCatalog catalog;
        private readonly FixtureEquipmentInventory inventory;
        private readonly StoreCashState cash;
        private readonly List<FixtureEquipmentOrder> orders =
            new List<FixtureEquipmentOrder>();
        private long nextOrderNumber = 1;


        public FixtureEquipmentOrderService(
            FixtureEquipmentCatalog catalog,
            FixtureEquipmentInventory inventory,
            StoreCashState cash)
        {
            this.catalog = catalog
                ?? throw new ArgumentNullException(nameof(catalog));
            this.inventory = inventory
                ?? throw new ArgumentNullException(nameof(inventory));
            this.cash = cash
                ?? throw new ArgumentNullException(nameof(cash));
        }


        public event Action OrdersChanged;


        public FixtureEquipmentOrderResult TryPlaceOrders(
            IReadOnlyDictionary<FixtureDefinitionId, int> requestedQuantities,
            long currentGameSeconds)
        {
            if (requestedQuantities == null
                || requestedQuantities.Count == 0)
            {
                return FixtureEquipmentOrderResult.Rejected(
                    FixtureEquipmentOrderFailure.EmptyOrder);
            }

            if (currentGameSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentGameSeconds));
            }

            long totalCostCents = 0;
            List<OrderLine> lines =
                new List<OrderLine>(requestedQuantities.Count);

            foreach (KeyValuePair<FixtureDefinitionId, int> request
                     in requestedQuantities)
            {
                if (request.Value <= 0)
                {
                    return FixtureEquipmentOrderResult.Rejected(
                        FixtureEquipmentOrderFailure.InvalidQuantity);
                }

                if (!catalog.TryGet(
                        request.Key,
                        out FixtureEquipmentDefinition definition))
                {
                    return FixtureEquipmentOrderResult.Rejected(
                        FixtureEquipmentOrderFailure.UnknownEquipment);
                }

                try
                {
                    long lineCost = checked(
                        definition.UnitPriceCents * request.Value);
                    totalCostCents = checked(totalCostCents + lineCost);
                    _ = checked(
                        currentGameSeconds
                        + definition.DeliveryLeadTimeSeconds);
                    lines.Add(
                        new OrderLine(
                            definition,
                            request.Value,
                            lineCost));
                }
                catch (OverflowException)
                {
                    return FixtureEquipmentOrderResult.Rejected(
                        FixtureEquipmentOrderFailure.AccountingLimitReached);
                }
            }

            if (!cash.TrySpend(totalCostCents))
            {
                return FixtureEquipmentOrderResult.Rejected(
                    FixtureEquipmentOrderFailure.InsufficientFunds);
            }

            List<FixtureEquipmentOrder> placed =
                new List<FixtureEquipmentOrder>(lines.Count);

            for (int index = 0; index < lines.Count; index++)
            {
                OrderLine line = lines[index];
                FixtureEquipmentOrder order =
                    new FixtureEquipmentOrder(
                        nextOrderNumber++,
                        line.Definition.FixtureDefinitionId,
                        line.Quantity,
                        line.TotalCostCents,
                        currentGameSeconds,
                        checked(
                            currentGameSeconds
                            + line.Definition.DeliveryLeadTimeSeconds));
                orders.Add(order);
                placed.Add(order);
            }

            OrdersChanged?.Invoke();
            return FixtureEquipmentOrderResult.Success(
                placed,
                totalCostCents);
        }

        public int AdvanceTo(long currentGameSeconds)
        {
            if (currentGameSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentGameSeconds));
            }

            int changedCount = 0;

            for (int index = 0; index < orders.Count; index++)
            {
                FixtureEquipmentOrder order = orders[index];

                if (order.Status == FixtureEquipmentOrderStatus.Scheduled
                    && currentGameSeconds >= order.ReadyAtGameSeconds
                    && order.MarkReady())
                {
                    changedCount++;
                }
            }

            if (changedCount > 0)
            {
                OrdersChanged?.Invoke();
            }

            return changedCount;
        }

        public FixtureEquipmentOrderResult Receive(long orderNumber)
        {
            FixtureEquipmentOrder order = FindOrder(orderNumber);

            if (order == null)
            {
                return FixtureEquipmentOrderResult.Rejected(
                    FixtureEquipmentOrderFailure.OrderNotFound);
            }

            if (order.Status != FixtureEquipmentOrderStatus.ReadyToReceive)
            {
                return FixtureEquipmentOrderResult.Rejected(
                    FixtureEquipmentOrderFailure.OrderNotReady);
            }

            inventory.Add(order.FixtureDefinitionId, order.Quantity);
            order.MarkReceived();
            OrdersChanged?.Invoke();
            return FixtureEquipmentOrderResult.Success(
                new[] { order },
                order.TotalCostCents);
        }

        public int GetOutstandingQuantity(
            FixtureDefinitionId fixtureDefinitionId)
        {
            int quantity = 0;

            for (int index = 0; index < orders.Count; index++)
            {
                FixtureEquipmentOrder order = orders[index];

                if (order.FixtureDefinitionId == fixtureDefinitionId
                    && order.Status != FixtureEquipmentOrderStatus.Received)
                {
                    quantity = checked(quantity + order.Quantity);
                }
            }

            return quantity;
        }

        public IEnumerable<FixtureEquipmentOrder> EnumerateOrders()
        {
            for (int index = 0; index < orders.Count; index++)
            {
                yield return orders[index];
            }
        }

        public IEnumerable<FixtureEquipmentOrder> EnumerateReadyOrders()
        {
            for (int index = 0; index < orders.Count; index++)
            {
                if (orders[index].Status
                    == FixtureEquipmentOrderStatus.ReadyToReceive)
                {
                    yield return orders[index];
                }
            }
        }

        private FixtureEquipmentOrder FindOrder(long orderNumber)
        {
            for (int index = 0; index < orders.Count; index++)
            {
                if (orders[index].OrderNumber == orderNumber)
                {
                    return orders[index];
                }
            }

            return null;
        }

        private readonly struct OrderLine
        {
            public FixtureEquipmentDefinition Definition { get; }
            public int Quantity { get; }
            public long TotalCostCents { get; }

            public OrderLine(
                FixtureEquipmentDefinition definition,
                int quantity,
                long totalCostCents)
            {
                Definition = definition;
                Quantity = quantity;
                TotalCostCents = totalCostCents;
            }
        }
    }
}
