using BigRetail.Purchasing.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class CommercialDirectoryViewTests
    {
        private const string UxmlPath =
            "Assets/UI/Purchasing/PC/CommercialDirectory.uxml";


        [Test]
        public void SetModel_RendersSelectedBrandAndSupplierCatalogs()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            CommercialDirectoryView view = new CommercialDirectoryView(root);

            try
            {
                CommercialDirectoryModel brandsModel = CreateModel(
                    CommercialDirectorySection.Brands);
                view.SetModel(brandsModel);

                Assert.That(
                    root.Q<VisualElement>("directory-content").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<Label>("directory-section-title").text,
                    Is.EqualTo("Opening brands"));
                Assert.That(
                    root.Q<Label>("directory-section-count").text,
                    Is.EqualTo("1 BRAND"));

                CommercialDirectoryModel suppliersModel = CreateModel(
                    CommercialDirectorySection.Suppliers);
                view.SetModel(suppliersModel);

                Assert.That(
                    root.Q<VisualElement>("directory-content").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<Label>("directory-section-title").text,
                    Is.EqualTo("Opening suppliers"));
                Assert.That(
                    root.Q<Label>("directory-section-count").text,
                    Is.EqualTo("1 SUPPLIER"));
                Assert.That(
                    root.Query<VisualElement>(className: "supplier-card")
                        .ToList().Count,
                    Is.EqualTo(1));
            }
            finally
            {
                view.Dispose();
            }
        }


        private static CommercialDirectoryModel CreateModel(
            CommercialDirectorySection selectedSection)
        {
            return new CommercialDirectoryModel(
                selectedSection,
                new[]
                {
                    new CommercialBrandItem(
                        "Bright Beverage Co.",
                        "Ubiquitous mainstream beverage company",
                        null,
                        Color.red,
                        new[] { "Bright Cola" })
                },
                new[]
                {
                    new CommercialSupplierItem(
                        "BIG Wholesale",
                        "Broadline emergency wholesaler",
                        "Small packs and same-day certainty.",
                        "Within 3 hours",
                        0,
                        null,
                        Color.red,
                        new[] { "Bright Cola" })
                });
        }
    }
}
