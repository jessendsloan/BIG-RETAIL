using BigRetail.Map.Domain;

namespace BigRetail.Map.Construction
{
    /// <summary>
    /// Answers whether construction is currently legal on one map cell.
    ///
    /// Implementations may combine permanent physical eligibility with
    /// mutable rules such as land ownership.
    /// </summary>
    public interface IConstructionCellEligibility
    {
        bool IsEligible(GridPosition position);
    }
}
