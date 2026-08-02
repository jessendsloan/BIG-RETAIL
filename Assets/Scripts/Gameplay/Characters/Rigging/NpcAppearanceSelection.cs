using System;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// One exact, unsaved appearance recipe. Character templates produce
    /// selections; saved appearance profiles preserve them as project assets.
    /// </summary>
    [Serializable]
    public sealed class NpcAppearanceSelection
    {
        [SerializeField]
        private NpcBodySilhouette bodySilhouette;

        [SerializeField]
        private NpcSkinPalette skinPalette;

        [SerializeField]
        private NpcOutfitSet outfitSet;

        [SerializeField]
        private NpcHairSet hairSet;


        public NpcBodySilhouette BodySilhouette => bodySilhouette;

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
        {
            Configure(
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
            bodySilhouette = newBodySilhouette;
            skinPalette = newSkinPalette;
            outfitSet = newOutfitSet;
            hairSet = newHairSet;
        }


        public NpcAppearanceSelection Copy()
        {
            return new NpcAppearanceSelection(
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

            if (hairSet == null)
            {
                failureReason = "No hair set is selected.";
                return false;
            }

            return hairSet.TryValidate(out failureReason);
        }
    }


    [Serializable]
    public sealed class NpcAppearanceLocks
    {
        [SerializeField]
        private bool body;

        [SerializeField]
        private bool skin;

        [SerializeField]
        private bool outfit;

        [SerializeField]
        private bool hair;


        public bool Body => body;

        public bool Skin => skin;

        public bool Outfit => outfit;

        public bool Hair => hair;


        public void Configure(
            bool lockBody,
            bool lockSkin,
            bool lockOutfit,
            bool lockHair)
        {
            body = lockBody;
            skin = lockSkin;
            outfit = lockOutfit;
            hair = lockHair;
        }
    }
}
