using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using UnityEngine;

namespace BigRetail.Merchandise.Unity
{
    /// <summary>
    /// Unity authoring asset that assembles product assets into one runtime
    /// product catalog.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ProductCatalog",
        menuName = "Big Retail/Merchandise/Product Catalog")]
    public sealed class ProductCatalogAsset :
        ScriptableObject
    {
        [SerializeField]
        private ProductDefinitionAsset[] products =
            Array.Empty<ProductDefinitionAsset>();


        public bool TryCreateCatalog(
            out ProductCatalog catalog,
            out string error)
        {
            List<ProductDefinition> definitions =
                new List<ProductDefinition>(
                    products == null
                        ? 0
                        : products.Length);

            if (products == null)
            {
                catalog = null;
                error =
                    $"{name}: Product asset collection is missing.";

                return false;
            }

            for (int index = 0;
                 index < products.Length;
                 index++)
            {
                ProductDefinitionAsset product =
                    products[index];

                if (product == null)
                {
                    catalog = null;
                    error =
                        $"{name}: Product entry {index} is missing.";

                    return false;
                }

                if (!product.TryCreateDefinition(
                        out ProductDefinition definition,
                        out error))
                {
                    catalog = null;
                    return false;
                }

                definitions.Add(definition);
            }

            try
            {
                catalog =
                    new ProductCatalog(definitions);

                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                catalog = null;
                error =
                    $"{name}: {exception.Message}";

                return false;
            }
        }
    }
}
