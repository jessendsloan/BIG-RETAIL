using System;
using System.Collections.Generic;

namespace BigRetail.StoreLayouts
{
    /// <summary>
    /// Primitive-only authored store snapshot. Public fields intentionally
    /// support Unity and other field-based serializers without introducing a
    /// UnityEngine dependency into the domain assembly.
    /// </summary>
    [Serializable]
    public sealed class StoreLayoutData
    {
        public int SchemaVersion =
            StoreLayoutSchema.CurrentLayoutVersion;

        public string LayoutId = string.Empty;
        public string DisplayName = string.Empty;
        public string MapId = string.Empty;
        public string MapFingerprint = string.Empty;

        public StoreCellData LogicalOrigin;

        public List<string> OwnedLandRegionIds =
            new List<string>();

        public List<StoreCellData> Foundations =
            new List<StoreCellData>();

        public List<StoreCellData> Sidewalks =
            new List<StoreCellData>();

        public List<StoreFloorData> Floors =
            new List<StoreFloorData>();

        public List<StoreWallData> Walls =
            new List<StoreWallData>();

        public List<StoreOpeningData> Openings =
            new List<StoreOpeningData>();

        public List<StoreFixtureData> Fixtures =
            new List<StoreFixtureData>();

        // Added as an optional, backward-compatible schema-v1 record. Layouts
        // authored before equipment planning existed deserialize as no plans.
        public List<StoreFixturePlanData> FixturePlans =
            new List<StoreFixturePlanData>();

        public List<StoreDepartmentData> Departments =
            new List<StoreDepartmentData>();

        public List<StoreCellData> ReceivingCells =
            new List<StoreCellData>();
    }


    [Serializable]
    public struct StoreCellData :
        IEquatable<StoreCellData>,
        IComparable<StoreCellData>
    {
        public int X;
        public int Y;
        public int Level;


        public StoreCellData(
            int x,
            int y,
            int level = 0)
        {
            X = x;
            Y = y;
            Level = level;
        }


        public int CompareTo(
            StoreCellData other)
        {
            int levelComparison =
                Level.CompareTo(other.Level);

            if (levelComparison != 0)
            {
                return levelComparison;
            }

            int yComparison =
                Y.CompareTo(other.Y);

            return yComparison != 0
                ? yComparison
                : X.CompareTo(other.X);
        }

        public bool Equals(
            StoreCellData other)
        {
            return X == other.X
                && Y == other.Y
                && Level == other.Level;
        }

        public override bool Equals(
            object obj)
        {
            return obj is StoreCellData other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + X;
                hash = (hash * 31) + Y;
                hash = (hash * 31) + Level;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y}, Level {Level})";
        }

        public static bool operator ==(
            StoreCellData left,
            StoreCellData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            StoreCellData left,
            StoreCellData right)
        {
            return !left.Equals(right);
        }
    }


    public enum StoreEdgeDirection
    {
        NorthEast = 0,
        NorthWest = 1
    }


    [Serializable]
    public struct StoreEdgeData :
        IEquatable<StoreEdgeData>,
        IComparable<StoreEdgeData>
    {
        public StoreCellData AnchorCell;
        public StoreEdgeDirection Direction;


        public StoreEdgeData(
            StoreCellData anchorCell,
            StoreEdgeDirection direction)
        {
            AnchorCell = anchorCell;
            Direction = direction;
        }


        public StoreCellData FirstCell =>
            AnchorCell;

        public StoreCellData SecondCell =>
            Direction == StoreEdgeDirection.NorthEast
                ? new StoreCellData(
                    AnchorCell.X + 1,
                    AnchorCell.Y,
                    AnchorCell.Level)
                : new StoreCellData(
                    AnchorCell.X,
                    AnchorCell.Y + 1,
                    AnchorCell.Level);


        public bool HasSupportedDirection()
        {
            return Direction == StoreEdgeDirection.NorthEast
                || Direction == StoreEdgeDirection.NorthWest;
        }

        public int CompareTo(
            StoreEdgeData other)
        {
            int cellComparison =
                AnchorCell.CompareTo(other.AnchorCell);

            return cellComparison != 0
                ? cellComparison
                : Direction.CompareTo(other.Direction);
        }

        public bool Equals(
            StoreEdgeData other)
        {
            return AnchorCell == other.AnchorCell
                && Direction == other.Direction;
        }

        public override bool Equals(
            object obj)
        {
            return obj is StoreEdgeData other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AnchorCell.GetHashCode() * 397)
                    ^ (int)Direction;
            }
        }

        public override string ToString()
        {
            return $"{AnchorCell} — {Direction}";
        }
    }


    public enum StoreOrientation
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }


    [Serializable]
    public sealed class StoreFloorData
    {
        public StoreCellData Cell;
        public string FinishId = string.Empty;


        public StoreFloorData()
        {
        }

        public StoreFloorData(
            StoreCellData cell,
            string finishId)
        {
            Cell = cell;
            FinishId = finishId ?? string.Empty;
        }
    }


    [Serializable]
    public sealed class StoreWallData
    {
        public StoreEdgeData Edge;
        public string FirstCellFinishId = string.Empty;
        public string SecondCellFinishId = string.Empty;


        public StoreWallData()
        {
        }

        public StoreWallData(
            StoreEdgeData edge,
            string firstCellFinishId,
            string secondCellFinishId)
        {
            Edge = edge;
            FirstCellFinishId =
                firstCellFinishId ?? string.Empty;
            SecondCellFinishId =
                secondCellFinishId ?? string.Empty;
        }
    }


    [Serializable]
    public sealed class StoreOpeningData
    {
        public string InstanceId = string.Empty;
        public string DefinitionId = string.Empty;
        public List<StoreEdgeData> Edges =
            new List<StoreEdgeData>();
    }


    [Serializable]
    public sealed class StoreFixtureData
    {
        public string InstanceId = string.Empty;
        public string DefinitionId = string.Empty;
        public StoreCellData AnchorCell;
        public StoreOrientation Orientation;
        public List<StoreCellData> OccupiedCells =
            new List<StoreCellData>();
    }


    [Serializable]
    public sealed class StoreFixturePlanData
    {
        public string InstanceId = string.Empty;
        public string DefinitionId = string.Empty;
        public StoreCellData AnchorCell;
        public StoreOrientation Orientation;
        public List<StoreCellData> OccupiedCells =
            new List<StoreCellData>();
    }


    [Serializable]
    public sealed class StoreDepartmentData
    {
        public string InstanceId = string.Empty;
        public string DefinitionId = string.Empty;
        public List<StoreCellData> Cells =
            new List<StoreCellData>();
    }
}
