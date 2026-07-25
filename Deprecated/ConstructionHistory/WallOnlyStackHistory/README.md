# Wall-only stack history

This folder preserves the wall-only Undo/Redo implementation retired by
the neutral construction-history refactor.

Former active locations:

- `Assets/Scripts/Gameplay/Map/Walls/WallEditHistory.cs`
- `Assets/Scripts/Gameplay/Map/Walls/WallHistoryFailure.cs`
- `Assets/Scripts/Gameplay/Map/Walls/WallHistoryResult.cs`
- `Assets/Scripts/Gameplay/Construction/Unity/History/WallEditHistoryHost.cs`
- `Assets/Scripts/Gameplay/Construction/Unity/History/WallHistoryInputController.cs`
- `Assets/Tests/EditMode/MapWalls/WallEditHistoryTests.cs`

Replacements:

- `BigRetail.Map.Construction.ConstructionHistory`
- `BigRetail.Map.Construction.IReversibleConstructionAction`
- `ReversibleWallEditAction`
- `ReversibleFloorEditAction`
- `ConstructionHistoryHost`
- `ConstructionHistoryInputController`

These files live outside `Assets` so Unity does not import or compile them.
