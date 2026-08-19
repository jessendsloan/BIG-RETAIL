using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Purchasing.Unity.UI
{
    public enum CommercialDirectorySection
    {
        Brands = 0,
        Suppliers = 1
    }


    public sealed class CommercialBrandItem
    {
        public string DisplayName { get; }

        public string Identity { get; }

        public Sprite Logo { get; }

        public Color AccentColor { get; }

        public IReadOnlyList<string> ProductNames { get; }


        public CommercialBrandItem(
            string displayName,
            string identity,
            Sprite logo,
            Color accentColor,
            IReadOnlyList<string> productNames)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(
                nameof(displayName));
            Identity = identity ?? string.Empty;
            Logo = logo;
            AccentColor = accentColor;
            ProductNames = productNames ?? throw new ArgumentNullException(
                nameof(productNames));
        }
    }


    public sealed class CommercialSupplierItem
    {
        public string DisplayName { get; }

        public string Specialty { get; }

        public string Description { get; }

        public string DeliverySummary { get; }

        public long MinimumOrderCents { get; }

        public Sprite Logo { get; }

        public Color AccentColor { get; }

        public IReadOnlyList<string> ProductNames { get; }


        public CommercialSupplierItem(
            string displayName,
            string specialty,
            string description,
            string deliverySummary,
            long minimumOrderCents,
            Sprite logo,
            Color accentColor,
            IReadOnlyList<string> productNames)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(
                nameof(displayName));
            Specialty = specialty ?? string.Empty;
            Description = description ?? string.Empty;
            DeliverySummary = deliverySummary ?? string.Empty;
            MinimumOrderCents = minimumOrderCents;
            Logo = logo;
            AccentColor = accentColor;
            ProductNames = productNames ?? throw new ArgumentNullException(
                nameof(productNames));
        }
    }


    public sealed class CommercialDirectoryModel
    {
        public CommercialDirectorySection SelectedSection { get; }

        public IReadOnlyList<CommercialBrandItem> Brands { get; }

        public IReadOnlyList<CommercialSupplierItem> Suppliers { get; }


        public CommercialDirectoryModel(
            CommercialDirectorySection selectedSection,
            IReadOnlyList<CommercialBrandItem> brands,
            IReadOnlyList<CommercialSupplierItem> suppliers)
        {
            if (!Enum.IsDefined(typeof(CommercialDirectorySection), selectedSection))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectedSection),
                    selectedSection,
                    "The commercial directory section is not supported.");
            }

            SelectedSection = selectedSection;
            Brands = brands ?? throw new ArgumentNullException(nameof(brands));
            Suppliers = suppliers ?? throw new ArgumentNullException(
                nameof(suppliers));
        }
    }
}
