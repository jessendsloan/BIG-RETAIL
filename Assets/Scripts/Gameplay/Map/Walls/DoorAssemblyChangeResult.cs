using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes evaluation or mutation of one complete door assembly.
    /// Rejected operations never partially occupy or release wall edges.
    /// </summary>
    public readonly struct DoorAssemblyChangeResult
    {
        public bool Succeeded { get; }

        public bool Changed { get; }

        public DoorAssembly Assembly { get; }

        public DoorAssemblyId AssemblyId { get; }

        public DoorDefinitionId DefinitionId { get; }

        public DoorAssemblyChangeFailure Failure { get; }

        public CellEdge FailedEdge { get; }

        public int SegmentCount { get; }


        private DoorAssemblyChangeResult(
            bool succeeded,
            bool changed,
            DoorAssembly assembly,
            DoorAssemblyId assemblyId,
            DoorDefinitionId definitionId,
            DoorAssemblyChangeFailure failure,
            CellEdge failedEdge,
            int segmentCount)
        {
            Succeeded = succeeded;
            Changed = changed;
            Assembly = assembly;
            AssemblyId = assemblyId;
            DefinitionId = definitionId;
            Failure = failure;
            FailedEdge = failedEdge;
            SegmentCount = segmentCount;
        }


        public static DoorAssemblyChangeResult Approved(
            DoorAssemblyId assemblyId,
            DoorDefinitionId definitionId,
            int segmentCount)
        {
            return new DoorAssemblyChangeResult(
                true,
                false,
                null,
                assemblyId,
                definitionId,
                DoorAssemblyChangeFailure.None,
                default,
                segmentCount);
        }

        public static DoorAssemblyChangeResult Approved(
            DoorAssembly assembly)
        {
            return CreateSuccess(
                assembly,
                false);
        }

        public static DoorAssemblyChangeResult Success(
            DoorAssembly assembly)
        {
            return CreateSuccess(
                assembly,
                true);
        }

        public static DoorAssemblyChangeResult Rejected(
            DoorAssemblyId assemblyId,
            DoorDefinitionId definitionId,
            DoorAssemblyChangeFailure failure,
            CellEdge failedEdge = default)
        {
            return new DoorAssemblyChangeResult(
                false,
                false,
                null,
                assemblyId,
                definitionId,
                failure,
                failedEdge,
                0);
        }


        private static DoorAssemblyChangeResult CreateSuccess(
            DoorAssembly assembly,
            bool changed)
        {
            return new DoorAssemblyChangeResult(
                true,
                changed,
                assembly,
                assembly.Id,
                assembly.DefinitionId,
                DoorAssemblyChangeFailure.None,
                default,
                assembly.SegmentCount);
        }
    }
}
