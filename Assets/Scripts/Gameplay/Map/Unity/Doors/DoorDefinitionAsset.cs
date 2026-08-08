using System;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Unity authoring data for one door model. Topology creates the
    /// engine-free definition; optional presentation artwork layers over any
    /// wall finish when the selected visual set is complete.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Big Retail/Doors/Door Definition",
        fileName = "DoorDefinition")]
    public sealed class DoorDefinitionAsset : ScriptableObject
    {
        private const int LayeredPanelCount = 4;


        [SerializeField]
        private string definitionId;

        [SerializeField]
        private string displayName;

        [Min(1)]
        [SerializeField]
        private int segmentCount = 1;

        [SerializeField]
        private int[] passageSegmentIndices =
            { 0 };

        [Tooltip(
            "Optional player-facing catalog icon. Door placement and "
            + "rendering do not depend on it.")]
        [SerializeField]
        private Sprite catalogIcon;

        [Tooltip(
            "Selects the visual and animation model used for this door.")]
        [SerializeField]
        private DoorPresentationStyle presentationStyle =
            DoorPresentationStyle.SlidingFourPanel;

        [Tooltip(
            "Optional generic frame, aperture, fixed-glass, and movable-door "
            + "artwork. All twelve sprites must be present before layered "
            + "rendering is enabled.")]
        [SerializeField]
        private DoorAssemblySpriteSet assemblyVisuals =
            new DoorAssemblySpriteSet();

        [Tooltip(
            "Optional frame and movable panel artwork for a one-wall hinged "
            + "door. All four directional sprites must be present before "
            + "hinged rendering is enabled.")]
        [SerializeField]
        private HingedDoorSpriteSet hingedVisuals =
            new HingedDoorSpriteSet();

        [Tooltip(
            "Optional static frame and assembly-wide aperture artwork for an "
            + "always-open doorway. All four directional sprites must be "
            + "present before doorway rendering is enabled.")]
        [SerializeField]
        private DoorwaySpriteSet doorwayVisuals =
            new DoorwaySpriteSet();

        [Range(0f, 1f)]
        [Tooltip(
            "Vertical start of the opening cut from each supporting wall, "
            + "measured from the wall sprite's bottom.")]
        [SerializeField]
        private float apertureBottomNormalized = 0f;

        [Range(0f, 1f)]
        [Tooltip(
            "Height of the opening cut from each supporting wall, measured "
            + "as a fraction of the wall sprite's height.")]
        [SerializeField]
        private float apertureHeightNormalized = 0.82f;


        public string DefinitionId =>
            definitionId;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(displayName)
                ? definitionId
                : displayName;

        public int SegmentCount =>
            segmentCount;

        public Sprite CatalogIcon =>
            catalogIcon;

        public DoorPresentationStyle PresentationStyle =>
            presentationStyle;

        public bool HasCompleteAssemblyVisuals =>
            assemblyVisuals != null
            && assemblyVisuals.IsComplete;

        public bool HasCompleteHingedVisuals =>
            hingedVisuals != null
            && hingedVisuals.IsComplete;

        public bool HasCompleteDoorwayVisuals =>
            doorwayVisuals != null
            && doorwayVisuals.IsComplete;

        public bool HasCompleteVisuals =>
            presentationStyle switch
            {
                DoorPresentationStyle.SlidingFourPanel =>
                    HasCompleteAssemblyVisuals,

                DoorPresentationStyle.HingedSinglePanel =>
                    HasCompleteHingedVisuals,

                DoorPresentationStyle.StaticDoorway =>
                    HasCompleteDoorwayVisuals,

                _ => false
            };

        public float ApertureBottomNormalized =>
            apertureBottomNormalized;

        public float ApertureHeightNormalized =>
            apertureHeightNormalized;

        public DoorDefinitionId Id
        {
            get
            {
                ValidateIdentifier();
                return new DoorDefinitionId(definitionId);
            }
        }


        public DoorDefinition CreateDomainDefinition()
        {
            ValidateConfiguration();

            return new DoorDefinition(
                Id,
                segmentCount,
                passageSegmentIndices);
        }


        public bool TryGetAssemblySprites(
            WallDisplaySlope displaySlope,
            out DoorAssemblySprites sprites)
        {
            if (assemblyVisuals == null)
            {
                sprites = default;
                return false;
            }

            return assemblyVisuals.TryGetSprites(
                displaySlope,
                out sprites);
        }


        public bool TryGetHingedSprites(
            WallDisplaySlope displaySlope,
            out HingedDoorSprites sprites)
        {
            if (presentationStyle
                    != DoorPresentationStyle.HingedSinglePanel
                || hingedVisuals == null)
            {
                sprites = default;
                return false;
            }

            return hingedVisuals.TryGetSprites(
                displaySlope,
                out sprites);
        }


        public bool TryGetDoorwaySprites(
            WallDisplaySlope displaySlope,
            out DoorwaySprites sprites)
        {
            if (presentationStyle
                    != DoorPresentationStyle.StaticDoorway
                || doorwayVisuals == null)
            {
                sprites = default;
                return false;
            }

            return doorwayVisuals.TryGetSprites(
                displaySlope,
                out sprites);
        }


        public bool TryGetApertureSprite(
            WallDisplaySlope displaySlope,
            out Sprite sprite)
        {
            switch (presentationStyle)
            {
                case DoorPresentationStyle.SlidingFourPanel:
                    if (TryGetAssemblySprites(
                            displaySlope,
                            out DoorAssemblySprites assemblySprites))
                    {
                        sprite = assemblySprites.Aperture;
                        return true;
                    }

                    break;

                case DoorPresentationStyle.HingedSinglePanel:
                    if (TryGetHingedSprites(
                            displaySlope,
                            out HingedDoorSprites hingedSprites))
                    {
                        sprite = hingedSprites.Door;
                        return true;
                    }

                    break;

                case DoorPresentationStyle.StaticDoorway:
                    if (TryGetDoorwaySprites(
                            displaySlope,
                            out DoorwaySprites doorwaySprites))
                    {
                        sprite = doorwaySprites.Aperture;
                        return true;
                    }

                    break;
            }

            sprite = null;
            return false;
        }


        public void ValidateConfiguration()
        {
            _ = new DoorDefinition(
                Id,
                segmentCount,
                passageSegmentIndices);

            if (presentationStyle
                    == DoorPresentationStyle.SlidingFourPanel
                && HasCompleteAssemblyVisuals
                && segmentCount != LayeredPanelCount)
            {
                throw new InvalidOperationException(
                    $"Layered door definition '{Id}' requires exactly "
                    + $"{LayeredPanelCount} wall panels.");
            }

            if (presentationStyle
                    == DoorPresentationStyle.HingedSinglePanel
                && HasCompleteHingedVisuals
                && segmentCount != 1)
            {
                throw new InvalidOperationException(
                    $"Hinged door definition '{Id}' requires exactly one "
                    + "wall panel.");
            }

            if (apertureBottomNormalized < 0f
                || apertureHeightNormalized <= 0f
                || apertureBottomNormalized
                    + apertureHeightNormalized > 1f)
            {
                throw new InvalidOperationException(
                    $"Door definition '{Id}' requires an aperture inside "
                    + "the normalized wall height.");
            }
        }


        private void ValidateIdentifier()
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new InvalidOperationException(
                    $"{nameof(DoorDefinitionAsset)} '{name}' requires a "
                    + "definition identifier.");
            }
        }


        private void OnValidate()
        {
            if (definitionId != null)
            {
                definitionId =
                    definitionId.Trim();
            }

            if (displayName != null)
            {
                displayName =
                    displayName.Trim();
            }
        }
    }
}
