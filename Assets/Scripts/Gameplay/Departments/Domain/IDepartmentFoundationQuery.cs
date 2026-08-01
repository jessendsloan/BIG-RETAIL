using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    /// <summary>
    /// Narrow physical-map boundary required to designate a department cell.
    /// Department planning depends on structural support, not a particular
    /// Foundation implementation.
    /// </summary>
    public interface IDepartmentFoundationQuery
    {
        bool HasFoundation(GridPosition cell);
    }
}
