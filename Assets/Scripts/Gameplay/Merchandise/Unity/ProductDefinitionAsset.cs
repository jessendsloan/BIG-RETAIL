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

        [Tooltip(
            "Optional on-shelf fullness images for a visible edge that rises "
            + "to the left, ordered from least to most full.")]
        [SerializeField]
        private Sprite[] onShelfRisingLeftImages =
            Array.Empty<Sprite>();

        [Tooltip(
            "Optional on-shelf fullness images for a visible edge that rises "
            + "to the right, ordered from least to most full.")]
        [SerializeField]
        private Sprite[] onShelfRisingRightImages =
            Array.Empty<Sprite>();

        [Tooltip(
            "Optional individual-package artwork used away from a shelf "
            + "when the visible edge rises to the left.")]
        [SerializeField]
        private Sprite offShelfRisingLeftImage;

        [Tooltip(
            "Optional individual-package artwork used away from a shelf "
            + "when the visible edge rises to the right.")]
        [SerializeField]
        private Sprite offShelfRisingRightImage;

        [SerializeField]
        private StockUnit stockUnit =
            StockUnit.Each;

        [Tooltip(
            "Physical sellable units held by one planogram frontage slot.")]
        [SerializeField]
        [Min(1)]
        private int displayUnitsPerFrontageUnit =
            ProductDefinition.DefaultDisplayUnitsPerFrontageUnit;

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

        public int OnShelfImageCount =>
            Math.Max(
                GetSpriteCount(onShelfRisingLeftImages),
                GetSpriteCount(onShelfRisingRightImages));

        public Sprite OffShelfRisingLeftImage =>
            offShelfRisingLeftImage;

        public Sprite OffShelfRisingRightImage =>
            offShelfRisingRightImage;

        public int DisplayUnitsPerFrontageUnit =>
            displayUnitsPerFrontageUnit > 0
                ? displayUnitsPerFrontageUnit
                : ProductDefinition.DefaultDisplayUnitsPerFrontageUnit;


        public Sprite GetCaseImage(
            bool risingLeft)
        {
            return risingLeft
                ? caseRisingLeftImage
                : caseRisingRightImage;
        }


        public Sprite GetOnShelfImage(
            bool risingLeft,
            float fillRatio)
        {
            Sprite[] images =
                risingLeft
                    ? onShelfRisingLeftImages
                    : onShelfRisingRightImages;
            int imageCount = GetSpriteCount(images);

            if (imageCount == 0 || fillRatio <= 0f)
            {
                return null;
            }

            int imageIndex =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        Mathf.Clamp01(fillRatio)
                        * imageCount)
                    - 1,
                    0,
                    imageCount - 1);

            return images[imageIndex];
        }


        public Sprite GetOffShelfImage(
            bool risingLeft)
        {
            return risingLeft
                ? offShelfRisingLeftImage
                : offShelfRisingRightImage;
        }


        private static int GetSpriteCount(
            Sprite[] sprites)
        {
            return sprites == null
                ? 0
                : sprites.Length;
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
                        retailUnitPriceCents,
                        DisplayUnitsPerFrontageUnit);

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

            if (displayUnitsPerFrontageUnit <= 0)
            {
                displayUnitsPerFrontageUnit =
                    ProductDefinition.DefaultDisplayUnitsPerFrontageUnit;
            }

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
