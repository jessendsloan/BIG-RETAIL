namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Explains why newly received stock could not enter inventory.
    /// </summary>
    public enum StockAdditionFailure
    {
        None = 0,
        InvalidQuantity = 1,
        UnknownProduct = 2,
        UnknownLocation = 3,
        QuantityOverflow = 4
    }
}
