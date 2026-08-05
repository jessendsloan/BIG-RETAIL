using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Departments
{
    /// <summary>
    /// One player-designated operational area. It is a plan, not a room:
    /// sales departments may be open and irregular rather than enclosed.
    /// </summary>
    public sealed class DepartmentPlan
    {
        private readonly HashSet<GridPosition> cells;


        public DepartmentPlanId Id { get; }

        public DepartmentDefinitionId DefinitionId { get; }

        public int CellCount =>
            cells.Count;


        internal DepartmentPlan(
            DepartmentPlanId id,
            DepartmentDefinitionId definitionId,
            IEnumerable<GridPosition> initialCells)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A department plan requires a valid ID.",
                    nameof(id));
            }

            if (!definitionId.IsValid)
            {
                throw new ArgumentException(
                    "A department plan requires a valid definition ID.",
                    nameof(definitionId));
            }

            if (initialCells == null)
            {
                throw new ArgumentNullException(
                    nameof(initialCells));
            }

            cells =
                new HashSet<GridPosition>(initialCells);

            if (cells.Count == 0)
            {
                throw new ArgumentException(
                    "A department plan requires at least one cell.",
                    nameof(initialCells));
            }

            Id = id;
            DefinitionId = definitionId;
        }


        public bool ContainsCell(
            GridPosition cell)
        {
            return cells.Contains(cell);
        }


        public IEnumerable<GridPosition> EnumerateCells()
        {
            foreach (GridPosition cell in cells)
            {
                yield return cell;
            }
        }


        internal int AddCells(
            IReadOnlyList<GridPosition> newCells)
        {
            int addedCount = 0;

            for (int index = 0;
                 index < newCells.Count;
                 index++)
            {
                if (cells.Add(newCells[index]))
                {
                    addedCount++;
                }
            }

            return addedCount;
        }
    }
}
