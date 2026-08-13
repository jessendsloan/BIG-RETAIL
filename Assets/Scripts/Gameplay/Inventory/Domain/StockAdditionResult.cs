namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Reports the outcome of stock entering one inventory location.
    /// </summary>
    public readonly struct StockAdditionResult
    {
        public bool Succeeded =>
            Failure == StockAdditionFailure.None;

        public StockAdditionFailure Failure { get; }

        public int QuantityAdded { get; }

        public int QuantityAfter { get; }


        private StockAdditionResult(
            StockAdditionFailure failure,
            int quantityAdded,
            int quantityAfter)
        {
            Failure = failure;
            QuantityAdded = quantityAdded;
            QuantityAfter = quantityAfter;
        }


        internal static StockAdditionResult Success(
            int quantityAdded,
            int quantityAfter)
        {
            return new StockAdditionResult(
                StockAdditionFailure.None,
                quantityAdded,
                quantityAfter);
        }

        internal static StockAdditionResult Failed(
            StockAdditionFailure failure,
            int quantity)
        {
            return new StockAdditionResult(
                failure,
                0,
                quantity);
        }
    }
}
