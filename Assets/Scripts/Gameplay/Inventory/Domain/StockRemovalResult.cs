namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Reports the outcome of stock leaving one inventory location.
    /// </summary>
    public readonly struct StockRemovalResult
    {
        public bool Succeeded =>
            Failure == StockRemovalFailure.None;

        public StockRemovalFailure Failure { get; }

        public int QuantityRemoved { get; }

        public int QuantityAfter { get; }


        private StockRemovalResult(
            StockRemovalFailure failure,
            int quantityRemoved,
            int quantityAfter)
        {
            Failure = failure;
            QuantityRemoved = quantityRemoved;
            QuantityAfter = quantityAfter;
        }


        internal static StockRemovalResult Success(
            int quantityRemoved,
            int quantityAfter)
        {
            return new StockRemovalResult(
                StockRemovalFailure.None,
                quantityRemoved,
                quantityAfter);
        }

        internal static StockRemovalResult Failed(
            StockRemovalFailure failure,
            int quantity)
        {
            return new StockRemovalResult(
                failure,
                0,
                quantity);
        }
    }
}
