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

        public FixtureAccessClearancePolicy ClearancePolicy { get; }

        public bool HasAnyAccess =>
            North != FixtureAccessMode.None
            || East != FixtureAccessMode.None
            || South != FixtureAccessMode.None
            || West != FixtureAccessMode.None;


        public FixtureAccessProfile(
            FixtureAccessMode north,
            FixtureAccessMode east,
            FixtureAccessMode south,
            FixtureAccessMode west,
            FixtureAccessClearancePolicy clearancePolicy =
                FixtureAccessClearancePolicy.AllAuthoredAccessPoints)
        {
            ValidateMode(north, nameof(north));
            ValidateMode(east, nameof(east));
            ValidateMode(south, nameof(south));
            ValidateMode(west, nameof(west));

            if (!Enum.IsDefined(
                    typeof(FixtureAccessClearancePolicy),
                    clearancePolicy))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clearancePolicy),
                    clearancePolicy,
                    "The fixture access-clearance policy is not supported.");
            }

            North = north;
            East = east;
            South = south;
            West = west;
            ClearancePolicy = clearancePolicy;

            if (clearancePolicy
                    == FixtureAccessClearancePolicy.AtLeastOneCompleteSide
                && !HasAnyAccess)
            {
                throw new ArgumentException(
                    "A fixture requiring one complete access side must "
                    + "author at least one access side.",
                    nameof(clearancePolicy));
            }
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
