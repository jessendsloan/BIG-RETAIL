using System;

namespace BigRetail.Purchasing.Domain
{
    /// <summary>
    /// Identifies one outside company that can supply the store.
    /// </summary>
    public readonly struct SupplierId : IEquatable<SupplierId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public SupplierId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A supplier identifier cannot be empty.",
                    nameof(value));
            }

            this.value = value.Trim().ToUpperInvariant();
        }


        public bool Equals(SupplierId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SupplierId other && Equals(other);
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


        public static bool operator ==(SupplierId left, SupplierId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SupplierId left, SupplierId right)
        {
            return !left.Equals(right);
        }
    }
}
