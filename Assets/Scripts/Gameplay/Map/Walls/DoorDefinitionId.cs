using System;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies one authored door model such as a single door or a
    /// four-panel automatic storefront entrance.
    /// </summary>
    public readonly struct DoorDefinitionId :
        IEquatable<DoorDefinitionId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public DoorDefinitionId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A door definition identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            DoorDefinitionId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is DoorDefinitionId other
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
            DoorDefinitionId left,
            DoorDefinitionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DoorDefinitionId left,
            DoorDefinitionId right)
        {
            return !left.Equals(right);
        }
    }
}
