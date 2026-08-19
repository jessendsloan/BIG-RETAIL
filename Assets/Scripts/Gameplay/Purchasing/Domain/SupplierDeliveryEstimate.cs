namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// The scheduled consequence of a supplier delivery rule. Route service
    /// commits to a day without inventing an arrival hour that is not authored.
    /// </summary>
    public readonly struct SupplierDeliveryEstimate
    {
        public CommercialTime EarliestArrival { get; }

        public bool HasExactArrivalTime { get; }


        private SupplierDeliveryEstimate(
            CommercialTime earliestArrival,
            bool hasExactArrivalTime)
        {
            EarliestArrival = earliestArrival;
            HasExactArrivalTime = hasExactArrivalTime;
        }


        public static SupplierDeliveryEstimate Exact(
            CommercialTime arrival)
        {
            return new SupplierDeliveryEstimate(arrival, true);
        }

        public static SupplierDeliveryEstimate RouteDay(int dayIndex)
        {
            return new SupplierDeliveryEstimate(
                new CommercialTime(dayIndex, 0, 0),
                false);
        }
    }
}
