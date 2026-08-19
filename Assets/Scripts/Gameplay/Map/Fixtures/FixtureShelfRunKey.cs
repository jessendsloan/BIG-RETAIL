using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Stable identity for one physical shelf run on one placed fixture.
    /// Authored local face identity keeps the key stable through camera and
    /// fixture presentation rotation.
    /// </summary>
    public readonly struct FixtureShelfRunKey :
        IEquatable<FixtureShelfRunKey>
    {
        public FixtureInstanceId FixtureId { get; }

        public FixtureSide LocalDisplaySide { get; }

        public int ShelfRunIndex { get; }


        public FixtureShelfRunKey(
            FixtureInstanceId fixtureId,
            FixtureSide localDisplaySide,
            int shelfRunIndex)
        {
            if (!fixtureId.IsValid)
            {
                throw new ArgumentException(
                    "A shelf run requires a valid fixture identifier.",
                    nameof(fixtureId));
            }

            if (!localDisplaySide.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localDisplaySide),
                    localDisplaySide,
                    "The shelf run display side is not supported.");
            }

            if (shelfRunIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(shelfRunIndex),
                    shelfRunIndex,
                    "A shelf run index cannot be negative.");
            }

            FixtureId = fixtureId;
            LocalDisplaySide = localDisplaySide;
            ShelfRunIndex = shelfRunIndex;
        }


        public bool Equals(FixtureShelfRunKey other)
        {
            return FixtureId == other.FixtureId
                && LocalDisplaySide == other.LocalDisplaySide
                && ShelfRunIndex == other.ShelfRunIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is FixtureShelfRunKey other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = FixtureId.GetHashCode();
                hash = (hash * 397) ^ (int)LocalDisplaySide;
                hash = (hash * 397) ^ ShelfRunIndex;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{FixtureId}:{LocalDisplaySide}:SHELF-{ShelfRunIndex + 1}";
        }


        public static bool operator ==(
            FixtureShelfRunKey left,
            FixtureShelfRunKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FixtureShelfRunKey left,
            FixtureShelfRunKey right)
        {
            return !left.Equals(right);
        }
    }
}
