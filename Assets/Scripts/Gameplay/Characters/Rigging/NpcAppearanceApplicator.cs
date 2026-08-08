using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// Applies one exact appearance selection to the shared cutout rig.
    /// Both saved authoring profiles and runtime population identities use
    /// this path, so previewed people and spawned people render identically.
    /// </summary>
    public static class NpcAppearanceApplicator
    {
        public static void ApplyBonePlacements(
            NpcAppearanceSelection selection,
            NpcCutoutRig rig)
        {
            selection?.BodySilhouette?.ApplyBonePlacements(rig);
        }


        public static void ApplyPart(
            NpcAppearanceSelection selection,
            NpcRigPartId partId,
            SpriteRenderer renderer,
            NpcAuthoredDirection direction)
        {
            if (selection == null || renderer == null)
            {
                return;
            }

            NpcBodySilhouette bodySilhouette =
                selection.BodySilhouette;
            NpcSkinPalette skinPalette =
                selection.SkinPalette;
            NpcOutfitSet outfitSet =
                selection.OutfitSet;
            NpcHairSet hairSet =
                selection.HairSet;

            NpcAppearancePartShape shape = null;

            bodySilhouette?.TryGetPartShape(
                partId,
                out shape);

            bool finalVisible = shape == null || shape.Visible;

            NpcOutfitPartStyle hairSpriteStyle = null;
            NpcAppearancePartShape hairShape = null;

            bool isHair = hairSet != null
                && hairSet.TryGetStyle(
                    partId,
                    out hairSpriteStyle,
                    out hairShape);

            if (isHair)
            {
                Sprite hairSprite =
                    hairSpriteStyle?.GetSprite(direction);

                if (hairSprite != null)
                {
                    renderer.sprite = hairSprite;
                }

                if (hairShape != null)
                {
                    shape = hairShape;
                }

                renderer.color = hairSet.HairColor;
                finalVisible &=
                    hairSpriteStyle == null
                    || hairSpriteStyle.Visible;
            }
            else if (NpcAppearanceUtility.IsAlwaysSkin(partId))
            {
                renderer.color = skinPalette != null
                    ? skinPalette.GetColor(
                        NpcAppearanceUtility
                            .IsFarPart(partId))
                    : renderer.color;
            }
            else if (outfitSet != null
                     && outfitSet.TryGetPartStyle(
                         partId,
                         out NpcOutfitPartStyle outfitStyle))
            {
                Sprite outfitSprite =
                    outfitStyle.GetSprite(direction);

                if (outfitSprite != null)
                {
                    renderer.sprite = outfitSprite;
                }

                if (outfitStyle.MaterialOverride != null)
                {
                    renderer.sharedMaterial =
                        outfitStyle.MaterialOverride;
                }

                if (outfitStyle.ColorRole
                    != NpcAppearanceColorRole.Preserve)
                {
                    renderer.color = outfitSet.GetColor(
                        outfitStyle.ColorRole,
                        skinPalette,
                        NpcAppearanceUtility
                            .IsFarPart(partId));
                }

                finalVisible &= outfitStyle.Visible;
            }

            shape?.Apply(renderer);
            renderer.enabled = finalVisible;
        }
    }
}
