using System;

namespace BigRetail.Merchandise.Domain
{
    /// <summary>
    /// Describes one recurring consumer identity in the shelf world.
    /// Supplier relationships and visual artwork remain outside this domain
    /// record.
    /// </summary>
    public sealed class BrandDefinition
    {
        public BrandId Id { get; }

        public string DisplayName { get; }


        public BrandDefinition(
            BrandId id,
            string displayName)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A brand definition requires a valid identifier.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A brand definition requires a display name.",
                    nameof(displayName));
            }

            Id = id;
            DisplayName = displayName.Trim();
        }
    }
}
