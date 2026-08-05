namespace BigRetail.Departments
{
    /// <summary>
    /// The currently knowable physical readiness of one department plan.
    /// Fixtures, merchandise, staffing, and customer-access requirements are
    /// intentionally added by later systems.
    /// </summary>
    public readonly struct DepartmentSpatialReadiness
    {
        public DepartmentPlanId PlanId { get; }

        public int AssignedCellCount { get; }

        public int MinimumCellCount { get; }

        public int MissingFoundationCount { get; }

        public int MissingFloorCount { get; }

        public bool MeetsMinimumArea =>
            AssignedCellCount >= MinimumCellCount;

        public bool HasCompleteFoundation =>
            MissingFoundationCount == 0;

        public bool HasCompleteFloor =>
            MissingFloorCount == 0;

        public bool IsSpatiallyReady =>
            MeetsMinimumArea
            && HasCompleteFoundation
            && HasCompleteFloor;


        public DepartmentSpatialReadiness(
            DepartmentPlanId planId,
            int assignedCellCount,
            int minimumCellCount,
            int missingFoundationCount,
            int missingFloorCount)
        {
            PlanId = planId;
            AssignedCellCount = assignedCellCount;
            MinimumCellCount = minimumCellCount;
            MissingFoundationCount = missingFoundationCount;
            MissingFloorCount = missingFloorCount;
        }
    }
}
