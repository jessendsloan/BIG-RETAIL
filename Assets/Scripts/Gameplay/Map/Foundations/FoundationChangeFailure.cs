namespace BigRetail.Map.Foundations
{
    /// <summary>
    /// Identifies why a requested foundation operation was rejected.
    /// </summary>
    public enum FoundationChangeFailure
    {
        None,

        /// <summary>
        /// The supplied collection contained no cells.
        /// </summary>
        EmptyRequest,

        /// <summary>
        /// The requested cell is not part of the authored map.
        /// </summary>
        OutsideMap,

        /// <summary>
        /// The requested cell is not eligible for construction.
        /// </summary>
        OutsideConstructionArea,

        /// <summary>
        /// Removing the requested Foundation would leave a Floor or Wall
        /// without structural support.
        /// </summary>
        SupportsConstruction,

        /// <summary>
        /// A foundation already occupies the requested cell.
        /// </summary>
        AlreadyExists,

        /// <summary>
        /// No foundation exists in the requested cell.
        /// </summary>
        NotFound
    }
}
