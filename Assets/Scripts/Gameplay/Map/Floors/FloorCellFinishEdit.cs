using System;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Records one exact before-and-after finish change for a Floor cell.
    /// </summary>
    public readonly struct FloorCellFinishEdit :
        IEquatable<FloorCellFinishEdit>
    {
        public GridPosition Cell { get; }

        public FloorFinishId BeforeFinishId { get; }

        public FloorFinishId AfterFinishId { get; }


        public FloorCellFinishEdit(
            GridPosition cell,
            FloorFinishId beforeFinishId,
            FloorFinishId afterFinishId)
        {
            if (!beforeFinishId.IsValid)
            {
                throw new ArgumentException(
                    "A Floor finish edit requires a valid previous finish.",
                    nameof(beforeFinishId));
            }

            if (!afterFinishId.IsValid)
            {
                throw new ArgumentException(
                    "A Floor finish edit requires a valid next finish.",
                    nameof(afterFinishId));
            }

            if (beforeFinishId == afterFinishId)
            {
                throw new ArgumentException(
                    "A Floor finish edit must represent a real change.",
                    nameof(afterFinishId));
            }

            Cell = cell;
            BeforeFinishId = beforeFinishId;
            AfterFinishId = afterFinishId;
        }


        public FloorCellFinishEdit Inverse()
        {
            return new FloorCellFinishEdit(
                Cell,
                AfterFinishId,
                BeforeFinishId);
        }


        public bool Equals(
            FloorCellFinishEdit other)
        {
            return Cell.Equals(other.Cell)
                && BeforeFinishId.Equals(other.BeforeFinishId)
                && AfterFinishId.Equals(other.AfterFinishId);
        }

        public override bool Equals(
            object obj)
        {
            return obj is FloorCellFinishEdit other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Cell.GetHashCode();
                hash = (hash * 31) + BeforeFinishId.GetHashCode();
                hash = (hash * 31) + AfterFinishId.GetHashCode();
                return hash;
            }
        }
    }
}
