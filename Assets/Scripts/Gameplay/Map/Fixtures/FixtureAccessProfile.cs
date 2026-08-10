using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Immutable authored access rules for the four local sides of a fixture.
    /// The profile does not decide whether a resolved world cell is currently
    /// reachable; navigation owns that later decision.
    /// </summary>
    public sealed class FixtureAccessProfile
    {
        public static FixtureAccessProfile None { get; } =
            new FixtureAccessProfile(
                FixtureAccessMode.None,
                FixtureAccessMode.None,
                FixtureAccessMode.None,
                FixtureAccessMode.None);


        public FixtureAccessMode North { get; }

        public FixtureAccessMode East { get; }

        public FixtureAccessMode South { get; }

        public FixtureAccessMode West { get; }

        public bool HasAnyAccess =>
            North != FixtureAccessMode.None
            || East != FixtureAccessMode.None
            || South != FixtureAccessMode.None
            || West != FixtureAccessMode.None;


        public FixtureAccessProfile(
            FixtureAccessMode north,
            FixtureAccessMode east,
            FixtureAccessMode south,
            FixtureAccessMode west)
        {
            ValidateMode(north, nameof(north));
            ValidateMode(east, nameof(east));
            ValidateMode(south, nameof(south));
            ValidateMode(west, nameof(west));

            North = north;
            East = east;
            South = south;
            West = west;
        }


        public FixtureAccessMode GetMode(
            FixtureSide localSide)
        {
            return localSide switch
            {
                FixtureSide.North => North,
                FixtureSide.East => East,
                FixtureSide.South => South,
                FixtureSide.West => West,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(localSide),
                    localSide,
                    "The fixture side is not supported.")
            };
        }


        private static void ValidateMode(
            FixtureAccessMode mode,
            string parameterName)
        {
            if (!mode.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    mode,
                    "The fixture access mode contains unsupported values.");
            }
        }
    }
}
