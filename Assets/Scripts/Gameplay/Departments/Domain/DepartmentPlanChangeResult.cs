using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    public readonly struct DepartmentPlanChangeResult
    {
        public bool Succeeded { get; }

        public bool Changed { get; }

        public DepartmentPlanId PlanId { get; }

        public int AddedCellCount { get; }

        public DepartmentPlanChangeFailure Failure { get; }

        public GridPosition FailureCell { get; }


        private DepartmentPlanChangeResult(
            bool succeeded,
            bool changed,
            DepartmentPlanId planId,
            int addedCellCount,
            DepartmentPlanChangeFailure failure,
            GridPosition failureCell)
        {
            Succeeded = succeeded;
            Changed = changed;
            PlanId = planId;
            AddedCellCount = addedCellCount;
            Failure = failure;
            FailureCell = failureCell;
        }


        public static DepartmentPlanChangeResult Success(
            DepartmentPlanId planId,
            int addedCellCount)
        {
            return new DepartmentPlanChangeResult(
                true,
                addedCellCount > 0,
                planId,
                addedCellCount,
                DepartmentPlanChangeFailure.None,
                default);
        }


        public static DepartmentPlanChangeResult Rejected(
            DepartmentPlanId planId,
            DepartmentPlanChangeFailure failure,
            GridPosition failureCell = default)
        {
            return new DepartmentPlanChangeResult(
                false,
                false,
                planId,
                0,
                failure,
                failureCell);
        }
    }
}
