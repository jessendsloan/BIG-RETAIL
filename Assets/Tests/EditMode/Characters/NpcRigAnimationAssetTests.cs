using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using NUnit.Framework;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcRigAnimationAssetTests
    {
        private const string PrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        private const string IdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle.anim";

        private const string SouthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_SouthFacing.anim";

        private const string NorthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_NorthFacing.anim";

        private const string ControllerPath =
            "Assets/Animations/Characters/Core/Person.controller";


        [Test]
        public void PersonPrefab_HasValidRigAndAnimationController()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);

            Assert.That(
                prefab,
                Is.Not.Null);

            NpcCutoutRig rig =
                prefab.GetComponent<NpcCutoutRig>();

            Assert.That(
                rig,
                Is.Not.Null);

            Assert.That(
                rig.TryValidate(
                    out string failureReason),
                Is.True,
                failureReason);

            Animator animator =
                prefab.GetComponent<Animator>();

            Assert.That(
                animator,
                Is.Not.Null);

            Assert.That(
                AssetDatabase.GetAssetPath(
                    animator.runtimeAnimatorController),
                Is.EqualTo(
                    ControllerPath));

            string[] requiredDepthChains =
            {
                "Directional Visual/Root/Pelvis/SpineLower/Chest/ShoulderForeground",
                "Directional Visual/Root/Pelvis/SpineLower/Chest/ShoulderBackground",
                "Directional Visual/Root/Pelvis/ThighForeground",
                "Directional Visual/Root/Pelvis/ThighBackground"
            };

            foreach (string path in requiredDepthChains)
            {
                Assert.That(
                    prefab.transform.Find(path),
                    Is.Not.Null,
                    $"Person prefab is missing the stable depth chain: {path}");
            }

            Assert.That(
                prefab.GetComponentsInChildren<Transform>(true).Any(
                    child => child.name.Contains("SourceCamera")),
                Is.False,
                "The active Person prefab must not retain legacy "
                + "screen-side hierarchy names.");
        }


        [Test]
        public void PersonPrefab_AuthoredNorthEastFeetUseNorthFacingHeading()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            NpcCutoutRig rig = prefab.GetComponent<NpcCutoutRig>();
            SerializedObject serializedRig = new SerializedObject(rig);

            SerializedProperty southPose =
                serializedRig.FindProperty("southEastBonePose");
            SerializedProperty northPose =
                serializedRig.FindProperty("northEastBonePose");

            NpcRigBoneId[] footBones =
            {
                NpcRigBoneId.FootForeground,
                NpcRigBoneId.FootBackground
            };

            for (int index = 0; index < footBones.Length; index++)
            {
                float southAngle = GetAuthoredFootAngle(
                    southPose,
                    footBones[index]);
                float northAngle = GetAuthoredFootAngle(
                    northPose,
                    footBones[index]);
                float southHorizontalPosition =
                    GetAuthoredFootHorizontalPosition(
                        southPose,
                        footBones[index]);
                float northHorizontalPosition =
                    GetAuthoredFootHorizontalPosition(
                        northPose,
                        footBones[index]);

                Assert.That(
                    southAngle,
                    Is.LessThan(0f),
                    $"{footBones[index]} must point east in the authored "
                    + "SouthEast source pose.");
                Assert.That(
                    northAngle,
                    Is.GreaterThan(0f),
                    $"{footBones[index]} must use the authored NorthEast "
                    + "heading before NorthWest mirroring.");
                Assert.That(
                    Mathf.Abs(northAngle),
                    Is.EqualTo(Mathf.Abs(southAngle)).Within(0.001f),
                    $"{footBones[index]} should preserve the authored foot "
                    + "tilt magnitude while reversing its North/South "
                    + "heading.");
                Assert.That(
                    southHorizontalPosition,
                    Is.GreaterThan(0f),
                    $"{footBones[index]} must sit on the east side in the "
                    + "authored SouthEast source pose.");
                Assert.That(
                    northHorizontalPosition,
                    Is.GreaterThan(0f),
                    $"{footBones[index]} must sit on the east side in the "
                    + "authored NorthEast source pose before NorthWest "
                    + "mirroring.");
            }
        }


        [Test]
        public void PersonAnimationLibrary_HasNamedIndependentWalkViews()
        {
            AnimationClip idleClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    IdleClipPath);

            AnimationClip southFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);

            AnimationClip northFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);

            Assert.That(
                idleClip,
                Is.Not.Null);

            Assert.That(
                southFacingWalkClip,
                Is.Not.Null);

            Assert.That(
                northFacingWalkClip,
                Is.Not.Null);

            Assert.That(
                southFacingWalkClip.name,
                Is.EqualTo("Person_Walk_SouthFacing"));

            Assert.That(
                northFacingWalkClip.name,
                Is.EqualTo("Person_Walk_NorthFacing"));

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    idleClip).loopTime,
                Is.True);

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    southFacingWalkClip).loopTime,
                Is.True);

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    northFacingWalkClip).loopTime,
                Is.True);

            Assert.That(
                southFacingWalkClip.length,
                Is.GreaterThan(0f));

            Assert.That(
                northFacingWalkClip.length,
                Is.GreaterThan(0f));

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    idleClip).Length,
                Is.GreaterThanOrEqualTo(3));

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    southFacingWalkClip).Length,
                Is.GreaterThanOrEqualTo(12));

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    northFacingWalkClip).Length,
                Is.GreaterThanOrEqualTo(12));

            EditorCurveBinding[] southBindings =
                AnimationUtility.GetCurveBindings(southFacingWalkClip);
            EditorCurveBinding[] northBindings =
                AnimationUtility.GetCurveBindings(northFacingWalkClip);

            Assert.That(
                southBindings.Any(
                    binding => binding.path.Contains("Foreground")),
                Is.True,
                "The south-facing walk must target the Foreground chain.");

            Assert.That(
                southBindings.Any(
                    binding => binding.path.Contains("Background")),
                Is.True,
                "The south-facing walk must target the Background chain.");

            Assert.That(
                southBindings.Any(
                    binding => binding.path.Contains("SourceCamera")),
                Is.False,
                "The south-facing walk must not retain legacy screen-side "
                + "binding paths.");

            Assert.That(
                northBindings.Any(
                    binding => binding.path.Contains("SourceCamera")),
                Is.False,
                "The north-facing walk must not retain legacy screen-side "
                + "binding paths.");

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            foreach (EditorCurveBinding binding in
                     southBindings.Concat(northBindings))
            {
                if (string.IsNullOrEmpty(binding.path))
                {
                    continue;
                }

                Assert.That(
                    prefab.transform.Find(binding.path),
                    Is.Not.Null,
                    $"Authored walk binding path does not resolve: "
                    + binding.path);
            }
        }


        [Test]
        public void PersonController_UsesSpeedForIdleWalkTransitions()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            Assert.That(
                controller,
                Is.Not.Null);

            AnimatorControllerParameter speedParameter =
                controller.parameters.SingleOrDefault(
                    parameter =>
                        parameter.name == "Speed");

            Assert.That(
                speedParameter,
                Is.Not.Null);

            Assert.That(
                speedParameter.type,
                Is.EqualTo(
                    AnimatorControllerParameterType.Float));

            AnimatorControllerParameter facingNorthParameter =
                controller.parameters.SingleOrDefault(
                    parameter =>
                        parameter.name == "FacingNorth");

            Assert.That(
                facingNorthParameter,
                Is.Not.Null);

            Assert.That(
                facingNorthParameter.type,
                Is.EqualTo(
                    AnimatorControllerParameterType.Float));

            Assert.That(
                controller.parameters.Any(
                    parameter => parameter.name == "MotionMirror"),
                Is.False);

            string[] stateNames =
                controller.layers[0]
                    .stateMachine.states
                    .Select(
                        childState =>
                            childState.state.name)
                    .ToArray();

            CollectionAssert.Contains(
                stateNames,
                "Idle");

            CollectionAssert.Contains(
                stateNames,
                "Walk");

            AnimatorState walkState =
                controller.layers[0]
                    .stateMachine.states
                    .Single(
                        childState =>
                            childState.state.name == "Walk")
                    .state;

            Assert.That(
                walkState.motion,
                Is.TypeOf<BlendTree>());

            BlendTree facingWalk = (BlendTree)walkState.motion;

            Assert.That(
                facingWalk.name,
                Is.EqualTo("Walk Facing Direction"));

            Assert.That(
                facingWalk.blendParameter,
                Is.EqualTo("FacingNorth"));

            Assert.That(
                facingWalk.children.Length,
                Is.EqualTo(2));

            Assert.That(
                AssetDatabase.GetAssetPath(
                    facingWalk.children[0].motion),
                Is.EqualTo(SouthFacingWalkClipPath));

            Assert.That(
                facingWalk.children[0].threshold,
                Is.EqualTo(0f));

            Assert.That(
                AssetDatabase.GetAssetPath(
                    facingWalk.children[1].motion),
                Is.EqualTo(NorthFacingWalkClipPath));

            Assert.That(
                facingWalk.children[1].threshold,
                Is.EqualTo(1f));
        }


        [Test]
        public void AppearanceCorePlacements_DoNotAccumulateOnRefresh()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            NpcBodySilhouette body =
                ScriptableObject.CreateInstance<NpcBodySilhouette>();
            NpcAppearanceProfile profile =
                ScriptableObject.CreateInstance<NpcAppearanceProfile>();

            NpcRigBoneId[] coreBones =
            {
                NpcRigBoneId.Pelvis,
                NpcRigBoneId.SpineLower,
                NpcRigBoneId.Chest,
                NpcRigBoneId.Neck,
                NpcRigBoneId.Head
            };

            try
            {
                List<NpcAppearanceBonePlacement> placements =
                    new List<NpcAppearanceBonePlacement>();

                for (int index = 0; index < coreBones.Length; index++)
                {
                    Assert.That(
                        NpcRigDefinition.TryGetBoneDefinition(
                            coreBones[index],
                            out NpcRigBoneDefinition definition),
                        Is.True);

                    placements.Add(
                        new NpcAppearanceBonePlacement(
                            coreBones[index],
                            definition.LocalPosition
                            + new Vector3(
                                0.01f * (index + 1),
                                0.02f * (index + 1),
                                0f)));
                }

                body.Configure(
                    "Core Placement Regression Body",
                    NpcBodySilhouetteKind.Masculine,
                    System.Array.Empty<NpcAppearancePartShape>(),
                    placements);
                profile.Configure(
                    "Core Placement Regression Profile",
                    body,
                    null,
                    null,
                    null);

                NpcCutoutRig rig = instance.GetComponent<NpcCutoutRig>();
                rig.SetAppearancePreview(profile);

                Dictionary<NpcRigBoneId, Vector3> firstPositions =
                    new Dictionary<NpcRigBoneId, Vector3>();

                for (int index = 0; index < coreBones.Length; index++)
                {
                    Assert.That(
                        rig.TryGetBone(
                            coreBones[index],
                            out Transform bone),
                        Is.True);
                    firstPositions[coreBones[index]] = bone.localPosition;
                }

                rig.SetAppearancePreview(profile);
                rig.SetFacing(rig.Facing);

                for (int index = 0; index < coreBones.Length; index++)
                {
                    Assert.That(
                        rig.TryGetBone(
                            coreBones[index],
                            out Transform bone),
                        Is.True);
                    Assert.That(
                        Vector3.Distance(
                            bone.localPosition,
                            firstPositions[coreBones[index]]),
                        Is.LessThan(0.00001f),
                        $"{coreBones[index]} moved after an identical "
                        + "appearance refresh.");
                }
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(instance);
            }
        }


        [Test]
        public void BodyNeutralPose_CanBeEditedAndCleared()
        {
            NpcBodySilhouette body =
                ScriptableObject.CreateInstance<NpcBodySilhouette>();

            try
            {
                body.SetNeutralPoseAngle(
                    NpcRigBoneId.Chest,
                    12f);

                Assert.That(
                    body.GetNeutralPoseAngle(NpcRigBoneId.Chest),
                    Is.EqualTo(12f).Within(0.0001f));
                Assert.That(
                    body.NeutralPoseAngles.Count,
                    Is.EqualTo(1));

                body.SetNeutralPoseAngle(
                    NpcRigBoneId.Chest,
                    -8f);

                Assert.That(
                    body.GetNeutralPoseAngle(NpcRigBoneId.Chest),
                    Is.EqualTo(-8f).Within(0.0001f));
                Assert.That(
                    body.NeutralPoseAngles.Count,
                    Is.EqualTo(1));

                body.SetNeutralPoseAngle(
                    NpcRigBoneId.Neck,
                    5f);
                body.RemoveNeutralPoseAngle(NpcRigBoneId.Chest);

                Assert.That(
                    body.GetNeutralPoseAngle(NpcRigBoneId.Chest),
                    Is.Zero);
                Assert.That(
                    body.GetNeutralPoseAngle(NpcRigBoneId.Neck),
                    Is.EqualTo(5f).Within(0.0001f));

                body.ClearNeutralPose();

                Assert.That(
                    body.NeutralPoseAngles,
                    Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }


        [Test]
        public void BodyNeutralPose_AppliesWithoutAccumulating()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            NpcBodySilhouette body =
                ScriptableObject.CreateInstance<NpcBodySilhouette>();
            NpcAppearanceProfile profile =
                ScriptableObject.CreateInstance<NpcAppearanceProfile>();

            try
            {
                body.Configure(
                    "Neutral Pose Regression Body",
                    NpcBodySilhouetteKind.Masculine,
                    System.Array.Empty<NpcAppearancePartShape>(),
                    System.Array.Empty<NpcAppearanceBonePlacement>());
                profile.Configure(
                    "Neutral Pose Regression Profile",
                    body,
                    null,
                    null,
                    null);

                NpcCutoutRig rig = instance.GetComponent<NpcCutoutRig>();
                rig.SetAppearancePreview(profile);

                Assert.That(
                    rig.TryGetBone(
                        NpcRigBoneId.Chest,
                        out Transform chest),
                    Is.True);

                float baselineAngle = chest.localEulerAngles.z;

                body.SetNeutralPoseAngle(
                    NpcRigBoneId.Chest,
                    12f);
                rig.SetAppearancePreview(profile);

                float firstAppliedAngle = chest.localEulerAngles.z;

                Assert.That(
                    Mathf.DeltaAngle(
                        baselineAngle,
                        firstAppliedAngle),
                    Is.EqualTo(12f).Within(0.001f));

                rig.SetAppearancePreview(profile);
                rig.SetFacing(rig.Facing);

                Assert.That(
                    Mathf.DeltaAngle(
                        firstAppliedAngle,
                        chest.localEulerAngles.z),
                    Is.Zero.Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(body);
                Object.DestroyImmediate(instance);
            }
        }


        private static float GetAuthoredFootAngle(
            SerializedProperty pose,
            NpcRigBoneId footBone)
        {
            Assert.That(pose, Is.Not.Null);

            for (int index = 0; index < pose.arraySize; index++)
            {
                SerializedProperty entry =
                    pose.GetArrayElementAtIndex(index);

                if (entry.FindPropertyRelative("id").intValue
                    != (int)footBone)
                {
                    continue;
                }

                Vector3 eulerAngles =
                    entry.FindPropertyRelative(
                        "localEulerAngles").vector3Value;

                return Mathf.DeltaAngle(0f, eulerAngles.z);
            }

            Assert.Fail(
                $"The Person prefab is missing an authored pose for "
                + $"{footBone}.");
            return 0f;
        }


        private static float GetAuthoredFootHorizontalPosition(
            SerializedProperty pose,
            NpcRigBoneId footBone)
        {
            Assert.That(pose, Is.Not.Null);

            for (int index = 0; index < pose.arraySize; index++)
            {
                SerializedProperty entry =
                    pose.GetArrayElementAtIndex(index);

                if (entry.FindPropertyRelative("id").intValue
                    != (int)footBone)
                {
                    continue;
                }

                return entry.FindPropertyRelative(
                    "localPosition").vector3Value.x;
            }

            Assert.Fail(
                $"The Person prefab is missing an authored pose for "
                + $"{footBone}.");
            return 0f;
        }
    }
}
