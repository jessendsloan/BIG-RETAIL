using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Produces normalized, deterministically ordered copies after validation.
    /// Source templates are never mutated.
    /// </summary>
    public sealed class StoreDataCanonicalizer
    {
        public StoreLayoutData CreateCanonicalCopy(
            StoreLayoutData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            StoreLayoutData copy =
                new StoreLayoutData
                {
                    SchemaVersion = source.SchemaVersion,
                    LayoutId = NormalizeId(source.LayoutId),
                    DisplayName = Trim(source.DisplayName),
                    MapId = NormalizeId(source.MapId),
                    MapFingerprint = Trim(source.MapFingerprint),
                    LogicalOrigin = source.LogicalOrigin
                };

            CopyIds(
                source.OwnedLandRegionIds,
                copy.OwnedLandRegionIds);

            CopyCells(source.Foundations, copy.Foundations);
            CopyCells(source.Sidewalks, copy.Sidewalks);
            CopyCells(source.ReceivingCells, copy.ReceivingCells);

            CopyFloors(source.Floors, copy.Floors);
            CopyWalls(source.Walls, copy.Walls);
            CopyOpenings(source.Openings, copy.Openings);
            CopyFixtures(source.Fixtures, copy.Fixtures);
            CopyDepartments(source.Departments, copy.Departments);

            return copy;
        }


        public StoreScenarioData CreateCanonicalCopy(
            StoreScenarioData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            StoreScenarioData copy =
                new StoreScenarioData
                {
                    SchemaVersion = source.SchemaVersion,
                    ScenarioId = NormalizeId(source.ScenarioId),
                    DisplayName = Trim(source.DisplayName),
                    MapId = NormalizeId(source.MapId),
                    LayoutId = NormalizeId(source.LayoutId),
                    StartingGameSeconds = source.StartingGameSeconds,
                    StartingSimulationSpeed =
                        source.StartingSimulationSpeed,
                    StartingStoreCashCents =
                        source.StartingStoreCashCents,
                    DeterministicSeed = source.DeterministicSeed
                };

            CopyPlanograms(
                source.PlanogramAssignments,
                copy.PlanogramAssignments);

            CopyDisplayInventory(
                source.DisplayInventory,
                copy.DisplayInventory);

            CopyInventoryLines(
                source.BackstockInventory,
                copy.BackstockInventory);

            CopyCheckouts(source.Checkouts, copy.Checkouts);
            CopyDeliveries(source.Deliveries, copy.Deliveries);
            CopySpawns(source.Spawns, copy.Spawns);
            CopyStoryFlags(source.StoryFlags, copy.StoryFlags);

            return copy;
        }


        private static void CopyIds(
            IReadOnlyList<string> source,
            ICollection<string> destination)
        {
            RequireList(source, nameof(source));

            List<string> normalized =
                new List<string>(source.Count);

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                normalized.Add(NormalizeId(source[index]));
            }

            normalized.Sort(StringComparer.Ordinal);

            for (int index = 0;
                 index < normalized.Count;
                 index++)
            {
                destination.Add(normalized[index]);
            }
        }


        private static void CopyCells(
            IReadOnlyList<StoreCellData> source,
            List<StoreCellData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                destination.Add(source[index]);
            }

            destination.Sort();
        }


        private static void CopyFloors(
            IReadOnlyList<StoreFloorData> source,
            List<StoreFloorData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreFloorData item =
                    RequireItem(source[index], "floor");

                destination.Add(
                    new StoreFloorData(
                        item.Cell,
                        NormalizeId(item.FinishId)));
            }

            destination.Sort(
                (left, right) =>
                    left.Cell.CompareTo(right.Cell));
        }


        private static void CopyWalls(
            IReadOnlyList<StoreWallData> source,
            List<StoreWallData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreWallData item =
                    RequireItem(source[index], "wall");

                destination.Add(
                    new StoreWallData(
                        item.Edge,
                        NormalizeId(item.FirstCellFinishId),
                        NormalizeId(item.SecondCellFinishId)));
            }

            destination.Sort(
                (left, right) =>
                    left.Edge.CompareTo(right.Edge));
        }


        private static void CopyOpenings(
            IReadOnlyList<StoreOpeningData> source,
            List<StoreOpeningData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreOpeningData item =
                    RequireItem(source[index], "opening");

                StoreOpeningData copy =
                    new StoreOpeningData
                    {
                        InstanceId = NormalizeId(item.InstanceId),
                        DefinitionId = NormalizeId(item.DefinitionId)
                    };

                CopyEdges(item.Edges, copy.Edges);
                destination.Add(copy);
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.InstanceId, right.InstanceId));
        }


        private static void CopyFixtures(
            IReadOnlyList<StoreFixtureData> source,
            List<StoreFixtureData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreFixtureData item =
                    RequireItem(source[index], "fixture");

                StoreFixtureData copy =
                    new StoreFixtureData
                    {
                        InstanceId = NormalizeId(item.InstanceId),
                        DefinitionId = NormalizeId(item.DefinitionId),
                        AnchorCell = item.AnchorCell,
                        Orientation = item.Orientation
                    };

                CopyCells(item.OccupiedCells, copy.OccupiedCells);
                destination.Add(copy);
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.InstanceId, right.InstanceId));
        }


        private static void CopyDepartments(
            IReadOnlyList<StoreDepartmentData> source,
            List<StoreDepartmentData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreDepartmentData item =
                    RequireItem(source[index], "department");

                StoreDepartmentData copy =
                    new StoreDepartmentData
                    {
                        InstanceId = NormalizeId(item.InstanceId),
                        DefinitionId = NormalizeId(item.DefinitionId)
                    };

                CopyCells(item.Cells, copy.Cells);
                destination.Add(copy);
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.InstanceId, right.InstanceId));
        }


        private static void CopyEdges(
            IReadOnlyList<StoreEdgeData> source,
            List<StoreEdgeData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                destination.Add(source[index]);
            }

            destination.Sort();
        }


        private static void CopyPlanograms(
            IReadOnlyList<StorePlanogramAssignmentData> source,
            List<StorePlanogramAssignmentData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StorePlanogramAssignmentData item =
                    RequireItem(source[index], "planogram assignment");

                destination.Add(
                    new StorePlanogramAssignmentData
                    {
                        FixtureInstanceId =
                            NormalizeId(item.FixtureInstanceId),
                        DisplayFaceIndex = item.DisplayFaceIndex,
                        ShelfRunIndex = item.ShelfRunIndex,
                        FrontageUnitIndex = item.FrontageUnitIndex,
                        ProductId = NormalizeId(item.ProductId)
                    });
            }

            destination.Sort(ComparePlanograms);
        }


        private static void CopyDisplayInventory(
            IReadOnlyList<StoreDisplayInventoryData> source,
            List<StoreDisplayInventoryData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreDisplayInventoryData item =
                    RequireItem(source[index], "display inventory line");

                destination.Add(
                    new StoreDisplayInventoryData
                    {
                        FixtureInstanceId =
                            NormalizeId(item.FixtureInstanceId),
                        ProductId = NormalizeId(item.ProductId),
                        Quantity = item.Quantity
                    });
            }

            destination.Sort(
                (left, right) =>
                {
                    int fixtureComparison =
                        CompareIds(
                            left.FixtureInstanceId,
                            right.FixtureInstanceId);

                    return fixtureComparison != 0
                        ? fixtureComparison
                        : CompareIds(
                            left.ProductId,
                            right.ProductId);
                });
        }


        private static void CopyInventoryLines(
            IReadOnlyList<StoreInventoryLineData> source,
            List<StoreInventoryLineData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreInventoryLineData item =
                    RequireItem(source[index], "inventory line");

                destination.Add(
                    new StoreInventoryLineData
                    {
                        ProductId = NormalizeId(item.ProductId),
                        Quantity = item.Quantity
                    });
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.ProductId, right.ProductId));
        }


        private static void CopyCheckouts(
            IReadOnlyList<StoreCheckoutData> source,
            List<StoreCheckoutData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreCheckoutData item =
                    RequireItem(source[index], "checkout");

                destination.Add(
                    new StoreCheckoutData
                    {
                        FixtureInstanceId =
                            NormalizeId(item.FixtureInstanceId),
                        IsOpen = item.IsOpen
                    });
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(
                        left.FixtureInstanceId,
                        right.FixtureInstanceId));
        }


        private static void CopyDeliveries(
            IReadOnlyList<StoreDeliveryData> source,
            List<StoreDeliveryData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreDeliveryData item =
                    RequireItem(source[index], "delivery");

                StoreDeliveryData copy =
                    new StoreDeliveryData
                    {
                        DeliveryId = NormalizeId(item.DeliveryId),
                        SupplierId = NormalizeId(item.SupplierId),
                        ArrivalGameSeconds = item.ArrivalGameSeconds,
                        Status = item.Status
                    };

                CopyInventoryLines(item.Lines, copy.Lines);
                destination.Add(copy);
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.DeliveryId, right.DeliveryId));
        }


        private static void CopySpawns(
            IReadOnlyList<StoreSpawnData> source,
            List<StoreSpawnData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreSpawnData item =
                    RequireItem(source[index], "spawn");

                destination.Add(
                    new StoreSpawnData
                    {
                        SpawnId = NormalizeId(item.SpawnId),
                        RoleId = NormalizeId(item.RoleId),
                        MarkerId = NormalizeId(item.MarkerId)
                    });
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.SpawnId, right.SpawnId));
        }


        private static void CopyStoryFlags(
            IReadOnlyList<StoreStoryFlagData> source,
            List<StoreStoryFlagData> destination)
        {
            RequireList(source, nameof(source));

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                StoreStoryFlagData item =
                    RequireItem(source[index], "story flag");

                destination.Add(
                    new StoreStoryFlagData
                    {
                        Key = NormalizeId(item.Key),
                        Value = Trim(item.Value)
                    });
            }

            destination.Sort(
                (left, right) =>
                    CompareIds(left.Key, right.Key));
        }


        private static int ComparePlanograms(
            StorePlanogramAssignmentData left,
            StorePlanogramAssignmentData right)
        {
            int fixtureComparison =
                CompareIds(
                    left.FixtureInstanceId,
                    right.FixtureInstanceId);

            if (fixtureComparison != 0)
            {
                return fixtureComparison;
            }

            int faceComparison =
                left.DisplayFaceIndex.CompareTo(
                    right.DisplayFaceIndex);

            if (faceComparison != 0)
            {
                return faceComparison;
            }

            int shelfComparison =
                left.ShelfRunIndex.CompareTo(
                    right.ShelfRunIndex);

            return shelfComparison != 0
                ? shelfComparison
                : left.FrontageUnitIndex.CompareTo(
                    right.FrontageUnitIndex);
        }


        private static int CompareIds(
            string left,
            string right)
        {
            return string.Compare(
                left,
                right,
                StringComparison.Ordinal);
        }

        private static string NormalizeId(
            string value)
        {
            return StoreDataIdentity.TryNormalize(
                value,
                out string normalized)
                ? normalized
                : string.Empty;
        }

        private static string Trim(
            string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static T RequireItem<T>(
            T item,
            string label)
            where T : class
        {
            return item
                ?? throw new ArgumentException(
                    $"Cannot canonicalize a null {label}.");
        }

        private static void RequireList<T>(
            IReadOnlyList<T> list,
            string parameterName)
        {
            if (list == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
