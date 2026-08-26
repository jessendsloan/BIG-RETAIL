using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Primitive-only starting state layered over one validated store layout.
    /// It intentionally excludes live history, undo, and template mutation.
    /// </summary>
    [Serializable]
    public sealed class StoreScenarioData
    {
        public int SchemaVersion =
            StoreLayoutSchema.CurrentScenarioVersion;

        public string ScenarioId = string.Empty;
        public string DisplayName = string.Empty;
        public string MapId = string.Empty;
        public string LayoutId = string.Empty;

        public long StartingGameSeconds;
        public int StartingSimulationSpeed = 1;
        public long StartingStoreCashCents;
        public int DeterministicSeed;

        public List<StorePlanogramAssignmentData>
            PlanogramAssignments =
                new List<StorePlanogramAssignmentData>();

        public List<StoreDisplayInventoryData>
            DisplayInventory =
                new List<StoreDisplayInventoryData>();

        public List<StoreInventoryLineData>
            BackstockInventory =
                new List<StoreInventoryLineData>();

        public List<StoreCheckoutData> Checkouts =
            new List<StoreCheckoutData>();

        public List<StoreDeliveryData> Deliveries =
            new List<StoreDeliveryData>();

        public List<StoreSpawnData> Spawns =
            new List<StoreSpawnData>();

        public List<StoreStoryFlagData> StoryFlags =
            new List<StoreStoryFlagData>();
    }


    [Serializable]
    public sealed class StorePlanogramAssignmentData
    {
        public string FixtureInstanceId = string.Empty;
        public int DisplayFaceIndex;
        public int ShelfRunIndex;
        public int FrontageUnitIndex;
        public string ProductId = string.Empty;
    }


    [Serializable]
    public sealed class StoreDisplayInventoryData
    {
        public string FixtureInstanceId = string.Empty;
        public string ProductId = string.Empty;
        public int Quantity;
    }


    [Serializable]
    public sealed class StoreInventoryLineData
    {
        public string ProductId = string.Empty;
        public int Quantity;
    }


    [Serializable]
    public sealed class StoreCheckoutData
    {
        public string FixtureInstanceId = string.Empty;
        public bool IsOpen = true;
    }


    public enum StoreDeliveryStatus
    {
        Scheduled = 0,
        ReadyToReceive = 1,
        Received = 2
    }


    [Serializable]
    public sealed class StoreDeliveryData
    {
        public string DeliveryId = string.Empty;
        public string SupplierId = string.Empty;
        public long ArrivalGameSeconds;
        public StoreDeliveryStatus Status;
        public List<StoreInventoryLineData> Lines =
            new List<StoreInventoryLineData>();
    }


    [Serializable]
    public sealed class StoreSpawnData
    {
        public string SpawnId = string.Empty;
        public string RoleId = string.Empty;
        public string MarkerId = string.Empty;
    }


    [Serializable]
    public sealed class StoreStoryFlagData
    {
        public string Key = string.Empty;
        public string Value = string.Empty;
    }
}
