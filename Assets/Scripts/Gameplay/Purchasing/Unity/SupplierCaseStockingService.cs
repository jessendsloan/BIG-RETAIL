using System;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Coordinates one-case supplier stocking commands between a staged
    /// purchase order and a worker-selected physical storage rack. Player
    /// input and future worker agents share this API.
    /// </summary>
    public sealed class SupplierCaseStockingService
    {
        private readonly PurchaseOrderFulfillmentService fulfillment;
        private readonly FixtureBackstockService backstock;


        public SupplierCaseStockingService(
            PurchaseOrderFulfillmentService fulfillment,
            FixtureBackstockService backstock)
        {
            this.fulfillment = fulfillment
                ?? throw new ArgumentNullException(nameof(fulfillment));
            this.backstock = backstock
                ?? throw new ArgumentNullException(nameof(backstock));
        }


        public bool TryGetNextCase(
            long orderNumber,
            out InboundPurchasePack supplierCase)
        {
            return fulfillment.TryGetNextPurchasePack(
                orderNumber,
                out supplierCase);
        }

        public SupplierCaseStockingResult TryStockCase(
            InboundPurchasePack supplierCase,
            FixtureInstanceId rackFixtureId)
        {
            if (!supplierCase.IsValid
                || !fulfillment.TryGetNextPurchasePack(
                    supplierCase.OrderNumber,
                    out InboundPurchasePack currentCase)
                || currentCase != supplierCase)
            {
                return SupplierCaseStockingResult.Failed(
                    supplierCase,
                    rackFixtureId,
                    SupplierCaseStockingFailure.DeliveryChanged);
            }

            TargetRackReceiver receiver =
                new TargetRackReceiver(
                    backstock,
                    rackFixtureId);
            PurchaseOrderReceivingResult receivingResult =
                fulfillment.ReceivePurchasePack(
                    supplierCase,
                    receiver);

            if (receivingResult.ReceivedUnitCount > 0)
            {
                return SupplierCaseStockingResult.Success(
                    supplierCase,
                    rackFixtureId,
                    receivingResult.ReceivedUnitCount,
                    receivingResult.CompletedOrderCount);
            }

            return SupplierCaseStockingResult.Failed(
                supplierCase,
                rackFixtureId,
                MapFailure(receiver.LastResult.Failure));
        }


        private static SupplierCaseStockingFailure MapFailure(
            FixtureBackstockReceiptFailure failure)
        {
            return failure switch
            {
                FixtureBackstockReceiptFailure.UnknownRack =>
                    SupplierCaseStockingFailure.UnknownRack,

                FixtureBackstockReceiptFailure
                    .NoAvailableCaseSlot =>
                    SupplierCaseStockingFailure
                        .NoAvailableRackCaseSlot,

                FixtureBackstockReceiptFailure.InvalidQuantity =>
                    SupplierCaseStockingFailure.InvalidCase,

                _ => SupplierCaseStockingFailure.InventoryRejected
            };
        }


        private sealed class TargetRackReceiver :
            IPurchaseOrderReceiver
        {
            private readonly FixtureBackstockService backstock;
            private readonly FixtureInstanceId rackFixtureId;


            public FixtureBackstockReceiptResult LastResult
            {
                get;
                private set;
            }


            public TargetRackReceiver(
                FixtureBackstockService backstock,
                FixtureInstanceId rackFixtureId)
            {
                this.backstock = backstock;
                this.rackFixtureId = rackFixtureId;
            }


            public bool TryReceive(
                ProductId productId,
                int unitCount)
            {
                LastResult = backstock.TryReceiveInboundAtRack(
                    rackFixtureId,
                    productId,
                    unitCount);
                return LastResult.Succeeded;
            }
        }
    }


    public enum SupplierCaseStockingFailure
    {
        None = 0,
        InvalidCase = 1,
        DeliveryChanged = 2,
        UnknownRack = 3,
        NoAvailableRackCaseSlot = 4,
        InventoryRejected = 5
    }


    public readonly struct SupplierCaseStockingResult
    {
        public InboundPurchasePack SupplierCase { get; }

        public FixtureInstanceId RackFixtureId { get; }

        public int ReceivedUnitCount { get; }

        public int CompletedOrderCount { get; }

        public SupplierCaseStockingFailure Failure { get; }

        public bool Succeeded =>
            Failure == SupplierCaseStockingFailure.None
            && ReceivedUnitCount > 0;


        private SupplierCaseStockingResult(
            InboundPurchasePack supplierCase,
            FixtureInstanceId rackFixtureId,
            int receivedUnitCount,
            int completedOrderCount,
            SupplierCaseStockingFailure failure)
        {
            SupplierCase = supplierCase;
            RackFixtureId = rackFixtureId;
            ReceivedUnitCount = receivedUnitCount;
            CompletedOrderCount = completedOrderCount;
            Failure = failure;
        }


        internal static SupplierCaseStockingResult Success(
            InboundPurchasePack supplierCase,
            FixtureInstanceId rackFixtureId,
            int receivedUnitCount,
            int completedOrderCount)
        {
            return new SupplierCaseStockingResult(
                supplierCase,
                rackFixtureId,
                receivedUnitCount,
                completedOrderCount,
                SupplierCaseStockingFailure.None);
        }

        internal static SupplierCaseStockingResult Failed(
            InboundPurchasePack supplierCase,
            FixtureInstanceId rackFixtureId,
            SupplierCaseStockingFailure failure)
        {
            return new SupplierCaseStockingResult(
                supplierCase,
                rackFixtureId,
                receivedUnitCount: 0,
                completedOrderCount: 0,
                failure);
        }
    }
}
