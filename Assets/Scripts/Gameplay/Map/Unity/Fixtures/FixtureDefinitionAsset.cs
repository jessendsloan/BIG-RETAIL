using System;
using System.Collections.Generic;
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

        [Header("Directional Presentation Layers")]

        [Tooltip(
            "Optional back-to-front layers for the north presentation. "
            + "An empty collection keeps the combined north sprite.")]
        [SerializeField]
        private Sprite[] northPresentationLayers = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] eastPresentationLayers = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] southPresentationLayers = Array.Empty<Sprite>();

        [SerializeField]
        private Sprite[] westPresentationLayers = Array.Empty<Sprite>();

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

        [Header("Directional Merchandising Masks")]

        [Tooltip(
            "Optional authored shelf surfaces grouped by logical fixture "
            + "display face. A direction can remain empty when that face is "
            + "hidden by the matching presentation sprite.")]
        [SerializeField]
        private FixtureMerchandisingMaskSet[] merchandisingMaskSets =
            Array.Empty<FixtureMerchandisingMaskSet>();

        [Header("Directional Backstock Shelf Masks")]

        [Tooltip(
            "Optional authored shelf surfaces used to align visible "
            + "backstock case markers. Masks are ordered top to bottom.")]
        [SerializeField]
        private FixtureStorageShelfMaskSet storageShelfMasks =
            new FixtureStorageShelfMaskSet();

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

        [Tooltip(
            "All authored access points are normally required. Storage racks "
            + "may instead require one complete usable side.")]
        [SerializeField]
        private FixtureAccessClearancePolicy accessClearancePolicy =
            FixtureAccessClearancePolicy.AllAuthoredAccessPoints;

        [Header("Backstock Storage")]

        [Min(0)]
        [Tooltip(
            "Number of tracked supplier cases that physically fit on this "
            + "rack. Zero means the fixture is not backstock storage.")]
        [SerializeField]
        private int backstockCaseSlotCapacity;

        [Min(1)]
        [Tooltip(
            "Number of physical case positions across each authored shelf.")]
        [SerializeField]
        private int backstockCasesPerShelf = 3;

        [Min(0.01f)]
        [Tooltip(
            "Case width relative to one shelf position. Values above one "
            + "create the slight overlap used by the rack artwork.")]
        [SerializeField]
        private float backstockCaseWidthPerSlot = 0.72f;

        [Range(0.25f, 1f)]
        [Tooltip(
            "Spacing between physical case centers across a shelf. One uses "
            + "the full authored shelf width; lower values pack the row "
            + "closer together around the shelf center.")]
        [SerializeField]
        private float backstockCaseSpacingShare = 1f;

        [Range(-0.5f, 0.5f)]
        [Tooltip(
            "Moves the complete case row along the shelf frontage, measured "
            + "in fractions of one case position. Positive values move "
            + "toward the authored right side.")]
        [SerializeField]
        private float backstockCaseRowOffsetShare;

        [Range(0f, 1f)]
        [Tooltip(
            "How far cases move from the shelf-mask center toward its front "
            + "edge. One reaches the full measured shelf depth.")]
        [SerializeField]
        private float backstockCaseFrontOffsetShare = 0.20f;


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

        public bool HasLayeredPresentation =>
            GetLayerCount(northPresentationLayers) > 0
            || GetLayerCount(eastPresentationLayers) > 0
            || GetLayerCount(southPresentationLayers) > 0
            || GetLayerCount(westPresentationLayers) > 0;

        public bool HasAnyMerchandisingShelfMasks
        {
            get
            {
                if (merchandisingMaskSets == null)
                {
                    return false;
                }

                for (int index = 0;
                     index < merchandisingMaskSets.Length;
                     index++)
                {
                    if (merchandisingMaskSets[index]?.HasAnyMasks == true)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasStorageShelfMasks =>
            storageShelfMasks?.HasAnyMasks == true;

        public int BackstockCaseSlotCapacity =>
            Math.Max(0, backstockCaseSlotCapacity);

        public int BackstockCasesPerShelf =>
            Math.Max(1, backstockCasesPerShelf);

        public float BackstockCaseWidthPerSlot =>
            backstockCaseWidthPerSlot > 0f
                ? backstockCaseWidthPerSlot
                : 0.72f;

        public float BackstockCaseSpacingShare =>
            backstockCaseSpacingShare > 0f
                ? Mathf.Clamp(backstockCaseSpacingShare, 0.25f, 1f)
                : 1f;

        public float BackstockCaseRowOffsetShare =>
            Mathf.Clamp(backstockCaseRowOffsetShare, -0.5f, 0.5f);

        public float BackstockCaseFrontOffsetShare =>
            Mathf.Clamp01(backstockCaseFrontOffsetShare);


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
                    westAccess,
                    accessClearancePolicy),
                storageProfile:
                    new FixtureStorageProfile(
                        backstockCaseSlotCapacity));
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


        public IReadOnlyList<Sprite> GetPresentationLayers(
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
                0 => northPresentationLayers ?? Array.Empty<Sprite>(),
                1 => eastPresentationLayers ?? Array.Empty<Sprite>(),
                2 => southPresentationLayers ?? Array.Empty<Sprite>(),
                3 => westPresentationLayers ?? Array.Empty<Sprite>(),
                _ => Array.Empty<Sprite>()
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


        public IReadOnlyList<Sprite> GetMerchandisingShelfMasks(
            FixtureSide localDisplaySide,
            FixtureOrientation worldOrientation,
            IsometricViewOrientation viewOrientation)
        {
            if (merchandisingMaskSets == null)
            {
                return Array.Empty<Sprite>();
            }

            for (int index = 0;
                 index < merchandisingMaskSets.Length;
                 index++)
            {
                FixtureMerchandisingMaskSet maskSet =
                    merchandisingMaskSets[index];

                if (maskSet != null
                    && maskSet.LocalDisplaySide == localDisplaySide)
                {
                    return maskSet.GetShelfMasks(
                        worldOrientation,
                        viewOrientation);
                }
            }

            return Array.Empty<Sprite>();
        }


        public bool HasMerchandisingShelfMasks(
            FixtureSide localDisplaySide)
        {
            if (merchandisingMaskSets == null)
            {
                return false;
            }

            for (int index = 0;
                 index < merchandisingMaskSets.Length;
                 index++)
            {
                FixtureMerchandisingMaskSet maskSet =
                    merchandisingMaskSets[index];

                if (maskSet != null
                    && maskSet.LocalDisplaySide == localDisplaySide)
                {
                    return maskSet.HasAnyMasks;
                }
            }

            return false;
        }


        public IReadOnlyList<Sprite> GetStorageShelfMasks(
            FixtureOrientation worldOrientation,
            IsometricViewOrientation viewOrientation)
        {
            return storageShelfMasks != null
                ? storageShelfMasks.GetShelfMasks(
                    worldOrientation,
                    viewOrientation)
                : Array.Empty<Sprite>();
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

            if (!Enum.IsDefined(
                    typeof(FixtureAccessClearancePolicy),
                    accessClearancePolicy))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' contains an unsupported access-clearance policy.");
            }

            if (backstockCaseSlotCapacity < 0)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' cannot have negative backstock case-slot capacity.");
            }

            if (backstockCaseSlotCapacity > 0
                && (backstockCasesPerShelf <= 0
                    || backstockCaseWidthPerSlot <= 0f
                    || backstockCaseSpacingShare <= 0f
                    || backstockCaseSpacingShare > 1f
                    || backstockCaseRowOffsetShare < -0.5f
                    || backstockCaseRowOffsetShare > 0.5f
                    || backstockCaseFrontOffsetShare < 0f
                    || backstockCaseFrontOffsetShare > 1f))
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' contains invalid physical case-layout values.");
            }

            ValidatePresentationLayers();
            ValidateMerchandisingMasks();
            ValidateStorageShelfMasks();
        }


        private void ValidatePresentationLayers()
        {
            int expectedLayerCount =
                ResolveFirstLayerCount(
                    northPresentationLayers,
                    eastPresentationLayers,
                    southPresentationLayers,
                    westPresentationLayers);

            if (expectedLayerCount == 0)
            {
                return;
            }

            if (repeatSpritePerOccupiedCell)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' cannot use whole-fixture "
                    + "presentation layers while repeating its sprite per cell.");
            }

            ValidatePresentationDirection(
                "north",
                northSprite,
                northPresentationLayers,
                expectedLayerCount);
            ValidatePresentationDirection(
                "east",
                eastSprite,
                eastPresentationLayers,
                expectedLayerCount);
            ValidatePresentationDirection(
                "south",
                southSprite,
                southPresentationLayers,
                expectedLayerCount);
            ValidatePresentationDirection(
                "west",
                westSprite,
                westPresentationLayers,
                expectedLayerCount);

            if (storageShelfMasks?.HasAnyMasks != true)
            {
                return;
            }

            ValidateStorageLayerCount(
                "north",
                northPresentationLayers,
                storageShelfMasks.GetShelfMasks(
                    FixtureOrientation.North,
                    IsometricViewOrientation.North).Count);
            ValidateStorageLayerCount(
                "east",
                eastPresentationLayers,
                storageShelfMasks.GetShelfMasks(
                    FixtureOrientation.East,
                    IsometricViewOrientation.North).Count);
            ValidateStorageLayerCount(
                "south",
                southPresentationLayers,
                storageShelfMasks.GetShelfMasks(
                    FixtureOrientation.South,
                    IsometricViewOrientation.North).Count);
            ValidateStorageLayerCount(
                "west",
                westPresentationLayers,
                storageShelfMasks.GetShelfMasks(
                    FixtureOrientation.West,
                    IsometricViewOrientation.North).Count);
        }


        private void ValidatePresentationDirection(
            string directionName,
            Sprite fixtureSprite,
            Sprite[] layers,
            int expectedLayerCount)
        {
            int layerCount = GetLayerCount(layers);

            if (layerCount != expectedLayerCount)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' requires "
                    + $"{expectedLayerCount} {directionName} presentation "
                    + $"layer(s), but contains {layerCount}.");
            }

            for (int index = 0;
                 index < layers.Length;
                 index++)
            {
                Sprite layer = layers[index];

                if (layer == null)
                {
                    throw new InvalidOperationException(
                        $"Fixture definition '{name}' has an empty "
                        + $"{directionName} presentation layer at index "
                        + $"{index}.");
                }

                if (fixtureSprite == null
                    || layer.rect != fixtureSprite.rect
                    || Vector2.Distance(
                        layer.pivot,
                        fixtureSprite.pivot) > 0.0001f
                    || !Mathf.Approximately(
                        layer.pixelsPerUnit,
                        fixtureSprite.pixelsPerUnit))
                {
                    throw new InvalidOperationException(
                        $"Fixture definition '{name}' has a misaligned "
                        + $"{directionName} presentation layer "
                        + $"'{layer.name}'. Its canvas, pivot, and "
                        + "pixels-per-unit must match the combined fixture "
                        + $"sprite '{fixtureSprite?.name ?? "<null>"}'.");
                }
            }
        }


        private void ValidateStorageLayerCount(
            string directionName,
            Sprite[] layers,
            int shelfMaskCount)
        {
            if (shelfMaskCount == 0)
            {
                return;
            }

            int expectedLayerCount = shelfMaskCount + 1;

            if (GetLayerCount(layers) != expectedLayerCount)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' requires one more "
                    + $"{directionName} presentation layer than storage "
                    + $"shelf masks ({expectedLayerCount} layers for "
                    + $"{shelfMaskCount} shelves).");
            }
        }


        private static int ResolveFirstLayerCount(
            params Sprite[][] directionalLayers)
        {
            for (int index = 0;
                 index < directionalLayers.Length;
                 index++)
            {
                int count = GetLayerCount(directionalLayers[index]);

                if (count > 0)
                {
                    return count;
                }
            }

            return 0;
        }


        private static int GetLayerCount(
            Sprite[] layers)
        {
            return layers?.Length ?? 0;
        }


        private void ValidateStorageShelfMasks()
        {
            if (storageShelfMasks?.HasAnyMasks != true)
            {
                return;
            }

            if (backstockCaseSlotCapacity <= 0)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' authors storage shelf masks but provides no physical case slots.");
            }

            storageShelfMasks.ValidateConfiguration(
                name,
                northSprite,
                eastSprite,
                southSprite,
                westSprite);

            int shelfCount =
                storageShelfMasks.GetShelfMasks(
                    FixtureOrientation.North,
                    IsometricViewOrientation.North).Count;
            int authoredCaseSlotCount =
                shelfCount * BackstockCasesPerShelf;

            if (backstockCaseSlotCapacity > authoredCaseSlotCount)
            {
                throw new InvalidOperationException(
                    $"Fixture definition '{name}' provides "
                    + $"{backstockCaseSlotCapacity} physical case slots but "
                    + $"its shelf layout only authors {authoredCaseSlotCount}.");
            }
        }


        private void ValidateMerchandisingMasks()
        {
            if (merchandisingMaskSets == null)
            {
                return;
            }

            FixtureMerchandisingProfile profile =
                FixtureMerchandisingProfile
                    .CreateForCustomerBrowseSides(
                        new FixtureAccessProfile(
                            northAccess,
                            eastAccess,
                            southAccess,
                            westAccess));

            for (int index = 0;
                 index < merchandisingMaskSets.Length;
                 index++)
            {
                FixtureMerchandisingMaskSet maskSet =
                    merchandisingMaskSets[index]
                    ?? throw new InvalidOperationException(
                        $"Fixture definition '{name}' contains an empty merchandising-mask set at index {index}.");

                for (int priorIndex = 0;
                     priorIndex < index;
                     priorIndex++)
                {
                    if (merchandisingMaskSets[priorIndex].LocalDisplaySide
                        == maskSet.LocalDisplaySide)
                    {
                        throw new InvalidOperationException(
                            $"Fixture definition '{name}' repeats merchandising masks for side '{maskSet.LocalDisplaySide}'.");
                    }
                }

                maskSet.ValidateConfiguration(
                    name,
                    profile,
                    northSprite,
                    eastSprite,
                    southSprite,
                    westSprite);
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
