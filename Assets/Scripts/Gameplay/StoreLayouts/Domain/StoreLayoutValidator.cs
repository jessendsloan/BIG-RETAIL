using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Performs complete preflight validation without mutating the layout or
    /// any runtime store state.
    /// </summary>
    public sealed class StoreLayoutValidator
    {
        public StoreDataValidationResult Validate(
            StoreLayoutData layout,
            StoreLocationValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            StoreDataValidationResult result =
                new StoreDataValidationResult();

            if (layout == null)
            {
                result.Add(
                    StoreDataValidationCode.MissingData,
                    "layout",
                    "No store layout was supplied.");

                return result;
            }

            ValidateHeader(layout, context, result);
            ValidateLandRegions(layout, context, result);

            HashSet<StoreCellData> foundationCells =
                ValidateCellList(
                    layout.Foundations,
                    "foundations",
                    context,
                    result);

            HashSet<StoreCellData> sidewalkCells =
                ValidateCellList(
                    layout.Sidewalks,
                    "sidewalks",
                    context,
                    result);

            ValidateSurfaceOverlap(
                foundationCells,
                sidewalkCells,
                result);

            HashSet<StoreCellData> floorCells =
                ValidateFloors(
                    layout.Floors,
                    foundationCells,
                    context,
                    result);

            HashSet<StoreEdgeData> wallEdges =
                ValidateWalls(
                    layout.Walls,
                    foundationCells,
                    context,
                    result);

            ValidateOpenings(
                layout.Openings,
                wallEdges,
                context,
                result);

            HashSet<StoreCellData> fixtureCells =
                ValidateFixtures(
                    layout.Fixtures,
                    foundationCells,
                    context,
                    result,
                    out HashSet<string> fixtureInstanceIds);

            ValidateFixturePlans(
                layout.FixturePlans,
                foundationCells,
                fixtureCells,
                fixtureInstanceIds,
                context,
                result);

            ValidateDepartments(
                layout.Departments,
                floorCells,
                context,
                result);

            ValidateReceivingCells(
                layout.ReceivingCells,
                floorCells,
                fixtureCells,
                context,
                result);

            return result;
        }


        private static void ValidateSurfaceOverlap(
            ISet<StoreCellData> foundationCells,
            IEnumerable<StoreCellData> sidewalkCells,
            StoreDataValidationResult result)
        {
            foreach (StoreCellData cell in sidewalkCells)
            {
                if (!foundationCells.Contains(cell))
                {
                    continue;
                }

                result.Add(
                    StoreDataValidationCode.OccupiedCellOverlap,
                    "sidewalks",
                    $"Cell {cell} cannot contain both a foundation "
                    + "and a sidewalk.");
            }
        }


        private static void ValidateHeader(
            StoreLayoutData layout,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (layout.SchemaVersion
                != StoreLayoutSchema.CurrentLayoutVersion)
            {
                result.Add(
                    StoreDataValidationCode
                        .UnsupportedSchemaVersion,
                    "schemaVersion",
                    $"Layout schema {layout.SchemaVersion} is not "
                    + $"supported; expected "
                    + $"{StoreLayoutSchema.CurrentLayoutVersion}.");
            }

            ValidateRequiredId(
                layout.LayoutId,
                "layoutId",
                "Layout",
                result);

            ValidateRequiredId(
                layout.MapId,
                "mapId",
                "Map",
                result);

            if (!string.IsNullOrWhiteSpace(layout.MapId)
                && !StoreDataIdentity.Equals(
                    layout.MapId,
                    context.MapId))
            {
                result.Add(
                    StoreDataValidationCode.MapMismatch,
                    "mapId",
                    $"Layout map '{layout.MapId}' does not match "
                    + $"location '{context.MapId}'.");
            }

            if (string.IsNullOrWhiteSpace(
                    layout.MapFingerprint))
            {
                result.Add(
                    StoreDataValidationCode.MissingIdentifier,
                    "mapFingerprint",
                    "The layout requires a map fingerprint.");
            }
            else if (!string.Equals(
                         layout.MapFingerprint.Trim(),
                         context.MapFingerprint,
                         StringComparison.Ordinal))
            {
                result.Add(
                    StoreDataValidationCode
                        .MapFingerprintMismatch,
                    "mapFingerprint",
                    "The layout was authored for a different map "
                    + "geometry fingerprint.");
            }
        }


        private static void ValidateLandRegions(
            StoreLayoutData layout,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (layout.OwnedLandRegionIds == null)
            {
                AddMissingList(
                    "ownedLandRegionIds",
                    result);
                return;
            }

            HashSet<string> seen =
                new HashSet<string>(
                    StringComparer.Ordinal);

            for (int index = 0;
                 index < layout.OwnedLandRegionIds.Count;
                 index++)
            {
                string path =
                    $"ownedLandRegionIds[{index}]";

                if (!TryAddUniqueId(
                        seen,
                        layout.OwnedLandRegionIds[index],
                        path,
                        "Land region",
                        StoreDataValidationCode.DuplicateRecord,
                        result,
                        out string normalizedId))
                {
                    continue;
                }

                if (!context.ContainsLandRegion(normalizedId))
                {
                    result.Add(
                        StoreDataValidationCode.UnknownLandRegion,
                        path,
                        $"Land region '{normalizedId}' is not "
                        + "defined by this location.");
                }
            }
        }


        private static HashSet<StoreCellData> ValidateCellList(
            IReadOnlyList<StoreCellData> cells,
            string path,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            HashSet<StoreCellData> seen =
                new HashSet<StoreCellData>();

            if (cells == null)
            {
                AddMissingList(path, result);
                return seen;
            }

            for (int index = 0;
                 index < cells.Count;
                 index++)
            {
                StoreCellData cell = cells[index];
                string itemPath = $"{path}[{index}]";

                if (!seen.Add(cell))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        itemPath,
                        $"Cell {cell} is duplicated.");
                }

                if (!context.ContainsCell(cell))
                {
                    result.Add(
                        StoreDataValidationCode.OutsideMap,
                        itemPath,
                        $"Cell {cell} is outside this location.");
                }
            }

            return seen;
        }


        private static HashSet<StoreCellData> ValidateFloors(
            IReadOnlyList<StoreFloorData> floors,
            ISet<StoreCellData> foundationCells,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            HashSet<StoreCellData> seen =
                new HashSet<StoreCellData>();

            if (floors == null)
            {
                AddMissingList("floors", result);
                return seen;
            }

            for (int index = 0;
                 index < floors.Count;
                 index++)
            {
                string path = $"floors[{index}]";
                StoreFloorData floor = floors[index];

                if (floor == null)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        path,
                        "The floor record is null.");
                    continue;
                }

                if (!seen.Add(floor.Cell))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        path,
                        $"Floor cell {floor.Cell} is duplicated.");
                }

                ValidateSupportedCell(
                    floor.Cell,
                    path,
                    context,
                    result);

                if (!foundationCells.Contains(floor.Cell))
                {
                    result.Add(
                        StoreDataValidationCode.MissingFoundation,
                        path,
                        $"Floor cell {floor.Cell} has no foundation.");
                }

                ValidateDefinition(
                    StoreDefinitionKind.FloorFinish,
                    floor.FinishId,
                    $"{path}.finishId",
                    context,
                    result);
            }

            return seen;
        }


        private static HashSet<StoreEdgeData> ValidateWalls(
            IReadOnlyList<StoreWallData> walls,
            ISet<StoreCellData> foundationCells,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            HashSet<StoreEdgeData> seen =
                new HashSet<StoreEdgeData>();

            if (walls == null)
            {
                AddMissingList("walls", result);
                return seen;
            }

            for (int index = 0;
                 index < walls.Count;
                 index++)
            {
                string path = $"walls[{index}]";
                StoreWallData wall = walls[index];

                if (wall == null)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        path,
                        "The wall record is null.");
                    continue;
                }

                if (!wall.Edge.HasSupportedDirection())
                {
                    result.Add(
                        StoreDataValidationCode.UnsupportedValue,
                        $"{path}.edge.direction",
                        "Wall edges must use a canonical direction.");
                    continue;
                }

                if (!seen.Add(wall.Edge))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        path,
                        $"Wall edge {wall.Edge} is duplicated.");
                }

                if (!context.ContainsEdge(wall.Edge))
                {
                    result.Add(
                        StoreDataValidationCode.OutsideMap,
                        path,
                        $"Wall edge {wall.Edge} does not touch this map.");
                }

                if (!foundationCells.Contains(wall.Edge.FirstCell)
                    && !foundationCells.Contains(wall.Edge.SecondCell))
                {
                    result.Add(
                        StoreDataValidationCode.MissingFoundation,
                        path,
                        $"Wall edge {wall.Edge} has no foundation support.");
                }

                ValidateDefinition(
                    StoreDefinitionKind.WallFinish,
                    wall.FirstCellFinishId,
                    $"{path}.firstCellFinishId",
                    context,
                    result);

                ValidateDefinition(
                    StoreDefinitionKind.WallFinish,
                    wall.SecondCellFinishId,
                    $"{path}.secondCellFinishId",
                    context,
                    result);
            }

            return seen;
        }


        private static void ValidateOpenings(
            IReadOnlyList<StoreOpeningData> openings,
            ISet<StoreEdgeData> wallEdges,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (openings == null)
            {
                AddMissingList("openings", result);
                return;
            }

            HashSet<string> instanceIds =
                new HashSet<string>(StringComparer.Ordinal);

            HashSet<StoreEdgeData> occupiedEdges =
                new HashSet<StoreEdgeData>();

            for (int index = 0;
                 index < openings.Count;
                 index++)
            {
                string path = $"openings[{index}]";
                StoreOpeningData opening = openings[index];

                if (opening == null)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        path,
                        "The opening record is null.");
                    continue;
                }

                TryAddUniqueId(
                    instanceIds,
                    opening.InstanceId,
                    $"{path}.instanceId",
                    "Opening instance",
                    StoreDataValidationCode.DuplicateInstanceId,
                    result,
                    out _);

                ValidateDefinition(
                    StoreDefinitionKind.Opening,
                    opening.DefinitionId,
                    $"{path}.definitionId",
                    context,
                    result);

                if (opening.Edges == null
                    || opening.Edges.Count == 0)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        $"{path}.edges",
                        "An opening requires at least one wall edge.");
                    continue;
                }

                HashSet<StoreEdgeData> localEdges =
                    new HashSet<StoreEdgeData>();

                for (int edgeIndex = 0;
                     edgeIndex < opening.Edges.Count;
                     edgeIndex++)
                {
                    StoreEdgeData edge = opening.Edges[edgeIndex];
                    string edgePath =
                        $"{path}.edges[{edgeIndex}]";

                    if (!edge.HasSupportedDirection())
                    {
                        result.Add(
                            StoreDataValidationCode.UnsupportedValue,
                            edgePath,
                            "Opening edges must use a canonical direction.");
                        continue;
                    }

                    if (!localEdges.Add(edge))
                    {
                        result.Add(
                            StoreDataValidationCode.DuplicateRecord,
                            edgePath,
                            $"Opening edge {edge} is duplicated.");
                    }

                    if (!wallEdges.Contains(edge))
                    {
                        result.Add(
                            StoreDataValidationCode.MissingReference,
                            edgePath,
                            $"Opening edge {edge} has no authored wall.");
                    }

                    if (!occupiedEdges.Add(edge))
                    {
                        result.Add(
                            StoreDataValidationCode.OccupiedCellOverlap,
                            edgePath,
                            $"Wall edge {edge} is used by another opening.");
                    }
                }
            }
        }


        private static HashSet<StoreCellData> ValidateFixtures(
            IReadOnlyList<StoreFixtureData> fixtures,
            ISet<StoreCellData> foundationCells,
            StoreLocationValidationContext context,
            StoreDataValidationResult result,
            out HashSet<string> instanceIds)
        {
            HashSet<StoreCellData> occupiedCells =
                new HashSet<StoreCellData>();

            instanceIds =
                new HashSet<string>(StringComparer.Ordinal);

            if (fixtures == null)
            {
                AddMissingList("fixtures", result);
                return occupiedCells;
            }

            for (int index = 0;
                 index < fixtures.Count;
                 index++)
            {
                string path = $"fixtures[{index}]";
                StoreFixtureData fixture = fixtures[index];

                if (fixture == null)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        path,
                        "The fixture record is null.");
                    continue;
                }

                TryAddUniqueId(
                    instanceIds,
                    fixture.InstanceId,
                    $"{path}.instanceId",
                    "Fixture instance",
                    StoreDataValidationCode.DuplicateInstanceId,
                    result,
                    out _);

                ValidateDefinition(
                    StoreDefinitionKind.Fixture,
                    fixture.DefinitionId,
                    $"{path}.definitionId",
                    context,
                    result);

                if (fixture.Orientation < StoreOrientation.North
                    || fixture.Orientation > StoreOrientation.West)
                {
                    result.Add(
                        StoreDataValidationCode.UnsupportedValue,
                        $"{path}.orientation",
                        $"Fixture orientation '{fixture.Orientation}' "
                        + "is unsupported.");
                }

                ValidateSupportedCell(
                    fixture.AnchorCell,
                    $"{path}.anchorCell",
                    context,
                    result);

                if (fixture.OccupiedCells == null
                    || fixture.OccupiedCells.Count == 0)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        $"{path}.occupiedCells",
                        "A fixture requires occupied cells.");
                    continue;
                }

                HashSet<StoreCellData> localCells =
                    new HashSet<StoreCellData>();

                for (int cellIndex = 0;
                     cellIndex < fixture.OccupiedCells.Count;
                     cellIndex++)
                {
                    StoreCellData cell =
                        fixture.OccupiedCells[cellIndex];

                    string cellPath =
                        $"{path}.occupiedCells[{cellIndex}]";

                    if (!localCells.Add(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.DuplicateRecord,
                            cellPath,
                            $"Fixture cell {cell} is duplicated.");
                    }

                    ValidateSupportedCell(
                        cell,
                        cellPath,
                        context,
                        result);

                    if (!foundationCells.Contains(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.MissingFoundation,
                            cellPath,
                            $"Fixture cell {cell} has no foundation support.");
                    }

                    if (!occupiedCells.Add(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.OccupiedCellOverlap,
                            cellPath,
                            $"Fixture cell {cell} is already occupied.");
                    }
                }

                if (!localCells.Contains(fixture.AnchorCell))
                {
                    result.Add(
                        StoreDataValidationCode.MissingReference,
                        $"{path}.anchorCell",
                        "The fixture anchor is not one of its occupied cells.");
                }
            }

            return occupiedCells;
        }


        private static void ValidateFixturePlans(
            IReadOnlyList<StoreFixturePlanData> fixturePlans,
            ISet<StoreCellData> foundationCells,
            IEnumerable<StoreCellData> installedFixtureCells,
            HashSet<string> fixtureInstanceIds,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            // Missing means an older schema-v1 layout with no saved plans.
            if (fixturePlans == null)
            {
                return;
            }

            HashSet<StoreCellData> occupiedCells =
                new HashSet<StoreCellData>(installedFixtureCells);

            for (int index = 0;
                 index < fixturePlans.Count;
                 index++)
            {
                string path = $"fixturePlans[{index}]";
                StoreFixturePlanData fixturePlan = fixturePlans[index];

                if (fixturePlan == null)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        path,
                        "The fixture plan record is null.");
                    continue;
                }

                TryAddUniqueId(
                    fixtureInstanceIds,
                    fixturePlan.InstanceId,
                    $"{path}.instanceId",
                    "Fixture plan instance",
                    StoreDataValidationCode.DuplicateInstanceId,
                    result,
                    out _);

                ValidateDefinition(
                    StoreDefinitionKind.Fixture,
                    fixturePlan.DefinitionId,
                    $"{path}.definitionId",
                    context,
                    result);

                if (fixturePlan.Orientation < StoreOrientation.North
                    || fixturePlan.Orientation > StoreOrientation.West)
                {
                    result.Add(
                        StoreDataValidationCode.UnsupportedValue,
                        $"{path}.orientation",
                        $"Fixture plan orientation "
                        + $"'{fixturePlan.Orientation}' is unsupported.");
                }

                ValidateSupportedCell(
                    fixturePlan.AnchorCell,
                    $"{path}.anchorCell",
                    context,
                    result);

                if (fixturePlan.OccupiedCells == null
                    || fixturePlan.OccupiedCells.Count == 0)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        $"{path}.occupiedCells",
                        "A fixture plan requires occupied cells.");
                    continue;
                }

                HashSet<StoreCellData> localCells =
                    new HashSet<StoreCellData>();

                for (int cellIndex = 0;
                     cellIndex < fixturePlan.OccupiedCells.Count;
                     cellIndex++)
                {
                    StoreCellData cell =
                        fixturePlan.OccupiedCells[cellIndex];
                    string cellPath =
                        $"{path}.occupiedCells[{cellIndex}]";

                    if (!localCells.Add(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.DuplicateRecord,
                            cellPath,
                            $"Fixture plan cell {cell} is duplicated.");
                    }

                    ValidateSupportedCell(
                        cell,
                        cellPath,
                        context,
                        result);

                    if (!foundationCells.Contains(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.MissingFoundation,
                            cellPath,
                            $"Fixture plan cell {cell} has no foundation "
                            + "support.");
                    }

                    if (!occupiedCells.Add(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.OccupiedCellOverlap,
                            cellPath,
                            $"Fixture plan cell {cell} is already occupied.");
                    }
                }

                if (!localCells.Contains(fixturePlan.AnchorCell))
                {
                    result.Add(
                        StoreDataValidationCode.MissingReference,
                        $"{path}.anchorCell",
                        "The fixture plan anchor is not one of its occupied "
                        + "cells.");
                }
            }
        }


        private static void ValidateDepartments(
            IReadOnlyList<StoreDepartmentData> departments,
            ISet<StoreCellData> floorCells,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (departments == null)
            {
                AddMissingList("departments", result);
                return;
            }

            HashSet<string> instanceIds =
                new HashSet<string>(StringComparer.Ordinal);

            HashSet<StoreCellData> assignedCells =
                new HashSet<StoreCellData>();

            for (int index = 0;
                 index < departments.Count;
                 index++)
            {
                string path = $"departments[{index}]";
                StoreDepartmentData department = departments[index];

                if (department == null)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        path,
                        "The department record is null.");
                    continue;
                }

                TryAddUniqueId(
                    instanceIds,
                    department.InstanceId,
                    $"{path}.instanceId",
                    "Department instance",
                    StoreDataValidationCode.DuplicateInstanceId,
                    result,
                    out _);

                ValidateDefinition(
                    StoreDefinitionKind.Department,
                    department.DefinitionId,
                    $"{path}.definitionId",
                    context,
                    result);

                if (department.Cells == null
                    || department.Cells.Count == 0)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        $"{path}.cells",
                        "A department requires at least one cell.");
                    continue;
                }

                HashSet<StoreCellData> localCells =
                    new HashSet<StoreCellData>();

                for (int cellIndex = 0;
                     cellIndex < department.Cells.Count;
                     cellIndex++)
                {
                    StoreCellData cell = department.Cells[cellIndex];
                    string cellPath =
                        $"{path}.cells[{cellIndex}]";

                    if (!localCells.Add(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.DuplicateRecord,
                            cellPath,
                            $"Department cell {cell} is duplicated.");
                    }

                    ValidateSupportedCell(
                        cell,
                        cellPath,
                        context,
                        result);

                    if (!floorCells.Contains(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.MissingFloor,
                            cellPath,
                            $"Department cell {cell} has no finished floor.");
                    }

                    if (!assignedCells.Add(cell))
                    {
                        result.Add(
                            StoreDataValidationCode.OccupiedCellOverlap,
                            cellPath,
                            $"Department cell {cell} is already assigned.");
                    }
                }
            }
        }


        private static void ValidateReceivingCells(
            IReadOnlyList<StoreCellData> receivingCells,
            ISet<StoreCellData> floorCells,
            ISet<StoreCellData> fixtureCells,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (receivingCells == null)
            {
                AddMissingList("receivingCells", result);
                return;
            }

            HashSet<StoreCellData> seen =
                new HashSet<StoreCellData>();

            for (int index = 0;
                 index < receivingCells.Count;
                 index++)
            {
                StoreCellData cell = receivingCells[index];
                string path = $"receivingCells[{index}]";

                if (!seen.Add(cell))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        path,
                        $"Receiving cell {cell} is duplicated.");
                }

                ValidateSupportedCell(
                    cell,
                    path,
                    context,
                    result);

                if (!floorCells.Contains(cell))
                {
                    result.Add(
                        StoreDataValidationCode.MissingFloor,
                        path,
                        $"Receiving cell {cell} has no finished floor.");
                }

                if (fixtureCells.Contains(cell))
                {
                    result.Add(
                        StoreDataValidationCode.OccupiedCellOverlap,
                        path,
                        $"Receiving cell {cell} is obstructed by a fixture.");
                }
            }
        }


        private static void ValidateSupportedCell(
            StoreCellData cell,
            string path,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (!context.ContainsCell(cell))
            {
                result.Add(
                    StoreDataValidationCode.OutsideMap,
                    path,
                    $"Cell {cell} is outside this location.");
            }
        }


        private static void ValidateDefinition(
            StoreDefinitionKind kind,
            string definitionId,
            string path,
            StoreLocationValidationContext context,
            StoreDataValidationResult result)
        {
            if (!StoreDataIdentity.TryNormalize(
                    definitionId,
                    out string normalizedId))
            {
                result.Add(
                    StoreDataValidationCode.MissingIdentifier,
                    path,
                    $"A {kind} identifier is required.");
                return;
            }

            if (!context.Definitions.Contains(kind, normalizedId))
            {
                result.Add(
                    StoreDataValidationCode.UnknownDefinition,
                    path,
                    $"{kind} '{normalizedId}' is not in the active catalog.");
            }
        }


        private static bool TryAddUniqueId(
            ISet<string> seen,
            string value,
            string path,
            string label,
            StoreDataValidationCode duplicateCode,
            StoreDataValidationResult result,
            out string normalizedId)
        {
            if (!StoreDataIdentity.TryNormalize(
                    value,
                    out normalizedId))
            {
                result.Add(
                    StoreDataValidationCode.MissingIdentifier,
                    path,
                    $"{label} requires an identifier.");
                return false;
            }

            if (!seen.Add(normalizedId))
            {
                result.Add(
                    duplicateCode,
                    path,
                    $"{label} '{normalizedId}' is duplicated.");
                return false;
            }

            return true;
        }


        private static void ValidateRequiredId(
            string value,
            string path,
            string label,
            StoreDataValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result.Add(
                    StoreDataValidationCode.MissingIdentifier,
                    path,
                    $"{label} requires an identifier.");
            }
        }


        private static void AddMissingList(
            string path,
            StoreDataValidationResult result)
        {
            result.Add(
                StoreDataValidationCode.MissingData,
                path,
                "The serialized collection is missing.");
        }
    }
}
