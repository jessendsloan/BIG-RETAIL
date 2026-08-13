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
    /// These are applied before display mirroring, so one south/front pose
    /// and one north/back pose cover all four directions.
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
            "Stored south/front artwork. It is unmirrored for SouthEast " +
            "and mirrored for SouthWest.")]
        [SerializeField]
        private Sprite southEastSprite;

        [Tooltip(
            "Stored north/back artwork. It is unmirrored for NorthEast " +
            "and mirrored for NorthWest.")]
        [SerializeField]
        private Sprite northEastSprite;

        [SerializeField, HideInInspector]
        private Sprite placeholderSprite;

        [Tooltip(
            "The presentation material restored before an appearance " +
            "applies an optional material override.")]
        [SerializeField, HideInInspector]
        private Material defaultMaterial;


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
            defaultMaterial =
                spriteRenderer != null
                    ? spriteRenderer.sharedMaterial
                    : null;
        }


        public void Apply(
            NpcAuthoredDirection direction)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            // Older serialized rigs may not have this field yet. Capture it
            // once while the renderer is still using its prefab material.
            if (defaultMaterial == null)
            {
                defaultMaterial = spriteRenderer.sharedMaterial;
            }

            // Appearance previews and runtime population changes reuse the
            // same rig. Always return to the prefab's presentation material
            // before the selected outfit applies an optional override.
            spriteRenderer.sharedMaterial = defaultMaterial;

            Sprite authoredSprite =
                direction == NpcAuthoredDirection.SouthEast
                    ? southEastSprite
                    : northEastSprite;

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
        private static readonly Vector2 DefaultBadgeTorsoAnchor =
            new Vector2(0.32f, 0.10f);

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
            "Four-part appearance recipe: body silhouette, skin " +
            "palette, outfit set, and hair set.")]
        [SerializeField]
        private NpcAppearanceProfile appearanceProfile;

        [NonSerialized]
        private NpcAppearanceProfile appearancePreview;

        [NonSerialized]
        private NpcAppearanceSelection runtimeAppearance;

        [Tooltip(
            "Presentation details that belong on the front of the " +
            "character, such as a name badge. They are hidden when " +
            "the north/back view is displayed.")]
        [SerializeField]
        private List<SpriteRenderer> northHiddenDetails =
            new List<SpriteRenderer>();

        [Header("Authored Direction Poses")]

        [Tooltip(
            "Local bone values for the stored south/front pose. " +
            "SouthWest displays it through mirroring.")]
        [SerializeField]
        private List<NpcRigDirectionalBonePose> southEastBonePose =
            new List<NpcRigDirectionalBonePose>();

        [Tooltip(
            "Local bone values for the stored north/back pose. NorthEast " +
            "uses it directly; NorthWest displays it through mirroring.")]
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

        [NonSerialized]
        private Transform hairDetailRoot;

        [NonSerialized]
        private List<SpriteRenderer> hairDetailRenderers =
            new List<SpriteRenderer>();


        public NpcFacing Facing => facing;

        public NpcAppearanceProfile AppearanceProfile =>
            appearanceProfile;

        public NpcAppearanceSelection RuntimeAppearance =>
            runtimeAppearance?.Copy();

        public int BoneCount => bones.Count;

        public int PartCount => parts.Count;

        public IReadOnlyList<SpriteRenderer> HairDetailRenderers =>
            hairDetailRenderers;


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
        /// Applies one composable appearance recipe without replacing the
        /// skeleton, Animator, or gameplay components.
        /// </summary>
        public void SetAppearanceProfile(
            NpcAppearanceProfile newAppearanceProfile)
        {
            appearanceProfile = newAppearanceProfile;
            appearancePreview = null;
            runtimeAppearance = null;
            ApplyFacing();
        }

        /// <summary>
        /// Applies one exact runtime-generated appearance without creating a
        /// ScriptableObject or replacing the shared rig and Animator.
        /// </summary>
        public bool TrySetAppearanceSelection(
            NpcAppearanceSelection selection,
            out string failureReason)
        {
            if (selection == null)
            {
                failureReason = "No appearance selection was supplied.";
                return false;
            }

            NpcAppearanceSelection validatedSelection = selection.Copy();

            if (!validatedSelection.TryValidate(out failureReason))
            {
                return false;
            }

            runtimeAppearance = validatedSelection;
            appearancePreview = null;
            ApplyFacing();
            failureReason = string.Empty;
            return true;
        }

        /// <summary>
        /// Clears the generated runtime appearance and returns to the saved
        /// prefab profile, when one is assigned.
        /// </summary>
        public void ClearAppearanceSelection()
        {
            runtimeAppearance = null;
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
            NpcFacingPresentation presentation =
                NpcFacingUtility.GetPresentation(facing);
            NpcAuthoredDirection authoredDirection =
                presentation.AuthoredDirection;

            NpcAppearanceSelection effectiveAppearance =
                GetEffectiveAppearance();

            ResetAppearanceBonePositions(authoredDirection);

            for (int index = 0; index < parts.Count; index++)
            {
                NpcRigPartBinding binding = parts[index];

                binding?.Apply(authoredDirection);

                if (binding != null)
                {
                    NpcAppearanceApplicator.ApplyPart(
                        effectiveAppearance,
                        binding.Id,
                        binding.SpriteRenderer,
                        authoredDirection);
                }
            }

            ApplyAuthoredBonePose(authoredDirection);
            NpcAppearanceApplicator.ApplyBonePlacements(
                effectiveAppearance,
                this,
                authoredDirection);

            ApplyHairDetails(
                effectiveAppearance?.HairSet,
                authoredDirection);

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
                presentation.MirrorHorizontally;

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


        private NpcAppearanceSelection GetEffectiveAppearance()
        {
            if (appearancePreview != null)
            {
                return appearancePreview.CreateSelection();
            }

            if (runtimeAppearance != null)
            {
                return runtimeAppearance;
            }

            return appearanceProfile != null
                ? appearanceProfile.CreateSelection()
                : null;
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


        private void ResetAppearanceBonePositions(
            NpcAuthoredDirection authoredDirection)
        {
            IReadOnlyList<NpcRigBoneDefinition> definitions =
                NpcRigDefinition.BoneDefinitions;

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneDefinition definition = definitions[index];

                if (!IsAppearancePositionBone(definition.Id)
                    || !TryGetBone(definition.Id, out Transform bone))
                {
                    continue;
                }

                bone.localPosition =
                    NpcFacingUtility.ResolveAuthoredBonePosition(
                        authoredDirection,
                        definition.Id,
                        definition.LocalPosition);
            }
        }


        private static bool IsAppearancePositionBone(
            NpcRigBoneId id)
        {
            switch (id)
            {
                case NpcRigBoneId.Pelvis:
                case NpcRigBoneId.SpineLower:
                case NpcRigBoneId.Chest:
                case NpcRigBoneId.Neck:
                case NpcRigBoneId.Head:
                case NpcRigBoneId.ShoulderForeground:
                case NpcRigBoneId.UpperArmForeground:
                case NpcRigBoneId.ForearmForeground:
                case NpcRigBoneId.HandForeground:
                case NpcRigBoneId.ShoulderBackground:
                case NpcRigBoneId.UpperArmBackground:
                case NpcRigBoneId.ForearmBackground:
                case NpcRigBoneId.HandBackground:
                case NpcRigBoneId.ThighForeground:
                case NpcRigBoneId.ShinForeground:
                case NpcRigBoneId.FootForeground:
                case NpcRigBoneId.ThighBackground:
                case NpcRigBoneId.ShinBackground:
                case NpcRigBoneId.FootBackground:
                    return true;

                default:
                    return false;
            }
        }


        private void ApplyDirectionalDetailVisibility(
            NpcAuthoredDirection authoredDirection,
            NpcAppearanceSelection effectiveAppearance)
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

            Vector2 badgeAnchor = effectiveAppearance?.OutfitSet != null
                ? effectiveAppearance.OutfitSet.BadgeTorsoAnchor
                : DefaultBadgeTorsoAnchor;

            for (int index = 0;
                 index < northHiddenDetails.Count;
                 index++)
            {
                SpriteRenderer detail =
                    northHiddenDetails[index];

                if (detail != null)
                {
                    AnchorDetailToTorso(
                        detail,
                        badgeAnchor);
                    detail.enabled = showFrontDetails;

                    if (effectiveAppearance?.OutfitSet != null)
                    {
                        detail.color =
                            effectiveAppearance.OutfitSet.BadgeColor;
                    }
                }
            }
        }


        private void AnchorDetailToTorso(
            SpriteRenderer detail,
            Vector2 normalizedAnchor)
        {
            if (detail == null
                || !TryGetPartRenderer(
                    NpcRigPartId.Torso,
                    out SpriteRenderer torso)
                || torso.sprite == null)
            {
                return;
            }

            Bounds torsoBounds = torso.sprite.bounds;
            Vector3 torsoLocalAnchor = torsoBounds.center
                + new Vector3(
                    torsoBounds.size.x * normalizedAnchor.x,
                    torsoBounds.size.y * normalizedAnchor.y,
                    0f);
            Vector3 worldAnchor =
                torso.transform.TransformPoint(torsoLocalAnchor);
            Transform detailTransform = detail.transform;

            if (detailTransform.parent != null)
            {
                Vector3 localAnchor = detailTransform.parent
                    .InverseTransformPoint(worldAnchor);
                localAnchor.z = detailTransform.localPosition.z;
                detailTransform.localPosition = localAnchor;
            }
            else
            {
                Vector3 position = worldAnchor;
                position.z = detailTransform.position.z;
                detailTransform.position = position;
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


        private void ApplyHairDetails(
            NpcHairSet hairSet,
            NpcAuthoredDirection authoredDirection)
        {
            IReadOnlyList<NpcHairDetailLayer> detailLayers =
                hairSet?.DetailLayers;

            if (detailLayers == null || detailLayers.Count == 0)
            {
                SetHairDetailRenderersEnabled(false);
                return;
            }

            if (!TryGetBone(NpcRigBoneId.Head, out Transform head))
            {
                SetHairDetailRenderersEnabled(false);
                return;
            }

            EnsureHairDetailRoot(head);
            EnsureHairDetailRendererCount(detailLayers.Count);

            int headSortingOrder =
                NpcFacingUtility.GetPresentationSortingOrder(
                    facing,
                    NpcRigPartId.Head);

            for (int index = 0; index < detailLayers.Count; index++)
            {
                NpcHairDetailLayer layer = detailLayers[index];
                SpriteRenderer renderer = hairDetailRenderers[index];

                if (layer == null || renderer == null)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }

                    continue;
                }

                layer.Apply(
                    renderer,
                    authoredDirection,
                    hairSet.HairColor,
                    GetHairDetailSortingOrder(
                        layer.Depth,
                        headSortingOrder));
            }

            for (int index = detailLayers.Count;
                 index < hairDetailRenderers.Count;
                 index++)
            {
                if (hairDetailRenderers[index] != null)
                {
                    hairDetailRenderers[index].enabled = false;
                }
            }
        }


        private void EnsureHairDetailRoot(
            Transform head)
        {
            if (hairDetailRoot != null)
            {
                if (hairDetailRoot.parent != head)
                {
                    hairDetailRoot.SetParent(head, false);
                }

                return;
            }

            hairDetailRenderers ??= new List<SpriteRenderer>();
            hairDetailRenderers.Clear();

            GameObject rootObject =
                new GameObject("Hair Details (Generated)");
            rootObject.hideFlags = HideFlags.HideAndDontSave;
            hairDetailRoot = rootObject.transform;
            hairDetailRoot.SetParent(head, false);
            hairDetailRoot.localPosition = Vector3.zero;
            hairDetailRoot.localRotation = Quaternion.identity;
            hairDetailRoot.localScale = Vector3.one;
        }


        private void EnsureHairDetailRendererCount(
            int requiredCount)
        {
            hairDetailRenderers ??= new List<SpriteRenderer>();

            SpriteRenderer presentationSource = null;

            if (!TryGetPartRenderer(
                    NpcRigPartId.HairFront,
                    out presentationSource))
            {
                TryGetPartRenderer(
                    NpcRigPartId.Head,
                    out presentationSource);
            }

            while (hairDetailRenderers.Count < requiredCount)
            {
                int layerNumber = hairDetailRenderers.Count + 1;
                GameObject layerObject =
                    new GameObject($"Hair Detail {layerNumber}");
                layerObject.hideFlags = HideFlags.HideAndDontSave;
                layerObject.transform.SetParent(hairDetailRoot, false);

                SpriteRenderer renderer =
                    layerObject.AddComponent<SpriteRenderer>();

                if (presentationSource != null)
                {
                    renderer.sortingLayerID =
                        presentationSource.sortingLayerID;
                    renderer.sharedMaterial =
                        presentationSource.sharedMaterial;
                    renderer.maskInteraction =
                        presentationSource.maskInteraction;
                }

                hairDetailRenderers.Add(renderer);
            }
        }


        private void SetHairDetailRenderersEnabled(
            bool enabled)
        {
            if (hairDetailRenderers == null)
            {
                return;
            }

            for (int index = 0;
                 index < hairDetailRenderers.Count;
                 index++)
            {
                SpriteRenderer renderer =
                    hairDetailRenderers[index];

                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }


        private static int GetHairDetailSortingOrder(
            NpcHairLayerDepth depth,
            int headSortingOrder)
        {
            switch (depth)
            {
                case NpcHairLayerDepth.BehindHead:
                    return headSortingOrder - 1;

                case NpcHairLayerDepth.Crown:
                    return headSortingOrder + 1;

                case NpcHairLayerDepth.Fringe:
                    return headSortingOrder + 2;

                default:
                    return headSortingOrder + 1;
            }
        }


        private void OnValidate()
        {
#if UNITY_EDITOR
            // SpriteRenderer changes can trigger Unity's internal bounds
            // notifications, which are forbidden during OnValidate itself.
            // Refresh on the next editor tick so Inspector changes still
            // preview immediately without producing lifecycle errors.
            UnityEditor.EditorApplication.delayCall -=
                ApplyFacingAfterValidation;
            UnityEditor.EditorApplication.delayCall +=
                ApplyFacingAfterValidation;
#endif
        }


#if UNITY_EDITOR
        private void ApplyFacingAfterValidation()
        {
            UnityEditor.EditorApplication.delayCall -=
                ApplyFacingAfterValidation;

            if (this == null)
            {
                return;
            }

            ApplyFacing();
        }
#endif
    }
}
