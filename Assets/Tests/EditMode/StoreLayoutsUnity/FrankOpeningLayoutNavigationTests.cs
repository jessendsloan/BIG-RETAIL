using System.Collections.Generic;
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

            Assert.That(layout.Sidewalks.Count, Is.EqualTo(486));
            HashSet<StoreCellData> walkableCells =
                new HashSet<StoreCellData>(layout.Sidewalks);

            for (int x = -67; x <= 28; x++)
            {
                for (int y = 25; y <= 27; y++)
                {
                    CollectionAssert.Contains(
                        walkableCells,
                        new StoreCellData(x, y, 0));
                }
            }

            for (int x = 11; x <= 14; x++)
            {
                for (int y = 28; y <= 51; y++)
                {
                    CollectionAssert.Contains(
                        walkableCells,
                        new StoreCellData(x, y, 0));
                }
            }

            CollectionAssert.Contains(
                walkableCells,
                new StoreCellData(12, 52, 0));
            CollectionAssert.Contains(
                walkableCells,
                new StoreCellData(-8, 47, 0));
            CollectionAssert.DoesNotContain(
                walkableCells,
                new StoreCellData(-20, 22, 0));
            CollectionAssert.DoesNotContain(
                walkableCells,
                new StoreCellData(11, 52, 0));
            CollectionAssert.DoesNotContain(
                walkableCells,
                new StoreCellData(-7, 47, 0));
        }
    }
}
