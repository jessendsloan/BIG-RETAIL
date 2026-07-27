using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Stores only non-default wall-face finish overrides.
    ///
    /// A missing entry means the catalog's default finish applies. This keeps
    /// common default/default walls out of save data and simulation memory.
    /// </summary>
    public sealed class WallFinishState
    {
        private readonly Dictionary<
            WallFaceKey,
            WallFinishId> overrides;


        public int OverrideCount =>
            overrides.Count;


        public WallFinishState()
        {
            overrides =
                new Dictionary<
                    WallFaceKey,
                    WallFinishId>();
        }


        public bool TryGetOverride(
            WallFaceKey face,
            out WallFinishId finishId)
        {
            return overrides.TryGetValue(
                face,
                out finishId);
        }

        public IEnumerable<KeyValuePair<WallFaceKey, WallFinishId>>
            EnumerateOverrides()
        {
            foreach (
                KeyValuePair<WallFaceKey, WallFinishId> entry
                in overrides)
            {
                yield return entry;
            }
        }


        internal bool SetOverride(
            WallFaceKey face,
            WallFinishId finishId)
        {
            if (overrides.TryGetValue(
                    face,
                    out WallFinishId existingFinishId)
                && existingFinishId == finishId)
            {
                return false;
            }

            overrides[face] = finishId;
            return true;
        }

        internal bool ResetOverride(
            WallFaceKey face)
        {
            return overrides.Remove(face);
        }

        internal int ClearOverrides(
            CellEdge edge)
        {
            int removedCount = 0;

            if (overrides.Remove(
                    new WallFaceKey(
                        edge,
                        edge.FirstCell)))
            {
                removedCount++;
            }

            if (overrides.Remove(
                    new WallFaceKey(
                        edge,
                        edge.SecondCell)))
            {
                removedCount++;
            }

            return removedCount;
        }
    }
}
