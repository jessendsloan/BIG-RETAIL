using System;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Identifies a merchandise category without requiring a fixed enum.
    ///
    /// Categories can expand with the store without changing the inventory
    /// domain's type hierarchy.
    /// </summary>
    public readonly struct ProductCategoryId :
        IEquatable<ProductCategoryId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public ProductCategoryId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A product category identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            ProductCategoryId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is ProductCategoryId other
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
            ProductCategoryId left,
            ProductCategoryId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ProductCategoryId left,
            ProductCategoryId right)
        {
            return !left.Equals(right);
        }
    }
}
