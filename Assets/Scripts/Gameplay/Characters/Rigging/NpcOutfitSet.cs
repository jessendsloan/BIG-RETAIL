using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    [Serializable]
    public sealed class NpcOutfitPartStyle
    {
        [SerializeField]
        private NpcRigPartId id;

        [SerializeField]
        private NpcAppearanceColorRole colorRole;

        [SerializeField]
        private Sprite southEastSprite;

        [SerializeField]
        private Sprite northEastSprite;

        [Tooltip(
            "Optional shared material for this body piece. Use the " +
            "texture-driven garment material for painted clothing; leave " +
            "empty to use the Person prefab's plain fallback material.")]
        [SerializeField]
        private Material materialOverride;

        [SerializeField]
        private bool visible = true;


        public NpcRigPartId Id => id;

        public NpcAppearanceColorRole ColorRole => colorRole;

        public bool Visible => visible;

        public Material MaterialOverride => materialOverride;


        public NpcOutfitPartStyle(
            NpcRigPartId id,
            NpcAppearanceColorRole colorRole,
            Sprite southEastSprite,
            Sprite northEastSprite,
            bool visible = true)
        {
            this.id = id;
            this.colorRole = colorRole;
            this.southEastSprite = southEastSprite;
            this.northEastSprite = northEastSprite;
            this.visible = visible;
        }


        public NpcOutfitPartStyle(
            NpcRigPartId id,
            NpcAppearanceColorRole colorRole,
            Sprite southEastSprite,
            Sprite northEastSprite,
            Material materialOverride,
            bool visible = true)
        {
            this.id = id;
            this.colorRole = colorRole;
            this.southEastSprite = southEastSprite;
            this.northEastSprite = northEastSprite;
            this.materialOverride = materialOverride;
            this.visible = visible;
        }


        public Sprite GetSprite(
            NpcAuthoredDirection direction)
        {
            return direction == NpcAuthoredDirection.SouthEast
                ? southEastSprite
                : northEastSprite;
        }
    }

    [CreateAssetMenu(
        fileName = "OutfitSet",
        menuName = "Big Retail/Characters/Outfit Set")]
    public sealed class NpcOutfitSet : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Outfit";

        [SerializeField]
        private Color primaryFabric = Color.white;

        [SerializeField]
        private Color secondaryFabric = Color.gray;

        [SerializeField]
        private Color footwear = Color.black;

        [SerializeField]
        private Color accent = Color.white;

        [SerializeField]
        private bool showBadge;

        [Tooltip(
            "Normalized position within the torso sprite. Zero is the " +
            "torso center; 0.5 reaches its camera-right or top edge.")]
        [SerializeField]
        private Vector2 badgeTorsoAnchor =
            new Vector2(0.32f, 0.10f);

        [SerializeField]
        private NpcGenderCompatibility supportedGenders =
            NpcGenderCompatibility.Everyone;

        [SerializeField]
        private List<NpcOutfitPartStyle> partStyles =
            new List<NpcOutfitPartStyle>();


        public string DisplayName => displayName;

        public bool ShowBadge => showBadge;

        public NpcGenderCompatibility SupportedGenders =>
            supportedGenders;

        public Color BadgeColor => accent;

        public Vector2 BadgeTorsoAnchor => badgeTorsoAnchor;


        public void Configure(
            string newDisplayName,
            Color newPrimaryFabric,
            Color newSecondaryFabric,
            Color newFootwear,
            Color newAccent,
            bool newShowBadge,
            IEnumerable<NpcOutfitPartStyle> newPartStyles)
        {
            Configure(
                newDisplayName,
                newPrimaryFabric,
                newSecondaryFabric,
                newFootwear,
                newAccent,
                newShowBadge,
                NpcGenderCompatibility.Everyone,
                newPartStyles);
        }


        public void Configure(
            string newDisplayName,
            Color newPrimaryFabric,
            Color newSecondaryFabric,
            Color newFootwear,
            Color newAccent,
            bool newShowBadge,
            NpcGenderCompatibility newSupportedGenders,
            IEnumerable<NpcOutfitPartStyle> newPartStyles)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            primaryFabric = newPrimaryFabric;
            secondaryFabric = newSecondaryFabric;
            footwear = newFootwear;
            accent = newAccent;
            showBadge = newShowBadge;
            supportedGenders = newSupportedGenders;
            partStyles = newPartStyles != null
                ? new List<NpcOutfitPartStyle>(newPartStyles)
                : new List<NpcOutfitPartStyle>();
        }


        public bool Supports(
            NpcPersonGender gender)
        {
            return supportedGenders.Supports(gender);
        }


        public bool TryGetPartStyle(
            NpcRigPartId partId,
            out NpcOutfitPartStyle style)
        {
            if (partStyles == null)
            {
                style = null;
                return false;
            }

            for (int index = 0; index < partStyles.Count; index++)
            {
                NpcOutfitPartStyle candidate = partStyles[index];

                if (candidate != null
                    && candidate.Id == partId)
                {
                    style = candidate;
                    return true;
                }
            }

            style = null;
            return false;
        }


        public Color GetColor(
            NpcAppearanceColorRole role,
            NpcSkinPalette skinPalette,
            bool shadeForDepth)
        {
            Color color;

            switch (role)
            {
                case NpcAppearanceColorRole.Skin:
                    return skinPalette != null
                        ? skinPalette.GetColor(shadeForDepth)
                        : Color.magenta;

                case NpcAppearanceColorRole.PrimaryFabric:
                    color = primaryFabric;
                    break;

                case NpcAppearanceColorRole.SecondaryFabric:
                    color = secondaryFabric;
                    break;

                case NpcAppearanceColorRole.Footwear:
                    color = footwear;
                    break;

                case NpcAppearanceColorRole.Accent:
                    color = accent;
                    break;

                default:
                    return Color.white;
            }

            return shadeForDepth
                ? NpcAppearanceUtility.Shade(color, 0.82f)
                : color;
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (supportedGenders == NpcGenderCompatibility.None)
            {
                failureReason =
                    "The outfit is not enabled for men or women.";
                return false;
            }

            if (partStyles == null)
            {
                failureReason = "Outfit part rules are missing.";
                return false;
            }

            HashSet<NpcRigPartId> uniqueParts =
                new HashSet<NpcRigPartId>();

            for (int index = 0; index < partStyles.Count; index++)
            {
                NpcOutfitPartStyle style = partStyles[index];

                if (style == null)
                {
                    failureReason =
                        $"Outfit part {index} is missing.";
                    return false;
                }

                if (!uniqueParts.Add(style.Id))
                {
                    failureReason =
                        $"Outfit part {style.Id} is duplicated.";
                    return false;
                }
            }

            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (definition.Id == NpcRigPartId.HairRear
                    || definition.Id == NpcRigPartId.HairFront
                    || definition.Id == NpcRigPartId.Head
                    || definition.Id == NpcRigPartId.Neck)
                {
                    continue;
                }

                if (!uniqueParts.Contains(definition.Id))
                {
                    failureReason =
                        $"Outfit has no rule for {definition.Id}.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
