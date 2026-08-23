using System.Collections.Generic;
using BigRetail.Merchandise.Domain;
using BigRetail.Purchasing.Domain;
using BigRetail.Purchasing.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class PurchasingWorkspaceViewTests
    {
        private const string UxmlPath =
            "Assets/UI/Purchasing/PC/PurchasingWorkspace.uxml";


        [Test]
        public void SetModel_RendersProductAndSupplierDraft()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            PurchasingWorkspaceView view = new PurchasingWorkspaceView(root);

            try
            {
                view.SetModel(CreateModel());

                Assert.That(
                    root.Q<VisualElement>("product-list").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<VisualElement>("draft-list").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<Label>("product-count").text,
                    Is.EqualTo("1 product"));
                Assert.That(
                    root.Q<Label>("draft-grand-total").text,
                    Is.EqualTo("$12.00"));
                Assert.That(
                    root.Query<Button>(className: "quantity-control__button")
                        .ToList().Count,
                    Is.EqualTo(2));
            }
            finally
            {
                view.Dispose();
            }
        }

        [Test]
        public void SetModel_DoesNotRenderSupplierWithoutStagedLines()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            PurchasingWorkspaceView view = new PurchasingWorkspaceView(root);

            try
            {
                SupplierId supplierId = new SupplierId("BIG");
                PurchasingDraftItem emptyDraft =
                    new PurchasingDraftItem(
                        supplierId,
                        "BIG Wholesale",
                        Color.red,
                        "Within 3 hours",
                        0,
                        0,
                        0,
                        new List<PurchasingDraftLineItem>());

                view.SetModel(CreateModel(new[] { emptyDraft }, 0));

                Assert.That(
                    root.Q<VisualElement>("draft-list").childCount,
                    Is.Zero);
                Assert.That(
                    root.Q<Label>("draft-empty-state").style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));
            }
            finally
            {
                view.Dispose();
            }
        }

        [Test]
        public void SetModel_RendersTemporalOrderReview()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            PurchasingWorkspaceView view = new PurchasingWorkspaceView(root);

            try
            {
                PurchasingReviewModel review =
                    new PurchasingReviewModel(
                        false,
                        "PLACING MONDAY · 9:00 AM",
                        new[]
                        {
                            new PurchasingReviewOrderItem(
                                null,
                                "BIG Wholesale",
                                Color.red,
                                "TODAY · 12:00 PM",
                                1200,
                                "READY · NO ORDER MINIMUM",
                                true,
                                new[]
                                {
                                    new PurchasingDraftLineItem(
                                        "Bright Cola",
                                        1,
                                        1200)
                                })
                        },
                        1200,
                        string.Empty);

                view.SetModel(CreateModel(review: review));

                Assert.That(
                    root.Q<VisualElement>("order-review-overlay")
                        .style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));
                Assert.That(
                    root.Q<VisualElement>("order-review-list").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<Label>("order-review-timing").text,
                    Is.EqualTo("PLACING MONDAY · 9:00 AM"));
                Assert.That(
                    root.Q<Button>("place-orders-button").enabledSelf,
                    Is.True);
            }
            finally
            {
                view.Dispose();
            }
        }

        [Test]
        public void SetVisible_PreservesWorkspaceAcrossFiveCloseCycles()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            PurchasingWorkspaceView view = new PurchasingWorkspaceView(root);

            try
            {
                for (int cycle = 0; cycle < 5; cycle++)
                {
                    view.SetVisible(true);
                    Assert.That(
                        root.style.display.value,
                        Is.EqualTo(DisplayStyle.Flex));

                    view.SetVisible(false);
                    Assert.That(
                        root.style.display.value,
                        Is.EqualTo(DisplayStyle.None));
                }

                view.SetVisible(true);
                view.SetModel(CreateModel());

                Assert.That(
                    root.Q<Label>("product-count").text,
                    Is.EqualTo("1 product"));
                Assert.That(
                    root.Q<Button>("close-purchasing-button"),
                    Is.Not.Null);
            }
            finally
            {
                view.Dispose();
            }
        }


        private static PurchasingWorkspaceModel CreateModel(
            IReadOnlyList<PurchasingDraftItem> drafts = null,
            long grandTotalCents = 1200,
            PurchasingReviewModel review = null)
        {
            SupplierId supplierId = new SupplierId("BIG");
            SupplierOfferId offerId = new SupplierOfferId("BIG-COLA");
            PurchasingOfferItem offer =
                new PurchasingOfferItem(
                    offerId,
                    supplierId,
                    "BIG Wholesale",
                    Color.red,
                    12,
                    1200,
                    100m,
                    "Within 3 hours",
                    1,
                    true);

            return new PurchasingWorkspaceModel(
                new[]
                {
                    new PurchasingFilterItem(string.Empty, "All products", 1),
                    new PurchasingFilterItem("BEVERAGES", "Beverages", 1)
                },
                new[]
                {
                    new PurchasingSupplierFilterItem(
                        supplierId,
                        "BIG Wholesale",
                        Color.red)
                },
                new[]
                {
                    new PurchasingProductItem(
                        new ProductId("BRIGHT-COLA-20OZ"),
                        "Bright Beverage Co.",
                        "Bright Cola",
                        "Cola",
                        "20 oz Bottle",
                        "Beverages",
                        "Standard",
                        null,
                        Color.red,
                        new[] { offer })
                },
                drafts
                    ?? new[]
                    {
                        new PurchasingDraftItem(
                            supplierId,
                            "BIG Wholesale",
                            Color.red,
                            "Within 3 hours",
                            0,
                            1200,
                            0,
                            new List<PurchasingDraftLineItem>
                            {
                                new PurchasingDraftLineItem(
                                    "Bright Cola",
                                    1,
                                    1200)
                            })
                    },
                string.Empty,
                null,
                grandTotalCents,
                "MONDAY · 9:00 AM",
                review);
        }
    }
}
