using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Identifies one side of a rectangular fixture. North is positive grid
    /// Y and East is positive grid X. Definition sides are authored locally;
    /// resolved access points expose their world-grid side.
    /// </summary>
    public enum FixtureSide
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }


    public static class FixtureSideExtensions
    {
        public static bool IsSupported(
            this FixtureSide side)
        {
            return side >= FixtureSide.North
                && side <= FixtureSide.West;
        }


        public static FixtureSide Rotate(
            this FixtureSide localSide,
            FixtureOrientation orientation)
        {
            if (!localSide.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localSide),
                    localSide,
                    "The fixture side is not supported.");
            }

            if (!orientation.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation),
                    orientation,
                    "The fixture orientation is not supported.");
            }

            // Fixture orientation follows the project's isometric art order.
            // In canonical grid coordinates that turns an authored side in
            // the opposite numeric direction: East-facing art maps local
            // South to world East, while West-facing art maps it to West.
            return (FixtureSide)(
                ((int)localSide - (int)orientation + 4) % 4);
        }
    }
}
