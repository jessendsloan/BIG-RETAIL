using System;
using System.Collections.Generic;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Defines the floor finishes available to one store simulation.
    /// </summary>
    public sealed class FloorFinishCatalog
    {
        private readonly HashSet<FloorFinishId> finishIds;


        public FloorFinishId DefaultFinishId { get; }

        public int Count =>
            finishIds.Count;


        public FloorFinishCatalog(
            FloorFinishId defaultFinishId,
            IEnumerable<FloorFinishId> finishIds)
        {
            if (!defaultFinishId.IsValid)
            {
                throw new ArgumentException(
                    "A floor finish catalog requires a valid default finish.",
                    nameof(defaultFinishId));
            }

            if (finishIds == null)
            {
                throw new ArgumentNullException(
                    nameof(finishIds));
            }

            this.finishIds =
                new HashSet<FloorFinishId>();

            foreach (FloorFinishId finishId in finishIds)
            {
                if (!finishId.IsValid)
                {
                    throw new ArgumentException(
                        "A floor finish catalog cannot contain an invalid identifier.",
                        nameof(finishIds));
                }

                if (!this.finishIds.Add(finishId))
                {
                    throw new ArgumentException(
                        $"Floor finish '{finishId}' is duplicated.",
                        nameof(finishIds));
                }
            }

            if (!this.finishIds.Contains(defaultFinishId))
            {
                throw new ArgumentException(
                    $"Default floor finish '{defaultFinishId}' must exist in the catalog.",
                    nameof(defaultFinishId));
            }

            DefaultFinishId = defaultFinishId;
        }


        public bool Contains(
            FloorFinishId finishId)
        {
            return finishIds.Contains(finishId);
        }

        public IEnumerable<FloorFinishId> EnumerateFinishIds()
        {
            foreach (FloorFinishId finishId in finishIds)
            {
                yield return finishId;
            }
        }
    }
}
