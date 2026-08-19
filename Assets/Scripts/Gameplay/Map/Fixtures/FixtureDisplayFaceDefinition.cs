using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Defines the stable merchandising surface on one authored fixture side.
    /// The local side does not change when the camera rotates.
    /// </summary>
    public sealed class FixtureDisplayFaceDefinition
    {
        public FixtureSide LocalSide { get; }

        public int ShelfRunCount { get; }

        public int FrontageUnitsPerRun { get; }


        public FixtureDisplayFaceDefinition(
            FixtureSide localSide,
            int shelfRunCount,
            int frontageUnitsPerRun)
        {
            if (!localSide.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localSide),
                    localSide,
                    "The fixture display side is not supported.");
            }

            if (shelfRunCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shelfRunCount),
                    shelfRunCount,
                    "A display face requires at least one shelf run.");
            }

            if (frontageUnitsPerRun <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frontageUnitsPerRun),
                    frontageUnitsPerRun,
                    "A shelf run requires at least one frontage unit.");
            }

            LocalSide = localSide;
            ShelfRunCount = shelfRunCount;
            FrontageUnitsPerRun = frontageUnitsPerRun;
        }
    }
}
