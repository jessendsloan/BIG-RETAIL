using System.Collections.Generic;
using System.Linq;
using BigRetail.Characters.Editor;
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

        private const string SouthFacingIdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle_SouthFacing.anim";

        private const string NorthFacingIdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle_NorthFacing.anim";

        private const string SouthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_SouthFacing.anim";

        private const string NorthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_NorthFacing.anim";

        private const string SouthFacingShelfGrabClipPath =
            NpcShelfGrabAnimationAssetBuilder.SouthFacingClipPath;

        private const string NorthFacingShelfGrabClipPath =
            NpcShelfGrabAnimationAssetBuilder.NorthFacingClipPath;

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
        public void PersonAnimationLibrary_HasNamedIndependentDirectionalViews()
        {
            AnimationClip southFacingIdleClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingIdleClipPath);

            AnimationClip northFacingIdleClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingIdleClipPath);

            AnimationClip southFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);

            AnimationClip northFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);

            Assert.That(
                southFacingIdleClip,
                Is.Not.Null);

            Assert.That(
                northFacingIdleClip,
                Is.Not.Null);

            Assert.That(
                southFacingWalkClip,
                Is.Not.Null);

            Assert.That(
                northFacingWalkClip,
                Is.Not.Null);

            Assert.That(
                southFacingIdleClip.name,
                Is.EqualTo("Person_Idle_SouthFacing"));

            Assert.That(
                northFacingIdleClip.name,
                Is.EqualTo("Person_Idle_NorthFacing"));

            Assert.That(
                southFacingWalkClip.name,
                Is.EqualTo("Person_Walk_SouthFacing"));

            Assert.That(
                northFacingWalkClip.name,
                Is.EqualTo("Person_Walk_NorthFacing"));

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    southFacingIdleClip).loopTime,
                Is.True);

            Assert.That(
                AnimationUtility.GetAnimationClipSettings(
                    northFacingIdleClip).loopTime,
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
                southFacingIdleClip.length,
                Is.GreaterThan(0f));

            Assert.That(
                northFacingIdleClip.length,
                Is.GreaterThan(0f));

            Assert.That(
                southFacingWalkClip.length,
                Is.GreaterThan(0f));

            Assert.That(
                northFacingWalkClip.length,
                Is.GreaterThan(0f));

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    southFacingIdleClip).Length,
                Is.GreaterThanOrEqualTo(3));

            Assert.That(
                AnimationUtility.GetCurveBindings(
                    northFacingIdleClip).Length,
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
            EditorCurveBinding[] southIdleBindings =
                AnimationUtility.GetCurveBindings(southFacingIdleClip);
            EditorCurveBinding[] northIdleBindings =
                AnimationUtility.GetCurveBindings(northFacingIdleClip);

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

            string[] northArmBones =
            {
                "ShoulderForeground",
                "UpperArmForeground",
                "ForearmForeground",
                "HandForeground",
                "ShoulderBackground",
                "UpperArmBackground",
                "ForearmBackground",
                "HandBackground"
            };

            for (int index = 0; index < northArmBones.Length; index++)
            {
                string armBone = northArmBones[index];

                Assert.That(
                    northBindings.Any(
                        binding =>
                            binding.path.EndsWith("/" + armBone)),
                    Is.True,
                    $"The north-facing walk must animate {armBone}.");

                EditorCurveBinding northArmBinding =
                    northBindings.First(
                        binding =>
                            binding.path.EndsWith("/" + armBone)
                            && binding.propertyName.EndsWith(".z")
                            && binding.propertyName.Contains("EulerAngles"));
                EditorCurveBinding southSameSideBinding =
                    southBindings.First(
                        binding =>
                            binding.path == northArmBinding.path
                            && binding.type == northArmBinding.type
                            && binding.propertyName
                            == northArmBinding.propertyName);
                AnimationCurve northArmCurve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalkClip,
                        northArmBinding);
                AnimationCurve southSameSideCurve =
                    AnimationUtility.GetEditorCurve(
                        southFacingWalkClip,
                        southSameSideBinding);

                Assert.That(
                    AnimationCurvesMatch(
                        northArmCurve,
                        southSameSideCurve),
                    Is.False,
                    $"The rear-view {armBone} must not retain the "
                    + "same-side south-facing motion.");
            }

            string[] northFootBones =
            {
                "FootForeground",
                "FootBackground"
            };

            for (int index = 0; index < northFootBones.Length; index++)
            {
                string footBone = northFootBones[index];
                EditorCurveBinding footBinding =
                    northBindings.First(
                        binding =>
                            binding.path.EndsWith("/" + footBone)
                            && binding.propertyName.EndsWith(".z")
                            && binding.propertyName.Contains("EulerAngles"));
                AnimationCurve footCurve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalkClip,
                        footBinding);
                float meanSignedFootAngle =
                    footCurve.keys.Average(
                        key => Mathf.DeltaAngle(0f, key.value));

                Assert.That(
                    meanSignedFootAngle,
                    Is.GreaterThan(0f),
                    $"The north-facing {footBone} curve must preserve its "
                    + "north-oriented positive heading.");
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            foreach (EditorCurveBinding binding in
                     southBindings
                         .Concat(northBindings)
                         .Concat(southIdleBindings)
                         .Concat(northIdleBindings))
            {
                if (string.IsNullOrEmpty(binding.path))
                {
                    continue;
                }

                Assert.That(
                    prefab.transform.Find(binding.path),
                    Is.Not.Null,
                    $"Authored animation binding path does not resolve: "
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

            CollectionAssert.DoesNotContain(
                stateNames,
                "Person_Idle_NorthFacing");

            CollectionAssert.DoesNotContain(
                stateNames,
                "Person_Walk_NorthFacing");

            AnimatorState idleState =
                controller.layers[0]
                    .stateMachine.states
                    .Single(
                        childState =>
                            childState.state.name == "Idle")
                    .state;

            AnimatorState walkState =
                controller.layers[0]
                    .stateMachine.states
                    .Single(
                        childState =>
                            childState.state.name == "Walk")
                    .state;

            AssertDirectionalBlendTree(
                idleState,
                "Idle Facing Direction",
                SouthFacingIdleClipPath,
                NorthFacingIdleClipPath);
            AssertDirectionalBlendTree(
                walkState,
                "Walk Facing Direction",
                SouthFacingWalkClipPath,
                NorthFacingWalkClipPath);
        }


        [Test]
        public void ShelfGrabAnimationLibrary_HasIndependentDirectionalViews()
        {
            AnimationClip southClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingShelfGrabClipPath);
            AnimationClip northClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingShelfGrabClipPath);

            Assert.That(southClip, Is.Not.Null);
            Assert.That(northClip, Is.Not.Null);
            Assert.That(
                southClip.name,
                Is.EqualTo("Person_ShelfGrab_SouthFacing"));
            Assert.That(
                northClip.name,
                Is.EqualTo("Person_ShelfGrab_NorthFacing"));

            AnimationClip[] clips =
            {
                southClip,
                northClip
            };
            string[] requiredArmBones =
            {
                "ShoulderForeground",
                "UpperArmForeground",
                "ForearmForeground",
                "HandForeground",
                "ShoulderBackground",
                "UpperArmBackground",
                "ForearmBackground",
                "HandBackground"
            };
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            foreach (AnimationClip clip in clips)
            {
                Assert.That(
                    AnimationUtility.GetAnimationClipSettings(clip).loopTime,
                    Is.False,
                    $"{clip.name} must remain a one-shot interaction.");
                Assert.That(
                    clip.frameRate,
                    Is.EqualTo(
                        NpcShelfGrabAnimationAssetBuilder.FrameRate)
                        .Within(0.001f));
                Assert.That(
                    clip.length,
                    Is.EqualTo(
                        NpcShelfGrabAnimationAssetBuilder.DurationSeconds)
                        .Within(0.001f));

                EditorCurveBinding[] bindings =
                    AnimationUtility.GetCurveBindings(clip);

                Assert.That(
                    bindings.Any(
                        binding => binding.path.Contains("Thigh")
                                   || binding.path.Contains("Shin")
                                   || binding.path.Contains("Foot")),
                    Is.False,
                    "A shelf grab should preserve the planted leg pose.");

                for (int index = 0;
                     index < requiredArmBones.Length;
                     index++)
                {
                    string boneName = requiredArmBones[index];
                    EditorCurveBinding binding = bindings.Single(
                        candidate =>
                            candidate.path.EndsWith("/" + boneName)
                            && candidate.propertyName
                                == "localEulerAnglesRaw.z");
                    AnimationCurve curve =
                        AnimationUtility.GetEditorCurve(clip, binding);

                    Assert.That(curve, Is.Not.Null);
                    Assert.That(
                        Mathf.DeltaAngle(
                            curve.keys[0].value,
                            curve.keys[curve.length - 1].value),
                        Is.Zero.Within(0.001f),
                        $"{clip.name} must settle {boneName} back into its "
                        + "directional starting pose.");
                    Assert.That(
                        prefab.transform.Find(binding.path),
                        Is.Not.Null,
                        $"{clip.name} targets a missing rig path: "
                        + binding.path);
                }
            }

            AnimationCurve southReach = GetLocalZCurve(
                southClip,
                "UpperArmBackground");
            AnimationCurve northReach = GetLocalZCurve(
                northClip,
                "UpperArmForeground");
            AnimationCurve southElbow = GetLocalZCurve(
                southClip,
                "ForearmBackground");
            AnimationCurve northElbow = GetLocalZCurve(
                northClip,
                "ForearmForeground");

            Assert.That(
                southReach.keys.Max(key => key.value),
                Is.GreaterThan(55f),
                "The south-facing shelf-side arm must visibly reach without "
                + "crossing the torso.");
            Assert.That(
                northReach.keys.Max(key => key.value),
                Is.GreaterThan(75f),
                "The north-facing foreground arm must travel higher to "
                + "suggest depth toward the shelf.");
            Assert.That(
                AnimationCurvesMatch(southReach, northReach),
                Is.False,
                "North and south shelf grabs must remain independently "
                + "authored perspective views.");
            Assert.That(
                southElbow.Evaluate(1f),
                Is.EqualTo(14f).Within(0.001f),
                "The south arm must remain visually straight at full reach.");
            Assert.That(
                northElbow.Evaluate(1f),
                Is.Zero.Within(0.001f),
                "The north arm must not hinge backward at full reach.");

            foreach (AnimationClip clip in clips)
            {
                EditorCurveBinding pelvisForwardBinding =
                    EditorCurveBinding.FloatCurve(
                        "Directional Visual/Root/Pelvis",
                        typeof(Transform),
                        "m_LocalPosition.x");
                AnimationCurve pelvisForward =
                    AnimationUtility.GetEditorCurve(
                        clip,
                        pelvisForwardBinding);

                Assert.That(pelvisForward, Is.Not.Null);
                Assert.That(
                    pelvisForward.keys.Max(key => key.value),
                    Is.GreaterThan(0.03f),
                    $"{clip.name} must advance the body slightly into the "
                    + "reach.");
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            AnimatorState southState = controller.layers[0]
                .stateMachine.states
                .Single(
                    childState => childState.state.name
                        == southClip.name)
                .state;
            AnimatorState northState = controller.layers[0]
                .stateMachine.states
                .Single(
                    childState => childState.state.name
                        == northClip.name)
                .state;

            Assert.That(southState.motion, Is.SameAs(southClip));
            Assert.That(northState.motion, Is.SameAs(northClip));
            Assert.That(
                southState.transitions,
                Is.Empty,
                "The south shelf grab is selectable for authoring but must "
                + "not change gameplay transitions yet.");
            Assert.That(
                northState.transitions,
                Is.Empty,
                "The north shelf grab is selectable for authoring but must "
                + "not change gameplay transitions yet.");
        }


        [Test]
        public void NorthWalkGaitRepair_SwapsPreviouslyCopiedShoulderSides()
        {
            const string armRoot =
                "Directional Visual/Root/Pelvis/SpineLower/Chest/";
            EditorCurveBinding foregroundBinding =
                EditorCurveBinding.FloatCurve(
                    armRoot + "ShoulderForeground",
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            EditorCurveBinding backgroundBinding =
                EditorCurveBinding.FloatCurve(
                    armRoot + "ShoulderBackground",
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            AnimationCurve southForeground =
                AnimationCurve.Linear(0f, -12f, 1f, 18f);
            AnimationCurve southBackground =
                AnimationCurve.Linear(0f, 32f, 1f, -24f);
            AnimationClip southClip = new AnimationClip();
            AnimationClip northClip = new AnimationClip();

            try
            {
                AnimationUtility.SetEditorCurve(
                    southClip,
                    foregroundBinding,
                    southForeground);
                AnimationUtility.SetEditorCurve(
                    southClip,
                    backgroundBinding,
                    southBackground);

                // Reproduce the old bug: each north shoulder received the
                // motion from the identically named south shoulder.
                AnimationUtility.SetEditorCurve(
                    northClip,
                    foregroundBinding,
                    new AnimationCurve(southForeground.keys));
                AnimationUtility.SetEditorCurve(
                    northClip,
                    backgroundBinding,
                    new AnimationCurve(southBackground.keys));

                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.True);

                Assert.That(
                    AnimationCurvesMatch(
                        AnimationUtility.GetEditorCurve(
                            northClip,
                            foregroundBinding),
                        southBackground),
                    Is.True,
                    "The north foreground shoulder must receive the south "
                    + "background shoulder motion.");
                Assert.That(
                    AnimationCurvesMatch(
                        AnimationUtility.GetEditorCurve(
                            northClip,
                            backgroundBinding),
                        southForeground),
                    Is.True,
                    "The north background shoulder must receive the south "
                    + "foreground shoulder motion.");

                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.False,
                    "A second repair pass must be idempotent.");
            }
            finally
            {
                Object.DestroyImmediate(northClip);
                Object.DestroyImmediate(southClip);
            }
        }


        [Test]
        public void NorthWalkGaitRepair_TightensGeneratedBackgroundUpperArm()
        {
            const string armRoot =
                "Directional Visual/Root/Pelvis/SpineLower/Chest/";
            EditorCurveBinding sourceBinding =
                EditorCurveBinding.FloatCurve(
                    armRoot
                    + "ShoulderForeground/UpperArmForeground",
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            EditorCurveBinding targetBinding =
                EditorCurveBinding.FloatCurve(
                    armRoot
                    + "ShoulderBackground/UpperArmBackground",
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            AnimationCurve generatedSource = new AnimationCurve(
                new Keyframe(0f, 22f),
                new Keyframe(0.33333334f, 0f),
                new Keyframe(0.8333333f, -25f),
                new Keyframe(1.1666666f, 0f),
                new Keyframe(1.6666666f, 22f));
            AnimationClip southClip = new AnimationClip();
            AnimationClip northClip = new AnimationClip();

            try
            {
                AnimationUtility.SetEditorCurve(
                    southClip,
                    sourceBinding,
                    generatedSource);
                AnimationUtility.SetEditorCurve(
                    northClip,
                    targetBinding,
                    new AnimationCurve(generatedSource.keys));

                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.True);

                AnimationCurve tightened =
                    AnimationUtility.GetEditorCurve(
                        northClip,
                        targetBinding);

                Assert.That(
                    tightened.keys.Min(key => key.value),
                    Is.EqualTo(-13.75f).Within(0.001f));
                Assert.That(
                    tightened.keys.Max(key => key.value),
                    Is.EqualTo(22f).Within(0.001f),
                    "The forward half of the swing must remain unchanged.");
                Assert.That(
                    generatedSource.keys.Min(key => key.value),
                    Is.EqualTo(-25f).Within(0.001f),
                    "The south-facing source clip must remain unchanged.");

                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.False,
                    "The range adjustment must be idempotent.");
            }
            finally
            {
                Object.DestroyImmediate(northClip);
                Object.DestroyImmediate(southClip);
            }
        }


        [Test]
        public void NorthWalkGaitRepair_SmoothsAuthoredBackgroundShoulder()
        {
            const string shoulderPath =
                "Directional Visual/Root/Pelvis/SpineLower/Chest/"
                + "ShoulderBackground";
            EditorCurveBinding shoulderBinding =
                EditorCurveBinding.FloatCurve(
                    shoulderPath,
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            AnimationCurve authoredShoulder = new AnimationCurve(
                new Keyframe(0f, 42f),
                new Keyframe(0.16666667f, 11f),
                new Keyframe(0.33333334f, 25f),
                new Keyframe(0.5f, 28f),
                new Keyframe(0.6666667f, 20f),
                new Keyframe(0.8333333f, 6f),
                new Keyframe(1f, -2f),
                new Keyframe(1.1666666f, 0f),
                new Keyframe(1.3333334f, 2f),
                new Keyframe(1.6666666f, 42f));
            AnimationClip southClip = new AnimationClip();
            AnimationClip northClip = new AnimationClip();
            float[] expectedValues =
            {
                24.25f,
                22.25f,
                22.25f,
                25.25f,
                18.5f,
                7.5f,
                0.5f,
                0f,
                11.5f,
                24.25f
            };

            try
            {
                AnimationUtility.SetEditorCurve(
                    northClip,
                    shoulderBinding,
                    authoredShoulder);

                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.True);

                AnimationCurve smoothedShoulder =
                    AnimationUtility.GetEditorCurve(
                        northClip,
                        shoulderBinding);
                Keyframe[] smoothedKeys = smoothedShoulder.keys;

                Assert.That(
                    smoothedKeys.Length,
                    Is.EqualTo(expectedValues.Length));

                for (int index = 0;
                     index < expectedValues.Length;
                     index++)
                {
                    Assert.That(
                        smoothedKeys[index].value,
                        Is.EqualTo(expectedValues[index]).Within(0.001f),
                        $"Unexpected smoothed shoulder value at key "
                        + index);
                }

                Assert.That(
                    smoothedKeys[0].inTangent,
                    Is.EqualTo(smoothedKeys[smoothedKeys.Length - 1]
                        .inTangent).Within(0.001f),
                    "The loop endpoints must share a tangent.");
                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.False,
                    "The shoulder smoothing pass must be idempotent.");
            }
            finally
            {
                Object.DestroyImmediate(northClip);
                Object.DestroyImmediate(southClip);
            }
        }


        [Test]
        public void NorthWalkGaitRepair_ShiftsBothUpperArmRangesForward()
        {
            const string armRoot =
                "Directional Visual/Root/Pelvis/SpineLower/Chest/";
            EditorCurveBinding foregroundBinding =
                EditorCurveBinding.FloatCurve(
                    armRoot
                    + "ShoulderForeground/UpperArmForeground",
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            EditorCurveBinding backgroundBinding =
                EditorCurveBinding.FloatCurve(
                    armRoot
                    + "ShoulderBackground/UpperArmBackground",
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            AnimationCurve foregroundCurve = new AnimationCurve(
                new Keyframe(0f, -42f),
                new Keyframe(0.16666667f, -32f),
                new Keyframe(0.33333334f, -20f),
                new Keyframe(0.6666667f, -5f),
                new Keyframe(0.8333333f, 5f),
                new Keyframe(1f, -5f),
                new Keyframe(1.1666666f, -20f),
                new Keyframe(1.3333334f, -32f),
                new Keyframe(1.6666666f, -42f));
            AnimationCurve backgroundCurve = new AnimationCurve(
                new Keyframe(0f, 22f),
                new Keyframe(0.16666667f, 16f),
                new Keyframe(0.33333334f, 0f),
                new Keyframe(0.6666667f, -10f),
                new Keyframe(0.8333333f, -13.75f),
                new Keyframe(1f, -9.900001f),
                new Keyframe(1.1666666f, 0f),
                new Keyframe(1.3333334f, 16f),
                new Keyframe(1.6666666f, 22f));
            AnimationClip southClip = new AnimationClip();
            AnimationClip northClip = new AnimationClip();

            try
            {
                AnimationUtility.SetEditorCurve(
                    northClip,
                    foregroundBinding,
                    foregroundCurve);
                AnimationUtility.SetEditorCurve(
                    northClip,
                    backgroundBinding,
                    backgroundCurve);

                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.True);

                AnimationCurve shiftedForeground =
                    AnimationUtility.GetEditorCurve(
                        northClip,
                        foregroundBinding);
                AnimationCurve shiftedBackground =
                    AnimationUtility.GetEditorCurve(
                        northClip,
                        backgroundBinding);

                AssertCurveOffsetBy(
                    foregroundCurve,
                    shiftedForeground,
                    8f);
                AssertCurveOffsetBy(
                    backgroundCurve,
                    shiftedBackground,
                    8f);
                Assert.That(
                    shiftedForeground.keys.Max(key => key.value)
                    - shiftedForeground.keys.Min(key => key.value),
                    Is.EqualTo(47f).Within(0.001f),
                    "The foreground swing width must remain unchanged.");
                Assert.That(
                    shiftedBackground.keys.Max(key => key.value)
                    - shiftedBackground.keys.Min(key => key.value),
                    Is.EqualTo(35.75f).Within(0.001f),
                    "The background swing width must remain unchanged.");
                Assert.That(
                    NpcDirectionalAnimationAssetBuilder
                        .EnsureNorthWalkGaitCurves(
                            southClip,
                            northClip),
                    Is.False,
                    "The forward-bias adjustment must be idempotent.");
            }
            finally
            {
                Object.DestroyImmediate(northClip);
                Object.DestroyImmediate(southClip);
            }
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


        private static AnimationCurve GetLocalZCurve(
            AnimationClip clip,
            string boneName)
        {
            EditorCurveBinding binding =
                AnimationUtility.GetCurveBindings(clip).Single(
                    candidate =>
                        candidate.path.EndsWith("/" + boneName)
                        && candidate.propertyName
                            == "localEulerAnglesRaw.z");

            return AnimationUtility.GetEditorCurve(clip, binding);
        }


        private static void AssertDirectionalBlendTree(
            AnimatorState state,
            string expectedName,
            string southClipPath,
            string northClipPath)
        {
            Assert.That(
                state.motion,
                Is.TypeOf<BlendTree>());

            BlendTree blendTree = (BlendTree)state.motion;

            Assert.That(
                blendTree.name,
                Is.EqualTo(expectedName));

            Assert.That(
                blendTree.blendParameter,
                Is.EqualTo("FacingNorth"));

            Assert.That(
                blendTree.children.Length,
                Is.EqualTo(2));

            Assert.That(
                AssetDatabase.GetAssetPath(
                    blendTree.children[0].motion),
                Is.EqualTo(southClipPath));

            Assert.That(
                blendTree.children[0].threshold,
                Is.EqualTo(0f));

            Assert.That(
                AssetDatabase.GetAssetPath(
                    blendTree.children[1].motion),
                Is.EqualTo(northClipPath));

            Assert.That(
                blendTree.children[1].threshold,
                Is.EqualTo(1f));
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


        private static void AssertCurveOffsetBy(
            AnimationCurve original,
            AnimationCurve shifted,
            float expectedOffset)
        {
            Assert.That(shifted, Is.Not.Null);
            Assert.That(
                shifted.length,
                Is.EqualTo(original.length));

            Keyframe[] originalKeys = original.keys;
            Keyframe[] shiftedKeys = shifted.keys;

            for (int index = 0; index < originalKeys.Length; index++)
            {
                Assert.That(
                    shiftedKeys[index].time,
                    Is.EqualTo(originalKeys[index].time).Within(0.0001f));
                Assert.That(
                    shiftedKeys[index].value
                    - originalKeys[index].value,
                    Is.EqualTo(expectedOffset).Within(0.001f));
                Assert.That(
                    shiftedKeys[index].inTangent,
                    Is.EqualTo(originalKeys[index].inTangent)
                        .Within(0.001f));
                Assert.That(
                    shiftedKeys[index].outTangent,
                    Is.EqualTo(originalKeys[index].outTangent)
                        .Within(0.001f));
            }
        }


        private static bool AnimationCurvesMatch(
            AnimationCurve first,
            AnimationCurve second)
        {
            if (first == null
                || second == null
                || first.length != second.length)
            {
                return false;
            }

            Keyframe[] firstKeys = first.keys;
            Keyframe[] secondKeys = second.keys;

            for (int index = 0; index < firstKeys.Length; index++)
            {
                if (!Mathf.Approximately(
                        firstKeys[index].time,
                        secondKeys[index].time)
                    || !Mathf.Approximately(
                        firstKeys[index].value,
                        secondKeys[index].value))
                {
                    return false;
                }
            }

            return true;
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
