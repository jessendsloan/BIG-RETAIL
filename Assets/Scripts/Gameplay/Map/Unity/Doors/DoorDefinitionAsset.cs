using System;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Unity authoring data for one door model. Topology creates the
    /// engine-free definition; optional generic assembly visuals layer over
    /// any wall finish when the complete ten-sprite set is available.
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
            "Optional generic frame, fixed-glass, and movable-door artwork. "
            + "All ten "
            + "sprites must be present before layered rendering is enabled.")]
        [SerializeField]
        private DoorAssemblySpriteSet assemblyVisuals =
            new DoorAssemblySpriteSet();

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

        public bool HasCompleteAssemblyVisuals =>
            assemblyVisuals != null
            && assemblyVisuals.IsComplete;

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


        public void ValidateConfiguration()
        {
            _ = new DoorDefinition(
                Id,
                segmentCount,
                passageSegmentIndices);

            if (HasCompleteAssemblyVisuals
                && segmentCount
                    != LayeredPanelCount)
            {
                throw new InvalidOperationException(
                    $"Layered door definition '{Id}' requires exactly "
                    + $"{LayeredPanelCount} wall panels.");
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
