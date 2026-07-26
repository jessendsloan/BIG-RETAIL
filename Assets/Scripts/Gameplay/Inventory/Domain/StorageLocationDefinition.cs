using System;

namespace BigRetail.Inventory.Domain
{
    /// <summary>
    /// Describes one logical stock location without depending on a scene,
    /// fixture, room, or map coordinate.
    /// </summary>
    public sealed class StorageLocationDefinition
    {
        public StorageLocationId Id { get; }
        public string DisplayName { get; }
        public StorageRole Role { get; }


        public StorageLocationDefinition(
            StorageLocationId id,
            string displayName,
            StorageRole role)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException(
                    "A storage location requires a valid identifier.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A storage location requires a display name.",
                    nameof(displayName));
            }

            if (!Enum.IsDefined(
                    typeof(StorageRole),
                    role))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(role),
                    role,
                    "The storage role is not supported.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            Role = role;
        }
    }
}
