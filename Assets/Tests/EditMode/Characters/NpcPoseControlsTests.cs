using BigRetail.Characters.Editor;
using BigRetail.Characters.Rigging;
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcPoseControlsTests
    {
        private const string PrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        [Test]
        public void TryResolveRig_FindsPersonFromRootAndBodyPart()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            NpcCutoutRig expectedRig =
                prefab.GetComponent<NpcCutoutRig>();

            Assert.That(prefab, Is.Not.Null);
            Assert.That(expectedRig, Is.Not.Null);
            Assert.That(
                NpcPoseControlsUtility.TryResolveRig(
                    prefab,
                    out NpcCutoutRig rootRig),
                Is.True);
            Assert.That(rootRig, Is.SameAs(expectedRig));

            Assert.That(
                expectedRig.TryGetBone(
                    NpcRigBoneId.HandForeground,
                    out Transform hand),
                Is.True);
            Assert.That(
                NpcPoseControlsUtility.TryResolveRig(
                    hand.gameObject,
                    out NpcCutoutRig childRig),
                Is.True);
            Assert.That(childRig, Is.SameAs(expectedRig));
        }


        [Test]
        public void TryResolveRig_RejectsAParentWithMultiplePeople()
        {
            GameObject root = new GameObject("Population");
            GameObject firstPerson = new GameObject("Person One");
            GameObject secondPerson = new GameObject("Person Two");

            try
            {
                firstPerson.transform.SetParent(root.transform);
                secondPerson.transform.SetParent(root.transform);
                firstPerson.AddComponent<NpcCutoutRig>();
                secondPerson.AddComponent<NpcCutoutRig>();

                Assert.That(
                    NpcPoseControlsUtility.TryResolveRig(
                        root,
                        out NpcCutoutRig resolvedRig),
                    Is.False);
                Assert.That(resolvedRig, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        [TestCase(0f, 0f)]
        [TestCase(45f, 45f)]
        [TestCase(315f, -45f)]
        [TestCase(405f, 45f)]
        [TestCase(-405f, -45f)]
        public void NormalizeAngle_ReturnsSignedRotation(
            float source,
            float expected)
        {
            Assert.That(
                NpcPoseControlsUtility.NormalizeAngle(source),
                Is.EqualTo(expected).Within(0.001f));
        }


        [Test]
        public void SetLocalZAngle_ChangesOnlyTheRequestedAxis()
        {
            GameObject boneObject = new GameObject("Bone");

            try
            {
                Transform bone = boneObject.transform;
                bone.localEulerAngles = new Vector3(0f, 0f, 15f);

                NpcPoseControlsUtility.SetLocalZAngle(bone, -32f);

                Assert.That(
                    NpcPoseControlsUtility.GetLocalZAngle(bone),
                    Is.EqualTo(-32f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(boneObject);
            }
        }


        [TestCase(0.5f, 0.5f, 1f, 2f, 1f)]
        [TestCase(0.5f, 0.5f, 0.5f, 2f, 0.75f)]
        [TestCase(0.5f, 0.5f, 2f, 2f, 1.5f)]
        [TestCase(1.75f, 0.5f, 1f, 2f, 0.25f)]
        [TestCase(0.5f, -1f, 1f, 2f, 0.5f)]
        [TestCase(0.5f, 0.5f, 1f, 0f, 0f)]
        public void AdvancePlaybackTime_AppliesSpeedAndLoops(
            float currentTime,
            float deltaTime,
            float playbackSpeed,
            float clipLength,
            float expectedTime)
        {
            Assert.That(
                NpcPoseControlsUtility.AdvancePlaybackTime(
                    currentTime,
                    deltaTime,
                    playbackSpeed,
                    clipLength),
                Is.EqualTo(expectedTime).Within(0.001f));
        }


        [TestCase(2f, 0, 11, 0f)]
        [TestCase(2f, 5, 11, 1f)]
        [TestCase(2f, 10, 11, 2f)]
        [TestCase(2f, -3, 11, 0f)]
        [TestCase(2f, 20, 11, 2f)]
        [TestCase(0f, 5, 11, 0f)]
        public void AnimationReviewSampleTime_SpansTheWholeClip(
            float clipLength,
            int sampleIndex,
            int sampleCount,
            float expectedTime)
        {
            Assert.That(
                NpcAnimationReviewCapture.GetSampleTime(
                    clipLength,
                    sampleIndex,
                    sampleCount),
                Is.EqualTo(expectedTime).Within(0.001f));
        }


        [TestCase("Person_Walk_NorthFacing", NpcFacing.NorthEast, true)]
        [TestCase("Person_Walk_NorthFacing", NpcFacing.SouthEast, false)]
        [TestCase("Person_Walk_SouthFacing", NpcFacing.SouthWest, true)]
        [TestCase("Person_Walk_SouthFacing", NpcFacing.NorthWest, false)]
        [TestCase("Person_Grab", NpcFacing.NorthWest, true)]
        public void AnimationReviewFacingCompatibility_UsesClipFamily(
            string clipName,
            NpcFacing facing,
            bool expectedCompatibility)
        {
            AnimationClip clip = new AnimationClip
            {
                name = clipName
            };

            try
            {
                Assert.That(
                    NpcAnimationReviewCapture.IsFacingCompatible(
                        clip,
                        facing),
                    Is.EqualTo(expectedCompatibility));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }


        [Test]
        public void AnimationReviewOutput_UsesPersistentIgnoredLogsFolder()
        {
            string expectedPath = Path.Combine(
                "Project",
                "Logs",
                "CodexAnimationReviews");

            Assert.That(
                NpcAnimationReviewCapture.GetReviewRootPath("Project"),
                Is.EqualTo(expectedPath));
        }


        [Test]
        public void DirectionBasePoses_KeepSouthAndNorthArmsIndependent()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                NpcCutoutRig rig =
                    instance.GetComponent<NpcCutoutRig>();

                Assert.That(rig, Is.Not.Null);
                rig.SetFacing(NpcFacing.SouthEast);
                Assert.That(
                    rig.TryGetBone(
                        NpcRigBoneId.UpperArmForeground,
                        out Transform southArm),
                    Is.True);
                Vector3 originalSouthPosition = southArm.localPosition;
                float originalSouthAngle =
                    NpcPoseControlsUtility.GetLocalZAngle(southArm);

                Assert.That(
                    rig.InitializeCompleteAuthoredBonePoses(),
                    Is.True);
                Assert.That(
                    rig.HasCompleteAuthoredBonePose(
                        NpcAuthoredDirection.SouthEast),
                    Is.True);
                Assert.That(
                    rig.HasCompleteAuthoredBonePose(
                        NpcAuthoredDirection.NorthEast),
                    Is.True);

                rig.SetFacing(NpcFacing.SouthEast);
                Assert.That(
                    Vector3.Distance(
                        southArm.localPosition,
                        originalSouthPosition),
                    Is.LessThan(0.001f));
                Assert.That(
                    NpcPoseControlsUtility.GetLocalZAngle(southArm),
                    Is.EqualTo(originalSouthAngle).Within(0.001f));

                rig.SetFacing(NpcFacing.NorthEast);
                Assert.That(
                    rig.TryGetBone(
                        NpcRigBoneId.UpperArmForeground,
                        out Transform northArm),
                    Is.True);
                float originalNorthAngle =
                    NpcPoseControlsUtility.GetLocalZAngle(northArm);

                rig.SetFacing(NpcFacing.SouthEast);
                Assert.That(
                    rig.TryGetBone(
                        NpcRigBoneId.UpperArmForeground,
                        out southArm),
                    Is.True);
                float editedSouthAngle =
                    NpcPoseControlsUtility.NormalizeAngle(
                        NpcPoseControlsUtility.GetLocalZAngle(southArm)
                        + 37f);
                NpcPoseControlsUtility.SetLocalZAngle(
                    southArm,
                    editedSouthAngle);

                Assert.That(
                    rig.CaptureAuthoredBoneRotation(
                        NpcAuthoredDirection.SouthEast,
                        NpcRigBoneId.UpperArmForeground),
                    Is.True);

                rig.SetFacing(NpcFacing.NorthEast);
                Assert.That(
                    NpcPoseControlsUtility.GetLocalZAngle(northArm),
                    Is.EqualTo(originalNorthAngle).Within(0.001f));

                rig.SetFacing(NpcFacing.SouthEast);
                Assert.That(
                    NpcPoseControlsUtility.GetLocalZAngle(southArm),
                    Is.EqualTo(editedSouthAngle).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
