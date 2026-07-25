namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies why a requested wall change was rejected.
    /// </summary>
    public enum WallChangeFailure
    {
        None,

        /// <summary>
        /// A batch operation contained no wall edges.
        /// </summary>
        EmptyRequest,

        /// <summary>
        /// The same canonical edge appeared more than once
        /// inside a single batch request.
        /// </summary>
        DuplicateRequest,

        /// <summary>
        /// The requested edge does not touch any valid map cell.
        /// </summary>
        OutsideMap,

        /// <summary>
        /// The requested edge does not touch a cell that is
        /// physically eligible for construction.
        /// </summary>
        OutsideConstructionArea,

        /// <summary>
        /// A wall already occupies the requested edge.
        /// </summary>
        AlreadyExists,

        /// <summary>
        /// No wall exists on the requested edge to remove.
        /// </summary>
        NotFound
    }
}