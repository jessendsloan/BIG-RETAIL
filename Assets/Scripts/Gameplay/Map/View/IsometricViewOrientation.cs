using System;

namespace BigRetail.Map.View
{
    /// <summary>
    /// Identifies one of the four discrete presentations of the
    /// canonical map.
    ///
    /// North is the authored/default presentation. The remaining
    /// values represent successive quarter turns of the view only;
    /// logical map coordinates never rotate.
    /// </summary>
    public enum IsometricViewOrientation
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class IsometricViewOrientationExtensions
    {
        public static IsometricViewOrientation RotateClockwise(
            this IsometricViewOrientation orientation)
        {
            Validate(orientation);

            return
                (IsometricViewOrientation)
                (((int)orientation + 1) % 4);
        }

        public static IsometricViewOrientation RotateCounterClockwise(
            this IsometricViewOrientation orientation)
        {
            Validate(orientation);

            return
                (IsometricViewOrientation)
                (((int)orientation + 3) % 4);
        }

        public static bool IsQuarterTurn(
            this IsometricViewOrientation orientation)
        {
            Validate(orientation);

            return orientation == IsometricViewOrientation.East
                || orientation == IsometricViewOrientation.West;
        }

        private static void Validate(
            IsometricViewOrientation orientation)
        {
            int orientationValue =
                (int)orientation;

            if (orientationValue
                    < (int)IsometricViewOrientation.North
                || orientationValue
                    > (int)IsometricViewOrientation.West)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientation),
                    orientation,
                    "Unsupported isometric-view orientation.");
            }
        }
    }
}
