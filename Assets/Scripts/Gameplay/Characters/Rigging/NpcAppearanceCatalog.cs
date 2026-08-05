using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BigRetail.Characters.Rigging
{
    [CreateAssetMenu(
        fileName = "AppearanceCatalog",
        menuName = "Big Retail/Characters/Appearance Catalog")]
    public sealed class NpcAppearanceCatalog : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Appearance Catalog";

        [FormerlySerializedAs("templates")]
        [SerializeField]
        private List<NpcPopulationDefinition> populationDefinitions =
            new List<NpcPopulationDefinition>();

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

        public IReadOnlyList<NpcPopulationDefinition> Definitions =>
            populationDefinitions;

        public IReadOnlyList<NpcBodySilhouette> Bodies => bodies;

        public IReadOnlyList<NpcSkinPalette> Skins => skins;

        public IReadOnlyList<NpcOutfitSet> Outfits => outfits;

        public IReadOnlyList<NpcHairSet> Hair => hair;


        public void Configure(
            string newDisplayName,
            IEnumerable<NpcPopulationDefinition> newDefinitions,
            IEnumerable<NpcBodySilhouette> newBodies,
            IEnumerable<NpcSkinPalette> newSkins,
            IEnumerable<NpcOutfitSet> newOutfits,
            IEnumerable<NpcHairSet> newHair)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            populationDefinitions = Copy(newDefinitions);
            bodies = Copy(newBodies);
            skins = Copy(newSkins);
            outfits = Copy(newOutfits);
            hair = Copy(newHair);
        }


        public IReadOnlyList<NpcPopulationDefinition> GetDefinitions(
            NpcCharacterRole role)
        {
            List<NpcPopulationDefinition> matches =
                new List<NpcPopulationDefinition>();

            if (populationDefinitions == null)
            {
                return matches;
            }

            for (int index = 0;
                 index < populationDefinitions.Count;
                 index++)
            {
                NpcPopulationDefinition candidate =
                    populationDefinitions[index];

                if (candidate != null && candidate.Role == role)
                {
                    matches.Add(candidate);
                }
            }

            return matches;
        }


        public bool AddDefinition(
            NpcPopulationDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            populationDefinitions ??=
                new List<NpcPopulationDefinition>();

            if (populationDefinitions.Contains(definition))
            {
                return false;
            }

            populationDefinitions.Add(definition);
            return true;
        }


        public bool RegisterAssetsFrom(
            NpcPopulationDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            definition.EnsureGenderAppearancePools();

            bool changed = RegisterAssetsFrom(definition.MenAppearance);
            changed |= RegisterAssetsFrom(definition.WomenAppearance);
            return changed;
        }

        private bool RegisterAssetsFrom(
            NpcPopulationAppearancePool pool)
        {
            if (pool == null)
            {
                return false;
            }

            bool changed = false;

            for (int index = 0; index < pool.Bodies.Count; index++)
            {
                changed |= RegisterAsset(
                    bodies,
                    pool.Bodies[index]?.Asset);
            }

            for (int index = 0; index < pool.Skins.Count; index++)
            {
                changed |= RegisterAsset(
                    skins,
                    pool.Skins[index]?.Asset);
            }

            for (int index = 0; index < pool.Outfits.Count; index++)
            {
                changed |= RegisterAsset(
                    outfits,
                    pool.Outfits[index]?.Asset);
            }

            for (int index = 0; index < pool.Hair.Count; index++)
            {
                changed |= RegisterAsset(
                    hair,
                    pool.Hair[index]?.Asset);
            }

            return changed;
        }


        public bool RegisterAsset(
            NpcBodySilhouette asset)
        {
            return RegisterAsset(bodies, asset);
        }


        public bool RegisterAsset(
            NpcSkinPalette asset)
        {
            return RegisterAsset(skins, asset);
        }


        public bool RegisterAsset(
            NpcOutfitSet asset)
        {
            return RegisterAsset(outfits, asset);
        }


        public bool RegisterAsset(
            NpcHairSet asset)
        {
            return RegisterAsset(hair, asset);
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (populationDefinitions == null
                || populationDefinitions.Count == 0)
            {
                failureReason =
                    "The catalog has no population definitions.";
                return false;
            }

            for (int index = 0;
                 index < populationDefinitions.Count;
                 index++)
            {
                NpcPopulationDefinition definition =
                    populationDefinitions[index];

                if (definition == null)
                {
                    failureReason =
                        "The catalog contains an empty population definition.";
                    return false;
                }

                if (!definition.TryValidate(out failureReason))
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


        private static bool RegisterAsset<T>(
            List<T> assets,
            T asset)
            where T : UnityEngine.Object
        {
            if (assets == null || asset == null || assets.Contains(asset))
            {
                return false;
            }

            assets.Add(asset);
            return true;
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
                    $"The catalog has no registered {label} assets.";
                return false;
            }

            HashSet<T> uniqueAssets = new HashSet<T>();

            for (int index = 0; index < assets.Count; index++)
            {
                T asset = assets[index];

                if (asset == null)
                {
                    failureReason =
                        $"The catalog contains an empty {label} asset.";
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
