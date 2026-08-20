using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using UnityEngine;

namespace BigRetail.Merchandise.Unity
{
    [CreateAssetMenu(
        fileName = "BrandCatalog",
        menuName = "Big Retail/Merchandise/Brand Catalog")]
    public sealed class BrandCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private BrandDefinitionAsset[] brands =
            Array.Empty<BrandDefinitionAsset>();


        public IReadOnlyList<BrandDefinitionAsset> Brands =>
            brands;


        public bool TryCreateCatalog(
            out BrandCatalog catalog,
            out string error)
        {
            if (brands == null)
            {
                catalog = null;
                error = $"{name}: Brand asset collection is missing.";
                return false;
            }

            List<BrandDefinition> definitions =
                new List<BrandDefinition>(brands.Length);

            for (int index = 0; index < brands.Length; index++)
            {
                BrandDefinitionAsset brand = brands[index];

                if (brand == null)
                {
                    catalog = null;
                    error = $"{name}: Brand entry {index} is missing.";
                    return false;
                }

                if (!brand.TryCreateDefinition(
                        out BrandDefinition definition,
                        out error))
                {
                    catalog = null;
                    return false;
                }

                definitions.Add(definition);
            }

            try
            {
                catalog = new BrandCatalog(definitions);
                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                catalog = null;
                error = $"{name}: {exception.Message}";
                return false;
            }
        }
    }
}
