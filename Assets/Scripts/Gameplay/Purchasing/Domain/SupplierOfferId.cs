using System;

namespace BigRetail.Purchasing.Domain
{
    public readonly struct SupplierOfferId : IEquatable<SupplierOfferId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public SupplierOfferId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A supplier offer identifier cannot be empty.",
                    nameof(value));
            }

            this.value = value.Trim().ToUpperInvariant();
        }


        public bool Equals(SupplierOfferId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SupplierOfferId other && Equals(other);
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
            SupplierOfferId left,
            SupplierOfferId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            SupplierOfferId left,
            SupplierOfferId right)
        {
            return !left.Equals(right);
        }
    }
}
