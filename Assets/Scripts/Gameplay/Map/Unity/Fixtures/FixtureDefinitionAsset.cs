using System;
using BigRetail.Map.Fixtures;
using BigRetail.Map.View;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Identifies which authored floor-contact point a whole-fixture sprite
    /// uses. Directional art can therefore anchor to a stable footprint
    /// corner instead of relying on a hand-tuned world offset.
    /// </summary>
    public enum FixtureSpriteAnchorCorner
    {
        ViewerNearest = 0,
        ViewerBackLeft = 1,
        ViewerBackRight = 2
    }


    /// <summary>
    /// Unity authoring data for one placeable fixture model.
    /// The footprint is engine-free; sprites are presentation only.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Fixtures/Fixture Definition",
        fileName = "FixtureDefinition")]
    public sealed class FixtureDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string definitionId = "standard-shelf";

        [SerializeField]
        private string displayName = "Standard Shelf";

        [Min(1)]
        [SerializeField]
        private int widthInCells = 2;

        [Min(1)]
        [SerializeField]
        private int depthInCells = 1;

        [SerializeField]
        private Sprite catalogIcon;

        [Header("Directional Presentation")]

        [SerializeField]
        private Sprite northSprite;

        [SerializeField]
        private Sprite eastSprite;

        [SerializeField]
        private Sprite southSprite;

        [SerializeField]
        private Sprite westSprite;

        [Header("Directional Sprite Anchors")]

        [Tooltip(
            "Footprint corner used by the north presentation sprite's "
            + "authored pivot.")]
        [SerializeField]
        private FixtureSpriteAnchorCorner northSpriteAnchorCorner =
            FixtureSpriteAnchorCorner.ViewerNearest;

        [SerializeField]
        private FixtureSpriteAnchorCorner eastSpriteAnchorCorner =
            FixtureSpriteAnchorCorner.ViewerNearest;

        [SerializeField]
        private FixtureSpriteAnchorCorner southSpriteAnchorCorner =
            FixtureSpriteAnchorCorner.ViewerNearest;

        [SerializeField]
        private FixtureSpriteAnchorCorner westSpriteAnchorCorner =
            FixtureSpriteAnchorCorner.ViewerNearest;

        [Tooltip(
            "Useful for tile-sized placeholder art. Disable this when a "
            + "future shelf sprite spans the whole footprint.")]
        [SerializeField]
        private bool repeatSpritePerOccupiedCell = true;

        [SerializeField]
        private Vector3 worldPositionOffset = Vector3.zero;

        [Header("Retail Access")]

        [Tooltip(
            "Interactions supported from the authored positive-Y side.")]
        [SerializeField]
        private FixtureAccessMode northAccess = FixtureAccessMode.None;

        [Tooltip(
            "Interactions supported from the authored positive-X side.")]
        [SerializeField]
        private FixtureAccessMode eastAccess = FixtureAccessMode.None;

        [Tooltip(
            "Interactions supported from the authored negative-Y side.")]
        [SerializeField]
        private FixtureAccessMode southAccess = FixtureAccessMode.None;

        [Tooltip(
            "Interactions supported from the authored negative-X side.")]
        [SerializeField]
        private FixtureAccessMode westAccess = FixtureAccessMode.None;


        public FixtureDefinitionId Id
        {
            get
            {
                ValidateIdentifier();
                return new FixtureDefinitionId(definitionId);
            }
        }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? definitionId
                : displayName;

        public int WidthInCells => widthInCells;

        public int DepthInCells => depthInCells;

        public Sprite CatalogIcon => catalogIcon;

        public bool RepeatSpritePerOccupiedCell =>
            repeatSpritePerOccupiedCell;

        public Vector3 WorldPositionOffset =>
            worldPositionOffset;


        public FixtureDefinition CreateDomainDefinition()
        {
            ValidateConfiguration();

            return new FixtureDefinition(
                Id,
                DisplayName,
                widthInCells,
                depthInCells,
                new FixtureAccessProfile(
                    northAccess,
                    eastAccess,
                    southAccess,
                    westAccess));
        }


        public Sprite GetSprite(
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

            return relativeOrientation switch
            {
                0 => northSprite,
                1 => eastSprite,
                2 => southSprite,
                3 => westSprite,
                _ => northSprite
            };
        }


        public FixtureSpriteAnchorCorner GetSpriteAnchorCorner(
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

            return relativeOrientation switch
            {
                0 => northSpriteAnchorCorner,
                1 => eastSpriteAnchorCorner,
                2 => southSpriteAnchorCorner,
                3 => westSpriteAnchorCorner,
                _ => FixtureSpriteAnchorCorner.ViewerNearest
            };
        }


        public void ValidateConfiguration()
        {
            ValidateIdentifier();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' requires a display name.");
            }

            if (widthInCells <= 0 || depthInCells <= 0)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' requires a positive footprint.");
            }

            if (northSprite == null
                || eastSprite == null
                || southSprite == null
                || westSprite == null)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' requires all four directional sprites.");
            }

            if (!Enum.IsDefined(
                    typeof(FixtureSpriteAnchorCorner),
                    northSpriteAnchorCorner)
                || !Enum.IsDefined(
                    typeof(FixtureSpriteAnchorCorner),
                    eastSpriteAnchorCorner)
                || !Enum.IsDefined(
                    typeof(FixtureSpriteAnchorCorner),
                    southSpriteAnchorCorner)
                || !Enum.IsDefined(
                    typeof(FixtureSpriteAnchorCorner),
                    westSpriteAnchorCorner))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' contains an "
                    + "unsupported sprite anchor corner.");
            }

            if (!northAccess.IsSupported()
                || !eastAccess.IsSupported()
                || !southAccess.IsSupported()
                || !westAccess.IsSupported())
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' contains an unsupported access mode.");
            }
        }


        private void ValidateIdentifier()
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' requires an identifier.");
            }
        }
    }
}
