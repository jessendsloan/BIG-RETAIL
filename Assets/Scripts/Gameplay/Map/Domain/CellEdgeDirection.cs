namespace BigRetail.Map.Domain
{
    /// <summary>
    /// Identifies one of the four visible edges surrounding
    /// an isometric map cell.
    ///
    /// These directions describe the diamond-shaped cell edges,
    /// not rectangular screen directions or Unity world axes.
    /// </summary>
    public enum CellEdgeDirection
    {
        NorthWest,
        NorthEast,
        SouthEast,
        SouthWest
    }
}