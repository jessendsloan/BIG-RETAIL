namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Explains why a stock transfer did not change inventory state.
    /// </summary>
    public enum StockTransferFailure
    {
        None = 0,
        InvalidQuantity = 1,
        UnknownProduct = 2,
        UnknownSourceLocation = 3,
        UnknownDestinationLocation = 4,
        SameLocation = 5,
        InsufficientSourceStock = 6,
        DestinationQuantityOverflow = 7
    }
}
