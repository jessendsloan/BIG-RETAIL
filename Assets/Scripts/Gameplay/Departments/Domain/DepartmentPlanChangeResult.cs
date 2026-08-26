using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    public readonly struct DepartmentPlanChangeResult
    {
        public bool Succeeded { get; }

        public bool Changed { get; }

        public DepartmentPlanId PlanId { get; }

        public int AddedCellCount { get; }

        public int RemovedCellCount { get; }

        public DepartmentPlanChangeFailure Failure { get; }

        public GridPosition FailureCell { get; }


        private DepartmentPlanChangeResult(
            bool succeeded,
            bool changed,
            DepartmentPlanId planId,
            int addedCellCount,
            int removedCellCount,
            DepartmentPlanChangeFailure failure,
            GridPosition failureCell)
        {
            Succeeded = succeeded;
            Changed = changed;
            PlanId = planId;
            AddedCellCount = addedCellCount;
            RemovedCellCount = removedCellCount;
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
                0,
                DepartmentPlanChangeFailure.None,
                default);
        }


        public static DepartmentPlanChangeResult RemovalSuccess(
            DepartmentPlanId planId,
            int removedCellCount)
        {
            return new DepartmentPlanChangeResult(
                true,
                removedCellCount > 0,
                planId,
                0,
                removedCellCount,
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
                0,
                failure,
                failureCell);
        }
    }
}
