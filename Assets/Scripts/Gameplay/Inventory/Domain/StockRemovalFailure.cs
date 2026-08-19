namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Explains why stock could not leave a logical inventory location.
    /// </summary>
    public enum StockRemovalFailure
    {
        None = 0,
        InvalidQuantity = 1,
        UnknownProduct = 2,
        UnknownLocation = 3,
        InsufficientStock = 4
    }
}
