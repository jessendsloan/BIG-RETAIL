namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies why two wall edges cannot form one straight wall run.
    /// </summary>
    public enum WallRunPlanFailure
    {
        None,

        /// <summary>
        /// The two edges belong to different logical floors.
        /// </summary>
        DifferentLevel,

        /// <summary>
        /// The two edges face different canonical directions.
        /// </summary>
        DifferentDirection,

        /// <summary>
        /// The two edges are parallel but do not belong to the same line.
        /// </summary>
        NotCollinear
    }
}