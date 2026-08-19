using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Owns placed fixture instances and their complete cell occupancy.
    /// Placement legality belongs to FixturePlacementService.
    /// </summary>
    public sealed class FixtureState
    {
        private readonly Dictionary<
            FixtureInstanceId,
            FixtureInstance> fixtures =
                new Dictionary<
                    FixtureInstanceId,
                    FixtureInstance>();

        private readonly Dictionary<
            GridPosition,
            FixtureInstanceId> cellAssignments =
                new Dictionary<
                    GridPosition,
                    FixtureInstanceId>();

        private readonly Dictionary<GridPosition, int>
            accessCellReservations =
                new Dictionary<GridPosition, int>();

        private readonly Dictionary<CellEdge, int>
            accessBoundaryReservations =
                new Dictionary<CellEdge, int>();

        private bool isPublishingChanges;


        public int FixtureCount =>
            fixtures.Count;

        public int OccupiedCellCount =>
            cellAssignments.Count;

        public int ReservedAccessCellCount =>
            accessCellReservations.Count;

        public int ReservedAccessBoundaryCount =>
            accessBoundaryReservations.Count;


        public event Action<FixtureInstance> FixtureAdded;

        public event Action<FixtureInstance> FixtureRemoved;


        public bool TryGetFixture(
            FixtureInstanceId instanceId,
            out FixtureInstance fixture)
        {
            return fixtures.TryGetValue(
                instanceId,
                out fixture);
        }

        public bool TryGetFixtureAtCell(
            GridPosition cell,
            out FixtureInstance fixture)
        {
            if (!cellAssignments.TryGetValue(
                    cell,
                    out FixtureInstanceId instanceId))
            {
                fixture = null;
                return false;
            }

            return fixtures.TryGetValue(
                instanceId,
                out fixture);
        }

        public bool IsOccupied(
            GridPosition cell)
        {
            return cellAssignments.ContainsKey(cell);
        }

        public bool IsAccessCellReserved(
            GridPosition cell)
        {
            return accessCellReservations.ContainsKey(cell);
        }

        public bool IsAccessBoundaryReserved(
            CellEdge edge)
        {
            return accessBoundaryReservations.ContainsKey(edge);
        }

        public IEnumerable<FixtureInstance> EnumerateFixtures()
        {
            foreach (FixtureInstance fixture in fixtures.Values)
            {
                yield return fixture;
            }
        }


        internal bool TryAddFixture(
            FixtureInstance fixture)
        {
            if (isPublishingChanges
                || fixture == null
                || fixtures.ContainsKey(fixture.Id))
            {
                return false;
            }

            for (int index = 0;
                 index < fixture.OccupiedCellCount;
                 index++)
            {
                if (cellAssignments.ContainsKey(
                        fixture.GetOccupiedCell(index)))
                {
                    return false;
                }
            }

            fixtures.Add(
                fixture.Id,
                fixture);

            for (int index = 0;
                 index < fixture.OccupiedCellCount;
                 index++)
            {
                cellAssignments.Add(
                    fixture.GetOccupiedCell(index),
                    fixture.Id);
            }

            ReserveFixtureAccess(fixture);

            PublishFixtureAdded(fixture);
            return true;
        }

        internal bool TryRemoveFixture(
            FixtureInstanceId instanceId,
            out FixtureInstance removedFixture)
        {
            if (isPublishingChanges
                || !fixtures.TryGetValue(
                    instanceId,
                    out removedFixture))
            {
                removedFixture = null;
                return false;
            }

            fixtures.Remove(instanceId);

            for (int index = 0;
                 index < removedFixture.OccupiedCellCount;
                 index++)
            {
                cellAssignments.Remove(
                    removedFixture.GetOccupiedCell(index));
            }

            ReleaseFixtureAccess(removedFixture);

            PublishFixtureRemoved(removedFixture);
            return true;
        }


        private void ReserveFixtureAccess(
            FixtureInstance fixture)
        {
            IReadOnlyList<FixtureAccessPoint> accessPoints =
                fixture.ReservedAccessPoints;

            for (int index = 0;
                 index < accessPoints.Count;
                 index++)
            {
                FixtureAccessPoint accessPoint =
                    accessPoints[index];

                AddReservation(
                    accessCellReservations,
                    accessPoint.Cell);

                AddReservation(
                    accessBoundaryReservations,
                    accessPoint.BoundaryEdge);
            }
        }


        private void ReleaseFixtureAccess(
            FixtureInstance fixture)
        {
            IReadOnlyList<FixtureAccessPoint> accessPoints =
                fixture.ReservedAccessPoints;

            for (int index = 0;
                 index < accessPoints.Count;
                 index++)
            {
                FixtureAccessPoint accessPoint =
                    accessPoints[index];

                RemoveReservation(
                    accessCellReservations,
                    accessPoint.Cell);

                RemoveReservation(
                    accessBoundaryReservations,
                    accessPoint.BoundaryEdge);
            }
        }


        private static void AddReservation<TKey>(
            IDictionary<TKey, int> reservations,
            TKey key)
        {
            reservations.TryGetValue(
                key,
                out int reservationCount);

            reservations[key] = reservationCount + 1;
        }


        private static void RemoveReservation<TKey>(
            IDictionary<TKey, int> reservations,
            TKey key)
        {
            if (!reservations.TryGetValue(
                    key,
                    out int reservationCount))
            {
                return;
            }

            if (reservationCount <= 1)
            {
                reservations.Remove(key);
                return;
            }

            reservations[key] = reservationCount - 1;
        }


        private void PublishFixtureAdded(
            FixtureInstance fixture)
        {
            isPublishingChanges = true;

            try
            {
                FixtureAdded?.Invoke(fixture);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }

        private void PublishFixtureRemoved(
            FixtureInstance fixture)
        {
            isPublishingChanges = true;

            try
            {
                FixtureRemoved?.Invoke(fixture);
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}
