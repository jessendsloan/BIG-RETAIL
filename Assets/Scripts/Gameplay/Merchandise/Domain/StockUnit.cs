namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Defines the smallest exact quantity used to track a product.
    ///
    /// Early merchandise can use Each. Weight- and volume-based products can
    /// use integer grams or milliliters without introducing floating-point
    /// stock balances.
    /// </summary>
    public enum StockUnit
    {
        Each = 0,
        Gram = 1,
        Milliliter = 2
    }
}
