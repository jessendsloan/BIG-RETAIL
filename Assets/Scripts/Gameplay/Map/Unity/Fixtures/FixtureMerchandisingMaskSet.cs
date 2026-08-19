using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Authored shelf-surface masks for one logical display face. Each
    /// directional array follows the same presentation selection as the
    /// fixture sprite and is ordered by logical shelf-run index.
    /// </summary>
    [Serializable]
    public sealed class FixtureMerchandisingMaskSet
    {
        private static readonly Sprite[] EmptyMasks = Array.Empty<Sprite>();

        [SerializeField]
        private FixtureSide localDisplaySide = FixtureSide.South;

        [SerializeField]
        private Sprite[] northShelfMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] eastShelfMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] southShelfMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] westShelfMasks = Array.Empty<Sprite>();


        public FixtureSide LocalDisplaySide => localDisplaySide;

        public bool HasAnyMasks =>
            GetMaskCount(northShelfMasks) > 0
            || GetMaskCount(eastShelfMasks) > 0
            || GetMaskCount(southShelfMasks) > 0
            || GetMaskCount(westShelfMasks) > 0;


        public IReadOnlyList<Sprite> GetShelfMasks(
            FixtureOrientation worldOrientation,
            IsometricViewOrientation viewOrientation)
        {
            if (!worldOrientation.IsSupported())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldOrientation));
            }

            int relativeOrientation =
                ((int)worldOrientation
                    - (int)viewOrientation
                    + 4)
                % 4;

            Sprite[] masks =
                relativeOrientation switch
                {
                    0 => northShelfMasks,
                    1 => eastShelfMasks,
                    2 => southShelfMasks,
                    3 => westShelfMasks,
                    _ => null
                };

            return masks ?? EmptyMasks;
        }


        public void ValidateConfiguration(
            string fixtureName,
            FixtureMerchandisingProfile merchandisingProfile,
            Sprite northSprite,
            Sprite eastSprite,
            Sprite southSprite,
            Sprite westSprite)
        {
            if (!localDisplaySide.IsSupported())
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' contains an unsupported merchandising-mask side.");
            }

            if (!merchandisingProfile.TryGetDisplayFace(
                    localDisplaySide,
                    out FixtureDisplayFaceDefinition displayFace))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' authors shelf masks for non-merchandisable side '{localDisplaySide}'.");
            }

            ValidateDirection(
                fixtureName,
                "north",
                northSprite,
                northShelfMasks,
                displayFace.ShelfRunCount);
            ValidateDirection(
                fixtureName,
                "east",
                eastSprite,
                eastShelfMasks,
                displayFace.ShelfRunCount);
            ValidateDirection(
                fixtureName,
                "south",
                southSprite,
                southShelfMasks,
                displayFace.ShelfRunCount);
            ValidateDirection(
                fixtureName,
                "west",
                westSprite,
                westShelfMasks,
                displayFace.ShelfRunCount);
        }


        private static void ValidateDirection(
            string fixtureName,
            string directionName,
            Sprite fixtureSprite,
            Sprite[] shelfMasks,
            int expectedShelfCount)
        {
            int maskCount = GetMaskCount(shelfMasks);

            if (maskCount == 0)
            {
                return;
            }

            if (maskCount != expectedShelfCount)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' requires {expectedShelfCount} {directionName} shelf mask(s), but contains {maskCount}.");
            }

            for (int index = 0; index < shelfMasks.Length; index++)
            {
                Sprite mask = shelfMasks[index];

                if (mask == null)
                {
                    throw new InvalidOperationException(
                        $"Fixture definition '{fixtureName}' has an empty {directionName} shelf mask at index {index}.");
                }

                ValidateAlignment(
                    fixtureName,
                    directionName,
                    fixtureSprite,
                    mask);
            }
        }


        private static void ValidateAlignment(
            string fixtureName,
            string directionName,
            Sprite fixtureSprite,
            Sprite mask)
        {
            if (fixtureSprite == null)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' cannot align {directionName} shelf masks without a directional sprite.");
            }

            // A one-pixel transparent export pad is visually inert when the
            // authored pivot and pixels-per-unit still match. Accept that
            // narrow tolerance while continuing to reject real misalignment.
            bool rectMatches =
                Mathf.Abs(mask.rect.width - fixtureSprite.rect.width) <= 1f
                && Mathf.Abs(mask.rect.height - fixtureSprite.rect.height) <= 1f;
            bool pivotMatches =
                Vector2.Distance(mask.pivot, fixtureSprite.pivot) <= 0.01f;
            bool scaleMatches =
                Mathf.Approximately(
                    mask.pixelsPerUnit,
                    fixtureSprite.pixelsPerUnit);

            if (rectMatches && pivotMatches && scaleMatches)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Fixture definition '{fixtureName}' has a misaligned {directionName} shelf mask '{mask.name}'. Its canvas, pivot, and pixels-per-unit must match '{fixtureSprite.name}'.");
        }


        private static int GetMaskCount(Sprite[] masks)
        {
            return masks?.Length ?? 0;
        }
    }
}
