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

        private const string WalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk.anim";

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
                "Directional Visual/Root/Pelvis/SpineLower/Chest/ShoulderNear",
                "Directional Visual/Root/Pelvis/SpineLower/Chest/ShoulderFar",
                "Directional Visual/Root/Pelvis/ThighNear",
                "Directional Visual/Root/Pelvis/ThighFar"
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
        public void PersonAnimationLibrary_HasLoopingIdleAndWalk()
        {
            AnimationClip idleClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    IdleClipPath);

            AnimationClip walkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    WalkClipPath);

            Assert.That(
                idleClip,
                Is.Not.Null);

            Assert.That(
                walkClip,
                Is.Not.Null);

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    idleClip).loopTime,
                Is.True);

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    walkClip).loopTime,
                Is.True);

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    idleClip).Length,
                Is.GreaterThanOrEqualTo(3));

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    walkClip).Length,
                Is.GreaterThanOrEqualTo(12));

            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(walkClip);

            Assert.That(
                bindings.Any(
                    binding => binding.path.Contains("Near")),
                Is.True,
                "The walk clip must target the migrated Near chain.");

            Assert.That(
                bindings.Any(
                    binding => binding.path.Contains("Far")),
                Is.True,
                "The walk clip must target the migrated Far chain.");

            Assert.That(
                bindings.Any(
                    binding => binding.path.Contains("SourceCamera")),
                Is.False,
                "The active walk clip must not retain legacy screen-side "
                + "binding paths.");

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            foreach (EditorCurveBinding binding in bindings)
            {
                if (string.IsNullOrEmpty(binding.path))
                {
                    continue;
                }

                Assert.That(
                    prefab.transform.Find(binding.path),
                    Is.Not.Null,
                    $"Walk binding path does not resolve: {binding.path}");
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
    }
}
