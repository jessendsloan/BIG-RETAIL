using BigRetail.Map.Domain;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Map.Unity.Walls.Tests
{
    public sealed class WallWorldOrientationShadingTests
    {
        [Test]
        public void Resolve_NorthWestWall_PreservesAuthoredColor()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthWest);

            Color tint =
                WallWorldOrientationShading.Resolve(edge);

            Assert.That(
                tint,
                Is.EqualTo(Color.white));
        }


        [Test]
        public void Resolve_NorthEastWall_UsesMeasuredGlobalShadeTint()
        {
            CellEdge edge =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthEast);

            Color tint =
                WallWorldOrientationShading.Resolve(edge);

            Assert.That(tint.r, Is.GreaterThan(tint.g));
            Assert.That(tint.g, Is.EqualTo(tint.b));
            Assert.That(tint.a, Is.EqualTo(1f));
        }


        [Test]
        public void Resolve_OppositeEdgeDescriptions_KeepPhysicalWallShade()
        {
            CellEdge northEast =
                new CellEdge(
                    new GridPosition(4, 7),
                    CellEdgeDirection.NorthEast);

            CellEdge samePhysicalEdge =
                new CellEdge(
                    new GridPosition(5, 7),
                    CellEdgeDirection.SouthWest);

            Assert.That(
                samePhysicalEdge,
                Is.EqualTo(northEast));

            Assert.That(
                WallWorldOrientationShading.Resolve(samePhysicalEdge),
                Is.EqualTo(
                    WallWorldOrientationShading.Resolve(northEast)));
        }
    }
}
