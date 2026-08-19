using System;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Identifies one consumer-facing brand independently of products and
    /// suppliers.
    /// </summary>
    public readonly struct BrandId : IEquatable<BrandId>
    {
        private readonly string value;


        public static BrandId Unbranded =>
            new BrandId("UNBRANDED");

        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public BrandId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A brand identifier cannot be empty.",
                    nameof(value));
            }

            this.value = value.Trim().ToUpperInvariant();
        }


        public bool Equals(BrandId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is BrandId other && Equals(other);
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


        public static bool operator ==(BrandId left, BrandId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BrandId left, BrandId right)
        {
            return !left.Equals(right);
        }
    }
}
