using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Identifies one placed fixture independently of its authored model.
    /// </summary>
    public readonly struct FixtureInstanceId :
        IEquatable<FixtureInstanceId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public FixtureInstanceId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A fixture instance identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            FixtureInstanceId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is FixtureInstanceId other
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
            FixtureInstanceId left,
            FixtureInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FixtureInstanceId left,
            FixtureInstanceId right)
        {
            return !left.Equals(right);
        }
    }
}
