namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Identifies why two grid positions cannot describe
    /// one rectangular cell area.
    /// </summary>
    public enum RectangularCellAreaPlanFailure
    {
        None,

        /// <summary>
        /// The two positions belong to different logical floors.
        /// </summary>
        DifferentLevel
    }
}