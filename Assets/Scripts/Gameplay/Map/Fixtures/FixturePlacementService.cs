using System;
using System.Collections.Generic;
using BigRetail.Map.Construction;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Validates and atomically applies fixture placements on constructed
    /// floor cells.
    /// </summary>
    public sealed class FixturePlacementService
    {
        private readonly GridMapDefinition mapDefinition;
        private readonly IConstructionCellEligibility constructionArea;
        private readonly FixtureDefinitionCatalog definitionCatalog;
        private readonly FixtureState fixtureState;
        private readonly IFixturePlacementSurfaceQuery surfaceQuery;


        public FixturePlacementService(
            GridMapDefinition mapDefinition,
            IConstructionCellEligibility constructionArea,
            FixtureDefinitionCatalog definitionCatalog,
            FixtureState fixtureState,
            IFixturePlacementSurfaceQuery surfaceQuery)
        {
            this.mapDefinition =
                mapDefinition
                ?? throw new ArgumentNullException(
                    nameof(mapDefinition));

            this.constructionArea =
                constructionArea
                ?? throw new ArgumentNullException(
                    nameof(constructionArea));

            this.definitionCatalog =
                definitionCatalog
                ?? throw new ArgumentNullException(
                    nameof(definitionCatalog));

            this.fixtureState =
                fixtureState
                ?? throw new ArgumentNullException(
                    nameof(fixtureState));

            this.surfaceQuery =
                surfaceQuery
                ?? throw new ArgumentNullException(
                    nameof(surfaceQuery));
        }


        public FixturePlacementResult EvaluatePlacement(
            FixtureInstanceId instanceId,
            FixtureDefinitionId definitionId,
            GridPosition anchorCell,
            FixtureOrientation orientation)
        {
            if (!instanceId.IsValid)
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    FixturePlacementFailure.InvalidInstanceId);
            }

            if (!definitionCatalog.TryGetDefinition(
                    definitionId,
                    out FixtureDefinition definition))
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    FixturePlacementFailure.UnknownDefinition);
            }

            if (fixtureState.TryGetFixture(
                    instanceId,
                    out _))
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    FixturePlacementFailure.FixtureAlreadyExists);
            }

            if (!orientation.IsSupported())
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    FixturePlacementFailure.UnsupportedOrientation);
            }

            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    definition,
                    anchorCell,
                    orientation);

            for (int index = 0;
                 index < footprint.CellCount;
                 index++)
            {
                GridPosition cell =
                    footprint.GetCell(index);

                FixturePlacementFailure failure =
                    EvaluateCell(cell);

                if (failure == FixturePlacementFailure.None)
                {
                    continue;
                }

                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    failure,
                    cell,
                    footprint);
            }

            FixturePlacementFailure barrierFailure =
                EvaluateInternalWalls(
                    footprint,
                    out GridPosition blockedCell);

            if (barrierFailure != FixturePlacementFailure.None)
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    barrierFailure,
                    blockedCell,
                    footprint);
            }

            FixturePlacementFailure accessFailure =
                EvaluateAccessClearance(
                    definition,
                    footprint,
                    out blockedCell,
                    out _);

            if (accessFailure != FixturePlacementFailure.None)
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    accessFailure,
                    blockedCell,
                    footprint);
            }

            return FixturePlacementResult.Approved(
                instanceId,
                definitionId,
                footprint);
        }


        public FixturePlacementResult TryPlaceFixture(
            FixtureInstanceId instanceId,
            FixtureDefinitionId definitionId,
            GridPosition anchorCell,
            FixtureOrientation orientation)
        {
            FixturePlacementResult evaluation =
                EvaluatePlacement(
                    instanceId,
                    definitionId,
                    anchorCell,
                    orientation);

            if (!evaluation.Succeeded)
            {
                return evaluation;
            }

            if (!definitionCatalog.TryGetDefinition(
                    definitionId,
                    out FixtureDefinition definition))
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    FixturePlacementFailure.UnknownDefinition);
            }

            FixtureInstance fixture =
                new FixtureInstance(
                    instanceId,
                    definition,
                    evaluation.Footprint,
                    ResolveReservableAccessPoints(
                        definition,
                        evaluation.Footprint));

            if (!fixtureState.TryAddFixture(fixture))
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    definitionId,
                    FixturePlacementFailure.StateConflict,
                    footprint: evaluation.Footprint);
            }

            return FixturePlacementResult.Success(
                fixture,
                FixtureEdit.AddFixture(fixture));
        }


        public FixturePlacementResult EvaluateRemoval(
            FixtureInstanceId instanceId)
        {
            if (!instanceId.IsValid)
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    default,
                    FixturePlacementFailure.InvalidInstanceId);
            }

            if (!fixtureState.TryGetFixture(
                    instanceId,
                    out FixtureInstance fixture))
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    default,
                    FixturePlacementFailure.FixtureNotFound);
            }

            return FixturePlacementResult.Approved(fixture);
        }


        public FixturePlacementResult TryRemoveFixture(
            FixtureInstanceId instanceId)
        {
            FixturePlacementResult evaluation =
                EvaluateRemoval(instanceId);

            if (!evaluation.Succeeded)
            {
                return evaluation;
            }

            if (!fixtureState.TryRemoveFixture(
                    instanceId,
                    out FixtureInstance removedFixture))
            {
                return FixturePlacementResult.Rejected(
                    instanceId,
                    evaluation.DefinitionId,
                    FixturePlacementFailure.StateConflict,
                    footprint: evaluation.Footprint);
            }

            return FixturePlacementResult.Success(
                removedFixture,
                FixtureEdit.RemoveFixture(removedFixture));
        }


        /// <summary>
        /// Removes the complete fixture occupying <paramref name="cell"/>.
        /// Any occupied footprint cell resolves to the same fixture instance.
        /// </summary>
        public FixturePlacementResult TryRemoveFixtureAtCell(
            GridPosition cell)
        {
            if (!fixtureState.TryGetFixtureAtCell(
                    cell,
                    out FixtureInstance fixture))
            {
                return FixturePlacementResult.Rejected(
                    default,
                    default,
                    FixturePlacementFailure.FixtureNotFound,
                    cell);
            }

            return TryRemoveFixture(fixture.Id);
        }


        public FixturePlacementResult TryApplyEdit(
            FixtureEdit edit)
        {
            if (edit.IsEmpty)
            {
                return FixturePlacementResult.Rejected(
                    default,
                    default,
                    FixturePlacementFailure.EmptyEdit);
            }

            if (edit.Kind == FixtureEditKind.AddFixture)
            {
                return TryPlaceFixture(
                    edit.Fixture.Id,
                    edit.Fixture.DefinitionId,
                    edit.Fixture.AnchorCell,
                    edit.Fixture.Orientation);
            }

            if (!fixtureState.TryGetFixture(
                    edit.Fixture.Id,
                    out FixtureInstance currentFixture))
            {
                return FixturePlacementResult.Rejected(
                    edit.Fixture.Id,
                    edit.Fixture.DefinitionId,
                    FixturePlacementFailure.FixtureNotFound,
                    footprint: edit.Fixture.Footprint);
            }

            if (!currentFixture.HasSamePlacementAs(edit.Fixture))
            {
                return FixturePlacementResult.Rejected(
                    edit.Fixture.Id,
                    edit.Fixture.DefinitionId,
                    FixturePlacementFailure.StateConflict,
                    footprint: edit.Fixture.Footprint);
            }

            return TryRemoveFixture(edit.Fixture.Id);
        }


        private FixturePlacementFailure EvaluateCell(
            GridPosition cell)
        {
            if (!mapDefinition.ContainsCell(cell))
            {
                return FixturePlacementFailure.OutsideMap;
            }

            if (!constructionArea.IsEligible(cell))
            {
                return FixturePlacementFailure
                    .OutsideConstructionArea;
            }

            if (!surfaceQuery.HasFloor(cell))
            {
                return FixturePlacementFailure.MissingFloor;
            }

            if (surfaceQuery.IsReservedForDoorPassage(cell))
            {
                return FixturePlacementFailure.BlocksDoorPassage;
            }

            if (fixtureState.IsOccupied(cell))
            {
                return FixturePlacementFailure.OverlapsFixture;
            }

            if (fixtureState.IsAccessCellReserved(cell))
            {
                return FixturePlacementFailure.BlockedAccess;
            }

            return FixturePlacementFailure.None;
        }


        private FixturePlacementFailure EvaluateInternalWalls(
            FixtureFootprint footprint,
            out GridPosition blockedCell)
        {
            for (int index = 0;
                 index < footprint.CellCount;
                 index++)
            {
                GridPosition cell =
                    footprint.GetCell(index);

                GridPosition eastNeighbor =
                    cell.Offset(1, 0);

                if (footprint.ContainsCell(eastNeighbor)
                    && surfaceQuery.HasWall(
                        new CellEdge(
                            cell,
                            CellEdgeDirection.NorthEast)))
                {
                    blockedCell = eastNeighbor;
                    return FixturePlacementFailure.CrossesWall;
                }

                GridPosition northNeighbor =
                    cell.Offset(0, 1);

                if (footprint.ContainsCell(northNeighbor)
                    && surfaceQuery.HasWall(
                        new CellEdge(
                            cell,
                            CellEdgeDirection.NorthWest)))
                {
                    blockedCell = northNeighbor;
                    return FixturePlacementFailure.CrossesWall;
                }
            }

            blockedCell = default;
            return FixturePlacementFailure.None;
        }


        private FixturePlacementFailure EvaluateAccessClearance(
            FixtureDefinition definition,
            FixtureFootprint footprint,
            out GridPosition blockedCell,
            out IReadOnlyList<FixtureAccessPoint> reservableAccessPoints)
        {
            IReadOnlyList<FixtureAccessPoint> accessPoints =
                FixtureAccessPointResolver.Resolve(
                    definition,
                    footprint);

            if (definition.AccessProfile.ClearancePolicy
                == FixtureAccessClearancePolicy.AtLeastOneCompleteSide)
            {
                return EvaluateOneCompleteSideClearance(
                    accessPoints,
                    out blockedCell,
                    out reservableAccessPoints);
            }

            for (int index = 0;
                 index < accessPoints.Count;
                 index++)
            {
                FixtureAccessPoint accessPoint =
                    accessPoints[index];

                if (!IsClearAccessPoint(accessPoint))
                {
                    blockedCell = accessPoint.Cell;
                    reservableAccessPoints =
                        Array.Empty<FixtureAccessPoint>();
                    return FixturePlacementFailure.BlockedAccess;
                }
            }

            blockedCell = default;
            reservableAccessPoints = accessPoints;
            return FixturePlacementFailure.None;
        }


        private FixturePlacementFailure EvaluateOneCompleteSideClearance(
            IReadOnlyList<FixtureAccessPoint> accessPoints,
            out GridPosition blockedCell,
            out IReadOnlyList<FixtureAccessPoint> reservableAccessPoints)
        {
            List<FixtureAccessPoint> clearPoints =
                new List<FixtureAccessPoint>(accessPoints.Count);
            bool foundClearSide = false;
            bool foundBlockedPoint = false;
            GridPosition firstBlockedCell = default;

            FixtureSide[] sides =
            {
                FixtureSide.North,
                FixtureSide.East,
                FixtureSide.South,
                FixtureSide.West
            };

            for (int sideIndex = 0;
                 sideIndex < sides.Length;
                 sideIndex++)
            {
                FixtureSide side = sides[sideIndex];
                bool sideHasPoints = false;
                bool sideIsClear = true;

                for (int pointIndex = 0;
                     pointIndex < accessPoints.Count;
                     pointIndex++)
                {
                    FixtureAccessPoint accessPoint =
                        accessPoints[pointIndex];

                    if (accessPoint.Side != side)
                    {
                        continue;
                    }

                    sideHasPoints = true;

                    if (IsClearAccessPoint(accessPoint))
                    {
                        continue;
                    }

                    sideIsClear = false;

                    if (!foundBlockedPoint)
                    {
                        foundBlockedPoint = true;
                        firstBlockedCell = accessPoint.Cell;
                    }
                }

                if (!sideHasPoints || !sideIsClear)
                {
                    continue;
                }

                foundClearSide = true;

                for (int pointIndex = 0;
                     pointIndex < accessPoints.Count;
                     pointIndex++)
                {
                    if (accessPoints[pointIndex].Side == side)
                    {
                        clearPoints.Add(accessPoints[pointIndex]);
                    }
                }
            }

            if (!foundClearSide)
            {
                blockedCell = firstBlockedCell;
                reservableAccessPoints =
                    Array.Empty<FixtureAccessPoint>();
                return FixturePlacementFailure.BlockedAccess;
            }

            blockedCell = default;
            reservableAccessPoints = clearPoints.ToArray();
            return FixturePlacementFailure.None;
        }


        private IReadOnlyList<FixtureAccessPoint>
            ResolveReservableAccessPoints(
                FixtureDefinition definition,
                FixtureFootprint footprint)
        {
            FixturePlacementFailure failure =
                EvaluateAccessClearance(
                    definition,
                    footprint,
                    out _,
                    out IReadOnlyList<FixtureAccessPoint> accessPoints);

            if (failure != FixturePlacementFailure.None)
            {
                throw new InvalidOperationException(
                    "An approved fixture placement no longer has valid access.");
            }

            return accessPoints;
        }


        private bool IsClearAccessPoint(
            FixtureAccessPoint accessPoint)
        {
            GridPosition cell = accessPoint.Cell;

            return mapDefinition.ContainsCell(cell)
                && constructionArea.IsEligible(cell)
                && surfaceQuery.HasFloor(cell)
                && !fixtureState.IsOccupied(cell)
                && !fixtureState.IsAccessCellReserved(cell)
                && !surfaceQuery.IsReservedForDoorPassage(cell)
                && !surfaceQuery.HasWall(
                    accessPoint.BoundaryEdge);
        }
    }
}
