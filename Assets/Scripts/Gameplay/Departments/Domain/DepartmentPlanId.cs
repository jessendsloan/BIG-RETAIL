using System;

namespace BigRetail.Departments
{
    /// <summary>
    /// Identifies one player-created department plan. Multiple plans may use
    /// the same DepartmentDefinitionId when a store deliberately has more
    /// than one area serving that retail function.
    /// </summary>
    public readonly struct DepartmentPlanId :
        IEquatable<DepartmentPlanId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public DepartmentPlanId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A department plan identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            DepartmentPlanId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is DepartmentPlanId other
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
            DepartmentPlanId left,
            DepartmentPlanId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DepartmentPlanId left,
            DepartmentPlanId right)
        {
            return !left.Equals(right);
        }
    }
}
