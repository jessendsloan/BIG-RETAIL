using System;

namespace BigRetail.Map.Fixtures
{
    public enum FixtureEquipmentOrderStatus
    {
        Scheduled = 0,
        ReadyToReceive = 1,
        Received = 2
    }


    /// <summary>
    /// One paid BIG Wholesale equipment shipment for a single fixture model.
    /// </summary>
    public sealed class FixtureEquipmentOrder
    {
        public const string ExclusiveSupplierId = "BIG";
        public const string ExclusiveSupplierDisplayName = "BIG Wholesale";

        public long OrderNumber { get; }

        public string SupplierId => ExclusiveSupplierId;

        public string SupplierDisplayName => ExclusiveSupplierDisplayName;

        public FixtureDefinitionId FixtureDefinitionId { get; }

        public int Quantity { get; }

        public long TotalCostCents { get; }

        public long PlacedAtGameSeconds { get; }

        public long ReadyAtGameSeconds { get; }

        public FixtureEquipmentOrderStatus Status { get; private set; }


        internal FixtureEquipmentOrder(
            long orderNumber,
            FixtureDefinitionId fixtureDefinitionId,
            int quantity,
            long totalCostCents,
            long placedAtGameSeconds,
            long readyAtGameSeconds)
        {
            if (orderNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(orderNumber));
            }

            if (!fixtureDefinitionId.IsValid)
            {
                throw new ArgumentException(
                    "An equipment order requires a fixture definition.",
                    nameof(fixtureDefinitionId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            if (totalCostCents <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCostCents));
            }

            if (placedAtGameSeconds < 0
                || readyAtGameSeconds < placedAtGameSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(readyAtGameSeconds));
            }

            OrderNumber = orderNumber;
            FixtureDefinitionId = fixtureDefinitionId;
            Quantity = quantity;
            TotalCostCents = totalCostCents;
            PlacedAtGameSeconds = placedAtGameSeconds;
            ReadyAtGameSeconds = readyAtGameSeconds;
            Status = FixtureEquipmentOrderStatus.Scheduled;
        }


        internal bool MarkReady()
        {
            if (Status != FixtureEquipmentOrderStatus.Scheduled)
            {
                return false;
            }

            Status = FixtureEquipmentOrderStatus.ReadyToReceive;
            return true;
        }

        internal bool MarkReceived()
        {
            if (Status != FixtureEquipmentOrderStatus.ReadyToReceive)
            {
                return false;
            }

            Status = FixtureEquipmentOrderStatus.Received;
            return true;
        }
    }
}
