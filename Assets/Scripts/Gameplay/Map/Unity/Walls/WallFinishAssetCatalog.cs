using System;
using System.Collections.Generic;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls
{
    /// <summary>
    /// Unity-authored lookup from simulation finish identifiers to directional
    /// wall-finish assets.
    ///
    /// This asset translates identity into presentation data. It does not own
    /// the effective finish assigned to any structural wall face.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Walls/Wall Finish Catalog",
        fileName = "WallFinishCatalog")]
    public sealed class WallFinishAssetCatalog : ScriptableObject
    {
        [SerializeField]
        private WallFinishAsset defaultFinish;

        [SerializeField]
        private WallFinishAsset[] additionalFinishes =
            Array.Empty<WallFinishAsset>();

        private Dictionary<WallFinishId, WallFinishAsset>
            assetsById;


        public WallFinishAsset DefaultFinish =>
            defaultFinish;

        public int Count
        {
            get
            {
                EnsureLookup();
                return assetsById.Count;
            }
        }


        public WallFinishCatalog CreateDomainCatalog()
        {
            EnsureLookup();

            return new WallFinishCatalog(
                defaultFinish.Id,
                assetsById.Keys);
        }


        public WallFinishAsset GetAsset(
            WallFinishId finishId)
        {
            EnsureLookup();

            if (!assetsById.TryGetValue(
                    finishId,
                    out WallFinishAsset finishAsset))
            {
                throw new KeyNotFoundException(
                    $"No authored wall finish asset is registered for "
                    + $"finish identifier '{finishId}'.");
            }

            return finishAsset;
        }


        public bool TryGetAsset(
            WallFinishId finishId,
            out WallFinishAsset finishAsset)
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


        private Dictionary<WallFinishId, WallFinishAsset>
            BuildLookup()
        {
            if (defaultFinish == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallFinishAssetCatalog)} '{name}' requires "
                    + "a default wall finish asset.");
            }

            if (additionalFinishes == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WallFinishAssetCatalog)} '{name}' has a null "
                    + "additional-finishes collection.");
            }

            Dictionary<WallFinishId, WallFinishAsset> lookup =
                new Dictionary<WallFinishId, WallFinishAsset>();

            AddAsset(
                lookup,
                defaultFinish);

            for (int index = 0;
                 index < additionalFinishes.Length;
                 index++)
            {
                WallFinishAsset finishAsset =
                    additionalFinishes[index];

                if (finishAsset == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(WallFinishAssetCatalog)} '{name}' has an "
                        + $"empty additional finish at index {index}.");
                }

                AddAsset(
                    lookup,
                    finishAsset);
            }

            return lookup;
        }


        private static void AddAsset(
            IDictionary<WallFinishId, WallFinishAsset> lookup,
            WallFinishAsset finishAsset)
        {
            finishAsset.ValidateConfiguration();

            WallFinishId finishId =
                finishAsset.Id;

            if (lookup.ContainsKey(finishId))
            {
                throw new InvalidOperationException(
                    $"Wall finish identifier '{finishId}' is registered "
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
