using BigRetail.Map.Unity.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FixtureShelfMaskGeometryTests
    {
        [Test]
        public void AuthoredShelfMask_DividesItsLongAxisIntoStableFrontages()
        {
            Sprite shelfMask = CreateShelfMask();

            try
            {
                Assert.That(
                    FixtureShelfMaskGeometry.TryCreate(
                        shelfMask,
                        out FixtureShelfMaskGeometry geometry),
                    Is.True);

                for (int index = 0; index < 4; index++)
                {
                    Vector2 center =
                        geometry.GetFrontageCenter(index, 4);

                    Assert.That(
                        FixtureShelfMaskGeometry.ContainsLocalPoint(
                            shelfMask,
                            center),
                        Is.True,
                        $"Frontage {index} should remain inside the authored shelf mask.");

                    Assert.That(
                        geometry.ResolveVisualFrontageIndex(center, 4),
                        Is.EqualTo(index));
                }
            }
            finally
            {
                DestroyShelfMask(shelfMask);
            }
        }


        [Test]
        public void AuthoredShelfMask_HitTestRejectsPointsOutsideItsMesh()
        {
            Sprite shelfMask = CreateShelfMask();

            try
            {
                Assert.That(
                    FixtureShelfMaskGeometry.ContainsLocalPoint(
                        shelfMask,
                        Vector2.zero),
                    Is.True);
                Assert.That(
                    FixtureShelfMaskGeometry.ContainsLocalPoint(
                        shelfMask,
                        new Vector2(0f, 4f)),
                    Is.False);
            }
            finally
            {
                DestroyShelfMask(shelfMask);
            }
        }


        private static Sprite CreateShelfMask()
        {
            Texture2D texture = new Texture2D(8, 8);
            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 8f, 8f),
                    new Vector2(0.5f, 0.5f),
                    1f);

            sprite.OverrideGeometry(
                new[]
                {
                    new Vector2(-4f, -2f),
                    new Vector2(-4f, -1f),
                    new Vector2(4f, 2f),
                    new Vector2(4f, 1f)
                },
                new ushort[]
                {
                    0, 1, 2,
                    0, 2, 3
                });

            return sprite;
        }


        private static void DestroyShelfMask(Sprite shelfMask)
        {
            Texture2D texture = shelfMask.texture;
            Object.DestroyImmediate(shelfMask);
            Object.DestroyImmediate(texture);
        }
    }
}
