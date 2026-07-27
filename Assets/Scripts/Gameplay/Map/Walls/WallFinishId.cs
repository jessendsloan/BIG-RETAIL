using System;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies one authored wall-face finish.
    ///
    /// Finish identifiers are normalized to trimmed uppercase text so the
    /// simulation can compare finish identity without Unity asset references.
    /// </summary>
    public readonly struct WallFinishId :
        IEquatable<WallFinishId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public WallFinishId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A wall finish identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            WallFinishId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is WallFinishId other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            return value == null
                ? 0
                : StringComparer.Ordinal.GetHashCode(value);
        }

        public override string ToString()
        {
            return Value;
        }


        public static bool operator ==(
            WallFinishId left,
            WallFinishId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            WallFinishId left,
            WallFinishId right)
        {
            return !left.Equals(right);
        }
    }
}
