namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// World-space orientation of a placed fixture. Camera rotation changes
    /// presentation only and never mutates this value.
    /// </summary>
    public enum FixtureOrientation
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }


    public static class FixtureOrientationExtensions
    {
        public static bool IsSupported(
            this FixtureOrientation orientation)
        {
            return orientation >= FixtureOrientation.North
                && orientation <= FixtureOrientation.West;
        }


        public static FixtureOrientation RotateClockwise(
            this FixtureOrientation orientation)
        {
            switch (orientation)
            {
                case FixtureOrientation.North:
                    return FixtureOrientation.East;

                case FixtureOrientation.East:
                    return FixtureOrientation.South;

                case FixtureOrientation.South:
                    return FixtureOrientation.West;

                case FixtureOrientation.West:
                    return FixtureOrientation.North;

                default:
                    return orientation;
            }
        }


        public static FixtureOrientation RotateCounterClockwise(
            this FixtureOrientation orientation)
        {
            switch (orientation)
            {
                case FixtureOrientation.North:
                    return FixtureOrientation.West;

                case FixtureOrientation.West:
                    return FixtureOrientation.South;

                case FixtureOrientation.South:
                    return FixtureOrientation.East;

                case FixtureOrientation.East:
                    return FixtureOrientation.North;

                default:
                    return orientation;
            }
        }
    }
}
