using BigRetail.Purchasing.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class EquipmentCatalogWorkspaceViewTests
    {
        private const string UxmlPath =
            "Assets/UI/Purchasing/PC/EquipmentCatalogWorkspace.uxml";


        [Test]
        public void SetModel_RendersCatalogDraftAndDeliveryPipeline()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            EquipmentCatalogWorkspaceView view =
                new EquipmentCatalogWorkspaceView(root);

            try
            {
                view.SetModel(CreateModel());

                Assert.That(
                    root.Q<VisualElement>("equipment-list").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<Label>("equipment-workspace-title").text,
                    Is.EqualTo("EQUIPMENT"));
                Assert.That(
                    root.Q<VisualElement>("equipment-draft-list").childCount,
                    Is.EqualTo(1));
                Assert.That(
                    root.Q<Label>("equipment-count").text,
                    Is.EqualTo("1 fixture"));
                Assert.That(
                    root.Q<Label>("equipment-draft-total").text,
                    Is.EqualTo("$480.00"));
                Assert.That(
                    root.Q<Label>("equipment-staged-shipments").text,
                    Is.EqualTo("STAGED IN RECEIVING  1"));
                Assert.That(
                    root.Q<Button>("equipment-place-order").enabledSelf,
                    Is.True);
                Assert.That(
                    root.Q<Button>("equipment-place-order").text,
                    Does.StartWith("PLACE BIG ORDER"));
                Assert.That(
                    root.Q<Label>(className: "equipment-card__price").text,
                    Does.StartWith("BIG WHOLESALE"));
                Assert.That(
                    root.Query<Button>(
                            className: "equipment-card__quantity-button")
                        .ToList().Count,
                    Is.EqualTo(2));
            }
            finally
            {
                view.Dispose();
            }
        }

        [Test]
        public void SetVisible_HidesAndRestoresWorkspaceRoot()
        {
            VisualTreeAsset tree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            EquipmentCatalogWorkspaceView view =
                new EquipmentCatalogWorkspaceView(root);

            try
            {
                view.SetVisible(false);
                Assert.That(
                    root.style.display.value,
                    Is.EqualTo(DisplayStyle.None));

                view.SetVisible(true);
                Assert.That(
                    root.style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));
            }
            finally
            {
                view.Dispose();
            }
        }


        private static EquipmentCatalogWorkspaceModel CreateModel()
        {
            return new EquipmentCatalogWorkspaceModel(
                new[]
                {
                    new EquipmentCatalogFilterItem("Sales Floor", 1)
                },
                new[]
                {
                    new EquipmentCatalogItem(
                        "STANDARD_SHELF",
                        "Standard Shelf",
                        "Sales Floor",
                        null,
                        24000,
                        "DELIVERS IN 2 GAME HOURS",
                        1,
                        3,
                        0,
                        2,
                        2)
                },
                new[]
                {
                    new EquipmentDraftLineItem(
                        "STANDARD_SHELF",
                        "Standard Shelf",
                        2,
                        48000,
                        "DELIVERS IN 2 GAME HOURS")
                },
                "Sales Floor",
                true,
                48000,
                100000,
                "MONDAY · 9:00 AM",
                2,
                1,
                0,
                true,
                "Plan requirements added to the draft.");
        }
    }
}
