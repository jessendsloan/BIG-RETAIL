using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Describes evaluation or mutation of one complete fixture placement.
    /// Rejected operations never partially occupy or release cells.
    /// </summary>
    public readonly struct FixturePlacementResult
    {
        public bool Succeeded { get; }

        public bool Changed { get; }

        public FixtureInstance Fixture { get; }

        public FixtureInstanceId InstanceId { get; }

        public FixtureDefinitionId DefinitionId { get; }

        public FixtureFootprint Footprint { get; }

        public FixturePlacementFailure Failure { get; }

        public GridPosition FailedCell { get; }

        public FixtureEdit Edit { get; }

        public int OccupiedCellCount =>
            Footprint?.CellCount
            ?? Fixture?.OccupiedCellCount
            ?? 0;


        private FixturePlacementResult(
            bool succeeded,
            bool changed,
            FixtureInstance fixture,
            FixtureInstanceId instanceId,
            FixtureDefinitionId definitionId,
            FixtureFootprint footprint,
            FixturePlacementFailure failure,
            GridPosition failedCell,
            FixtureEdit edit)
        {
            Succeeded = succeeded;
            Changed = changed;
            Fixture = fixture;
            InstanceId = instanceId;
            DefinitionId = definitionId;
            Footprint = footprint;
            Failure = failure;
            FailedCell = failedCell;
            Edit = edit;
        }


        public static FixturePlacementResult Approved(
            FixtureInstanceId instanceId,
            FixtureDefinitionId definitionId,
            FixtureFootprint footprint)
        {
            return new FixturePlacementResult(
                true,
                false,
                null,
                instanceId,
                definitionId,
                footprint,
                FixturePlacementFailure.None,
                default,
                default);
        }

        public static FixturePlacementResult Approved(
            FixtureInstance fixture)
        {
            return CreateSuccess(
                fixture,
                false,
                default);
        }

        public static FixturePlacementResult Success(
            FixtureInstance fixture,
            FixtureEdit edit)
        {
            return CreateSuccess(
                fixture,
                true,
                edit);
        }

        public static FixturePlacementResult Rejected(
            FixtureInstanceId instanceId,
            FixtureDefinitionId definitionId,
            FixturePlacementFailure failure,
            GridPosition failedCell = default,
            FixtureFootprint footprint = null)
        {
            return new FixturePlacementResult(
                false,
                false,
                null,
                instanceId,
                definitionId,
                footprint,
                failure,
                failedCell,
                default);
        }


        private static FixturePlacementResult CreateSuccess(
            FixtureInstance fixture,
            bool changed,
            FixtureEdit edit)
        {
            return new FixturePlacementResult(
                true,
                changed,
                fixture,
                fixture.Id,
                fixture.DefinitionId,
                fixture.Footprint,
                FixturePlacementFailure.None,
                default,
                edit);
        }
    }
}
