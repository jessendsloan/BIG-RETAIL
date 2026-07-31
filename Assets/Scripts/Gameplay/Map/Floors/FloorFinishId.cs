using System;

namespace BigRetail.Map.Floors
{
    /// <summary>
    /// Identifies one authored floor-surface finish.
    ///
    /// Identifiers are normalized so simulation state never depends on
    /// Unity asset names or references.
    /// </summary>
    public readonly struct FloorFinishId :
        IEquatable<FloorFinishId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public FloorFinishId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A floor finish identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            FloorFinishId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is FloorFinishId other
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
            FloorFinishId left,
            FloorFinishId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FloorFinishId left,
            FloorFinishId right)
        {
            return !left.Equals(right);
        }
    }
}
