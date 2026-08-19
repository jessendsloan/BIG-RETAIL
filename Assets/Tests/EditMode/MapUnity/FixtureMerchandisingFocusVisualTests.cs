using BigRetail.Map.Unity.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FixtureMerchandisingFocusVisualTests
    {
        [Test]
        public void SelectedFixtureKeepsColorAndRendersAboveWorldDepth()
        {
            Color baseColor = new Color(0.8f, 0.7f, 0.6f, 0.9f);

            Color focusedColor =
                FixtureViewSystem.ResolveMerchandisingFocusColor(
                    baseColor,
                    focusIsActive: true,
                    isFocusedFixture: true);

            int focusedOrder =
                FixtureViewSystem.ResolveMerchandisingFocusSortingOrder(
                    184,
                    focusIsActive: true,
                    isFocusedFixture: true);

            Assert.That(focusedColor, Is.EqualTo(baseColor));
            Assert.That(focusedOrder, Is.EqualTo(1184));
        }

        [Test]
        public void SurroundingFixtureIsSoftenedWithoutChangingDepth()
        {
            Color baseColor = new Color(0.8f, 0.7f, 0.6f, 0.9f);

            Color softenedColor =
                FixtureViewSystem.ResolveMerchandisingFocusColor(
                    baseColor,
                    focusIsActive: true,
                    isFocusedFixture: false);

            int softenedOrder =
                FixtureViewSystem.ResolveMerchandisingFocusSortingOrder(
                    184,
                    focusIsActive: true,
                    isFocusedFixture: false);

            Assert.That(softenedColor.r, Is.EqualTo(baseColor.r));
            Assert.That(softenedColor.g, Is.EqualTo(baseColor.g));
            Assert.That(softenedColor.b, Is.EqualTo(baseColor.b));
            Assert.That(softenedColor.a, Is.EqualTo(0.252f).Within(0.0001f));
            Assert.That(softenedOrder, Is.EqualTo(184));
        }

        [Test]
        public void ClearingFocusRestoresBaseVisuals()
        {
            Color baseColor = new Color(0.8f, 0.7f, 0.6f, 0.9f);

            Color restoredColor =
                FixtureViewSystem.ResolveMerchandisingFocusColor(
                    baseColor,
                    focusIsActive: false,
                    isFocusedFixture: false);

            int restoredOrder =
                FixtureViewSystem.ResolveMerchandisingFocusSortingOrder(
                    184,
                    focusIsActive: false,
                    isFocusedFixture: false);

            Assert.That(restoredColor, Is.EqualTo(baseColor));
            Assert.That(restoredOrder, Is.EqualTo(184));
        }
    }
}
