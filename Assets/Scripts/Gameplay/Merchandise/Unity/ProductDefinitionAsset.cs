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
        private BrandDefinitionAsset brand;

        [SerializeField]
        private string productLine;

        [SerializeField]
        private string categoryId;

        [SerializeField]
        private MarketPosition marketPosition =
            MarketPosition.Standard;

        [SerializeField]
        private string packageForm;

        [Header("Presentation")]

        [Tooltip("Optional package image. A branded stub is shown when absent.")]
        [SerializeField]
        private Sprite catalogImage;

        [Tooltip(
            "Optional full-case artwork for shelves whose visible edge rises "
            + "to the left. Backstock remains graybox when absent.")]
        [SerializeField]
        private Sprite caseRisingLeftImage;

        [Tooltip(
            "Optional full-case artwork for shelves whose visible edge rises "
            + "to the right. Backstock remains graybox when absent.")]
        [SerializeField]
        private Sprite caseRisingRightImage;

        [SerializeField]
        private StockUnit stockUnit =
            StockUnit.Each;

        [Tooltip("Temporary graybox cost. Permanent supplier prices belong to Supplier Offers.")]
        [SerializeField]
        [Min(0)]
        private long wholesaleCaseCostCents;

        [Tooltip("Temporary graybox shelf price used by the opening sales loop.")]
        [SerializeField]
        [Min(0)]
        private long retailUnitPriceCents;


        public string DisplayName =>
            displayName;

        public BrandDefinitionAsset Brand =>
            brand;

        public string ProductLine =>
            productLine;

        public string CategoryId =>
            categoryId;

        public MarketPosition MarketPosition =>
            marketPosition;

        public string PackageForm =>
            packageForm;

        public Sprite CatalogImage =>
            catalogImage;

        public ProductId Id =>
            string.IsNullOrWhiteSpace(productId)
                ? default
                : new ProductId(productId);

        public Sprite CaseRisingLeftImage =>
            caseRisingLeftImage;

        public Sprite CaseRisingRightImage =>
            caseRisingRightImage;


        public Sprite GetCaseImage(
            bool risingLeft)
        {
            return risingLeft
                ? caseRisingLeftImage
                : caseRisingRightImage;
        }


        public bool TryCreateDefinition(
            out ProductDefinition definition,
            out string error)
        {
            try
            {
                BrandId resolvedBrandId = BrandId.Unbranded;

                if (brand != null)
                {
                    if (!brand.TryCreateDefinition(
                            out BrandDefinition brandDefinition,
                            out error))
                    {
                        definition = null;
                        return false;
                    }

                    resolvedBrandId = brandDefinition.Id;
                }

                string resolvedProductLine =
                    string.IsNullOrWhiteSpace(productLine)
                        ? displayName
                        : productLine;

                string resolvedPackageForm =
                    string.IsNullOrWhiteSpace(packageForm)
                        ? stockUnit.ToString()
                        : packageForm;

                definition =
                    new ProductDefinition(
                        new ProductId(productId),
                        displayName,
                        resolvedBrandId,
                        resolvedProductLine,
                        new ProductCategoryId(categoryId),
                        marketPosition,
                        resolvedPackageForm,
                        stockUnit,
                        wholesaleCaseCostCents,
                        retailUnitPriceCents);

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

            productLine =
                productLine == null
                    ? string.Empty
                    : productLine.Trim();

            packageForm =
                packageForm == null
                    ? string.Empty
                    : packageForm.Trim();

            wholesaleCaseCostCents =
                Math.Max(0, wholesaleCaseCostCents);

            retailUnitPriceCents =
                Math.Max(0, retailUnitPriceCents);
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
