using System;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// One exact, unsaved appearance recipe. Population definitions produce
    /// selections; saved appearance profiles preserve them as project assets.
    /// </summary>
    [Serializable]
    public sealed class NpcAppearanceSelection
    {
        [SerializeField]
        private NpcPersonGender gender;

        [SerializeField]
        private NpcBodySilhouette bodySilhouette;

        [SerializeField]
        private NpcSkinPalette skinPalette;

        [SerializeField]
        private NpcOutfitSet outfitSet;

        [SerializeField]
        private NpcHairSet hairSet;


        public NpcBodySilhouette BodySilhouette => bodySilhouette;

        public NpcPersonGender Gender => gender;

        public NpcSkinPalette SkinPalette => skinPalette;

        public NpcOutfitSet OutfitSet => outfitSet;

        public NpcHairSet HairSet => hairSet;


        public NpcAppearanceSelection()
        {
        }


        public NpcAppearanceSelection(
            NpcBodySilhouette newBodySilhouette,
            NpcSkinPalette newSkinPalette,
            NpcOutfitSet newOutfitSet,
            NpcHairSet newHairSet)
            : this(
                InferGender(newBodySilhouette),
                newBodySilhouette,
                newSkinPalette,
                newOutfitSet,
                newHairSet)
        {
        }


        public NpcAppearanceSelection(
            NpcPersonGender newGender,
            NpcBodySilhouette newBodySilhouette,
            NpcSkinPalette newSkinPalette,
            NpcOutfitSet newOutfitSet,
            NpcHairSet newHairSet)
        {
            Configure(
                newGender,
                newBodySilhouette,
                newSkinPalette,
                newOutfitSet,
                newHairSet);
        }


        public void Configure(
            NpcBodySilhouette newBodySilhouette,
            NpcSkinPalette newSkinPalette,
            NpcOutfitSet newOutfitSet,
            NpcHairSet newHairSet)
        {
            Configure(
                InferGender(newBodySilhouette),
                newBodySilhouette,
                newSkinPalette,
                newOutfitSet,
                newHairSet);
        }


        public void Configure(
            NpcPersonGender newGender,
            NpcBodySilhouette newBodySilhouette,
            NpcSkinPalette newSkinPalette,
            NpcOutfitSet newOutfitSet,
            NpcHairSet newHairSet)
        {
            gender = newGender;
            bodySilhouette = newBodySilhouette;
            skinPalette = newSkinPalette;
            outfitSet = newOutfitSet;
            hairSet = newHairSet;
        }


        public NpcAppearanceSelection Copy()
        {
            return new NpcAppearanceSelection(
                gender,
                bodySilhouette,
                skinPalette,
                outfitSet,
                hairSet);
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (bodySilhouette == null)
            {
                failureReason = "No body silhouette is selected.";
                return false;
            }

            if (!bodySilhouette.TryValidate(out failureReason))
            {
                return false;
            }

            if (!bodySilhouette.Supports(gender))
            {
                failureReason =
                    $"{bodySilhouette.DisplayName} is not a valid body " +
                    $"for a {gender.ToString().ToLowerInvariant()}.";
                return false;
            }

            if (skinPalette == null)
            {
                failureReason = "No skin palette is selected.";
                return false;
            }

            if (outfitSet == null)
            {
                failureReason = "No outfit set is selected.";
                return false;
            }

            if (!outfitSet.TryValidate(out failureReason))
            {
                return false;
            }

            if (!outfitSet.Supports(gender))
            {
                failureReason =
                    $"{outfitSet.DisplayName} does not support {gender}.";
                return false;
            }

            if (hairSet == null)
            {
                failureReason = "No hair set is selected.";
                return false;
            }

            if (!hairSet.TryValidate(out failureReason))
            {
                return false;
            }

            if (!hairSet.Supports(gender))
            {
                failureReason =
                    $"{hairSet.DisplayName} does not support {gender}.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }


        private static NpcPersonGender InferGender(
            NpcBodySilhouette body)
        {
            return body != null
                ? body.Gender
                : NpcPersonGender.Man;
        }
    }


    [Serializable]
    public sealed class NpcAppearanceLocks
    {
        [SerializeField]
        private bool gender;

        [SerializeField]
        private bool body;

        [SerializeField]
        private bool skin;

        [SerializeField]
        private bool outfit;

        [SerializeField]
        private bool hair;


        public bool Body => body;

        public bool Gender => gender;

        public bool Skin => skin;

        public bool Outfit => outfit;

        public bool Hair => hair;


        public void Configure(
            bool lockBody,
            bool lockSkin,
            bool lockOutfit,
            bool lockHair)
        {
            Configure(
                false,
                lockBody,
                lockSkin,
                lockOutfit,
                lockHair);
        }


        public void Configure(
            bool lockGender,
            bool lockBody,
            bool lockSkin,
            bool lockOutfit,
            bool lockHair)
        {
            gender = lockGender;
            body = lockBody;
            skin = lockSkin;
            outfit = lockOutfit;
            hair = lockHair;
        }
    }
}
