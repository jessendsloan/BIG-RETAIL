using System;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Identifies one distinct sellable product.
    ///
    /// Product identifiers are normalized to trimmed uppercase text so every
    /// system can compare merchandise identity without case ambiguity.
    /// </summary>
    public readonly struct ProductId :
        IEquatable<ProductId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public ProductId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A product identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            ProductId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is ProductId other
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
            ProductId left,
            ProductId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ProductId left,
            ProductId right)
        {
            return !left.Equals(right);
        }
    }
}
