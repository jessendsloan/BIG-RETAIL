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
            "Assets/Prefabs/Characters/Prototype/"
            + "RoundedEmployeeRowan.prefab";

        private const string IdleClipPath =
            "Assets/Animations/Characters/Prototype/"
            + "Rowan_Idle.anim";

        private const string WalkClipPath =
            "Assets/Animations/Characters/Prototype/"
            + "Rowan_Walk.anim";

        private const string ControllerPath =
            "Assets/Animations/Characters/Prototype/"
            + "Rowan.controller";


        [Test]
        public void RowanPrefab_HasValidRigAndAnimationController()
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
        }


        [Test]
        public void RowanAnimationLibrary_HasLoopingIdleAndWalk()
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
                Is.GreaterThanOrEqualTo(9));
        }


        [Test]
        public void RowanController_UsesSpeedForIdleWalkTransitions()
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
    }
}
