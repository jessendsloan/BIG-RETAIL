using System;
using System.Collections.Generic;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Authored lookup for available door models. It creates the matching
    /// engine-free definition catalog and resolves presentation assets by ID.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Doors/Door Definition Catalog",
        fileName = "DoorDefinitionCatalog")]
    public sealed class DoorDefinitionAssetCatalog : ScriptableObject
    {
        [SerializeField]
        private DoorDefinitionAsset defaultDefinition;

        [SerializeField]
        private DoorDefinitionAsset[] additionalDefinitions =
            Array.Empty<DoorDefinitionAsset>();

        private Dictionary<DoorDefinitionId, DoorDefinitionAsset>
            assetsById;


        public DoorDefinitionAsset DefaultDefinition =>
            defaultDefinition;

        public int Count
        {
            get
            {
                EnsureLookup();
                return assetsById.Count;
            }
        }


        public IEnumerable<DoorDefinitionAsset> EnumerateAssets()
        {
            EnsureLookup();

            yield return defaultDefinition;

            for (int index = 0;
                 index < additionalDefinitions.Length;
                 index++)
            {
                yield return additionalDefinitions[index];
            }
        }


        public DoorDefinitionCatalog CreateDomainCatalog()
        {
            EnsureLookup();

            List<DoorDefinition> definitions =
                new List<DoorDefinition>(
                    assetsById.Count);

            foreach (
                DoorDefinitionAsset asset
                in EnumerateAssets())
            {
                definitions.Add(
                    asset.CreateDomainDefinition());
            }

            return new DoorDefinitionCatalog(
                definitions);
        }


        public bool TryGetAsset(
            DoorDefinitionId definitionId,
            out DoorDefinitionAsset asset)
        {
            EnsureLookup();

            return assetsById.TryGetValue(
                definitionId,
                out asset);
        }


        public DoorDefinitionAsset GetAsset(
            DoorDefinitionId definitionId)
        {
            if (TryGetAsset(
                    definitionId,
                    out DoorDefinitionAsset asset))
            {
                return asset;
            }

            throw new KeyNotFoundException(
                $"No authored door definition is registered for "
                + $"'{definitionId}'.");
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


        private Dictionary<DoorDefinitionId, DoorDefinitionAsset>
            BuildLookup()
        {
            if (defaultDefinition == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DoorDefinitionAssetCatalog)} '{name}' requires "
                    + "a default door definition.");
            }

            if (additionalDefinitions == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DoorDefinitionAssetCatalog)} '{name}' has a "
                    + "null additional-definitions collection.");
            }

            Dictionary<DoorDefinitionId, DoorDefinitionAsset> lookup =
                new Dictionary<DoorDefinitionId, DoorDefinitionAsset>();

            AddAsset(
                lookup,
                defaultDefinition);

            for (int index = 0;
                 index < additionalDefinitions.Length;
                 index++)
            {
                DoorDefinitionAsset asset =
                    additionalDefinitions[index]
                    ?? throw new InvalidOperationException(
                        $"{nameof(DoorDefinitionAssetCatalog)} '{name}' has "
                        + $"an empty definition at index {index}.");

                AddAsset(
                    lookup,
                    asset);
            }

            return lookup;
        }


        private static void AddAsset(
            IDictionary<DoorDefinitionId, DoorDefinitionAsset> lookup,
            DoorDefinitionAsset asset)
        {
            asset.ValidateConfiguration();

            DoorDefinitionId id =
                asset.Id;

            if (lookup.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Door definition '{id}' is registered more than once.");
            }

            lookup.Add(
                id,
                asset);
        }


        private void OnValidate()
        {
            assetsById = null;
        }
    }
}
