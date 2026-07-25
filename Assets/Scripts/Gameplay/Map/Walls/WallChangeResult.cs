using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes the outcome of a requested wall change.
    ///
    /// It reports:
    /// - Whether the request succeeded
    /// - Which cell edge was involved
    /// - Why the request failed, when applicable
    ///
    /// This value does not perform validation or modify wall state.
    /// </summary>
    public readonly struct WallChangeResult
    {
        /// <summary>
        /// True when the requested wall change completed successfully.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The cell edge targeted by the request.
        /// </summary>
        public CellEdge Edge { get; }

        /// <summary>
        /// The reason the request failed.
        ///
        /// This will be WallChangeFailure.None when Succeeded is true.
        /// </summary>
        public WallChangeFailure Failure { get; }

        private WallChangeResult(
            bool succeeded,
            CellEdge edge,
            WallChangeFailure failure)
        {
            Succeeded = succeeded;
            Edge = edge;
            Failure = failure;
        }

        /// <summary>
        /// Creates a successful wall-change result.
        /// </summary>
        public static WallChangeResult Success(CellEdge edge)
        {
            return new WallChangeResult(
                true,
                edge,
                WallChangeFailure.None);
        }

        /// <summary>
        /// Creates a rejected wall-change result.
        /// </summary>
        public static WallChangeResult Rejected(
            CellEdge edge,
            WallChangeFailure failure)
        {
            return new WallChangeResult(
                false,
                edge,
                failure);
        }

        public override string ToString()
        {
            if (Succeeded)
            {
                return $"Wall change succeeded at {Edge}.";
            }

            return
                $"Wall change rejected at {Edge}: " +
                $"{Failure}.";
        }
    }
}