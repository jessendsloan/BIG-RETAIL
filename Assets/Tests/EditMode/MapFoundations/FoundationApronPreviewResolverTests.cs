using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;
using NUnit.Framework;

namespace BigRetail.Map.Foundations.Tests
{
    public sealed class FoundationApronPreviewResolverTests
    {
        [Test]
        public void Resolve_SinglePreviewFoundation_ReturnsEightNeighbors()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronPreviewResolver.Resolve(
                    CreateMap(5, 5),
                    Array.Empty<GridPosition>(),
                    new[]
                    {
                        new GridPosition(2, 2)
                    });

            Assert.That(
                apron,
                Has.Count.EqualTo(8));
        }


        [Test]
        public void Resolve_TwoByTwoPreview_ReturnsCompleteOuterRing()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronPreviewResolver.Resolve(
                    CreateMap(5, 5),
                    Array.Empty<GridPosition>(),
                    new[]
                    {
                        new GridPosition(1, 1),
                        new GridPosition(2, 1),
                        new GridPosition(1, 2),
                        new GridPosition(2, 2)
                    });

            Assert.That(
                apron,
                Has.Count.EqualTo(12));
        }


        [Test]
        public void Resolve_AdjacentExistingFoundation_ShowsOnlyAffectedApron()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronPreviewResolver.Resolve(
                    CreateMap(5, 3),
                    new[]
                    {
                        new GridPosition(1, 1)
                    },
                    new[]
                    {
                        new GridPosition(2, 1)
                    });

            Assert.That(
                apron,
                Has.Count.EqualTo(7));

            Assert.That(
                apron,
                Has.None.EqualTo(
                    new GridPosition(0, 1)));

            Assert.That(
                apron,
                Has.None.EqualTo(
                    new GridPosition(2, 1)));
        }


        [Test]
        public void Resolve_SelectedExistingFoundation_ShowsItsApron()
        {
            GridPosition existing =
                new GridPosition(2, 2);

            IReadOnlyList<GridPosition> apron =
                FoundationApronPreviewResolver.Resolve(
                    CreateMap(5, 5),
                    new[] { existing },
                    new[] { existing });

            Assert.That(
                apron,
                Has.Count.EqualTo(8));
        }


        [Test]
        public void Resolve_OutsideMapPreview_IsIgnored()
        {
            IReadOnlyList<GridPosition> apron =
                FoundationApronPreviewResolver.Resolve(
                    CreateMap(3, 3),
                    Array.Empty<GridPosition>(),
                    new[]
                    {
                        new GridPosition(8, 8)
                    });

            Assert.That(
                apron,
                Is.Empty);
        }


        [Test]
        public void Resolve_NullInputs_Throw()
        {
            GridMapDefinition map =
                CreateMap(3, 3);

            Assert.Throws<ArgumentNullException>(
                () => FoundationApronPreviewResolver.Resolve(
                    null,
                    Array.Empty<GridPosition>(),
                    Array.Empty<GridPosition>()));

            Assert.Throws<ArgumentNullException>(
                () => FoundationApronPreviewResolver.Resolve(
                    map,
                    null,
                    Array.Empty<GridPosition>()));

            Assert.Throws<ArgumentNullException>(
                () => FoundationApronPreviewResolver.Resolve(
                    map,
                    Array.Empty<GridPosition>(),
                    null));
        }


        private static GridMapDefinition CreateMap(
            int width,
            int height)
        {
            List<GridPosition> cells =
                new List<GridPosition>();

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
                            y));
                }
            }

            return new GridMapDefinition(
                "foundation.apron.preview.test",
                cells);
        }
    }
}
