using System;
using System.Collections.Generic;
using BigRetail.Map.Domain;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Rotates authored access sides into canonical world-grid cells beside a
    /// resolved fixture footprint.
    /// </summary>
    public static class FixtureAccessPointResolver
    {
        public static IReadOnlyList<FixtureAccessPoint> Resolve(
            FixtureInstance fixture)
        {
            if (fixture == null)
            {
                throw new ArgumentNullException(nameof(fixture));
            }

            return Resolve(
                fixture.Definition,
                fixture.Footprint);
        }


        public static IReadOnlyList<FixtureAccessPoint> Resolve(
            FixtureDefinition definition,
            FixtureFootprint footprint)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (footprint == null)
            {
                throw new ArgumentNullException(nameof(footprint));
            }

            List<FixtureAccessPoint> points =
                new List<FixtureAccessPoint>(
                    2
                    * (footprint.WidthInCells
                        + footprint.DepthInCells));

            AddLocalSide(
                definition.AccessProfile,
                FixtureSide.North,
                footprint,
                points);

            AddLocalSide(
                definition.AccessProfile,
                FixtureSide.East,
                footprint,
                points);

            AddLocalSide(
                definition.AccessProfile,
                FixtureSide.South,
                footprint,
                points);

            AddLocalSide(
                definition.AccessProfile,
                FixtureSide.West,
                footprint,
                points);

            return points.ToArray();
        }


        private static void AddLocalSide(
            FixtureAccessProfile profile,
            FixtureSide localSide,
            FixtureFootprint footprint,
            ICollection<FixtureAccessPoint> points)
        {
            FixtureAccessMode mode =
                profile.GetMode(localSide);

            if (mode == FixtureAccessMode.None)
            {
                return;
            }

            FixtureSide worldSide =
                localSide.Rotate(footprint.Orientation);

            AddWorldSide(
                footprint,
                worldSide,
                mode,
                points);
        }


        private static void AddWorldSide(
            FixtureFootprint footprint,
            FixtureSide worldSide,
            FixtureAccessMode mode,
            ICollection<FixtureAccessPoint> points)
        {
            GridPosition anchor = footprint.AnchorCell;

            switch (worldSide)
            {
                case FixtureSide.North:
                    for (int xOffset = 0;
                         xOffset < footprint.WidthInCells;
                         xOffset++)
                    {
                        points.Add(
                            new FixtureAccessPoint(
                                anchor.Offset(
                                    xOffset,
                                    footprint.DepthInCells),
                                worldSide,
                                mode));
                    }
                    break;

                case FixtureSide.East:
                    for (int yOffset = 0;
                         yOffset < footprint.DepthInCells;
                         yOffset++)
                    {
                        points.Add(
                            new FixtureAccessPoint(
                                anchor.Offset(
                                    footprint.WidthInCells,
                                    yOffset),
                                worldSide,
                                mode));
                    }
                    break;

                case FixtureSide.South:
                    for (int xOffset = 0;
                         xOffset < footprint.WidthInCells;
                         xOffset++)
                    {
                        points.Add(
                            new FixtureAccessPoint(
                                anchor.Offset(xOffset, -1),
                                worldSide,
                                mode));
                    }
                    break;

                case FixtureSide.West:
                    for (int yOffset = 0;
                         yOffset < footprint.DepthInCells;
                         yOffset++)
                    {
                        points.Add(
                            new FixtureAccessPoint(
                                anchor.Offset(-1, yOffset),
                                worldSide,
                                mode));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(worldSide),
                        worldSide,
                        "The fixture side is not supported.");
            }
        }
    }
}
