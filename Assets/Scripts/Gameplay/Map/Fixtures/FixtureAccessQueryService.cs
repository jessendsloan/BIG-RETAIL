using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Resolves currently usable stand cells beside placed fixtures. The
    /// service filters static fixture occupancy and delegates the remaining
    /// physical-surface rules through a narrow query boundary.
    /// </summary>
    public sealed class FixtureAccessQueryService
    {
        private readonly FixtureState fixtureState;
        private readonly IFixtureAccessSurfaceQuery surfaceQuery;


        public FixtureAccessQueryService(
            FixtureState fixtureState,
            IFixtureAccessSurfaceQuery surfaceQuery)
        {
            this.fixtureState =
                fixtureState
                ?? throw new ArgumentNullException(
                    nameof(fixtureState));

            this.surfaceQuery =
                surfaceQuery
                ?? throw new ArgumentNullException(
                    nameof(surfaceQuery));
        }


        public IReadOnlyList<FixtureAccessPoint>
            GetAvailableAccessPoints(
                FixtureInstanceId instanceId,
                FixtureAccessMode requiredMode)
        {
            ValidateRequiredMode(requiredMode);

            if (!instanceId.IsValid
                || !fixtureState.TryGetFixture(
                    instanceId,
                    out FixtureInstance fixture))
            {
                return Array.Empty<FixtureAccessPoint>();
            }

            IReadOnlyList<FixtureAccessPoint> authoredPoints =
                FixtureAccessPointResolver.Resolve(fixture);

            List<FixtureAccessPoint> availablePoints =
                new List<FixtureAccessPoint>(
                    authoredPoints.Count);

            for (int index = 0;
                 index < authoredPoints.Count;
                 index++)
            {
                FixtureAccessPoint point =
                    authoredPoints[index];

                if (IsAvailable(point, requiredMode))
                {
                    availablePoints.Add(point);
                }
            }

            return availablePoints.ToArray();
        }


        public bool TryFindNearestAvailableAccessPoint(
            FixtureInstanceId instanceId,
            FixtureAccessMode requiredMode,
            GridPosition origin,
            out FixtureAccessPoint accessPoint)
        {
            ValidateRequiredMode(requiredMode);

            accessPoint = default;

            if (!instanceId.IsValid
                || !fixtureState.TryGetFixture(
                    instanceId,
                    out FixtureInstance fixture))
            {
                return false;
            }

            IReadOnlyList<FixtureAccessPoint> authoredPoints =
                FixtureAccessPointResolver.Resolve(fixture);

            bool found = false;
            long nearestDistance = long.MaxValue;

            for (int index = 0;
                 index < authoredPoints.Count;
                 index++)
            {
                FixtureAccessPoint candidate =
                    authoredPoints[index];

                if (!IsAvailable(candidate, requiredMode))
                {
                    continue;
                }

                long distance =
                    CalculateGridDistance(
                        origin,
                        candidate.Cell);

                if (found && distance >= nearestDistance)
                {
                    continue;
                }

                found = true;
                nearestDistance = distance;
                accessPoint = candidate;
            }

            return found;
        }


        private bool IsAvailable(
            FixtureAccessPoint point,
            FixtureAccessMode requiredMode)
        {
            return point.Mode.Includes(requiredMode)
                && !fixtureState.IsOccupied(point.Cell)
                && surfaceQuery.CanUseAccessPoint(point);
        }


        private static long CalculateGridDistance(
            GridPosition first,
            GridPosition second)
        {
            return Math.Abs((long)first.X - second.X)
                + Math.Abs((long)first.Y - second.Y)
                + Math.Abs((long)first.Level - second.Level);
        }


        private static void ValidateRequiredMode(
            FixtureAccessMode requiredMode)
        {
            if (requiredMode == FixtureAccessMode.None
                || !requiredMode.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredMode),
                    requiredMode,
                    "A fixture access query requires a supported "
                    + "interaction mode.");
            }
        }
    }
}
