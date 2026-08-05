using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Foundations.Tests
{
    public sealed class FoundationApronResolverTests
    {
        [Test]
        public void Resolve_NullMap_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => FoundationApronResolver.Resolve(
                    null,
                    Array.Empty<GridPosition>()));
        }


        [Test]
        public void Resolve_NullFoundations_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => FoundationApronResolver.Resolve(
                    CreateMap(3, 3),
                    null));
        }


        [Test]
        public void Resolve_NoFoundations_ReturnsNoApron()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    CreateMap(3, 3),
                    Array.Empty<GridPosition>());

            Assert.That(
                apron,
                Is.Empty);
        }


        [Test]
        public void Resolve_SingleInteriorFoundation_ReturnsEightNeighbors()
        {
            GridMapDefinition map =
                CreateMap(3, 3);

            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    map,
                    new[]
                    {
                        new GridPosition(1, 1)
                    });

            GridPosition[] expected =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(2, 0),
                new GridPosition(0, 1),
                new GridPosition(2, 1),
                new GridPosition(0, 2),
                new GridPosition(1, 2),
                new GridPosition(2, 2)
            };

            Assert.That(
                apron,
                Is.EquivalentTo(expected));
        }


        [Test]
        public void Resolve_TwoByTwoFoundation_ReturnsCompleteOuterRing()
        {
            GridMapDefinition map =
                CreateMap(4, 4);

            GridPosition[] foundations =
            {
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(1, 2),
                new GridPosition(2, 2)
            };

            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    map,
                    foundations);

            Assert.That(
                apron.Count,
                Is.EqualTo(12));

            HashSet<GridPosition> apronSet =
                new HashSet<GridPosition>(apron);

            foreach (GridPosition foundation in foundations)
            {
                Assert.That(
                    apronSet.Contains(foundation),
                    Is.False);
            }
        }


        [Test]
        public void Resolve_MapCorner_ClipsApronToValidCells()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    CreateMap(3, 3),
                    new[]
                    {
                        new GridPosition(0, 0)
                    });

            GridPosition[] expected =
            {
                new GridPosition(1, 0),
                new GridPosition(0, 1),
                new GridPosition(1, 1)
            };

            Assert.That(
                apron,
                Is.EquivalentTo(expected));
        }


        [Test]
        public void Resolve_OverlappingNeighborhoods_ReturnUniqueCells()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    CreateMap(5, 3),
                    new[]
                    {
                        new GridPosition(1, 1),
                        new GridPosition(3, 1),
                        new GridPosition(3, 1)
                    });

            HashSet<GridPosition> uniqueApron =
                new HashSet<GridPosition>(apron);

            Assert.That(
                uniqueApron.Count,
                Is.EqualTo(apron.Count));

            Assert.That(
                uniqueApron.Contains(
                    new GridPosition(2, 1)),
                Is.True);
        }


        [Test]
        public void Resolve_InternalGap_BecomesExposedApron()
        {
            GridPosition gap =
                new GridPosition(2, 2);

            List<GridPosition> foundations =
                new List<GridPosition>();

            for (int y = 1;
                 y <= 3;
                 y++)
            {
                for (int x = 1;
                     x <= 3;
                     x++)
                {
                    GridPosition cell =
                        new GridPosition(x, y);

                    if (cell != gap)
                    {
                        foundations.Add(cell);
                    }
                }
            }

            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    CreateMap(5, 5),
                    foundations);

            Assert.That(
                new HashSet<GridPosition>(apron)
                    .Contains(gap),
                Is.True);
        }


        [Test]
        public void Resolve_DifferentLevels_RemainIndependent()
        {
            GridMapDefinition map =
                CreateMap(
                    3,
                    3,
                    levels: 2);

            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    map,
                    new[]
                    {
                        new GridPosition(1, 1, 1)
                    });

            Assert.That(
                apron.Count,
                Is.EqualTo(8));

            foreach (GridPosition apronCell in apron)
            {
                Assert.That(
                    apronCell.Level,
                    Is.EqualTo(1));
            }
        }


        [Test]
        public void Resolve_ReturnsDeterministicLevelRowColumnOrder()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronResolver.Resolve(
                    CreateMap(3, 3),
                    new[]
                    {
                        new GridPosition(1, 1)
                    });

            GridPosition[] expected =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(2, 0),
                new GridPosition(0, 1),
                new GridPosition(2, 1),
                new GridPosition(0, 2),
                new GridPosition(1, 2),
                new GridPosition(2, 2)
            };

            Assert.That(
                apron,
                Is.EqualTo(expected));
        }


        private static GridMapDefinition CreateMap(
            int width,
            int height,
            int levels = 1)
        {
            List<GridPosition> cells =
                new List<GridPosition>();

            for (int level = 0;
                 level < levels;
                 level++)
            {
                for (int y = 0;
                     y < height;
                     y++)
                {
                    for (int x = 0;
                         x < width;
                         x++)
                    {
                        cells.Add(
                            new GridPosition(
                                x,
                                y,
                                level));
                    }
                }
            }

            return new GridMapDefinition(
                "foundation.apron.test",
                cells);
        }
    }
}
