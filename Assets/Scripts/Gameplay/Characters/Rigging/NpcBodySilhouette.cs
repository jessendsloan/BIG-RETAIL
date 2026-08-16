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

        [SerializeField]
        private List<NpcAuthoringPoseAngle> neutralPoseAngles =
            new List<NpcAuthoringPoseAngle>();


        public string DisplayName => displayName;

        public NpcBodySilhouetteKind Kind => kind;

        public NpcPersonGender Gender =>
            kind == NpcBodySilhouetteKind.Feminine
                ? NpcPersonGender.Woman
                : NpcPersonGender.Man;

        public IReadOnlyList<NpcAppearancePartShape> PartShapes =>
            partShapes;

        public IReadOnlyList<NpcAuthoringPoseAngle> NeutralPoseAngles =>
            neutralPoseAngles;


        public bool Supports(
            NpcPersonGender gender)
        {
            return Gender == gender;
        }


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


        public float GetNeutralPoseAngle(
            NpcRigBoneId boneId)
        {
            if (neutralPoseAngles == null)
            {
                return 0f;
            }

            for (int index = 0; index < neutralPoseAngles.Count; index++)
            {
                NpcAuthoringPoseAngle entry = neutralPoseAngles[index];

                if (entry.BoneId == boneId)
                {
                    return entry.Angle;
                }
            }

            return 0f;
        }


        public void SetNeutralPoseAngle(
            NpcRigBoneId boneId,
            float angle)
        {
            if (neutralPoseAngles == null)
            {
                neutralPoseAngles = new List<NpcAuthoringPoseAngle>();
            }

            for (int index = 0; index < neutralPoseAngles.Count; index++)
            {
                if (neutralPoseAngles[index].BoneId != boneId)
                {
                    continue;
                }

                if (Mathf.Approximately(angle, 0f))
                {
                    neutralPoseAngles.RemoveAt(index);
                }
                else
                {
                    neutralPoseAngles[index] =
                        new NpcAuthoringPoseAngle(boneId, angle);
                }

                return;
            }

            if (!Mathf.Approximately(angle, 0f))
            {
                neutralPoseAngles.Add(
                    new NpcAuthoringPoseAngle(boneId, angle));
                neutralPoseAngles.Sort(
                    (left, right) =>
                        left.BoneId.CompareTo(right.BoneId));
            }
        }


        public void RemoveNeutralPoseAngle(
            NpcRigBoneId boneId)
        {
            SetNeutralPoseAngle(boneId, 0f);
        }


        public void ClearNeutralPose()
        {
            neutralPoseAngles?.Clear();
        }


        public void ApplyBonePlacements(
            NpcCutoutRig rig)
        {
            ApplyBonePlacements(
                rig,
                NpcAuthoredDirection.SouthEast);
        }


        public void ApplyBonePlacements(
            NpcCutoutRig rig,
            NpcAuthoredDirection direction)
        {
            if (rig == null)
            {
                return;
            }

            if (bonePlacements != null)
            {
                for (int index = 0; index < bonePlacements.Count; index++)
                {
                    NpcAppearanceBonePlacement placement =
                        bonePlacements[index];

                    if (placement != null
                        && rig.TryGetBone(
                            placement.Id,
                            out Transform bone))
                    {
                        if (NpcRigDefinition.TryGetBoneDefinition(
                                placement.Id,
                                out NpcRigBoneDefinition definition))
                        {
                            // Body assets store their authored bind position.
                            // Apply its difference from the canonical rig so
                            // directional foot poses remain intact underneath.
                            Vector3 canonicalDelta =
                                placement.LocalPosition
                                - definition.LocalPosition;

                            bone.localPosition +=
                                NpcFacingUtility.ResolveAuthoredBonePosition(
                                    direction,
                                    placement.Id,
                                    canonicalDelta);
                        }
                        else
                        {
                            bone.localPosition =
                                NpcFacingUtility.ResolveAuthoredBonePosition(
                                    direction,
                                    placement.Id,
                                    placement.LocalPosition);
                        }
                    }
                }
            }

            ApplyNeutralPose(rig);
        }


        private void ApplyNeutralPose(
            NpcCutoutRig rig)
        {
            if (neutralPoseAngles == null)
            {
                return;
            }

            for (int index = 0; index < neutralPoseAngles.Count; index++)
            {
                NpcAuthoringPoseAngle entry = neutralPoseAngles[index];

                if (Mathf.Approximately(entry.Angle, 0f)
                    || !rig.TryGetBone(entry.BoneId, out Transform bone))
                {
                    continue;
                }

                bone.localRotation *=
                    Quaternion.Euler(0f, 0f, entry.Angle);
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
