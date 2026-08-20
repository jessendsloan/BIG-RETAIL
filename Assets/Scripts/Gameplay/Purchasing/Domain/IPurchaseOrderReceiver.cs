using BigRetail.Merchandise.Domain;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Boundary through which arrived supplier stock enters the store's
    /// authoritative receiving and inventory system.
    /// </summary>
    public interface IPurchaseOrderReceiver
    {
        bool TryReceive(
            ProductId productId,
            int unitCount);
    }
}
