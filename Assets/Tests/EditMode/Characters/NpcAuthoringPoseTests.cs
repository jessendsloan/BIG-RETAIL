using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcAuthoringPoseTests
    {
        [Test]
        public void SavedAssetType_HasMatchingMonoScript()
        {
            NpcAuthoringPose pose =
                ScriptableObject.CreateInstance<NpcAuthoringPose>();

            try
            {
                MonoScript script =
                    MonoScript.FromScriptableObject(pose);

                Assert.That(script, Is.Not.Null);
                Assert.That(script.name, Is.EqualTo("NpcAuthoringPose"));
            }
            finally
            {
                Object.DestroyImmediate(pose);
            }
        }


        [Test]
        public void ConfigureAndCopyAngles_PreserveReusablePose()
        {
            NpcAuthoringPose pose =
                ScriptableObject.CreateInstance<NpcAuthoringPose>();
            Dictionary<NpcRigBoneId, float> source =
                new Dictionary<NpcRigBoneId, float>
                {
                    { NpcRigBoneId.Chest, 0f },
                    {
                        NpcRigBoneId.UpperArmSourceCameraLeft,
                        -90f
                    },
                    {
                        NpcRigBoneId.UpperArmSourceCameraRight,
                        90f
                    }
                };
            Dictionary<NpcRigBoneId, float> destination =
                new Dictionary<NpcRigBoneId, float>
                {
                    { NpcRigBoneId.Head, 30f }
                };

            try
            {
                pose.Configure("T-Pose", source);
                pose.CopyAnglesTo(destination);

                Assert.That(pose.DisplayName, Is.EqualTo("T-Pose"));
                Assert.That(pose.BoneAngles.Count, Is.EqualTo(2));
                Assert.That(
                    destination.ContainsKey(NpcRigBoneId.Head),
                    Is.False);
                Assert.That(
                    destination[NpcRigBoneId.UpperArmSourceCameraLeft],
                    Is.EqualTo(-90f));
                Assert.That(
                    destination[NpcRigBoneId.UpperArmSourceCameraRight],
                    Is.EqualTo(90f));
            }
            finally
            {
                Object.DestroyImmediate(pose);
            }
        }
    }
}
