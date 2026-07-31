namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies why two grid vertices cannot define one straight wall run.
    /// </summary>
    public enum WallVertexRunPlanFailure
    {
        None = 0,

        DifferentLevel = 1,

        SameVertex = 2,

        NotAxisAligned = 3
    }
}
