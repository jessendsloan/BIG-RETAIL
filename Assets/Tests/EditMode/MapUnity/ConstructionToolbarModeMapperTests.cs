using BigRetail.Construction.Unity.Tools;
using BigRetail.Construction.Unity.UI.PC;
using NUnit.Framework;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class ConstructionToolbarModeMapperTests
    {
        [TestCase(
            ConstructionToolMode.None,
            ConstructionToolbarSection.None)]
        [TestCase(
            ConstructionToolMode.BuildWalls,
            ConstructionToolbarSection.Walls)]
        [TestCase(
            ConstructionToolMode.BuildFloors,
            ConstructionToolbarSection.Floors)]
        [TestCase(
            ConstructionToolMode.DemolishWalls,
            ConstructionToolbarSection.Demolition)]
        [TestCase(
            ConstructionToolMode.DemolishFloors,
            ConstructionToolbarSection.Demolition)]
        public void ToSection_MapsAuthoritativeToolMode(
            ConstructionToolMode mode,
            ConstructionToolbarSection expectedSection)
        {
            Assert.That(
                ConstructionToolbarModeMapper.ToSection(mode),
                Is.EqualTo(expectedSection));
        }
    }
}
