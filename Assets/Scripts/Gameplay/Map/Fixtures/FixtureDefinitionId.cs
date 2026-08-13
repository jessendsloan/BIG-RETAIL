using System;

namespace BigRetail.Map.Fixtures
{
    /// <summary>
    /// Identifies one authored fixture model such as a shelf, checkout,
    /// or refrigerator.
    /// </summary>
    public readonly struct FixtureDefinitionId :
        IEquatable<FixtureDefinitionId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public FixtureDefinitionId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A fixture definition identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            FixtureDefinitionId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is FixtureDefinitionId other
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
            FixtureDefinitionId left,
            FixtureDefinitionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FixtureDefinitionId left,
            FixtureDefinitionId right)
        {
            return !left.Equals(right);
        }
    }
}
