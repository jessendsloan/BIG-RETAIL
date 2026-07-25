using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Describes the result of planning one straight wall run.
    ///
    /// This value contains planned geometry only.
    /// It does not validate construction eligibility or modify WallState.
    /// </summary>
    public readonly struct WallRunPlanResult
    {
        private readonly CellEdge[] edges;

        public bool Succeeded { get; }

        public WallRunPlanFailure Failure { get; }

        public CellEdge StartEdge { get; }

        public CellEdge EndEdge { get; }

        public int SegmentCount =>
            edges?.Length ?? 0;

        public IReadOnlyList<CellEdge> Edges =>
            edges ?? Array.Empty<CellEdge>();


        private WallRunPlanResult(
            bool succeeded,
            CellEdge startEdge,
            CellEdge endEdge,
            WallRunPlanFailure failure,
            CellEdge[] edges)
        {
            Succeeded = succeeded;
            StartEdge = startEdge;
            EndEdge = endEdge;
            Failure = failure;

            this.edges =
                edges ?? Array.Empty<CellEdge>();
        }


        public static WallRunPlanResult Success(
            CellEdge startEdge,
            CellEdge endEdge,
            CellEdge[] edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(
                    nameof(edges));
            }

            if (edges.Length == 0)
            {
                throw new ArgumentException(
                    "A successful wall-run plan must contain " +
                    "at least one edge.",
                    nameof(edges));
            }

            return new WallRunPlanResult(
                true,
                startEdge,
                endEdge,
                WallRunPlanFailure.None,
                edges);
        }


        public static WallRunPlanResult Rejected(
            CellEdge startEdge,
            CellEdge endEdge,
            WallRunPlanFailure failure)
        {
            if (failure == WallRunPlanFailure.None)
            {
                throw new ArgumentException(
                    "A rejected wall-run plan requires a failure reason.",
                    nameof(failure));
            }

            return new WallRunPlanResult(
                false,
                startEdge,
                endEdge,
                failure,
                Array.Empty<CellEdge>());
        }


        public override string ToString()
        {
            if (Succeeded)
            {
                return
                    $"Straight wall run contains " +
                    $"{SegmentCount} segment(s): " +
                    $"{StartEdge} to {EndEdge}.";
            }

            return
                $"Straight wall run rejected: {Failure}. " +
                $"Start: {StartEdge}. End: {EndEdge}.";
        }
    }
}