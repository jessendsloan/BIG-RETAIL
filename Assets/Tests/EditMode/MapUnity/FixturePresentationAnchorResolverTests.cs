using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using BigRetail.Map.Unity.View;
using BigRetail.Map.Unity.Walls;
using BigRetail.Map.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class FixturePresentationAnchorResolverTests
    {
        private static readonly FixtureDefinition ShelfDefinition =
            new FixtureDefinition(
                new FixtureDefinitionId("STANDARD_SHELF"),
                "Standard Shelf",
                widthInCells: 2,
                depthInCells: 1);

        private static readonly FixtureDefinition HalfShelfDefinition =
            new FixtureDefinition(
                new FixtureDefinitionId("HALF_SHELF"),
                "Half Shelf",
                widthInCells: 2,
                depthInCells: 1,
                new FixtureAccessProfile(
                    FixtureAccessMode.None,
                    FixtureAccessMode.None,
                    FixtureAccessMode.CustomerBrowse,
                    FixtureAccessMode.None));

        private static readonly IsometricMapFootprint MapFootprint =
            new IsometricMapFootprint(
                minimumX: 0,
                minimumY: 0,
                maximumX: 9,
                maximumY: 9);


        [TestCase(IsometricViewOrientation.North, 3, 4)]
        [TestCase(IsometricViewOrientation.East, 4, 4)]
        [TestCase(IsometricViewOrientation.South, 4, 4)]
        [TestCase(IsometricViewOrientation.West, 3, 4)]
        public void ResolveViewerNearestCell_HorizontalShelfTracksCamera(
            IsometricViewOrientation viewOrientation,
            int expectedX,
            int expectedY)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    ShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.North);

            GridPosition result =
                FixturePresentationAnchorResolver
                    .ResolveViewerNearestCell(
                        footprint,
                        CreateProjection(viewOrientation));

            Assert.That(
                result,
                Is.EqualTo(
                    new GridPosition(
                        expectedX,
                        expectedY,
                        0)));
        }


        [TestCase(IsometricViewOrientation.North, 3, 4)]
        [TestCase(IsometricViewOrientation.East, 3, 4)]
        [TestCase(IsometricViewOrientation.South, 3, 5)]
        [TestCase(IsometricViewOrientation.West, 3, 5)]
        public void ResolveViewerNearestCell_VerticalShelfTracksCamera(
            IsometricViewOrientation viewOrientation,
            int expectedX,
            int expectedY)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    ShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.East);

            GridPosition result =
                FixturePresentationAnchorResolver
                    .ResolveViewerNearestCell(
                        footprint,
                        CreateProjection(viewOrientation));

            Assert.That(
                result,
                Is.EqualTo(
                    new GridPosition(
                        expectedX,
                        expectedY,
                        0)));
        }


        [Test]
        public void ResolveViewerNearestCell_RejectsMissingFootprint()
        {
            Assert.Throws<ArgumentNullException>(
                () => FixturePresentationAnchorResolver
                    .ResolveViewerNearestCell(
                        null,
                        CreateProjection(
                            IsometricViewOrientation.North)));
        }


        [Test]
        public void ResolveViewerNearestCell_RejectsMissingProjection()
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    ShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.North);

            Assert.Throws<ArgumentNullException>(
                () => FixturePresentationAnchorResolver
                    .ResolveViewerNearestCell(
                        footprint,
                        null));
        }


        [TestCase(IsometricViewOrientation.North, 3, 4)]
        [TestCase(IsometricViewOrientation.East, 4, 4)]
        [TestCase(IsometricViewOrientation.South, 3, 4)]
        [TestCase(IsometricViewOrientation.West, 4, 4)]
        public void ResolveWholeFixtureSortingCell_OneSidedShelfTracksFacingExtreme(
            IsometricViewOrientation viewOrientation,
            int expectedX,
            int expectedY)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    HalfShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.North);

            GridPosition result =
                FixturePresentationAnchorResolver
                    .ResolveWholeFixtureSortingCell(
                        HalfShelfDefinition,
                        footprint,
                        CreateProjection(viewOrientation));

            Assert.That(
                result,
                Is.EqualTo(
                    new GridPosition(
                        expectedX,
                        expectedY,
                        0)));
        }


        [TestCase(IsometricViewOrientation.North, true)]
        [TestCase(IsometricViewOrientation.East, true)]
        [TestCase(IsometricViewOrientation.South, false)]
        [TestCase(IsometricViewOrientation.West, false)]
        public void ResolveWholeFixtureSortingCell_StaysWhollyOnExpectedSideOfBackWall(
            IsometricViewOrientation viewOrientation,
            bool fixtureRendersAfterWall)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    HalfShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.North);

            IsometricViewProjection projection =
                CreateProjection(viewOrientation);

            GridPosition sortingCell =
                FixturePresentationAnchorResolver
                    .ResolveWholeFixtureSortingCell(
                        HalfShelfDefinition,
                        footprint,
                        projection);

            int fixtureOrder =
                IsometricRenderOrderResolver.ResolveCell(
                    projection.ToDisplayCell(sortingCell));

            for (int x = 3; x <= 4; x++)
            {
                CellEdge displayWall =
                    projection.ToDisplayEdge(
                        new CellEdge(
                            new GridPosition(x, 4, 0),
                            CellEdgeDirection.NorthWest));

                int wallOrder =
                    WallRenderOrderResolver.ResolveWall(
                        displayWall);

                if (fixtureRendersAfterWall)
                {
                    Assert.That(
                        fixtureOrder,
                        Is.GreaterThan(wallOrder));
                }
                else
                {
                    Assert.That(
                        fixtureOrder,
                        Is.LessThan(wallOrder));
                }
            }
        }


        [TestCase(IsometricViewOrientation.North, true)]
        [TestCase(IsometricViewOrientation.East, true)]
        [TestCase(IsometricViewOrientation.South, false)]
        [TestCase(IsometricViewOrientation.West, false)]
        public void ResolveWholeFixtureSortingCell_LowBackWallMatchesFullWallOcclusion(
            IsometricViewOrientation viewOrientation,
            bool fixtureRendersAfterWall)
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    HalfShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.North);

            IsometricViewProjection projection =
                CreateProjection(viewOrientation);

            GridPosition sortingCell =
                FixturePresentationAnchorResolver
                    .ResolveWholeFixtureSortingCell(
                        HalfShelfDefinition,
                        footprint,
                        projection);

            int fixtureOrder =
                IsometricRenderOrderResolver.ResolveCell(
                    projection.ToDisplayCell(sortingCell));

            for (int x = 3; x <= 4; x++)
            {
                CellEdge displayWall =
                    projection.ToDisplayEdge(
                        new CellEdge(
                            new GridPosition(x, 4, 0),
                            CellEdgeDirection.NorthWest));

                int lowWallOrder =
                    WallRenderOrderResolver.ResolveWall(
                        displayWall,
                        WallPresentationHeight.Low);

                if (fixtureRendersAfterWall)
                {
                    Assert.That(
                        fixtureOrder,
                        Is.GreaterThan(lowWallOrder));
                }
                else
                {
                    Assert.That(
                        fixtureOrder,
                        Is.LessThan(lowWallOrder));
                }
            }
        }


        [Test]
        public void ResolveWholeFixtureSortingCell_DoubleSidedShelfUsesViewerNearestCell()
        {
            FixtureFootprint footprint =
                FixtureFootprintResolver.Resolve(
                    ShelfDefinition,
                    new GridPosition(3, 4, 0),
                    FixtureOrientation.North);

            IsometricViewProjection projection =
                CreateProjection(
                    IsometricViewOrientation.South);

            Assert.That(
                FixturePresentationAnchorResolver
                    .ResolveWholeFixtureSortingCell(
                        ShelfDefinition,
                        footprint,
                        projection),
                Is.EqualTo(
                    FixturePresentationAnchorResolver
                        .ResolveViewerNearestCell(
                            footprint,
                            projection)));
        }


        [Test]
        public void CalculateViewerNearestCornerWorld_UsesBottomDiamondPoint()
        {
            GameObject gridObject =
                new GameObject(
                    "FixturePresentationAnchorResolverTests.Grid");

            try
            {
                Grid grid = gridObject.AddComponent<Grid>();
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(1f, 0.5f, 1f);

                GameObject tilemapObject =
                    new GameObject(
                        "FixturePresentationAnchorResolverTests.Tilemap");

                tilemapObject.transform.SetParent(
                    gridObject.transform,
                    worldPositionStays: false);

                Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
                Vector3Int displayCell = new Vector3Int(3, 4, 0);

                Vector3 center = tilemap.GetCellCenterWorld(displayCell);

                Vector3 result =
                    FixturePresentationAnchorResolver
                        .CalculateViewerNearestCornerWorld(
                            tilemap,
                            displayCell);

                Assert.That(
                    result.x,
                    Is.EqualTo(center.x).Within(0.00001f));

                Assert.That(
                    result.y,
                    Is.EqualTo(center.y - 0.25f).Within(0.00001f));

                Assert.That(
                    result.z,
                    Is.EqualTo(center.z).Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gridObject);
            }
        }


        [Test]
        public void CalculateViewerNearestCornerWorld_RejectsMissingTilemap()
        {
            Assert.Throws<ArgumentNullException>(
                () => FixturePresentationAnchorResolver
                    .CalculateViewerNearestCornerWorld(
                        null,
                        Vector3Int.zero));
        }


        [Test]
        public void CalculateFootprintAnchorWorld_UsesAuthoredBackCorners()
        {
            GameObject gridObject =
                new GameObject(
                    "FixturePresentationAnchorResolverTests.Grid");

            try
            {
                Grid grid = gridObject.AddComponent<Grid>();
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(1f, 0.5f, 1f);

                GameObject tilemapObject =
                    new GameObject(
                        "FixturePresentationAnchorResolverTests.Tilemap");

                tilemapObject.transform.SetParent(
                    gridObject.transform,
                    worldPositionStays: false);

                Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
                IsometricViewProjection projection =
                    CreateProjection(
                        IsometricViewOrientation.North);

                FixtureFootprint footprint =
                    FixtureFootprintResolver.Resolve(
                        ShelfDefinition,
                        new GridPosition(3, 4, 0),
                        FixtureOrientation.North);

                Vector3Int leftCell =
                    new Vector3Int(3, 4, 0);
                Vector3Int rightCell =
                    new Vector3Int(4, 4, 0);

                Vector3 leftCenter =
                    tilemap.GetCellCenterWorld(leftCell);
                Vector3 rightCenter =
                    tilemap.GetCellCenterWorld(rightCell);

                Vector3 halfPositiveX =
                    (tilemap.GetCellCenterWorld(
                        leftCell + Vector3Int.right)
                    - leftCenter)
                    * 0.5f;

                Vector3 halfPositiveY =
                    (tilemap.GetCellCenterWorld(
                        leftCell + Vector3Int.up)
                    - leftCenter)
                    * 0.5f;

                Vector3 expectedLeft =
                    leftCenter
                    - halfPositiveX
                    + halfPositiveY;

                Vector3 expectedRight =
                    rightCenter
                    + halfPositiveX
                    - halfPositiveY;

                Vector3 backLeft =
                    FixturePresentationAnchorResolver
                        .CalculateFootprintAnchorWorld(
                            tilemap,
                            footprint,
                            projection,
                            FixtureSpriteAnchorCorner.ViewerBackLeft,
                            unityCellZ: 0);

                Vector3 backRight =
                    FixturePresentationAnchorResolver
                        .CalculateFootprintAnchorWorld(
                            tilemap,
                            footprint,
                            projection,
                            FixtureSpriteAnchorCorner.ViewerBackRight,
                            unityCellZ: 0);

                Assert.That(
                    backLeft.x,
                    Is.EqualTo(expectedLeft.x).Within(0.00001f));
                Assert.That(
                    backLeft.y,
                    Is.EqualTo(expectedLeft.y).Within(0.00001f));

                Assert.That(
                    backRight.x,
                    Is.EqualTo(expectedRight.x).Within(0.00001f));
                Assert.That(
                    backRight.y,
                    Is.EqualTo(expectedRight.y).Within(0.00001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gridObject);
            }
        }


        [Test]
        public void CalculateFootprintAnchorWorld_RejectsUnsupportedCorner()
        {
            GameObject gridObject =
                new GameObject(
                    "FixturePresentationAnchorResolverTests.Grid");

            try
            {
                Grid grid = gridObject.AddComponent<Grid>();
                GameObject tilemapObject =
                    new GameObject(
                        "FixturePresentationAnchorResolverTests.Tilemap");

                tilemapObject.transform.SetParent(
                    gridObject.transform,
                    worldPositionStays: false);

                Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();

                FixtureFootprint footprint =
                    FixtureFootprintResolver.Resolve(
                        ShelfDefinition,
                        new GridPosition(3, 4, 0),
                        FixtureOrientation.North);

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => FixturePresentationAnchorResolver
                        .CalculateFootprintAnchorWorld(
                            tilemap,
                            footprint,
                            CreateProjection(
                                IsometricViewOrientation.North),
                            (FixtureSpriteAnchorCorner)999,
                            unityCellZ: 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gridObject);
            }
        }


        private static IsometricViewProjection CreateProjection(
            IsometricViewOrientation orientation)
        {
            return new IsometricViewProjection(
                MapFootprint,
                orientation);
        }
    }
}
