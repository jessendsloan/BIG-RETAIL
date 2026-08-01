using System;

namespace BigRetail.Departments
{
    /// <summary>
    /// Static operational expectations for one kind of department.
    /// Dynamic stock, fixture, staff, and customer requirements belong to
    /// later focused systems.
    /// </summary>
    public sealed class DepartmentDefinition
    {
        public DepartmentDefinitionId Id { get; }

        public int MinimumCellCount { get; }


        public DepartmentDefinition(
            DepartmentDefinitionId id,
            int minimumCellCount)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A department definition requires a valid ID.",
                    nameof(id));
            }

            if (minimumCellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumCellCount),
                    minimumCellCount,
                    "A department definition requires at least one cell.");
            }

            Id = id;
            MinimumCellCount = minimumCellCount;
        }
    }
}
