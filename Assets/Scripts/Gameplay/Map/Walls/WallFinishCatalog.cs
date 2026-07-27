using System;
using System.Collections.Generic;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Defines the finish identifiers available to one store simulation.
    ///
    /// Unity authoring assets can later map these identifiers to directional
    /// sprites without becoming the simulation's source of truth.
    /// </summary>
    public sealed class WallFinishCatalog
    {
        private readonly HashSet<WallFinishId> finishIds;


        public WallFinishId DefaultFinishId { get; }

        public int Count =>
            finishIds.Count;


        public WallFinishCatalog(
            WallFinishId defaultFinishId,
            IEnumerable<WallFinishId> finishIds)
        {
            if (!defaultFinishId.IsValid)
            {
                throw new ArgumentException(
                    "A wall finish catalog requires a valid default finish.",
                    nameof(defaultFinishId));
            }

            if (finishIds == null)
            {
                throw new ArgumentNullException(
                    nameof(finishIds));
            }

            this.finishIds =
                new HashSet<WallFinishId>();

            foreach (WallFinishId finishId in finishIds)
            {
                if (!finishId.IsValid)
                {
                    throw new ArgumentException(
                        "A wall finish catalog cannot contain an invalid finish identifier.",
                        nameof(finishIds));
                }

                if (!this.finishIds.Add(finishId))
                {
                    throw new ArgumentException(
                        $"Wall finish '{finishId}' is duplicated.",
                        nameof(finishIds));
                }
            }

            if (!this.finishIds.Contains(defaultFinishId))
            {
                throw new ArgumentException(
                    $"Default wall finish '{defaultFinishId}' must exist in the catalog.",
                    nameof(defaultFinishId));
            }

            DefaultFinishId = defaultFinishId;
        }


        public bool Contains(
            WallFinishId finishId)
        {
            return finishIds.Contains(finishId);
        }

        public IEnumerable<WallFinishId> EnumerateFinishIds()
        {
            foreach (WallFinishId finishId in finishIds)
            {
                yield return finishId;
            }
        }
    }
}
