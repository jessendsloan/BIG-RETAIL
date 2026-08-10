using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Authored lookup for available fixture models.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Fixtures/Fixture Definition Catalog",
        fileName = "FixtureDefinitionCatalog")]
    public sealed class FixtureDefinitionAssetCatalog : ScriptableObject
    {
        [SerializeField]
        private FixtureDefinitionAsset defaultDefinition;

        [SerializeField]
        private FixtureDefinitionAsset[] additionalDefinitions =
            Array.Empty<FixtureDefinitionAsset>();

        private Dictionary<FixtureDefinitionId, FixtureDefinitionAsset>
            assetsById;


        public FixtureDefinitionAsset DefaultDefinition =>
            defaultDefinition;

        public int Count
        {
            get
            {
                EnsureLookup();
                return assetsById.Count;
            }
        }


        public IEnumerable<FixtureDefinitionAsset> EnumerateAssets()
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


        public FixtureDefinitionCatalog CreateDomainCatalog()
        {
            EnsureLookup();

            List<FixtureDefinition> definitions =
                new List<FixtureDefinition>(assetsById.Count);

            foreach (FixtureDefinitionAsset asset in EnumerateAssets())
            {
                definitions.Add(asset.CreateDomainDefinition());
            }

            return new FixtureDefinitionCatalog(definitions);
        }


        public bool TryGetAsset(
            FixtureDefinitionId definitionId,
            out FixtureDefinitionAsset asset)
        {
            EnsureLookup();
            return assetsById.TryGetValue(definitionId, out asset);
        }


        public FixtureDefinitionAsset GetAsset(
            FixtureDefinitionId definitionId)
        {
            if (TryGetAsset(definitionId, out FixtureDefinitionAsset asset))
            {
                return asset;
            }

            throw new KeyNotFoundException(
                $"No authored fixture definition is registered for '{definitionId}'.");
        }


        public void ValidateConfiguration()
        {
            assetsById = BuildLookup();
        }


        private void EnsureLookup()
        {
            if (assetsById == null)
            {
                assetsById = BuildLookup();
            }
        }


        private Dictionary<FixtureDefinitionId, FixtureDefinitionAsset>
            BuildLookup()
        {
            if (defaultDefinition == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(FixtureDefinitionAssetCatalog)} '{name}' requires a default fixture definition.");
            }

            if (additionalDefinitions == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(FixtureDefinitionAssetCatalog)} '{name}' has a null additional-definitions collection.");
            }

            Dictionary<FixtureDefinitionId, FixtureDefinitionAsset> lookup =
                new Dictionary<FixtureDefinitionId, FixtureDefinitionAsset>();

            AddAsset(lookup, defaultDefinition);

            for (int index = 0;
                 index < additionalDefinitions.Length;
                 index++)
            {
                FixtureDefinitionAsset asset =
                    additionalDefinitions[index]
                    ?? throw new InvalidOperationException(
                        $"{nameof(FixtureDefinitionAssetCatalog)} '{name}' has an empty definition at index {index}.");

                AddAsset(lookup, asset);
            }

            return lookup;
        }


        private static void AddAsset(
            IDictionary<FixtureDefinitionId, FixtureDefinitionAsset> lookup,
            FixtureDefinitionAsset asset)
        {
            asset.ValidateConfiguration();

            FixtureDefinitionId id = asset.Id;

            if (lookup.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{id}' is registered more than once.");
            }

            lookup.Add(id, asset);
        }


        private void OnValidate()
        {
            assetsById = null;
        }
    }
}
