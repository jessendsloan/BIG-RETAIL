using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    public enum NpcCharacterRole
    {
        Customer = 0,
        Employee = 1
    }


    [Serializable]
    public sealed class NpcWeightedBodyChoice
    {
        [SerializeField]
        private NpcBodySilhouette asset;

        [Min(1)]
        [SerializeField]
        private int weight = 1;

        public NpcBodySilhouette Asset => asset;

        public int Weight => weight;

        public NpcWeightedBodyChoice(
            NpcBodySilhouette newAsset,
            int newWeight = 1)
        {
            asset = newAsset;
            weight = Mathf.Max(1, newWeight);
        }
    }


    [Serializable]
    public sealed class NpcWeightedSkinChoice
    {
        [SerializeField]
        private NpcSkinPalette asset;

        [Min(1)]
        [SerializeField]
        private int weight = 1;

        public NpcSkinPalette Asset => asset;

        public int Weight => weight;

        public NpcWeightedSkinChoice(
            NpcSkinPalette newAsset,
            int newWeight = 1)
        {
            asset = newAsset;
            weight = Mathf.Max(1, newWeight);
        }
    }


    [Serializable]
    public sealed class NpcWeightedOutfitChoice
    {
        [SerializeField]
        private NpcOutfitSet asset;

        [Min(1)]
        [SerializeField]
        private int weight = 1;

        public NpcOutfitSet Asset => asset;

        public int Weight => weight;

        public NpcWeightedOutfitChoice(
            NpcOutfitSet newAsset,
            int newWeight = 1)
        {
            asset = newAsset;
            weight = Mathf.Max(1, newWeight);
        }
    }


    [Serializable]
    public sealed class NpcWeightedHairChoice
    {
        [SerializeField]
        private NpcHairSet asset;

        [Min(1)]
        [SerializeField]
        private int weight = 1;

        public NpcHairSet Asset => asset;

        public int Weight => weight;

        public NpcWeightedHairChoice(
            NpcHairSet newAsset,
            int newWeight = 1)
        {
            asset = newAsset;
            weight = Mathf.Max(1, newWeight);
        }
    }


    [CreateAssetMenu(
        fileName = "CharacterTemplate",
        menuName = "Big Retail/Characters/Character Template")]
    public sealed class NpcCharacterTemplate : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Character Template";

        [SerializeField]
        private NpcCharacterRole role;

        [SerializeField]
        private List<NpcWeightedBodyChoice> bodies =
            new List<NpcWeightedBodyChoice>();

        [SerializeField]
        private List<NpcWeightedSkinChoice> skins =
            new List<NpcWeightedSkinChoice>();

        [SerializeField]
        private List<NpcWeightedOutfitChoice> outfits =
            new List<NpcWeightedOutfitChoice>();

        [SerializeField]
        private List<NpcWeightedHairChoice> hair =
            new List<NpcWeightedHairChoice>();


        public string DisplayName => displayName;

        public NpcCharacterRole Role => role;

        public IReadOnlyList<NpcWeightedBodyChoice> Bodies => bodies;

        public IReadOnlyList<NpcWeightedSkinChoice> Skins => skins;

        public IReadOnlyList<NpcWeightedOutfitChoice> Outfits => outfits;

        public IReadOnlyList<NpcWeightedHairChoice> Hair => hair;


        public void Configure(
            string newDisplayName,
            NpcCharacterRole newRole,
            IEnumerable<NpcWeightedBodyChoice> newBodies,
            IEnumerable<NpcWeightedSkinChoice> newSkins,
            IEnumerable<NpcWeightedOutfitChoice> newOutfits,
            IEnumerable<NpcWeightedHairChoice> newHair)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            role = newRole;
            bodies = CopyChoices(newBodies);
            skins = CopyChoices(newSkins);
            outfits = CopyChoices(newOutfits);
            hair = CopyChoices(newHair);
        }


        public bool Allows(
            NpcBodySilhouette candidate)
        {
            if (candidate == null || bodies == null)
            {
                return false;
            }

            for (int index = 0; index < bodies.Count; index++)
            {
                if (bodies[index]?.Asset == candidate)
                {
                    return true;
                }
            }

            return false;
        }


        public bool Allows(
            NpcSkinPalette candidate)
        {
            if (candidate == null || skins == null)
            {
                return false;
            }

            for (int index = 0; index < skins.Count; index++)
            {
                if (skins[index]?.Asset == candidate)
                {
                    return true;
                }
            }

            return false;
        }


        public bool Allows(
            NpcOutfitSet candidate)
        {
            if (candidate == null || outfits == null)
            {
                return false;
            }

            for (int index = 0; index < outfits.Count; index++)
            {
                if (outfits[index]?.Asset == candidate)
                {
                    return true;
                }
            }

            return false;
        }


        public bool Allows(
            NpcHairSet candidate)
        {
            if (candidate == null || hair == null)
            {
                return false;
            }

            for (int index = 0; index < hair.Count; index++)
            {
                if (hair[index]?.Asset == candidate)
                {
                    return true;
                }
            }

            return false;
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (!TryValidateChoices(
                    bodies,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    "body",
                    out failureReason))
            {
                return false;
            }

            if (!TryValidateChoices(
                    skins,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    "skin",
                    out failureReason))
            {
                return false;
            }

            if (!TryValidateChoices(
                    outfits,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    "outfit",
                    out failureReason))
            {
                return false;
            }

            return TryValidateChoices(
                hair,
                choice => choice?.Asset,
                choice => choice?.Weight ?? 0,
                "hair",
                out failureReason);
        }


        private static List<T> CopyChoices<T>(
            IEnumerable<T> source)
        {
            return source != null
                ? new List<T>(source)
                : new List<T>();
        }


        private static bool TryValidateChoices<TChoice, TAsset>(
            IReadOnlyList<TChoice> choices,
            Func<TChoice, TAsset> getAsset,
            Func<TChoice, int> getWeight,
            string label,
            out string failureReason)
            where TAsset : UnityEngine.Object
        {
            if (choices == null || choices.Count == 0)
            {
                failureReason =
                    $"The template has no allowed {label} choices.";
                return false;
            }

            HashSet<TAsset> uniqueAssets = new HashSet<TAsset>();

            for (int index = 0; index < choices.Count; index++)
            {
                TChoice choice = choices[index];
                TAsset asset = getAsset(choice);

                if (asset == null)
                {
                    failureReason =
                        $"The template has an empty {label} choice.";
                    return false;
                }

                if (getWeight(choice) <= 0)
                {
                    failureReason =
                        $"{asset.name} has a non-positive weight.";
                    return false;
                }

                if (!uniqueAssets.Add(asset))
                {
                    failureReason =
                        $"{asset.name} is listed twice in {label} choices.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
