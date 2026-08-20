using System;
using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using BigRetail.Merchandise.Unity;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity.UI
{
    /// <summary>
    /// Builds a read-only presentation of the authored Brand and Supplier catalogs.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CommercialDirectoryDocumentHost))]
    public sealed class CommercialDirectoryPresenter : MonoBehaviour
    {
        [SerializeField]
        private CommercialDirectoryDocumentHost documentHost;

        [SerializeField]
        private CommercialCatalogAsset commercialCatalog;

        private readonly List<CommercialBrandItem> brands =
            new List<CommercialBrandItem>();
        private readonly List<CommercialSupplierItem> suppliers =
            new List<CommercialSupplierItem>();

        private CommercialDirectoryView boundView;
        private CommercialDirectorySection selectedSection =
            CommercialDirectorySection.Brands;
        private string initializationError;


        private void Reset()
        {
            documentHost = GetComponent<CommercialDirectoryDocumentHost>();
        }

        private void Awake()
        {
            if (documentHost == null)
            {
                documentHost = GetComponent<CommercialDirectoryDocumentHost>();
            }

            InitializeDirectory();
        }

        private void OnEnable()
        {
            if (documentHost == null)
            {
                return;
            }

            documentHost.ViewReady += HandleViewReady;

            if (documentHost.HasView)
            {
                BindView(documentHost.View);
            }
        }

        private void OnDisable()
        {
            if (documentHost != null)
            {
                documentHost.ViewReady -= HandleViewReady;
            }

            UnbindView();
        }


        public void ShowSection(CommercialDirectorySection section)
        {
            if (!Enum.IsDefined(typeof(CommercialDirectorySection), section))
            {
                throw new ArgumentOutOfRangeException(nameof(section), section, null);
            }

            selectedSection = section;
            RefreshView();
        }


        private void InitializeDirectory()
        {
            brands.Clear();
            suppliers.Clear();
            initializationError = string.Empty;

            if (commercialCatalog == null)
            {
                initializationError =
                    "No commercial catalog is assigned to the directory.";
                Debug.LogError(initializationError, this);
                return;
            }

            if (!commercialCatalog.TryCreateCatalog(
                    out CommercialCatalog catalog,
                    out initializationError))
            {
                Debug.LogError(initializationError, commercialCatalog);
                return;
            }

            if (!BuildBrands(catalog, out initializationError)
                || !BuildSuppliers(catalog, out initializationError))
            {
                brands.Clear();
                suppliers.Clear();
                Debug.LogError(initializationError, commercialCatalog);
            }
        }

        private bool BuildBrands(CommercialCatalog catalog, out string error)
        {
            IReadOnlyList<BrandDefinitionAsset> authoredBrands =
                commercialCatalog.BrandCatalog.Brands;

            for (int index = 0; index < authoredBrands.Count; index++)
            {
                BrandDefinitionAsset asset = authoredBrands[index];

                if (asset == null)
                {
                    error = $"Brand directory entry {index} is missing.";
                    return false;
                }

                if (!asset.TryCreateDefinition(
                        out BrandDefinition brand,
                        out error))
                {
                    return false;
                }

                List<string> productNames = new List<string>();

                foreach (
                    ProductDefinition product
                    in catalog.Products.EnumerateDefinitions())
                {
                    if (product.BrandId == brand.Id)
                    {
                        productNames.Add(product.DisplayName);
                    }
                }

                brands.Add(
                    new CommercialBrandItem(
                        brand.DisplayName,
                        asset.Identity,
                        asset.Logo,
                        asset.AccentColor,
                        productNames));
            }

            error = string.Empty;
            return true;
        }

        private bool BuildSuppliers(
            CommercialCatalog catalog,
            out string error)
        {
            IReadOnlyList<SupplierDefinitionAsset> authoredSuppliers =
                commercialCatalog.SupplierCatalog.Suppliers;

            for (int index = 0; index < authoredSuppliers.Count; index++)
            {
                SupplierDefinitionAsset asset = authoredSuppliers[index];

                if (asset == null)
                {
                    error = $"Supplier directory entry {index} is missing.";
                    return false;
                }

                if (!asset.TryCreateDefinition(
                        out SupplierDefinition supplier,
                        out error))
                {
                    return false;
                }

                List<string> productNames = new List<string>();
                HashSet<ProductId> includedProducts = new HashSet<ProductId>();

                foreach (
                    SupplierOfferDefinition offer
                    in catalog.Offers.EnumerateForSupplier(supplier.Id))
                {
                    if (includedProducts.Add(offer.ProductId))
                    {
                        productNames.Add(
                            catalog.Products.GetRequired(offer.ProductId)
                                .DisplayName);
                    }
                }

                suppliers.Add(
                    new CommercialSupplierItem(
                        supplier.DisplayName,
                        supplier.Specialty,
                        asset.Description,
                        supplier.DeliveryRule.GetPlayerFacingSummary(),
                        supplier.MinimumOrderCents,
                        asset.Logo,
                        asset.AccentColor,
                        productNames));
            }

            error = string.Empty;
            return true;
        }

        private void HandleViewReady(CommercialDirectoryView view)
        {
            BindView(view);
        }

        private void BindView(CommercialDirectoryView view)
        {
            if (boundView == view)
            {
                RefreshView();
                return;
            }

            UnbindView();
            boundView = view;
            boundView.SectionRequested += HandleSectionRequested;
            RefreshView();
        }

        private void UnbindView()
        {
            if (boundView == null)
            {
                return;
            }

            boundView.SectionRequested -= HandleSectionRequested;
            boundView = null;
        }

        private void HandleSectionRequested(CommercialDirectorySection section)
        {
            ShowSection(section);
        }

        private void RefreshView()
        {
            if (boundView == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(initializationError))
            {
                boundView.ShowError(initializationError);
                return;
            }

            boundView.SetModel(
                new CommercialDirectoryModel(
                    selectedSection,
                    brands,
                    suppliers));
        }
    }
}
