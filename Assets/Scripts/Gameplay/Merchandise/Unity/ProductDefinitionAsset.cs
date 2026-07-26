using System;
using BigRetail.Merchandise.Domain;
using UnityEngine;

namespace BigRetail.Merchandise.Unity
{
    /// <summary>
    /// Unity authoring asset for one product definition.
    ///
    /// The asset stores editor-facing data and creates a pure domain definition
    /// for runtime systems. It does not contain stock quantity or mutable sales
    /// state.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ProductDefinition",
        menuName = "Big Retail/Merchandise/Product Definition")]
    public sealed class ProductDefinitionAsset :
        ScriptableObject
    {
        [SerializeField]
        private string productId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private string categoryId;

        [SerializeField]
        private StockUnit stockUnit =
            StockUnit.Each;


        public bool TryCreateDefinition(
            out ProductDefinition definition,
            out string error)
        {
            try
            {
                definition =
                    new ProductDefinition(
                        new ProductId(productId),
                        displayName,
                        new ProductCategoryId(categoryId),
                        stockUnit);

                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                definition = null;
                error =
                    $"{name}: {exception.Message}";

                return false;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            productId =
                NormalizeIdentifier(productId);

            categoryId =
                NormalizeIdentifier(categoryId);

            displayName =
                displayName == null
                    ? string.Empty
                    : displayName.Trim();
        }


        private static string NormalizeIdentifier(
            string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }
#endif
    }
}
