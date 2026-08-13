using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Authored shelf surfaces for a storage fixture. Directional masks share
    /// the canvas, pivot, and pixels-per-unit of the matching fixture sprite.
    /// They are ordered from the top shelf to the bottom shelf.
    /// </summary>
    [Serializable]
    public sealed class FixtureStorageShelfMaskSet
    {
        private static readonly Sprite[] EmptyMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] northShelfMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] eastShelfMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] southShelfMasks = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] westShelfMasks = Array.Empty<Sprite>();


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
            Sprite northSprite,
            Sprite eastSprite,
            Sprite southSprite,
            Sprite westSprite)
        {
            int expectedShelfCount = ResolveExpectedShelfCount(fixtureName);

            ValidateDirection(
                fixtureName,
                "north",
                northSprite,
                northShelfMasks,
                expectedShelfCount);
            ValidateDirection(
                fixtureName,
                "east",
                eastSprite,
                eastShelfMasks,
                expectedShelfCount);
            ValidateDirection(
                fixtureName,
                "south",
                southSprite,
                southShelfMasks,
                expectedShelfCount);
            ValidateDirection(
                fixtureName,
                "west",
                westSprite,
                westShelfMasks,
                expectedShelfCount);
        }


        private int ResolveExpectedShelfCount(string fixtureName)
        {
            int expectedShelfCount = 0;
            ResolveExpectedShelfCount(
                fixtureName,
                "north",
                northShelfMasks,
                ref expectedShelfCount);
            ResolveExpectedShelfCount(
                fixtureName,
                "east",
                eastShelfMasks,
                ref expectedShelfCount);
            ResolveExpectedShelfCount(
                fixtureName,
                "south",
                southShelfMasks,
                ref expectedShelfCount);
            ResolveExpectedShelfCount(
                fixtureName,
                "west",
                westShelfMasks,
                ref expectedShelfCount);
            return expectedShelfCount;
        }


        private static void ResolveExpectedShelfCount(
            string fixtureName,
            string directionName,
            Sprite[] shelfMasks,
            ref int expectedShelfCount)
        {
            int maskCount = GetMaskCount(shelfMasks);

            if (maskCount == 0)
            {
                return;
            }

            if (expectedShelfCount == 0)
            {
                expectedShelfCount = maskCount;
                return;
            }

            if (maskCount != expectedShelfCount)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' requires {expectedShelfCount} storage shelf mask(s) per authored direction, but {directionName} contains {maskCount}.");
            }
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
                    $"Fixture definition '{fixtureName}' requires {expectedShelfCount} {directionName} storage shelf mask(s), but contains {maskCount}.");
            }

            if (fixtureSprite == null)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' cannot align {directionName} storage shelf masks without a directional sprite.");
            }

            for (int index = 0; index < shelfMasks.Length; index++)
            {
                Sprite mask = shelfMasks[index];

                if (mask == null)
                {
                    throw new InvalidOperationException(
                        $"Fixture definition '{fixtureName}' has an empty {directionName} storage shelf mask at index {index}.");
                }

                bool rectMatches =
                    Mathf.Abs(mask.rect.width - fixtureSprite.rect.width) <= 1f
                    && Mathf.Abs(mask.rect.height - fixtureSprite.rect.height) <= 1f;
                bool pivotMatches =
                    Vector2.Distance(mask.pivot, fixtureSprite.pivot) <= 0.01f;
                bool scaleMatches =
                    Mathf.Approximately(
                        mask.pixelsPerUnit,
                        fixtureSprite.pixelsPerUnit);

                if (!rectMatches || !pivotMatches || !scaleMatches)
                {
                    throw new InvalidOperationException(
                        $"Fixture definition '{fixtureName}' has a misaligned {directionName} storage shelf mask '{mask.name}'. Its canvas, pivot, and pixels-per-unit must match '{fixtureSprite.name}'.");
                }
            }
        }


        private static int GetMaskCount(Sprite[] masks)
        {
            return masks?.Length ?? 0;
        }
    }
}
