using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    /// <summary>
    /// Narrow physical-world boundary used to evaluate whether a department
    /// plan has a constructed surface. Runtime hosts will adapt Foundation
    /// and Floor state to this contract.
    /// </summary>
    public interface IDepartmentSurfaceQuery
    {
        bool HasFoundation(GridPosition cell);

        bool HasFloor(GridPosition cell);
    }
}
