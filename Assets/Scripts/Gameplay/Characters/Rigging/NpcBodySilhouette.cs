using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    [CreateAssetMenu(
        fileName = "BodySilhouette",
        menuName = "Big Retail/Characters/Body Silhouette")]
    public sealed class NpcBodySilhouette : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Body Silhouette";

        [SerializeField]
        private NpcBodySilhouetteKind kind;

        [SerializeField]
        private List<NpcAppearancePartShape> partShapes =
            new List<NpcAppearancePartShape>();

        [SerializeField]
        private List<NpcAppearanceBonePlacement> bonePlacements =
            new List<NpcAppearanceBonePlacement>();


        public string DisplayName => displayName;

        public NpcBodySilhouetteKind Kind => kind;

        public IReadOnlyList<NpcAppearancePartShape> PartShapes =>
            partShapes;


        public void Configure(
            string newDisplayName,
            NpcBodySilhouetteKind newKind,
            IEnumerable<NpcAppearancePartShape> newPartShapes,
            IEnumerable<NpcAppearanceBonePlacement> newBonePlacements)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            kind = newKind;
            partShapes = newPartShapes != null
                ? new List<NpcAppearancePartShape>(newPartShapes)
                : new List<NpcAppearancePartShape>();
            bonePlacements = newBonePlacements != null
                ? new List<NpcAppearanceBonePlacement>(newBonePlacements)
                : new List<NpcAppearanceBonePlacement>();
        }


        public bool TryGetPartShape(
            NpcRigPartId partId,
            out NpcAppearancePartShape shape)
        {
            if (partShapes == null)
            {
                shape = null;
                return false;
            }

            for (int index = 0; index < partShapes.Count; index++)
            {
                NpcAppearancePartShape candidate = partShapes[index];

                if (candidate != null
                    && candidate.Id == partId)
                {
                    shape = candidate;
                    return true;
                }
            }

            shape = null;
            return false;
        }


        public void ApplyBonePlacements(
            NpcCutoutRig rig)
        {
            if (rig == null || bonePlacements == null)
            {
                return;
            }

            for (int index = 0; index < bonePlacements.Count; index++)
            {
                NpcAppearanceBonePlacement placement =
                    bonePlacements[index];

                if (placement != null
                    && rig.TryGetBone(
                        placement.Id,
                        out Transform bone))
                {
                    bone.localPosition = placement.LocalPosition;
                }
            }
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (partShapes == null)
            {
                failureReason = "Part shapes are missing.";
                return false;
            }

            HashSet<NpcRigPartId> uniqueParts =
                new HashSet<NpcRigPartId>();

            for (int index = 0; index < partShapes.Count; index++)
            {
                NpcAppearancePartShape shape = partShapes[index];

                if (shape == null)
                {
                    failureReason =
                        $"Part shape {index} is missing.";
                    return false;
                }

                if (!uniqueParts.Add(shape.Id))
                {
                    failureReason =
                        $"Part shape {shape.Id} is duplicated.";
                    return false;
                }
            }

            if (uniqueParts.Count != NpcRigDefinition.ExpectedPartCount)
            {
                failureReason =
                    $"Expected {NpcRigDefinition.ExpectedPartCount} " +
                    $"part shapes but found {uniqueParts.Count}.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
