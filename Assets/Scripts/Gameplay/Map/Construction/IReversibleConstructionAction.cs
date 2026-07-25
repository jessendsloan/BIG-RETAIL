namespace BigRetail.Map.Construction
{
    /// <summary>
    /// A successfully committed construction transaction that knows
    /// how to reverse and reapply itself through its owning domain.
    /// </summary>
    public interface IReversibleConstructionAction
    {
        string Description { get; }

        int ChangeCount { get; }

        ConstructionActionResult TryUndo();

        ConstructionActionResult TryRedo();
    }
}
