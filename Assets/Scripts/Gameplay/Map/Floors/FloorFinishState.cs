using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Stores only non-default floor-finish overrides.
    ///
    /// Missing entries use the active catalog's default finish.
    /// </summary>
    public sealed class FloorFinishState
    {
        private readonly Dictionary<GridPosition, FloorFinishId>
            overrides;


        public int OverrideCount =>
            overrides.Count;


        public FloorFinishState()
        {
            overrides =
                new Dictionary<GridPosition, FloorFinishId>();
        }


        public bool TryGetOverride(
            GridPosition cell,
            out FloorFinishId finishId)
        {
            return overrides.TryGetValue(
                cell,
                out finishId);
        }

        public IEnumerable<KeyValuePair<GridPosition, FloorFinishId>>
            EnumerateOverrides()
        {
            foreach (
                KeyValuePair<GridPosition, FloorFinishId> entry
                in overrides)
            {
                yield return entry;
            }
        }


        internal bool SetOverride(
            GridPosition cell,
            FloorFinishId finishId)
        {
            if (overrides.TryGetValue(
                    cell,
                    out FloorFinishId existingFinishId)
                && existingFinishId == finishId)
            {
                return false;
            }

            overrides[cell] = finishId;
            return true;
        }

        internal bool ResetOverride(
            GridPosition cell)
        {
            return overrides.Remove(cell);
        }
    }
}
