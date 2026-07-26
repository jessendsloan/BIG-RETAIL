namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Reports the outcome of one requested movement between stock locations.
    /// </summary>
    public readonly struct StockTransferResult
    {
        public bool Succeeded =>
            Failure == StockTransferFailure.None;

        public StockTransferFailure Failure { get; }
        public int QuantityMoved { get; }
        public int SourceQuantityAfter { get; }
        public int DestinationQuantityAfter { get; }


        private StockTransferResult(
            StockTransferFailure failure,
            int quantityMoved,
            int sourceQuantityAfter,
            int destinationQuantityAfter)
        {
            Failure = failure;
            QuantityMoved = quantityMoved;
            SourceQuantityAfter = sourceQuantityAfter;
            DestinationQuantityAfter = destinationQuantityAfter;
        }


        internal static StockTransferResult Success(
            int quantityMoved,
            int sourceQuantityAfter,
            int destinationQuantityAfter)
        {
            return new StockTransferResult(
                StockTransferFailure.None,
                quantityMoved,
                sourceQuantityAfter,
                destinationQuantityAfter);
        }

        internal static StockTransferResult Failed(
            StockTransferFailure failure,
            int sourceQuantity,
            int destinationQuantity)
        {
            return new StockTransferResult(
                failure,
                0,
                sourceQuantity,
                destinationQuantity);
        }
    }
}
