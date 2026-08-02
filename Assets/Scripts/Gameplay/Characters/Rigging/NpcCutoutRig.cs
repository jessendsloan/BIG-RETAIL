using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// A serialized reference to one generated skeleton bone.
    /// </summary>
    [Serializable]
    public sealed class NpcRigBoneBinding
    {
        [SerializeField]
        private NpcRigBoneId id;

        [SerializeField]
        private Transform bone;


        public NpcRigBoneId Id => id;

        public Transform Bone => bone;


        public NpcRigBoneBinding(
            NpcRigBoneId id,
            Transform bone)
        {
            this.id = id;
            this.bone = bone;
        }
    }

    /// <summary>
    /// One authored local transform for a bone in a directional bind pose.
    /// These are applied before the west-facing visual mirror, so one
    /// SouthEast pose and one NorthEast pose cover all four directions.
    /// </summary>
    [Serializable]
    public sealed class NpcRigDirectionalBonePose
    {
        [SerializeField]
        private NpcRigBoneId id;

        [SerializeField]
        private Vector3 localPosition;

        [SerializeField]
        private Vector3 localEulerAngles;

        [SerializeField]
        private Vector3 localScale = Vector3.one;


        public NpcRigBoneId Id => id;


        public NpcRigDirectionalBonePose(
            NpcRigBoneId id,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            this.id = id;
            this.localPosition = localPosition;
            this.localEulerAngles = localEulerAngles;
            this.localScale = localScale;
        }


        public void Apply(
            Transform bone)
        {
            if (bone == null)
            {
                return;
            }

            bone.localPosition = localPosition;
            bone.localEulerAngles = localEulerAngles;
            bone.localScale = localScale;
        }
    }

    /// <summary>
    /// A visible rig slot with one sprite for each authored direction.
    /// </summary>
    [Serializable]
    public sealed class NpcRigPartBinding
    {
        [SerializeField]
        private NpcRigPartId id;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        [Tooltip(
            "Original artwork displayed for SouthEast and mirrored " +
            "for SouthWest.")]
        [SerializeField]
        private Sprite southEastSprite;

        [Tooltip(
            "Original artwork displayed for NorthEast and mirrored " +
            "for NorthWest.")]
        [SerializeField]
        private Sprite northEastSprite;

        [SerializeField, HideInInspector]
        private Sprite placeholderSprite;


        public NpcRigPartId Id => id;

        public SpriteRenderer SpriteRenderer => spriteRenderer;

        public Sprite SouthEastSprite => southEastSprite;

        public Sprite NorthEastSprite => northEastSprite;


        public NpcRigPartBinding(
            NpcRigPartId id,
            SpriteRenderer spriteRenderer,
            Sprite placeholderSprite)
        {
            this.id = id;
            this.spriteRenderer = spriteRenderer;
            this.placeholderSprite = placeholderSprite;
            southEastSprite = null;
            northEastSprite = null;
        }


        public void Apply(
            NpcAuthoredDirection direction,
            NpcRigArtKit artKit)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Sprite authoredSprite = null;

            if (artKit != null)
            {
                artKit.TryGetSprite(
                    id,
                    direction,
                    out authoredSprite);
            }

            if (authoredSprite == null)
            {
                authoredSprite =
                    direction == NpcAuthoredDirection.SouthEast
                        ? southEastSprite
                        : northEastSprite;
            }

            spriteRenderer.sprite =
                authoredSprite != null
                    ? authoredSprite
                    : placeholderSprite;
        }
    }

    /// <summary>
    /// Owns the canonical Unity-native cutout rig.
    ///
    /// Gameplay can request one of four facings. The rig selects one
    /// of two authored sprite sets and mirrors the visual root for
    /// west-facing directions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcCutoutRig : MonoBehaviour
    {
        [Header("Facing")]

        [SerializeField]
        private NpcFacing facing =
            NpcFacing.SouthEast;

        [Tooltip(
            "Only this visual root is mirrored. The NPC world root " +
            "and its ground position remain unchanged.")]
        [SerializeField]
        private Transform mirrorRoot;

        [Tooltip(
            "The replaceable 36-sprite appearance kit shared by this " +
            "canonical skeleton.")]
        [SerializeField]
        private NpcRigArtKit artKit;

        [Tooltip(
            "Four-part appearance recipe: body silhouette, skin " +
            "palette, outfit set, and hair set.")]
        [SerializeField]
        private NpcAppearanceProfile appearanceProfile;

        [NonSerialized]
        private NpcAppearanceProfile appearancePreview;

        [Tooltip(
            "Presentation details that belong on the front of the " +
            "character, such as a name badge. They are hidden when " +
            "the authored NorthEast back view is displayed.")]
        [SerializeField]
        private List<SpriteRenderer> northHiddenDetails =
            new List<SpriteRenderer>();

        [Header("Authored Direction Poses")]

        [Tooltip(
            "Local bone values for the unmirrored SouthEast pose. " +
            "SouthWest inherits this pose through mirroring.")]
        [SerializeField]
        private List<NpcRigDirectionalBonePose> southEastBonePose =
            new List<NpcRigDirectionalBonePose>();

        [Tooltip(
            "Local bone values for the unmirrored NorthEast pose. " +
            "NorthWest inherits this pose through mirroring.")]
        [SerializeField]
        private List<NpcRigDirectionalBonePose> northEastBonePose =
            new List<NpcRigDirectionalBonePose>();

        [Header("Generated Bindings")]

        [SerializeField]
        private List<NpcRigBoneBinding> bones =
            new List<NpcRigBoneBinding>();

        [SerializeField]
        private List<NpcRigPartBinding> parts =
            new List<NpcRigPartBinding>();


        public NpcFacing Facing => facing;

        public NpcRigArtKit ArtKit => artKit;

        public NpcAppearanceProfile AppearanceProfile =>
            appearanceProfile;

        public int BoneCount => bones.Count;

        public int PartCount => parts.Count;


        private void Awake()
        {
            ApplyFacing();
        }


        /// <summary>
        /// Changes the displayed direction without changing the NPC's
        /// world position.
        /// </summary>
        public void SetFacing(
            NpcFacing newFacing)
        {
            facing = newFacing;
            ApplyFacing();
        }

        /// <summary>
        /// Replaces the complete character appearance without
        /// rebuilding the skeleton or animation hierarchy.
        /// </summary>
        public void SetArtKit(
            NpcRigArtKit newArtKit)
        {
            artKit = newArtKit;
            ApplyFacing();
        }

        /// <summary>
        /// Applies one composable appearance recipe without replacing the
        /// skeleton, Animator, or gameplay components.
        /// </summary>
        public void SetAppearanceProfile(
            NpcAppearanceProfile newAppearanceProfile)
        {
            appearanceProfile = newAppearanceProfile;
            appearancePreview = null;
            ApplyFacing();
        }

        /// <summary>
        /// Temporarily displays an editor-authored appearance without
        /// replacing the saved profile reference on the rig.
        /// </summary>
        public void SetAppearancePreview(
            NpcAppearanceProfile previewProfile)
        {
            appearancePreview = previewProfile;
            ApplyFacing();
        }

        /// <summary>
        /// Returns the rig to its saved appearance profile.
        /// </summary>
        public void ClearAppearancePreview()
        {
            appearancePreview = null;
            ApplyFacing();
        }

        /// <summary>
        /// Looks up a generated bone without relying on hierarchy
        /// searches or string names.
        /// </summary>
        public bool TryGetBone(
            NpcRigBoneId boneId,
            out Transform bone)
        {
            for (int index = 0; index < bones.Count; index++)
            {
                NpcRigBoneBinding binding =
                    bones[index];

                if (binding != null
                    && binding.Id == boneId)
                {
                    bone = binding.Bone;
                    return bone != null;
                }
            }

            bone = null;
            return false;
        }

        /// <summary>
        /// Returns the renderer for one canonical visible part.
        /// Authoring tools use this to capture and preview appearances
        /// without hierarchy-name searches.
        /// </summary>
        public bool TryGetPartRenderer(
            NpcRigPartId partId,
            out SpriteRenderer renderer)
        {
            for (int index = 0; index < parts.Count; index++)
            {
                NpcRigPartBinding binding = parts[index];

                if (binding != null
                    && binding.Id == partId)
                {
                    renderer = binding.SpriteRenderer;
                    return renderer != null;
                }
            }

            renderer = null;
            return false;
        }

        /// <summary>
        /// Checks that the generated hierarchy still fulfills the
        /// canonical rig contract.
        /// </summary>
        public bool TryValidate(
            out string failureReason)
        {
            if (mirrorRoot == null)
            {
                failureReason =
                    "The rig has no mirror root.";
                return false;
            }

            if (bones.Count
                != NpcRigDefinition.ExpectedBoneCount)
            {
                failureReason =
                    $"Expected {NpcRigDefinition.ExpectedBoneCount} " +
                    $"bones but found {bones.Count}.";
                return false;
            }

            if (parts.Count
                != NpcRigDefinition.ExpectedPartCount)
            {
                failureReason =
                    $"Expected {NpcRigDefinition.ExpectedPartCount} " +
                    $"parts but found {parts.Count}.";
                return false;
            }

            HashSet<NpcRigBoneId> uniqueBoneIds =
                new HashSet<NpcRigBoneId>();

            for (int index = 0; index < bones.Count; index++)
            {
                NpcRigBoneBinding binding =
                    bones[index];

                if (binding == null
                    || binding.Bone == null)
                {
                    failureReason =
                        $"Bone binding {index} is incomplete.";
                    return false;
                }

                if (!uniqueBoneIds.Add(binding.Id))
                {
                    failureReason =
                        $"Bone {binding.Id} is bound more than once.";
                    return false;
                }
            }

            HashSet<NpcRigPartId> uniquePartIds =
                new HashSet<NpcRigPartId>();

            for (int index = 0; index < parts.Count; index++)
            {
                NpcRigPartBinding binding =
                    parts[index];

                if (binding == null
                    || binding.SpriteRenderer == null)
                {
                    failureReason =
                        $"Part binding {index} is incomplete.";
                    return false;
                }

                if (!uniquePartIds.Add(binding.Id))
                {
                    failureReason =
                        $"Part {binding.Id} is bound more than once.";
                    return false;
                }
            }

            failureReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Checks whether the assigned art kit completely covers one
        /// authored direction.
        /// </summary>
        public bool TryValidateArt(
            NpcAuthoredDirection direction,
            out string failureReason)
        {
            if (artKit == null)
            {
                failureReason =
                    "The rig has no art kit assigned.";
                return false;
            }

            return artKit.TryValidateDirection(
                direction,
                out failureReason);
        }


        /// <summary>
        /// Used by the editor generator when it creates the canonical
        /// prefab. Runtime systems should not rebuild these bindings.
        /// </summary>
        public void ConfigureGeneratedRig(
            Transform generatedMirrorRoot,
            List<NpcRigBoneBinding> generatedBones,
            List<NpcRigPartBinding> generatedParts)
        {
            mirrorRoot = generatedMirrorRoot;
            bones = generatedBones
                ?? throw new ArgumentNullException(
                    nameof(generatedBones));
            parts = generatedParts
                ?? throw new ArgumentNullException(
                    nameof(generatedParts));

            ApplyFacing();
        }

        /// <summary>
        /// Sets the two authored local-pose tables used by generated rigs.
        /// Each table is intentionally separate from the mirrored display
        /// directions, so West never needs its own duplicate pose data.
        /// </summary>
        public void ConfigureAuthoredBonePoses(
            List<NpcRigDirectionalBonePose> generatedSouthEastPose,
            List<NpcRigDirectionalBonePose> generatedNorthEastPose)
        {
            southEastBonePose = generatedSouthEastPose
                ?? new List<NpcRigDirectionalBonePose>();
            northEastBonePose = generatedNorthEastPose
                ?? new List<NpcRigDirectionalBonePose>();

            ApplyFacing();
        }

        /// <summary>
        /// Sets optional front-only presentation details for a generated rig.
        /// </summary>
        public void ConfigureNorthHiddenDetails(
            List<SpriteRenderer> generatedDetails)
        {
            northHiddenDetails = generatedDetails
                ?? new List<SpriteRenderer>();

            ApplyFacing();
        }


        [ContextMenu("Apply Facing")]
        private void ApplyFacing()
        {
            NpcAuthoredDirection authoredDirection =
                NpcFacingUtility.GetAuthoredDirection(
                    facing);

            NpcAppearanceProfile effectiveAppearance =
                appearancePreview != null
                    ? appearancePreview
                    : appearanceProfile;

            effectiveAppearance?.ApplyBonePlacements(this);

            for (int index = 0; index < parts.Count; index++)
            {
                NpcRigPartBinding binding = parts[index];

                binding?.Apply(
                    authoredDirection,
                    artKit);

                if (binding != null)
                {
                    effectiveAppearance?.ApplyPart(
                        binding.Id,
                        binding.SpriteRenderer,
                        authoredDirection);
                }
            }

            ApplyAuthoredBonePose(authoredDirection);

            if (mirrorRoot == null)
            {
                return;
            }

            Vector3 scale =
                mirrorRoot.localScale;

            float horizontalMagnitude =
                Mathf.Abs(scale.x);

            if (horizontalMagnitude
                < 0.0001f)
            {
                horizontalMagnitude = 1f;
            }

            bool mirrored =
                NpcFacingUtility.IsMirrored(facing);

            scale.x =
                mirrored
                    ? -horizontalMagnitude
                    : horizontalMagnitude;

            mirrorRoot.localScale = scale;
            ApplyDepthLayering(facing);
            ApplyDirectionalDetailVisibility(
                authoredDirection,
                effectiveAppearance);
        }


        private void ApplyAuthoredBonePose(
            NpcAuthoredDirection authoredDirection)
        {
            List<NpcRigDirectionalBonePose> pose =
                authoredDirection == NpcAuthoredDirection.SouthEast
                    ? southEastBonePose
                    : northEastBonePose;

            if (pose == null)
            {
                return;
            }

            for (int index = 0; index < pose.Count; index++)
            {
                NpcRigDirectionalBonePose bonePose =
                    pose[index];

                if (bonePose == null
                    || !TryGetBone(
                        bonePose.Id,
                        out Transform bone))
                {
                    continue;
                }

                bonePose.Apply(bone);
            }
        }


        private void ApplyDirectionalDetailVisibility(
            NpcAuthoredDirection authoredDirection,
            NpcAppearanceProfile effectiveAppearance)
        {
            if (northHiddenDetails == null)
            {
                return;
            }

            bool showFrontDetails =
                authoredDirection == NpcAuthoredDirection.SouthEast
                && (effectiveAppearance == null
                    || effectiveAppearance.OutfitSet == null
                    || effectiveAppearance.OutfitSet.ShowBadge);

            for (int index = 0;
                 index < northHiddenDetails.Count;
                 index++)
            {
                SpriteRenderer detail =
                    northHiddenDetails[index];

                if (detail != null)
                {
                    detail.enabled = showFrontDetails;

                    if (effectiveAppearance?.OutfitSet != null)
                    {
                        detail.color =
                            effectiveAppearance.OutfitSet.BadgeColor;
                    }
                }
            }
        }


        private void ApplyDepthLayering(
            NpcFacing displayedFacing)
        {
            for (int index = 0; index < parts.Count; index++)
            {
                NpcRigPartBinding binding = parts[index];

                if (binding == null
                    || binding.SpriteRenderer == null)
                {
                    continue;
                }

                binding.SpriteRenderer.sortingOrder =
                    NpcFacingUtility.GetPresentationSortingOrder(
                        displayedFacing,
                        binding.Id);
            }
        }


        private void OnValidate()
        {
            ApplyFacing();
        }
    }
}
