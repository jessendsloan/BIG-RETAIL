using System;
using BigRetail.Map.Fixtures;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;

namespace BigRetail.Purchasing.Unity
{
    /// <summary>
    /// Adapts supplier receiving to the store's existing inbound, rack, and
    /// overflow inventory path.
    /// </summary>
    public sealed class FixtureBackstockPurchaseOrderReceiver :
        IPurchaseOrderReceiver
    {
        private readonly FixtureBackstockService backstock;


        public FixtureBackstockPurchaseOrderReceiver(
            FixtureBackstockService backstock)
        {
            this.backstock = backstock
                ?? throw new ArgumentNullException(nameof(backstock));
        }


        public bool TryReceive(
            ProductId productId,
            int unitCount)
        {
            return backstock.ReceiveInbound(
                    productId,
                    unitCount)
                .Succeeded;
        }
    }
}
