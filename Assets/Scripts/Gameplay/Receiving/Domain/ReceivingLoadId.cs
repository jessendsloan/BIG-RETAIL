using System;

namespace BigRetail.Receiving.Domain
{
    /// <summary>
    /// Stable identity for any physical load competing for Receiving space.
    /// The source keeps supplier and equipment order sequences independent.
    /// </summary>
    public readonly struct ReceivingLoadId : IEquatable<ReceivingLoadId>
    {
        public const string SupplierOrderSource = "SUPPLIER";
        public const string EquipmentOrderSource = "EQUIPMENT";

        public string Source { get; }

        public long Number { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(Source)
            && Number > 0;


        public ReceivingLoadId(
            string source,
            long number)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException(
                    "A Receiving load requires a source.",
                    nameof(source));
            }

            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    number,
                    "A Receiving load number must be positive.");
            }

            Source = source.Trim().ToUpperInvariant();
            Number = number;
        }


        public static ReceivingLoadId SupplierOrder(long orderNumber)
        {
            return new ReceivingLoadId(
                SupplierOrderSource,
                orderNumber);
        }

        public static ReceivingLoadId EquipmentOrder(long orderNumber)
        {
            return new ReceivingLoadId(
                EquipmentOrderSource,
                orderNumber);
        }


        public bool Equals(ReceivingLoadId other)
        {
            return Number == other.Number
                && string.Equals(
                    Source,
                    other.Source,
                    StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ReceivingLoadId other
                && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source != null
                        ? StringComparer.Ordinal.GetHashCode(Source)
                        : 0) * 397)
                    ^ Number.GetHashCode();
            }
        }

        public override string ToString()
        {
            return IsValid
                ? $"{Source}:{Number}"
                : string.Empty;
        }

        public static bool operator ==(
            ReceivingLoadId left,
            ReceivingLoadId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ReceivingLoadId left,
            ReceivingLoadId right)
        {
            return !left.Equals(right);
        }
    }
}
