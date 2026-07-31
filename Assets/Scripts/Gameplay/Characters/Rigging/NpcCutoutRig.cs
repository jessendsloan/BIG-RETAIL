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
            "Use when this appearance's unmirrored bind pose faces " +
            "SouthWest/NorthWest. Standard 36-sprite art remains " +
            "SouthEast/NorthEast-authored.")]
        [SerializeField]
        private bool unmirroredPresentationFacesWest;


        [Header("Generated Bindings")]

        [SerializeField]
        private List<NpcRigBoneBinding> bones =
            new List<NpcRigBoneBinding>();

        [SerializeField]
        private List<NpcRigPartBinding> parts =
            new List<NpcRigPartBinding>();


        public NpcFacing Facing => facing;

        public NpcRigArtKit ArtKit => artKit;

        public bool UnmirroredPresentationFacesWest =>
            unmirroredPresentationFacesWest;

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
            List<NpcRigPartBinding> generatedParts,
            bool generatedUnmirroredPresentationFacesWest = false)
        {
            mirrorRoot = generatedMirrorRoot;
            unmirroredPresentationFacesWest =
                generatedUnmirroredPresentationFacesWest;
            bones = generatedBones
                ?? throw new ArgumentNullException(
                    nameof(generatedBones));
            parts = generatedParts
                ?? throw new ArgumentNullException(
                    nameof(generatedParts));

            ApplyFacing();
        }


        [ContextMenu("Apply Facing")]
        private void ApplyFacing()
        {
            NpcAuthoredDirection authoredDirection =
                NpcFacingUtility.GetAuthoredDirection(
                    facing);

            for (int index = 0; index < parts.Count; index++)
            {
                parts[index]?.Apply(
                    authoredDirection,
                    artKit);
            }

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
                NpcFacingUtility.IsPresentationMirrored(
                    facing,
                    unmirroredPresentationFacesWest);

            scale.x =
                mirrored
                    ? -horizontalMagnitude
                    : horizontalMagnitude;

            mirrorRoot.localScale = scale;
            ApplyDepthLayering(mirrored);
        }


        private void ApplyDepthLayering(
            bool mirrored)
        {
            for (int index = 0; index < parts.Count; index++)
            {
                NpcRigPartBinding binding = parts[index];

                if (binding == null
                    || binding.SpriteRenderer == null)
                {
                    continue;
                }

                NpcRigPartId counterpart =
                    NpcFacingUtility.GetMirroredDepthPart(
                        binding.Id);

                if (counterpart == binding.Id)
                {
                    continue;
                }

                NpcRigPartId sortingPart = mirrored
                    ? counterpart
                    : binding.Id;

                binding.SpriteRenderer.sortingOrder =
                    GetContractSortingOrder(sortingPart);
            }
        }

        private static int GetContractSortingOrder(
            NpcRigPartId partId)
        {
            foreach (NpcRigPartDefinition definition
                     in NpcRigDefinition.PartDefinitions)
            {
                if (definition.Id == partId)
                {
                    return definition.SortingOrder;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(partId),
                partId,
                "Unknown NPC rig part.");
        }


        private void OnValidate()
        {
            ApplyFacing();
        }
    }
}
