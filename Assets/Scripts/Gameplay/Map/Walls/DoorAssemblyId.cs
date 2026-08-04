using System;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Identifies one placed door assembly. A multi-panel entrance owns one
    /// identity even when it occupies several structural wall edges.
    /// </summary>
    public readonly struct DoorAssemblyId :
        IEquatable<DoorAssemblyId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public DoorAssemblyId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A door assembly identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            DoorAssemblyId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is DoorAssemblyId other
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
            DoorAssemblyId left,
            DoorAssemblyId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DoorAssemblyId left,
            DoorAssemblyId right)
        {
            return !left.Equals(right);
        }
    }
}
