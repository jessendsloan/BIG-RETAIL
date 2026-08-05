using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Reports whether a requested Foundation-removal batch would preserve
    /// support for every dependent Floor and Wall.
    /// </summary>
    public readonly struct FoundationRemovalValidation
    {
        public bool IsAllowed { get; }

        public GridPosition BlockedCell { get; }


        private FoundationRemovalValidation(
            bool isAllowed,
            GridPosition blockedCell)
        {
            IsAllowed = isAllowed;
            BlockedCell = blockedCell;
        }


        public static FoundationRemovalValidation Allowed()
        {
            return new FoundationRemovalValidation(
                true,
                default);
        }


        public static FoundationRemovalValidation Blocked(
            GridPosition cell)
        {
            return new FoundationRemovalValidation(
                false,
                cell);
        }
    }
}
