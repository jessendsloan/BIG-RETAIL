namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Identifies why a requested floor operation was rejected.
    /// </summary>
    public enum FloorChangeFailure
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
        /// A floor already occupies the requested cell.
        /// </summary>
        AlreadyExists,

        /// <summary>
        /// No floor exists in the requested cell.
        /// </summary>
        NotFound
    }
}