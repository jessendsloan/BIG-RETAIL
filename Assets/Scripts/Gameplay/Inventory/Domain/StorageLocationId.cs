using System;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Identifies one logical place that can hold store-owned stock.
    ///
    /// Identifiers are normalized so saves, fixtures, and simulation systems
    /// can refer to the same location without case ambiguity.
    /// </summary>
    public readonly struct StorageLocationId :
        IEquatable<StorageLocationId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public StorageLocationId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A storage location identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            StorageLocationId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is StorageLocationId other
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
            StorageLocationId left,
            StorageLocationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            StorageLocationId left,
            StorageLocationId right)
        {
            return !left.Equals(right);
        }
    }
}
