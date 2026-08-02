using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    [CreateAssetMenu(
        fileName = "CharacterLibrary",
        menuName = "Big Retail/Characters/Character Library")]
    public sealed class NpcCharacterLibrary : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Character Library";

        [SerializeField]
        private List<NpcCharacterTemplate> templates =
            new List<NpcCharacterTemplate>();

        [SerializeField]
        private List<NpcBodySilhouette> bodies =
            new List<NpcBodySilhouette>();

        [SerializeField]
        private List<NpcSkinPalette> skins =
            new List<NpcSkinPalette>();

        [SerializeField]
        private List<NpcOutfitSet> outfits =
            new List<NpcOutfitSet>();

        [SerializeField]
        private List<NpcHairSet> hair =
            new List<NpcHairSet>();


        public string DisplayName => displayName;

        public IReadOnlyList<NpcCharacterTemplate> Templates => templates;

        public IReadOnlyList<NpcBodySilhouette> Bodies => bodies;

        public IReadOnlyList<NpcSkinPalette> Skins => skins;

        public IReadOnlyList<NpcOutfitSet> Outfits => outfits;

        public IReadOnlyList<NpcHairSet> Hair => hair;


        public void Configure(
            string newDisplayName,
            IEnumerable<NpcCharacterTemplate> newTemplates,
            IEnumerable<NpcBodySilhouette> newBodies,
            IEnumerable<NpcSkinPalette> newSkins,
            IEnumerable<NpcOutfitSet> newOutfits,
            IEnumerable<NpcHairSet> newHair)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            templates = Copy(newTemplates);
            bodies = Copy(newBodies);
            skins = Copy(newSkins);
            outfits = Copy(newOutfits);
            hair = Copy(newHair);
        }


        public NpcCharacterTemplate GetTemplate(
            NpcCharacterRole role)
        {
            if (templates == null)
            {
                return null;
            }

            for (int index = 0; index < templates.Count; index++)
            {
                NpcCharacterTemplate candidate = templates[index];

                if (candidate != null && candidate.Role == role)
                {
                    return candidate;
                }
            }

            return null;
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (templates == null || templates.Count == 0)
            {
                failureReason = "The library has no character templates.";
                return false;
            }

            for (int index = 0; index < templates.Count; index++)
            {
                NpcCharacterTemplate template = templates[index];

                if (template == null)
                {
                    failureReason =
                        "The library contains an empty character template.";
                    return false;
                }

                if (!template.TryValidate(out failureReason))
                {
                    return false;
                }
            }

            if (!TryValidateAssets(bodies, "body", out failureReason)
                || !TryValidateAssets(skins, "skin", out failureReason)
                || !TryValidateAssets(outfits, "outfit", out failureReason)
                || !TryValidateAssets(hair, "hair", out failureReason))
            {
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        private static List<T> Copy<T>(
            IEnumerable<T> source)
        {
            return source != null
                ? new List<T>(source)
                : new List<T>();
        }


        private static bool TryValidateAssets<T>(
            IReadOnlyList<T> assets,
            string label,
            out string failureReason)
            where T : UnityEngine.Object
        {
            if (assets == null || assets.Count == 0)
            {
                failureReason =
                    $"The library has no registered {label} assets.";
                return false;
            }

            HashSet<T> uniqueAssets = new HashSet<T>();

            for (int index = 0; index < assets.Count; index++)
            {
                T asset = assets[index];

                if (asset == null)
                {
                    failureReason =
                        $"The library contains an empty {label} asset.";
                    return false;
                }

                if (!uniqueAssets.Add(asset))
                {
                    failureReason =
                        $"{asset.name} is registered twice as a {label}.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
