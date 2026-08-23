using System.Collections.Generic;
using NUnit.Framework;

namespace BigRetail.StoreLayouts.Tests
{
    public sealed class StoreDataValidationTests
    {
        [Test]
        public void Layout_CompleteValidData_IsAccepted()
        {
            TestStoreData testData =
                TestStoreData.Create();

            StoreDataValidationResult result =
                new StoreLayoutValidator().Validate(
                    testData.Layout,
                    testData.Context);

            Assert.That(
                result.IsValid,
                Is.True,
                JoinIssues(result));
        }

        [Test]
        public void Layout_WrongVersionAndFingerprint_AreRejected()
        {
            TestStoreData testData =
                TestStoreData.Create();

            testData.Layout.SchemaVersion = 99;
            testData.Layout.MapFingerprint = "different-map";

            StoreDataValidationResult result =
                new StoreLayoutValidator().Validate(
                    testData.Layout,
                    testData.Context);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode
                        .UnsupportedSchemaVersion),
                Is.True);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode
                        .MapFingerprintMismatch),
                Is.True);
        }

        [Test]
        public void Layout_DuplicatesUnknownDefinitionsAndOverlaps_AreRejected()
        {
            TestStoreData testData =
                TestStoreData.Create();

            testData.Layout.Foundations.Add(
                testData.Layout.Foundations[0]);

            testData.Layout.Floors[0].FinishId =
                "unknown-finish";

            testData.Layout.Fixtures.Add(
                new StoreFixtureData
                {
                    InstanceId = "second-shelf",
                    DefinitionId = "standard-shelf",
                    AnchorCell = new StoreCellData(0, 1),
                    Orientation = StoreOrientation.South,
                    OccupiedCells =
                        new List<StoreCellData>
                        {
                            new StoreCellData(0, 1)
                        }
                });

            StoreDataValidationResult result =
                new StoreLayoutValidator().Validate(
                    testData.Layout,
                    testData.Context);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.DuplicateRecord),
                Is.True);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.UnknownDefinition),
                Is.True);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.OccupiedCellOverlap),
                Is.True);
        }

        [Test]
        public void Layout_OpeningWithoutAuthoredWall_IsRejected()
        {
            TestStoreData testData =
                TestStoreData.Create();

            testData.Layout.Openings[0].Edges[0] =
                new StoreEdgeData(
                    new StoreCellData(0, 1),
                    StoreEdgeDirection.NorthWest);

            StoreDataValidationResult result =
                new StoreLayoutValidator().Validate(
                    testData.Layout,
                    testData.Context);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.MissingReference),
                Is.True);
        }

        [Test]
        public void Scenario_CompleteValidData_IsAccepted()
        {
            TestStoreData testData =
                TestStoreData.Create();

            StoreDataValidationResult result =
                new StoreScenarioValidator().Validate(
                    testData.Scenario,
                    testData.Layout,
                    testData.Definitions);

            Assert.That(
                result.IsValid,
                Is.True,
                JoinIssues(result));
        }

        [Test]
        public void Scenario_MissingFixtureUnknownProductAndNegativeStock_AreRejected()
        {
            TestStoreData testData =
                TestStoreData.Create();

            testData.Scenario.DisplayInventory[0]
                .FixtureInstanceId = "missing-fixture";

            testData.Scenario.DisplayInventory[0]
                .ProductId = "unknown-product";

            testData.Scenario.DisplayInventory[0].Quantity = -1;

            StoreDataValidationResult result =
                new StoreScenarioValidator().Validate(
                    testData.Scenario,
                    testData.Layout,
                    testData.Definitions);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.MissingReference),
                Is.True);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.UnknownDefinition),
                Is.True);

            Assert.That(
                result.Contains(
                    StoreDataValidationCode.InvalidQuantity),
                Is.True);
        }

        [Test]
        public void Canonicalizer_SortsCopyWithoutMutatingTemplate()
        {
            TestStoreData testData =
                TestStoreData.Create();

            testData.Layout.Foundations.Reverse();
            StoreCellData originalFirst =
                testData.Layout.Foundations[0];

            testData.Layout.LayoutId = " frank-layout ";

            StoreLayoutData canonical =
                new StoreDataCanonicalizer()
                    .CreateCanonicalCopy(testData.Layout);

            Assert.That(
                canonical,
                Is.Not.SameAs(testData.Layout));

            Assert.That(
                canonical.LayoutId,
                Is.EqualTo("FRANK-LAYOUT"));

            Assert.That(
                canonical.Foundations[0],
                Is.EqualTo(new StoreCellData(0, 0)));

            Assert.That(
                testData.Layout.Foundations[0],
                Is.EqualTo(originalFirst));

            Assert.That(
                canonical.Fixtures[0],
                Is.Not.SameAs(testData.Layout.Fixtures[0]));
        }


        private static string JoinIssues(
            StoreDataValidationResult result)
        {
            List<string> messages =
                new List<string>();

            for (int index = 0;
                 index < result.IssueCount;
                 index++)
            {
                messages.Add(
                    result.Issues[index].ToString());
            }

            return string.Join("\n", messages);
        }


        private sealed class TestStoreData
        {
            public StoreDefinitionCatalog Definitions;
            public StoreLocationValidationContext Context;
            public StoreLayoutData Layout;
            public StoreScenarioData Scenario;


            public static TestStoreData Create()
            {
                StoreDefinitionCatalog definitions =
                    new StoreDefinitionCatalog()
                        .Add(
                            StoreDefinitionKind.FloorFinish,
                            "vinyl-tile")
                        .Add(
                            StoreDefinitionKind.WallFinish,
                            "white-paint")
                        .Add(
                            StoreDefinitionKind.Opening,
                            "fixed-window")
                        .Add(
                            StoreDefinitionKind.Fixture,
                            "standard-shelf")
                        .Add(
                            StoreDefinitionKind.Department,
                            "grocery")
                        .Add(
                            StoreDefinitionKind.Product,
                            "bright-cola")
                        .Add(
                            StoreDefinitionKind.Supplier,
                            "big-wholesale");

                List<StoreCellData> validCells =
                    new List<StoreCellData>
                    {
                        new StoreCellData(0, 0),
                        new StoreCellData(1, 0),
                        new StoreCellData(0, 1),
                        new StoreCellData(1, 1)
                    };

                StoreLocationValidationContext context =
                    new StoreLocationValidationContext(
                        "frank-roadside",
                        "frank-map-v1",
                        validCells,
                        new[] { "frank-footprint" },
                        definitions);

                StoreEdgeData frontWall =
                    new StoreEdgeData(
                        new StoreCellData(0, 0),
                        StoreEdgeDirection.NorthEast);

                StoreLayoutData layout =
                    new StoreLayoutData
                    {
                        LayoutId = "frank-layout",
                        DisplayName = "Frank's Store",
                        MapId = "frank-roadside",
                        MapFingerprint = "frank-map-v1",
                        OwnedLandRegionIds =
                            new List<string>
                            {
                                "frank-footprint"
                            },
                        Foundations =
                            new List<StoreCellData>(validCells),
                        Floors =
                            new List<StoreFloorData>
                            {
                                new StoreFloorData(
                                    validCells[0],
                                    "vinyl-tile"),
                                new StoreFloorData(
                                    validCells[1],
                                    "vinyl-tile"),
                                new StoreFloorData(
                                    validCells[2],
                                    "vinyl-tile"),
                                new StoreFloorData(
                                    validCells[3],
                                    "vinyl-tile")
                            },
                        Walls =
                            new List<StoreWallData>
                            {
                                new StoreWallData(
                                    frontWall,
                                    "white-paint",
                                    "white-paint")
                            },
                        Openings =
                            new List<StoreOpeningData>
                            {
                                new StoreOpeningData
                                {
                                    InstanceId = "front-window",
                                    DefinitionId = "fixed-window",
                                    Edges =
                                        new List<StoreEdgeData>
                                        {
                                            frontWall
                                        }
                                }
                            },
                        Fixtures =
                            new List<StoreFixtureData>
                            {
                                new StoreFixtureData
                                {
                                    InstanceId = "shelf-1",
                                    DefinitionId = "standard-shelf",
                                    AnchorCell = validCells[2],
                                    Orientation = StoreOrientation.North,
                                    OccupiedCells =
                                        new List<StoreCellData>
                                        {
                                            validCells[2]
                                        }
                                }
                            },
                        Departments =
                            new List<StoreDepartmentData>
                            {
                                new StoreDepartmentData
                                {
                                    InstanceId = "grocery-1",
                                    DefinitionId = "grocery",
                                    Cells =
                                        new List<StoreCellData>
                                        {
                                            validCells[3]
                                        }
                                }
                            },
                        ReceivingCells =
                            new List<StoreCellData>
                            {
                                validCells[1]
                            }
                    };

                StoreScenarioData scenario =
                    new StoreScenarioData
                    {
                        ScenarioId = "frank-opening-shift",
                        DisplayName = "Frank Opening Shift",
                        MapId = "frank-roadside",
                        LayoutId = "frank-layout",
                        StartingGameSeconds = 9 * 60 * 60,
                        StartingSimulationSpeed = 1,
                        StartingStoreCashCents = 250000,
                        DeterministicSeed = 1987,
                        PlanogramAssignments =
                            new List<StorePlanogramAssignmentData>
                            {
                                new StorePlanogramAssignmentData
                                {
                                    FixtureInstanceId = "shelf-1",
                                    ProductId = "bright-cola"
                                }
                            },
                        DisplayInventory =
                            new List<StoreDisplayInventoryData>
                            {
                                new StoreDisplayInventoryData
                                {
                                    FixtureInstanceId = "shelf-1",
                                    ProductId = "bright-cola",
                                    Quantity = 6
                                }
                            },
                        BackstockInventory =
                            new List<StoreInventoryLineData>
                            {
                                new StoreInventoryLineData
                                {
                                    ProductId = "bright-cola",
                                    Quantity = 24
                                }
                            },
                        Checkouts =
                            new List<StoreCheckoutData>
                            {
                                new StoreCheckoutData
                                {
                                    FixtureInstanceId = "shelf-1",
                                    IsOpen = true
                                }
                            },
                        Deliveries =
                            new List<StoreDeliveryData>
                            {
                                new StoreDeliveryData
                                {
                                    DeliveryId = "morning-delivery",
                                    SupplierId = "big-wholesale",
                                    ArrivalGameSeconds = 10 * 60 * 60,
                                    Status =
                                        StoreDeliveryStatus.Scheduled,
                                    Lines =
                                        new List<StoreInventoryLineData>
                                        {
                                            new StoreInventoryLineData
                                            {
                                                ProductId = "bright-cola",
                                                Quantity = 12
                                            }
                                        }
                                }
                            },
                        Spawns =
                            new List<StoreSpawnData>
                            {
                                new StoreSpawnData
                                {
                                    SpawnId = "founder",
                                    RoleId = "founder",
                                    MarkerId = "founder-start"
                                }
                            },
                        StoryFlags =
                            new List<StoreStoryFlagData>
                            {
                                new StoreStoryFlagData
                                {
                                    Key = "frank-opening-active",
                                    Value = "true"
                                }
                            }
                    };

                return new TestStoreData
                {
                    Definitions = definitions,
                    Context = context,
                    Layout = layout,
                    Scenario = scenario
                };
            }
        }
    }
}
