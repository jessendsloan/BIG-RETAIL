using System;
using System.Collections.Generic;
using BigRetail.Map.Floors;
using UnityEngine;

namespace BigRetail.Map.Unity.Floors
{
    /// <summary>
    /// Unity-authored lookup from simulation Floor-finish identifiers to
    /// Tilemap presentation assets.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Floors/Floor Finish Catalog",
        fileName = "FloorFinishCatalog")]
    public sealed class FloorFinishAssetCatalog : ScriptableObject
    {
        [SerializeField]
        private FloorFinishAsset defaultFinish;

        [SerializeField]
        private FloorFinishAsset[] additionalFinishes =
            Array.Empty<FloorFinishAsset>();

        private Dictionary<FloorFinishId, FloorFinishAsset>
            assetsById;


        public FloorFinishAsset DefaultFinish =>
            defaultFinish;

        public int Count
        {
            get
            {
                EnsureLookup();
                return assetsById.Count;
            }
        }


        public IEnumerable<FloorFinishAsset> EnumerateAssets()
        {
            EnsureLookup();

            yield return defaultFinish;

            for (int index = 0;
                 index < additionalFinishes.Length;
                 index++)
            {
                yield return additionalFinishes[index];
            }
        }


        public FloorFinishCatalog CreateDomainCatalog()
        {
            EnsureLookup();

            return new FloorFinishCatalog(
                defaultFinish.Id,
                assetsById.Keys);
        }


        public FloorFinishAsset GetAsset(
            FloorFinishId finishId)
        {
            EnsureLookup();

            if (!assetsById.TryGetValue(
                    finishId,
                    out FloorFinishAsset finishAsset))
            {
                throw new KeyNotFoundException(
                    $"No authored Floor finish asset is registered for "
                    + $"finish identifier '{finishId}'.");
            }

            return finishAsset;
        }


        public bool TryGetAsset(
            FloorFinishId finishId,
            out FloorFinishAsset finishAsset)
        {
            EnsureLookup();

            return assetsById.TryGetValue(
                finishId,
                out finishAsset);
        }


        public void ValidateConfiguration()
        {
            assetsById =
                BuildLookup();
        }


        private void EnsureLookup()
        {
            if (assetsById == null)
            {
                assetsById =
                    BuildLookup();
            }
        }


        private Dictionary<FloorFinishId, FloorFinishAsset>
            BuildLookup()
        {
            if (defaultFinish == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(FloorFinishAssetCatalog)} '{name}' requires "
                    + "a default Floor finish asset.");
            }

            if (additionalFinishes == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(FloorFinishAssetCatalog)} '{name}' has a null "
                    + "additional-finishes collection.");
            }

            Dictionary<FloorFinishId, FloorFinishAsset> lookup =
                new Dictionary<FloorFinishId, FloorFinishAsset>();

            AddAsset(
                lookup,
                defaultFinish);

            for (int index = 0;
                 index < additionalFinishes.Length;
                 index++)
            {
                FloorFinishAsset finishAsset =
                    additionalFinishes[index];

                if (finishAsset == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(FloorFinishAssetCatalog)} '{name}' has an "
                        + $"empty additional finish at index {index}.");
                }

                AddAsset(
                    lookup,
                    finishAsset);
            }

            return lookup;
        }


        private static void AddAsset(
            IDictionary<FloorFinishId, FloorFinishAsset> lookup,
            FloorFinishAsset finishAsset)
        {
            finishAsset.ValidateConfiguration();

            FloorFinishId finishId =
                finishAsset.Id;

            if (lookup.ContainsKey(finishId))
            {
                throw new InvalidOperationException(
                    $"Floor finish identifier '{finishId}' is registered "
                    + "more than once in the authored catalog.");
            }

            lookup.Add(
                finishId,
                finishAsset);
        }


        private void OnValidate()
        {
            assetsById =
                null;
        }
    }
}
