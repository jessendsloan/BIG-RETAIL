using BigRetail.StoreLayouts;
using BigRetail.StoreLayouts.Unity;
using NUnit.Framework;
using UnityEditor;

namespace BigRetail.StoreLayouts.Unity.Tests
{
    public sealed class FrankOpeningLayoutNavigationTests
    {
        private const string LayoutPath =
            "Assets/Design/StoreLayouts/FrankStoreLayoutV1.asset";


        [Test]
        public void OpeningLayout_ConnectsTrailerToStoreWithSidewalks()
        {
            StoreLayoutAsset asset =
                AssetDatabase.LoadAssetAtPath<StoreLayoutAsset>(
                    LayoutPath);

            Assert.That(asset, Is.Not.Null);

            StoreLayoutData layout = asset.CreateRuntimeCopy();

            Assert.That(layout.Sidewalks.Count, Is.EqualTo(125));
            CollectionAssert.Contains(
                layout.Sidewalks,
                new StoreCellData(-20, 22, 0));
            CollectionAssert.Contains(
                layout.Sidewalks,
                new StoreCellData(-17, 27, 0));
            CollectionAssert.Contains(
                layout.Sidewalks,
                new StoreCellData(-20, 28, 0));
        }
    }
}
