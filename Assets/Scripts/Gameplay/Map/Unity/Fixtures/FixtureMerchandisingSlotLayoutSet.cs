using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Optional, direction-specific local positions for merchandise artwork
    /// on one logical fixture face. Positions are stored top shelf to bottom
    /// shelf, then visually left to right within each shelf.
    /// </summary>
    [Serializable]
    public sealed class FixtureMerchandisingSlotLayoutSet
    {
        private static readonly Vector2[] EmptyAnchors =
            Array.Empty<Vector2>();

        [SerializeField]
        private FixtureSide localDisplaySide = FixtureSide.South;

        [HideInInspector]
        [SerializeField]
        private Vector2[] northProductAnchors = Array.Empty<Vector2>();

        [HideInInspector]
        [SerializeField]
        private Vector2[] eastProductAnchors = Array.Empty<Vector2>();

        [HideInInspector]
        [SerializeField]
        private Vector2[] southProductAnchors = Array.Empty<Vector2>();

        [HideInInspector]
        [SerializeField]
        private Vector2[] westProductAnchors = Array.Empty<Vector2>();


        public FixtureSide LocalDisplaySide => localDisplaySide;

        public bool HasAnyAnchors =>
            GetAnchorCount(northProductAnchors) > 0
            || GetAnchorCount(eastProductAnchors) > 0
            || GetAnchorCount(southProductAnchors) > 0
            || GetAnchorCount(westProductAnchors) > 0;


        public IReadOnlyList<Vector2> GetProductAnchors(
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

            Vector2[] anchors =
                relativeOrientation switch
                {
                    0 => northProductAnchors,
                    1 => eastProductAnchors,
                    2 => southProductAnchors,
                    3 => westProductAnchors,
                    _ => null
                };

            return anchors ?? EmptyAnchors;
        }


        public bool TryGetProductAnchor(
            FixtureOrientation worldOrientation,
            IsometricViewOrientation viewOrientation,
            int shelfIndex,
            int visualFrontageIndex,
            int frontageUnitsPerShelf,
            out Vector2 anchor)
        {
            anchor = default;

            if (shelfIndex < 0
                || visualFrontageIndex < 0
                || frontageUnitsPerShelf <= 0
                || visualFrontageIndex >= frontageUnitsPerShelf)
            {
                return false;
            }

            IReadOnlyList<Vector2> anchors =
                GetProductAnchors(
                    worldOrientation,
                    viewOrientation);
            int anchorIndex =
                shelfIndex * frontageUnitsPerShelf
                + visualFrontageIndex;

            if (anchorIndex < 0 || anchorIndex >= anchors.Count)
            {
                return false;
            }

            anchor = anchors[anchorIndex];
            return IsFinite(anchor);
        }


        public void ValidateConfiguration(
            string fixtureName,
            FixtureMerchandisingProfile merchandisingProfile)
        {
            if (!localDisplaySide.IsSupported())
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' contains an "
                    + "unsupported merchandising-slot-layout side.");
            }

            if (!merchandisingProfile.TryGetDisplayFace(
                    localDisplaySide,
                    out FixtureDisplayFaceDefinition displayFace))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' authors product "
                    + $"anchors for non-merchandisable side "
                    + $"'{localDisplaySide}'.");
            }

            int expectedAnchorCount =
                displayFace.ShelfRunCount
                * displayFace.FrontageUnitsPerRun;

            ValidateDirection(
                fixtureName,
                "north",
                northProductAnchors,
                expectedAnchorCount);
            ValidateDirection(
                fixtureName,
                "east",
                eastProductAnchors,
                expectedAnchorCount);
            ValidateDirection(
                fixtureName,
                "south",
                southProductAnchors,
                expectedAnchorCount);
            ValidateDirection(
                fixtureName,
                "west",
                westProductAnchors,
                expectedAnchorCount);
        }


        private static void ValidateDirection(
            string fixtureName,
            string directionName,
            Vector2[] anchors,
            int expectedAnchorCount)
        {
            int anchorCount = GetAnchorCount(anchors);

            if (anchorCount == 0)
            {
                return;
            }

            if (anchorCount != expectedAnchorCount)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{fixtureName}' requires "
                    + $"{expectedAnchorCount} {directionName} merchandise "
                    + $"anchors, but contains {anchorCount}.");
            }

            for (int index = 0; index < anchors.Length; index++)
            {
                if (!IsFinite(anchors[index]))
                {
                    throw new InvalidOperationException(
                        $"Fixture definition '{fixtureName}' contains a "
                        + $"non-finite {directionName} merchandise anchor "
                        + $"at index {index}.");
                }
            }
        }


        private static int GetAnchorCount(
            Vector2[] anchors)
        {
            return anchors?.Length ?? 0;
        }


        private static bool IsFinite(
            Vector2 value)
        {
            return !float.IsNaN(value.x)
                && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y)
                && !float.IsInfinity(value.y);
        }
    }
}
