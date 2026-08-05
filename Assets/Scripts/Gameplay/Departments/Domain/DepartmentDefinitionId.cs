using System;

namespace BigRetail.Departments
{
    /// <summary>
    /// Identifies one authored kind of store department, independent of
    /// Unity asset names or presentation.
    /// </summary>
    public readonly struct DepartmentDefinitionId :
        IEquatable<DepartmentDefinitionId>
    {
        private readonly string value;


        public string Value =>
            value ?? string.Empty;

        public bool IsValid =>
            !string.IsNullOrEmpty(value);


        public DepartmentDefinitionId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A department definition identifier cannot be empty.",
                    nameof(value));
            }

            this.value =
                value.Trim().ToUpperInvariant();
        }


        public bool Equals(
            DepartmentDefinitionId other)
        {
            return string.Equals(
                value,
                other.value,
                StringComparison.Ordinal);
        }

        public override bool Equals(
            object obj)
        {
            return obj is DepartmentDefinitionId other
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
            DepartmentDefinitionId left,
            DepartmentDefinitionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            DepartmentDefinitionId left,
            DepartmentDefinitionId right)
        {
            return !left.Equals(right);
        }
    }
}
