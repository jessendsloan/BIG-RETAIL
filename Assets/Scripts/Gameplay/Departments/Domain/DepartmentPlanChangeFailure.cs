namespace BigRetail.Departments
{
    public enum DepartmentPlanChangeFailure
    {
        None = 0,
        EmptyArea,
        UnknownDefinition,
        PlanAlreadyExists,
        PlanNotFound,
        OutsideMap,
        OutsideConstructionArea,
        MissingFoundation,
        OverlapsAnotherDepartment
    }
}
