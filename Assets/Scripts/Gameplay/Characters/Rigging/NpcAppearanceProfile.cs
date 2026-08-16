using System;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    public enum NpcBodySilhouetteKind
    {
        Masculine = 0,
        Feminine = 1
    }

    public enum NpcAppearanceColorRole
    {
        Preserve = 0,
        Skin = 1,
        PrimaryFabric = 2,
        SecondaryFabric = 3,
        Footwear = 4,
        Accent = 5
    }

    [Serializable]
    public sealed class NpcAppearancePartShape
    {
        [SerializeField]
        private NpcRigPartId id;

        [SerializeField]
        private Vector3 localPosition;

        [SerializeField]
        private Vector3 localEulerAngles;

        [SerializeField]
        private Vector2 size = Vector2.one;

        [SerializeField]
        private bool visible = true;


        public NpcRigPartId Id => id;

        public Vector3 LocalPosition => localPosition;

        public Vector3 LocalEulerAngles => localEulerAngles;

        public Vector2 Size => size;

        public bool Visible => visible;


        public NpcAppearancePartShape(
            NpcRigPartId id,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector2 size,
            bool visible = true)
        {
            this.id = id;
            this.localPosition = localPosition;
            this.localEulerAngles = localEulerAngles;
            this.size = size;
            this.visible = visible;
        }


        public NpcAppearancePartShape WithSize(
            Vector2 newSize)
        {
            return new NpcAppearancePartShape(
                id,
                localPosition,
                localEulerAngles,
                newSize,
                visible);
        }


        public void Apply(
            SpriteRenderer renderer,
            NpcAuthoredDirection direction)
        {
            if (renderer == null)
            {
                return;
            }

            Transform partTransform = renderer.transform;

            partTransform.localPosition =
                NpcFacingUtility.ResolveAuthoredPartPosition(
                    direction,
                    id,
                    localPosition);
            partTransform.localEulerAngles =
                NpcFacingUtility.ResolveAuthoredPartEulerAngles(
                    direction,
                    id,
                    localEulerAngles);
            NpcAppearanceUtility.ApplySpriteSize(renderer, size);
        }
    }

    [Serializable]
    public sealed class NpcAppearanceBonePlacement
    {
        [SerializeField]
        private NpcRigBoneId id;

        [SerializeField]
        private Vector3 localPosition;


        public NpcRigBoneId Id => id;

        public Vector3 LocalPosition => localPosition;


        public NpcAppearanceBonePlacement(
            NpcRigBoneId id,
            Vector3 localPosition)
        {
            this.id = id;
            this.localPosition = localPosition;
        }
    }
    [CreateAssetMenu(
        fileName = "PersonAppearance",
        menuName = "Big Retail/Characters/Person Appearance")]
    public sealed class NpcAppearanceProfile : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Person";

        [SerializeField]
        private NpcPersonGender gender;

        [SerializeField, HideInInspector]
        private bool hasExplicitGender;

        [SerializeField]
        private NpcBodySilhouette bodySilhouette;

        [SerializeField]
        private NpcSkinPalette skinPalette;

        [SerializeField]
        private NpcOutfitSet outfitSet;

        [SerializeField]
        private NpcHairSet hairSet;


        public string DisplayName => displayName;

        public NpcPersonGender Gender => hasExplicitGender
            ? gender
            : bodySilhouette != null
                ? bodySilhouette.Gender
                : gender;

        public NpcBodySilhouette BodySilhouette => bodySilhouette;

        public NpcSkinPalette SkinPalette => skinPalette;

        public NpcOutfitSet OutfitSet => outfitSet;

        public NpcHairSet HairSet => hairSet;


        public void Configure(
            string newDisplayName,
            NpcBodySilhouette newBodySilhouette,
            NpcSkinPalette newSkinPalette,
            NpcOutfitSet newOutfitSet,
            NpcHairSet newHairSet)
        {
            Configure(
                newDisplayName,
                newBodySilhouette != null
                    ? newBodySilhouette.Gender
                    : NpcPersonGender.Man,
                newBodySilhouette,
                newSkinPalette,
                newOutfitSet,
                newHairSet);
        }


        public void Configure(
            string newDisplayName,
            NpcPersonGender newGender,
            NpcBodySilhouette newBodySilhouette,
            NpcSkinPalette newSkinPalette,
            NpcOutfitSet newOutfitSet,
            NpcHairSet newHairSet)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            gender = newGender;
            hasExplicitGender = true;
            bodySilhouette = newBodySilhouette;
            skinPalette = newSkinPalette;
            outfitSet = newOutfitSet;
            hairSet = newHairSet;
        }


        public void Configure(
            string newDisplayName,
            NpcAppearanceSelection selection)
        {
            Configure(
                newDisplayName,
                selection?.Gender ?? NpcPersonGender.Man,
                selection?.BodySilhouette,
                selection?.SkinPalette,
                selection?.OutfitSet,
                selection?.HairSet);
        }


        public NpcAppearanceSelection CreateSelection()
        {
            return new NpcAppearanceSelection(
                Gender,
                bodySilhouette,
                skinPalette,
                outfitSet,
                hairSet);
        }


        public void ApplyBonePlacements(
            NpcCutoutRig rig)
        {
            ApplyBonePlacements(
                rig,
                NpcAuthoredDirection.SouthEast);
        }


        public void ApplyBonePlacements(
            NpcCutoutRig rig,
            NpcAuthoredDirection direction)
        {
            NpcAppearanceApplicator.ApplyBonePlacements(
                CreateSelection(),
                rig,
                direction);
        }


        public void ApplyPart(
            NpcRigPartId partId,
            SpriteRenderer renderer,
            NpcAuthoredDirection direction)
        {
            NpcAppearanceApplicator.ApplyPart(
                CreateSelection(),
                partId,
                renderer,
                direction);
        }


        public bool TryValidate(
            out string failureReason)
        {
            return CreateSelection().TryValidate(out failureReason);
        }
    }

    public static class NpcAppearanceUtility
    {
        public static void ApplySpriteSize(
            SpriteRenderer renderer,
            Vector2 size)
        {
            if (renderer == null)
            {
                return;
            }

            Transform partTransform = renderer.transform;
            Sprite sprite = renderer.sprite;

            if (sprite == null)
            {
                renderer.drawMode = SpriteDrawMode.Simple;
                partTransform.localScale =
                    new Vector3(size.x, size.y, 1f);
                return;
            }

            // Bordered sprites are the scalable building blocks for the
            // cutout person. Resize their sliced geometry instead of
            // stretching the Transform so corners and drawn edges retain
            // their intended thickness at different body proportions.
            if (sprite.border.sqrMagnitude > 0.0001f)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
                partTransform.localScale = Vector3.one;
                return;
            }

            Vector2 spriteSize = sprite.bounds.size;

            renderer.drawMode = SpriteDrawMode.Simple;
            partTransform.localScale =
                new Vector3(
                    size.x / Mathf.Max(spriteSize.x, 0.0001f),
                    size.y / Mathf.Max(spriteSize.y, 0.0001f),
                    1f);
        }


        public static bool IsAlwaysSkin(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.Head:
                case NpcRigPartId.Neck:
                case NpcRigPartId.HandForeground:
                case NpcRigPartId.HandBackground:
                    return true;

                default:
                    return false;
            }
        }


        public static bool IsNearPart(
            NpcRigPartId partId)
        {
            return NpcFacingUtility.IsNearPart(partId);
        }


        public static bool IsFarPart(
            NpcRigPartId partId)
        {
            return NpcFacingUtility.IsFarPart(partId);
        }


        public static Color Shade(
            Color color,
            float multiplier)
        {
            return new Color(
                color.r * multiplier,
                color.g * multiplier,
                color.b * multiplier,
                color.a);
        }
    }
}
