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


    /// <summary>
    /// The appearance choices for one gender inside one gameplay population.
    /// Customer and Employee behavior remains owned by the parent definition.
    /// </summary>
    [Serializable]
    public sealed class NpcPopulationAppearancePool
    {
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


        public IReadOnlyList<NpcWeightedBodyChoice> Bodies => bodies;

        public IReadOnlyList<NpcWeightedSkinChoice> Skins => skins;

        public IReadOnlyList<NpcWeightedOutfitChoice> Outfits => outfits;

        public IReadOnlyList<NpcWeightedHairChoice> Hair => hair;


        public void Configure(
            IEnumerable<NpcWeightedBodyChoice> newBodies,
            IEnumerable<NpcWeightedSkinChoice> newSkins,
            IEnumerable<NpcWeightedOutfitChoice> newOutfits,
            IEnumerable<NpcWeightedHairChoice> newHair)
        {
            bodies = CopyChoices(newBodies);
            skins = CopyChoices(newSkins);
            outfits = CopyChoices(newOutfits);
            hair = CopyChoices(newHair);
        }


        public bool Allows(
            NpcBodySilhouette candidate)
        {
            return Contains(bodies, choice => choice?.Asset, candidate);
        }


        public bool Allows(
            NpcSkinPalette candidate)
        {
            return Contains(skins, choice => choice?.Asset, candidate);
        }


        public bool Allows(
            NpcOutfitSet candidate)
        {
            return Contains(outfits, choice => choice?.Asset, candidate);
        }


        public bool Allows(
            NpcHairSet candidate)
        {
            return Contains(hair, choice => choice?.Asset, candidate);
        }


        private static bool Contains<TChoice, TAsset>(
            IReadOnlyList<TChoice> choices,
            Func<TChoice, TAsset> getAsset,
            TAsset candidate)
            where TAsset : UnityEngine.Object
        {
            if (candidate == null || choices == null)
            {
                return false;
            }

            for (int index = 0; index < choices.Count; index++)
            {
                if (getAsset(choices[index]) == candidate)
                {
                    return true;
                }
            }

            return false;
        }


        private static List<T> CopyChoices<T>(
            IEnumerable<T> source)
        {
            return source != null
                ? new List<T>(source)
                : new List<T>();
        }
    }


    [CreateAssetMenu(
        fileName = "PopulationDefinition",
        menuName = "Big Retail/Characters/Population Definition")]
    public sealed class NpcPopulationDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Population Definition";

        [SerializeField]
        private NpcCharacterRole role;

        [Min(0)]
        [SerializeField]
        private int menWeight = 1;

        [Min(0)]
        [SerializeField]
        private int womenWeight = 1;

        [SerializeField]
        private NpcPopulationAppearancePool menAppearance =
            new NpcPopulationAppearancePool();

        [SerializeField]
        private NpcPopulationAppearancePool womenAppearance =
            new NpcPopulationAppearancePool();

        [HideInInspector]
        [SerializeField]
        private bool hasGenderAppearancePools;

        // These fields preserve the original serialized layout. Existing
        // assets are split into the two gender pools once, then retained as
        // migration source data so no references are lost.
        [HideInInspector]
        [SerializeField]
        private List<NpcWeightedBodyChoice> bodies =
            new List<NpcWeightedBodyChoice>();

        [HideInInspector]
        [SerializeField]
        private List<NpcWeightedSkinChoice> skins =
            new List<NpcWeightedSkinChoice>();

        [HideInInspector]
        [SerializeField]
        private List<NpcWeightedOutfitChoice> outfits =
            new List<NpcWeightedOutfitChoice>();

        [HideInInspector]
        [SerializeField]
        private List<NpcWeightedHairChoice> hair =
            new List<NpcWeightedHairChoice>();


        public string DisplayName => displayName;

        public NpcCharacterRole Role => role;

        public int MenWeight => menWeight;

        public int WomenWeight => womenWeight;

        public bool HasGenderAppearancePools => hasGenderAppearancePools;

        public NpcPopulationAppearancePool MenAppearance =>
            GetAppearancePool(NpcPersonGender.Man);

        public NpcPopulationAppearancePool WomenAppearance =>
            GetAppearancePool(NpcPersonGender.Woman);


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
            hasGenderAppearancePools = false;
            EnsureGenderAppearancePools();
        }


        public void Configure(
            string newDisplayName,
            NpcCharacterRole newRole,
            NpcPopulationAppearancePool newMenAppearance,
            NpcPopulationAppearancePool newWomenAppearance,
            int newMenWeight = 1,
            int newWomenWeight = 1)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            role = newRole;
            menAppearance = CopyPool(newMenAppearance);
            womenAppearance = CopyPool(newWomenAppearance);
            menWeight = Mathf.Max(0, newMenWeight);
            womenWeight = Mathf.Max(0, newWomenWeight);
            hasGenderAppearancePools = true;
        }


        public bool EnsureGenderAppearancePools()
        {
            if (hasGenderAppearancePools)
            {
                menAppearance ??= new NpcPopulationAppearancePool();
                womenAppearance ??= new NpcPopulationAppearancePool();
                return false;
            }

            menAppearance = CreateFilteredPool(NpcPersonGender.Man);
            womenAppearance = CreateFilteredPool(NpcPersonGender.Woman);
            hasGenderAppearancePools = true;
            InferGenderWeightsFromPools();
            return true;
        }


        public NpcPopulationAppearancePool GetAppearancePool(
            NpcPersonGender gender)
        {
            EnsureGenderAppearancePools();

            return gender == NpcPersonGender.Woman
                ? womenAppearance
                : menAppearance;
        }


        public void SetMetadata(
            string newDisplayName,
            NpcCharacterRole newRole)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            role = newRole;
        }


        public void SetGenderWeights(
            int newMenWeight,
            int newWomenWeight)
        {
            menWeight = Mathf.Max(0, newMenWeight);
            womenWeight = Mathf.Max(0, newWomenWeight);
        }


        public int GetGenderWeight(
            NpcPersonGender gender)
        {
            return gender == NpcPersonGender.Woman
                ? womenWeight
                : menWeight;
        }


        public bool Allows(
            NpcPersonGender gender)
        {
            return GetGenderWeight(gender) > 0;
        }


        public bool Allows(
            NpcPersonGender gender,
            NpcBodySilhouette candidate)
        {
            return GetAppearancePool(gender).Allows(candidate);
        }


        public bool Allows(
            NpcPersonGender gender,
            NpcSkinPalette candidate)
        {
            return GetAppearancePool(gender).Allows(candidate);
        }


        public bool Allows(
            NpcPersonGender gender,
            NpcOutfitSet candidate)
        {
            return GetAppearancePool(gender).Allows(candidate);
        }


        public bool Allows(
            NpcPersonGender gender,
            NpcHairSet candidate)
        {
            return GetAppearancePool(gender).Allows(candidate);
        }


        public bool TryValidate(
            out string failureReason)
        {
            EnsureGenderAppearancePools();

            if (menWeight < 0 || womenWeight < 0)
            {
                failureReason =
                    "Gender generation weights cannot be negative.";
                return false;
            }

            if (menWeight == 0 && womenWeight == 0)
            {
                failureReason =
                    "Enable men, women, or both for this population.";
                return false;
            }

            if (menWeight > 0
                && !TryValidatePool(
                    menAppearance,
                    NpcPersonGender.Man,
                    out failureReason))
            {
                return false;
            }

            if (womenWeight > 0
                && !TryValidatePool(
                    womenAppearance,
                    NpcPersonGender.Woman,
                    out failureReason))
            {
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        private bool TryValidatePool(
            NpcPopulationAppearancePool pool,
            NpcPersonGender gender,
            out string failureReason)
        {
            string label = gender == NpcPersonGender.Woman
                ? "Women"
                : "Men";

            if (pool == null)
            {
                failureReason = $"{label} appearance pool is missing.";
                return false;
            }

            if (!TryValidateChoices(
                    pool.Bodies,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    label + " body",
                    asset => asset.Supports(gender),
                    out failureReason)
                || !TryValidateChoices(
                    pool.Skins,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    label + " skin",
                    asset => true,
                    out failureReason)
                || !TryValidateChoices(
                    pool.Outfits,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    label + " outfit",
                    asset => asset.Supports(gender),
                    out failureReason)
                || !TryValidateChoices(
                    pool.Hair,
                    choice => choice?.Asset,
                    choice => choice?.Weight ?? 0,
                    label + " hair",
                    asset => asset.Supports(gender),
                    out failureReason))
            {
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        private NpcPopulationAppearancePool CreateFilteredPool(
            NpcPersonGender gender)
        {
            NpcPopulationAppearancePool pool =
                new NpcPopulationAppearancePool();

            pool.Configure(
                FilterChoices(
                    bodies,
                    choice => choice?.Asset,
                    asset => asset.Supports(gender)),
                CopyChoices(skins),
                FilterChoices(
                    outfits,
                    choice => choice?.Asset,
                    asset => asset.Supports(gender)),
                FilterChoices(
                    hair,
                    choice => choice?.Asset,
                    asset => asset.Supports(gender)));

            return pool;
        }


        private void InferGenderWeightsFromPools()
        {
            menWeight = PoolHasCompleteRecipe(menAppearance)
                ? Mathf.Max(1, menWeight)
                : 0;

            womenWeight = PoolHasCompleteRecipe(womenAppearance)
                ? Mathf.Max(1, womenWeight)
                : 0;
        }


        private static bool PoolHasCompleteRecipe(
            NpcPopulationAppearancePool pool)
        {
            return pool != null
                   && pool.Bodies.Count > 0
                   && pool.Skins.Count > 0
                   && pool.Outfits.Count > 0
                   && pool.Hair.Count > 0;
        }


        private static NpcPopulationAppearancePool CopyPool(
            NpcPopulationAppearancePool source)
        {
            NpcPopulationAppearancePool copy =
                new NpcPopulationAppearancePool();

            copy.Configure(
                source?.Bodies,
                source?.Skins,
                source?.Outfits,
                source?.Hair);

            return copy;
        }


        private static List<TChoice> FilterChoices<TChoice, TAsset>(
            IReadOnlyList<TChoice> source,
            Func<TChoice, TAsset> getAsset,
            Func<TAsset, bool> isCompatible)
            where TAsset : UnityEngine.Object
        {
            List<TChoice> filtered = new List<TChoice>();

            if (source == null)
            {
                return filtered;
            }

            for (int index = 0; index < source.Count; index++)
            {
                TChoice choice = source[index];
                TAsset asset = getAsset(choice);

                if (asset != null && isCompatible(asset))
                {
                    filtered.Add(choice);
                }
            }

            return filtered;
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
            Func<TAsset, bool> isCompatible,
            out string failureReason)
            where TAsset : UnityEngine.Object
        {
            if (choices == null || choices.Count == 0)
            {
                failureReason =
                    $"The definition has no allowed {label} choices.";
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
                        $"The definition has an empty {label} choice.";
                    return false;
                }

                if (getWeight(choice) <= 0)
                {
                    failureReason =
                        $"{asset.name} has a non-positive weight.";
                    return false;
                }

                if (!isCompatible(asset))
                {
                    failureReason =
                        $"{asset.name} is incompatible with the {label} " +
                        "appearance pool.";
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
