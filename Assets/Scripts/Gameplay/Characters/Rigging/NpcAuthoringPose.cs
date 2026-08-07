using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    [Serializable]
    public struct NpcAuthoringPoseAngle
    {
        [SerializeField]
        private NpcRigBoneId boneId;

        [SerializeField]
        private float angle;


        public NpcRigBoneId BoneId => boneId;

        public float Angle => angle;


        public NpcAuthoringPoseAngle(
            NpcRigBoneId newBoneId,
            float newAngle)
        {
            boneId = newBoneId;
            angle = newAngle;
        }
    }


    /// <summary>
    /// Reusable preview-only pose for inspecting and aligning cutout artwork.
    /// Authoring poses never replace gameplay animation clips or the rig bind
    /// pose.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AuthoringPose",
        menuName = "Big Retail/Characters/Authoring Pose")]
    public sealed class NpcAuthoringPose : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Authoring Pose";

        [SerializeField]
        private List<NpcAuthoringPoseAngle> boneAngles =
            new List<NpcAuthoringPoseAngle>();


        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;

        public IReadOnlyList<NpcAuthoringPoseAngle> BoneAngles => boneAngles;


        public void Configure(
            string newDisplayName,
            IReadOnlyDictionary<NpcRigBoneId, float> newAngles)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            boneAngles.Clear();

            if (newAngles == null)
            {
                return;
            }

            List<NpcRigBoneId> ids =
                new List<NpcRigBoneId>(newAngles.Keys);
            ids.Sort((left, right) => left.CompareTo(right));

            for (int index = 0; index < ids.Count; index++)
            {
                NpcRigBoneId id = ids[index];
                float angle = newAngles[id];

                if (!Mathf.Approximately(angle, 0f))
                {
                    boneAngles.Add(
                        new NpcAuthoringPoseAngle(id, angle));
                }
            }
        }


        public void CopyAnglesTo(
            IDictionary<NpcRigBoneId, float> destination)
        {
            if (destination == null)
            {
                return;
            }

            destination.Clear();

            for (int index = 0; index < boneAngles.Count; index++)
            {
                NpcAuthoringPoseAngle entry = boneAngles[index];
                destination[entry.BoneId] = entry.Angle;
            }
        }
    }
}
