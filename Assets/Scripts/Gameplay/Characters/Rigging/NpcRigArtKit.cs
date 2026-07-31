using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// The two authored sprites for one visible NPC body part.
    /// West-facing display directions mirror these source sprites.
    /// </summary>
    [Serializable]
    public sealed class NpcRigArtPart
    {
        [SerializeField]
        private NpcRigPartId id;

        [SerializeField]
        private Sprite southEastSprite;

        [SerializeField]
        private Sprite northEastSprite;


        public NpcRigPartId Id => id;

        public Sprite SouthEastSprite => southEastSprite;

        public Sprite NorthEastSprite => northEastSprite;


        public NpcRigArtPart(
            NpcRigPartId id)
        {
            this.id = id;
        }


        public Sprite GetSprite(
            NpcAuthoredDirection direction)
        {
            switch (direction)
            {
                case NpcAuthoredDirection.SouthEast:
                    return southEastSprite;

                case NpcAuthoredDirection.NorthEast:
                    return northEastSprite;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unknown authored NPC direction.");
            }
        }

        public void SetSprite(
            NpcAuthoredDirection direction,
            Sprite sprite)
        {
            switch (direction)
            {
                case NpcAuthoredDirection.SouthEast:
                    southEastSprite = sprite;
                    break;

                case NpcAuthoredDirection.NorthEast:
                    northEastSprite = sprite;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Unknown authored NPC direction.");
            }
        }
    }

    /// <summary>
    /// A reusable appearance kit for the canonical cutout skeleton.
    ///
    /// The kit owns exactly one entry for each of the 18 visible
    /// parts. Each entry can hold SouthEast and NorthEast source art,
    /// producing all four displayed facings through mirroring.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NpcRigArtKit",
        menuName = "Big Retail/Characters/NPC Rig Art Kit")]
    public sealed class NpcRigArtKit : ScriptableObject
    {
        [SerializeField]
        private List<NpcRigArtPart> parts =
            new List<NpcRigArtPart>();


        public int PartCount => parts.Count;


        private void OnEnable()
        {
            if (parts == null
                || parts.Count == 0)
            {
                NormalizeCanonicalLayout();
            }
        }


        /// <summary>
        /// Rebuilds the list in canonical back-to-front order while
        /// preserving the first existing assignment for every part.
        /// </summary>
        public void NormalizeCanonicalLayout()
        {
            Dictionary<NpcRigPartId, NpcRigArtPart> existingParts =
                new Dictionary<NpcRigPartId, NpcRigArtPart>();

            if (parts != null)
            {
                for (int index = 0;
                     index < parts.Count;
                     index++)
                {
                    NpcRigArtPart part =
                        parts[index];

                    if (part != null
                        && !existingParts.ContainsKey(part.Id))
                    {
                        existingParts.Add(
                            part.Id,
                            part);
                    }
                }
            }

            List<NpcRigArtPart> normalizedParts =
                new List<NpcRigArtPart>(
                    NpcRigDefinition.ExpectedPartCount);

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (existingParts.TryGetValue(
                        definition.Id,
                        out NpcRigArtPart existingPart))
                {
                    normalizedParts.Add(
                        existingPart);
                }
                else
                {
                    normalizedParts.Add(
                        new NpcRigArtPart(
                            definition.Id));
                }
            }

            parts = normalizedParts;
        }

        /// <summary>
        /// Finds the authored sprite for one named body part.
        /// </summary>
        public bool TryGetSprite(
            NpcRigPartId partId,
            NpcAuthoredDirection direction,
            out Sprite sprite)
        {
            if (TryGetPart(
                    partId,
                    out NpcRigArtPart part))
            {
                sprite =
                    part.GetSprite(
                        direction);
                return sprite != null;
            }

            sprite = null;
            return false;
        }

        /// <summary>
        /// Assigns one authored sprite without exposing list order to
        /// editor tooling.
        /// </summary>
        public bool TrySetSprite(
            NpcRigPartId partId,
            NpcAuthoredDirection direction,
            Sprite sprite)
        {
            if (!TryGetPart(
                    partId,
                    out NpcRigArtPart part))
            {
                return false;
            }

            part.SetSprite(
                direction,
                sprite);
            return true;
        }

        public int CountAssignedSprites(
            NpcAuthoredDirection direction)
        {
            int assignedCount = 0;

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                NpcRigArtPart part =
                    parts[index];

                if (part != null
                    && part.GetSprite(direction) != null)
                {
                    assignedCount++;
                }
            }

            return assignedCount;
        }

        public void GetMissingParts(
            NpcAuthoredDirection direction,
            ICollection<NpcRigPartId> missingParts)
        {
            if (missingParts == null)
            {
                throw new ArgumentNullException(
                    nameof(missingParts));
            }

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (!TryGetSprite(
                        definition.Id,
                        direction,
                        out _))
                {
                    missingParts.Add(
                        definition.Id);
                }
            }
        }

        /// <summary>
        /// Validates the 18 stable part identifiers independently of
        /// whether artwork has been assigned yet.
        /// </summary>
        public bool TryValidateStructure(
            out string failureReason)
        {
            if (parts == null)
            {
                failureReason =
                    "The art kit has no part list.";
                return false;
            }

            if (parts.Count
                != NpcRigDefinition.ExpectedPartCount)
            {
                failureReason =
                    $"Expected {NpcRigDefinition.ExpectedPartCount} " +
                    $"art entries but found {parts.Count}.";
                return false;
            }

            HashSet<NpcRigPartId> uniquePartIds =
                new HashSet<NpcRigPartId>();

            HashSet<NpcRigPartId> canonicalPartIds =
                new HashSet<NpcRigPartId>();

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                canonicalPartIds.Add(
                    definition.Id);
            }

            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                NpcRigArtPart part =
                    parts[index];

                if (part == null)
                {
                    failureReason =
                        $"Art entry {index} is missing.";
                    return false;
                }

                if (!canonicalPartIds.Contains(part.Id))
                {
                    failureReason =
                        $"Art entry {index} has non-canonical id " +
                        $"{part.Id}.";
                    return false;
                }

                if (!uniquePartIds.Add(part.Id))
                {
                    failureReason =
                        $"Art entry {part.Id} appears more than once.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Validates that one authored direction is complete and that
        /// no sprite was accidentally assigned to multiple parts.
        /// </summary>
        public bool TryValidateDirection(
            NpcAuthoredDirection direction,
            out string failureReason)
        {
            if (!TryValidateStructure(
                    out failureReason))
            {
                return false;
            }

            HashSet<Sprite> uniqueSprites =
                new HashSet<Sprite>();

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (!TryGetSprite(
                        definition.Id,
                        direction,
                        out Sprite sprite))
                {
                    failureReason =
                        $"{direction} is missing " +
                        $"{definition.Id}.";
                    return false;
                }

                if (!uniqueSprites.Add(sprite))
                {
                    failureReason =
                        $"{direction} assigns sprite " +
                        $"'{sprite.name}' to more than one part.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }


        private bool TryGetPart(
            NpcRigPartId partId,
            out NpcRigArtPart result)
        {
            for (int index = 0;
                 index < parts.Count;
                 index++)
            {
                NpcRigArtPart part =
                    parts[index];

                if (part != null
                    && part.Id == partId)
                {
                    result = part;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
