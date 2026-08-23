using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Validates scenario identity and every cross-reference before scenario
    /// bootstrap is allowed to mutate inventory, time, characters, or story.
    /// </summary>
    public sealed class StoreScenarioValidator
    {
        public StoreDataValidationResult Validate(
            StoreScenarioData scenario,
            StoreLayoutData layout,
            IStoreDefinitionCatalog definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(
                    nameof(definitions));
            }

            StoreDataValidationResult result =
                new StoreDataValidationResult();

            if (scenario == null)
            {
                result.Add(
                    StoreDataValidationCode.MissingData,
                    "scenario",
                    "No store scenario was supplied.");
                return result;
            }

            ValidateHeader(
                scenario,
                layout,
                result);

            HashSet<string> fixtureIds =
                CollectFixtureIds(layout);

            ValidatePlanograms(
                scenario.PlanogramAssignments,
                fixtureIds,
                definitions,
                result);

            ValidateDisplayInventory(
                scenario.DisplayInventory,
                fixtureIds,
                definitions,
                result);

            ValidateBackstock(
                scenario.BackstockInventory,
                definitions,
                result);

            ValidateCheckouts(
                scenario.Checkouts,
                fixtureIds,
                result);

            ValidateDeliveries(
                scenario.Deliveries,
                definitions,
                result);

            ValidateSpawns(
                scenario.Spawns,
                result);

            ValidateStoryFlags(
                scenario.StoryFlags,
                result);

            return result;
        }


        private static void ValidateHeader(
            StoreScenarioData scenario,
            StoreLayoutData layout,
            StoreDataValidationResult result)
        {
            if (scenario.SchemaVersion
                != StoreLayoutSchema.CurrentScenarioVersion)
            {
                result.Add(
                    StoreDataValidationCode
                        .UnsupportedSchemaVersion,
                    "schemaVersion",
                    $"Scenario schema {scenario.SchemaVersion} is not "
                    + $"supported; expected "
                    + $"{StoreLayoutSchema.CurrentScenarioVersion}.");
            }

            ValidateRequiredId(
                scenario.ScenarioId,
                "scenarioId",
                "Scenario",
                result);

            ValidateRequiredId(
                scenario.MapId,
                "mapId",
                "Map",
                result);

            ValidateRequiredId(
                scenario.LayoutId,
                "layoutId",
                "Layout",
                result);

            if (layout == null)
            {
                result.Add(
                    StoreDataValidationCode.MissingReference,
                    "layoutId",
                    "The scenario has no selected layout.");
            }
            else
            {
                if (!StoreDataIdentity.Equals(
                        scenario.MapId,
                        layout.MapId))
                {
                    result.Add(
                        StoreDataValidationCode.MapMismatch,
                        "mapId",
                        $"Scenario map '{scenario.MapId}' does not "
                        + $"match layout map '{layout.MapId}'.");
                }

                if (!StoreDataIdentity.Equals(
                        scenario.LayoutId,
                        layout.LayoutId))
                {
                    result.Add(
                        StoreDataValidationCode.MissingReference,
                        "layoutId",
                        $"Scenario layout '{scenario.LayoutId}' does not "
                        + $"match '{layout.LayoutId}'.");
                }
            }

            if (scenario.StartingGameSeconds < 0)
            {
                result.Add(
                    StoreDataValidationCode.InvalidValue,
                    "startingGameSeconds",
                    "Starting game time cannot be negative.");
            }

            if (scenario.StartingStoreCashCents < 0)
            {
                result.Add(
                    StoreDataValidationCode.InvalidValue,
                    "startingStoreCashCents",
                    "Starting store cash cannot be negative.");
            }

            if (scenario.StartingSimulationSpeed != 0
                && scenario.StartingSimulationSpeed != 1
                && scenario.StartingSimulationSpeed != 2
                && scenario.StartingSimulationSpeed != 4)
            {
                result.Add(
                    StoreDataValidationCode.UnsupportedValue,
                    "startingSimulationSpeed",
                    "Simulation speed must be 0, 1, 2, or 4.");
            }
        }


        private static HashSet<string> CollectFixtureIds(
            StoreLayoutData layout)
        {
            HashSet<string> result =
                new HashSet<string>(StringComparer.Ordinal);

            if (layout?.Fixtures == null)
            {
                return result;
            }

            for (int index = 0;
                 index < layout.Fixtures.Count;
                 index++)
            {
                StoreFixtureData fixture = layout.Fixtures[index];

                if (fixture != null
                    && StoreDataIdentity.TryNormalize(
                        fixture.InstanceId,
                        out string normalizedId))
                {
                    result.Add(normalizedId);
                }
            }

            return result;
        }


        private static void ValidatePlanograms(
            IReadOnlyList<StorePlanogramAssignmentData> assignments,
            ISet<string> fixtureIds,
            IStoreDefinitionCatalog definitions,
            StoreDataValidationResult result)
        {
            if (assignments == null)
            {
                AddMissingList("planogramAssignments", result);
                return;
            }

            HashSet<string> keys =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < assignments.Count;
                 index++)
            {
                string path = $"planogramAssignments[{index}]";
                StorePlanogramAssignmentData assignment =
                    assignments[index];

                if (assignment == null)
                {
                    AddNullRecord(path, "planogram assignment", result);
                    continue;
                }

                bool hasFixture =
                    ValidateFixtureReference(
                        assignment.FixtureInstanceId,
                        $"{path}.fixtureInstanceId",
                        fixtureIds,
                        result,
                        out string fixtureId);

                if (assignment.DisplayFaceIndex < 0
                    || assignment.ShelfRunIndex < 0
                    || assignment.FrontageUnitIndex < 0)
                {
                    result.Add(
                        StoreDataValidationCode.InvalidValue,
                        path,
                        "Planogram indices cannot be negative.");
                }

                ValidateDefinition(
                    StoreDefinitionKind.Product,
                    assignment.ProductId,
                    $"{path}.productId",
                    definitions,
                    result);

                if (hasFixture)
                {
                    string key =
                        $"{fixtureId}|{assignment.DisplayFaceIndex}|"
                        + $"{assignment.ShelfRunIndex}|"
                        + $"{assignment.FrontageUnitIndex}";

                    if (!keys.Add(key))
                    {
                        result.Add(
                            StoreDataValidationCode.DuplicateRecord,
                            path,
                            "The planogram frontage target is duplicated.");
                    }
                }
            }
        }


        private static void ValidateDisplayInventory(
            IReadOnlyList<StoreDisplayInventoryData> inventory,
            ISet<string> fixtureIds,
            IStoreDefinitionCatalog definitions,
            StoreDataValidationResult result)
        {
            if (inventory == null)
            {
                AddMissingList("displayInventory", result);
                return;
            }

            HashSet<string> keys =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < inventory.Count;
                 index++)
            {
                string path = $"displayInventory[{index}]";
                StoreDisplayInventoryData line = inventory[index];

                if (line == null)
                {
                    AddNullRecord(path, "display inventory line", result);
                    continue;
                }

                bool hasFixture =
                    ValidateFixtureReference(
                        line.FixtureInstanceId,
                        $"{path}.fixtureInstanceId",
                        fixtureIds,
                        result,
                        out string fixtureId);

                bool hasProduct =
                    ValidateDefinition(
                        StoreDefinitionKind.Product,
                        line.ProductId,
                        $"{path}.productId",
                        definitions,
                        result,
                        out string productId);

                ValidateNonNegativeQuantity(
                    line.Quantity,
                    $"{path}.quantity",
                    result);

                if (hasFixture
                    && hasProduct
                    && !keys.Add($"{fixtureId}|{productId}"))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        path,
                        "Display inventory is duplicated for this fixture "
                        + "and product.");
                }
            }
        }


        private static void ValidateBackstock(
            IReadOnlyList<StoreInventoryLineData> inventory,
            IStoreDefinitionCatalog definitions,
            StoreDataValidationResult result)
        {
            if (inventory == null)
            {
                AddMissingList("backstockInventory", result);
                return;
            }

            HashSet<string> productIds =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < inventory.Count;
                 index++)
            {
                string path = $"backstockInventory[{index}]";
                StoreInventoryLineData line = inventory[index];

                if (line == null)
                {
                    AddNullRecord(path, "backstock inventory line", result);
                    continue;
                }

                bool hasProduct =
                    ValidateDefinition(
                        StoreDefinitionKind.Product,
                        line.ProductId,
                        $"{path}.productId",
                        definitions,
                        result,
                        out string productId);

                ValidateNonNegativeQuantity(
                    line.Quantity,
                    $"{path}.quantity",
                    result);

                if (hasProduct
                    && !productIds.Add(productId))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        path,
                        $"Backstock product '{productId}' is duplicated.");
                }
            }
        }


        private static void ValidateCheckouts(
            IReadOnlyList<StoreCheckoutData> checkouts,
            ISet<string> fixtureIds,
            StoreDataValidationResult result)
        {
            if (checkouts == null)
            {
                AddMissingList("checkouts", result);
                return;
            }

            HashSet<string> checkoutFixtures =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < checkouts.Count;
                 index++)
            {
                string path = $"checkouts[{index}]";
                StoreCheckoutData checkout = checkouts[index];

                if (checkout == null)
                {
                    AddNullRecord(path, "checkout record", result);
                    continue;
                }

                if (ValidateFixtureReference(
                        checkout.FixtureInstanceId,
                        $"{path}.fixtureInstanceId",
                        fixtureIds,
                        result,
                        out string fixtureId)
                    && !checkoutFixtures.Add(fixtureId))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        path,
                        $"Checkout fixture '{fixtureId}' is duplicated.");
                }
            }
        }


        private static void ValidateDeliveries(
            IReadOnlyList<StoreDeliveryData> deliveries,
            IStoreDefinitionCatalog definitions,
            StoreDataValidationResult result)
        {
            if (deliveries == null)
            {
                AddMissingList("deliveries", result);
                return;
            }

            HashSet<string> deliveryIds =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < deliveries.Count;
                 index++)
            {
                string path = $"deliveries[{index}]";
                StoreDeliveryData delivery = deliveries[index];

                if (delivery == null)
                {
                    AddNullRecord(path, "delivery record", result);
                    continue;
                }

                if (TryNormalizeRequired(
                        delivery.DeliveryId,
                        $"{path}.deliveryId",
                        "Delivery",
                        result,
                        out string deliveryId)
                    && !deliveryIds.Add(deliveryId))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateInstanceId,
                        $"{path}.deliveryId",
                        $"Delivery '{deliveryId}' is duplicated.");
                }

                ValidateDefinition(
                    StoreDefinitionKind.Supplier,
                    delivery.SupplierId,
                    $"{path}.supplierId",
                    definitions,
                    result);

                if (delivery.ArrivalGameSeconds < 0)
                {
                    result.Add(
                        StoreDataValidationCode.InvalidValue,
                        $"{path}.arrivalGameSeconds",
                        "Delivery arrival time cannot be negative.");
                }

                if (!Enum.IsDefined(
                        typeof(StoreDeliveryStatus),
                        delivery.Status))
                {
                    result.Add(
                        StoreDataValidationCode.UnsupportedValue,
                        $"{path}.status",
                        $"Delivery status '{delivery.Status}' is unsupported.");
                }

                if (delivery.Lines == null
                    || delivery.Lines.Count == 0)
                {
                    result.Add(
                        StoreDataValidationCode.MissingData,
                        $"{path}.lines",
                        "A delivery requires at least one product line.");
                    continue;
                }

                HashSet<string> productIds =
                    new HashSet<string>(StringComparer.Ordinal);

                for (int lineIndex = 0;
                     lineIndex < delivery.Lines.Count;
                     lineIndex++)
                {
                    string linePath =
                        $"{path}.lines[{lineIndex}]";

                    StoreInventoryLineData line =
                        delivery.Lines[lineIndex];

                    if (line == null)
                    {
                        AddNullRecord(
                            linePath,
                            "delivery line",
                            result);
                        continue;
                    }

                    bool hasProduct =
                        ValidateDefinition(
                            StoreDefinitionKind.Product,
                            line.ProductId,
                            $"{linePath}.productId",
                            definitions,
                            result,
                            out string productId);

                    if (line.Quantity <= 0)
                    {
                        result.Add(
                            StoreDataValidationCode.InvalidQuantity,
                            $"{linePath}.quantity",
                            "A delivery quantity must be greater than zero.");
                    }

                    if (hasProduct
                        && !productIds.Add(productId))
                    {
                        result.Add(
                            StoreDataValidationCode.DuplicateRecord,
                            linePath,
                            $"Delivery product '{productId}' is duplicated.");
                    }
                }
            }
        }


        private static void ValidateSpawns(
            IReadOnlyList<StoreSpawnData> spawns,
            StoreDataValidationResult result)
        {
            if (spawns == null)
            {
                AddMissingList("spawns", result);
                return;
            }

            HashSet<string> spawnIds =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < spawns.Count;
                 index++)
            {
                string path = $"spawns[{index}]";
                StoreSpawnData spawn = spawns[index];

                if (spawn == null)
                {
                    AddNullRecord(path, "spawn record", result);
                    continue;
                }

                if (TryNormalizeRequired(
                        spawn.SpawnId,
                        $"{path}.spawnId",
                        "Spawn",
                        result,
                        out string spawnId)
                    && !spawnIds.Add(spawnId))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateInstanceId,
                        $"{path}.spawnId",
                        $"Spawn '{spawnId}' is duplicated.");
                }

                ValidateRequiredId(
                    spawn.RoleId,
                    $"{path}.roleId",
                    "Spawn role",
                    result);

                ValidateRequiredId(
                    spawn.MarkerId,
                    $"{path}.markerId",
                    "Spawn marker",
                    result);
            }
        }


        private static void ValidateStoryFlags(
            IReadOnlyList<StoreStoryFlagData> flags,
            StoreDataValidationResult result)
        {
            if (flags == null)
            {
                AddMissingList("storyFlags", result);
                return;
            }

            HashSet<string> keys =
                new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0;
                 index < flags.Count;
                 index++)
            {
                string path = $"storyFlags[{index}]";
                StoreStoryFlagData flag = flags[index];

                if (flag == null)
                {
                    AddNullRecord(path, "story flag", result);
                    continue;
                }

                if (TryNormalizeRequired(
                        flag.Key,
                        $"{path}.key",
                        "Story flag",
                        result,
                        out string key)
                    && !keys.Add(key))
                {
                    result.Add(
                        StoreDataValidationCode.DuplicateRecord,
                        $"{path}.key",
                        $"Story flag '{key}' is duplicated.");
                }
            }
        }


        private static bool ValidateFixtureReference(
            string value,
            string path,
            ISet<string> fixtureIds,
            StoreDataValidationResult result,
            out string normalizedId)
        {
            if (!TryNormalizeRequired(
                    value,
                    path,
                    "Fixture instance",
                    result,
                    out normalizedId))
            {
                return false;
            }

            if (!fixtureIds.Contains(normalizedId))
            {
                result.Add(
                    StoreDataValidationCode.MissingReference,
                    path,
                    $"Fixture '{normalizedId}' does not exist in the layout.");
                return false;
            }

            return true;
        }


        private static bool ValidateDefinition(
            StoreDefinitionKind kind,
            string value,
            string path,
            IStoreDefinitionCatalog definitions,
            StoreDataValidationResult result)
        {
            return ValidateDefinition(
                kind,
                value,
                path,
                definitions,
                result,
                out _);
        }

        private static bool ValidateDefinition(
            StoreDefinitionKind kind,
            string value,
            string path,
            IStoreDefinitionCatalog definitions,
            StoreDataValidationResult result,
            out string normalizedId)
        {
            if (!TryNormalizeRequired(
                    value,
                    path,
                    kind.ToString(),
                    result,
                    out normalizedId))
            {
                return false;
            }

            if (!definitions.Contains(kind, normalizedId))
            {
                result.Add(
                    StoreDataValidationCode.UnknownDefinition,
                    path,
                    $"{kind} '{normalizedId}' is not in the active catalog.");
                return false;
            }

            return true;
        }


        private static void ValidateNonNegativeQuantity(
            int quantity,
            string path,
            StoreDataValidationResult result)
        {
            if (quantity < 0)
            {
                result.Add(
                    StoreDataValidationCode.InvalidQuantity,
                    path,
                    "Inventory quantity cannot be negative.");
            }
        }


        private static bool TryNormalizeRequired(
            string value,
            string path,
            string label,
            StoreDataValidationResult result,
            out string normalizedId)
        {
            if (StoreDataIdentity.TryNormalize(
                    value,
                    out normalizedId))
            {
                return true;
            }

            result.Add(
                StoreDataValidationCode.MissingIdentifier,
                path,
                $"{label} requires an identifier.");

            return false;
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

        private static void AddNullRecord(
            string path,
            string label,
            StoreDataValidationResult result)
        {
            result.Add(
                StoreDataValidationCode.MissingData,
                path,
                $"The {label} is null.");
        }
    }
}
