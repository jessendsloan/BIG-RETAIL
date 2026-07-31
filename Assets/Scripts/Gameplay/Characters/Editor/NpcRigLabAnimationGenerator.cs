using System;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Owns the first generated animation library for the rounded NPC
    /// prototype. Re-running it updates the existing assets in place.
    /// </summary>
    internal static class NpcRigLabAnimationGenerator
    {
        public const string AnimationFolder =
            "Assets/Animations/Characters/Prototype";

        public const string IdleClipPath =
            AnimationFolder + "/Rowan_Idle.anim";

        public const string WalkClipPath =
            AnimationFolder + "/Rowan_Walk.anim";

        public const string ControllerPath =
            AnimationFolder + "/Rowan.controller";


        public static RuntimeAnimatorController
            CreateOrUpdateRowanController()
        {
            EnsureAssetFolder(
                AnimationFolder);

            AnimationClip idleClip =
                SaveOrUpdateClip(
                    CreateIdleClip(),
                    IdleClipPath);

            AnimationClip walkClip =
                SaveOrUpdateClip(
                    CreateWalkClip(),
                    WalkClipPath);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            if (controller == null)
            {
                controller =
                    AnimatorController
                        .CreateAnimatorControllerAtPath(
                            ControllerPath);
            }

            ConfigureController(
                controller,
                idleClip,
                walkClip);

            EditorUtility.SetDirty(
                controller);

            return controller;
        }


        private static AnimationClip CreateIdleClip()
        {
            AnimationClip clip =
                CreateLoopingClip(
                    "Rowan Idle");

            SetPositionYCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Pelvis),
                new Keyframe(0f, 0.90f),
                new Keyframe(0.80f, 0.915f),
                new Keyframe(1.60f, 0.90f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Chest),
                new Keyframe(0f, -0.8f),
                new Keyframe(0.80f, 0.8f),
                new Keyframe(1.60f, -0.8f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Head),
                new Keyframe(0f, 0.5f),
                new Keyframe(0.80f, -0.5f),
                new Keyframe(1.60f, 0.5f));

            return clip;
        }


        private static AnimationClip CreateWalkClip()
        {
            AnimationClip clip =
                CreateLoopingClip(
                    "Rowan Walk");

            SetPositionYCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Pelvis),
                new Keyframe(0f, 0.90f),
                new Keyframe(0.20f, 0.925f),
                new Keyframe(0.40f, 0.90f),
                new Keyframe(0.60f, 0.925f),
                new Keyframe(0.80f, 0.90f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ThighFar),
                AlternatingKeys(
                    -17f,
                    17f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ThighNear),
                AlternatingKeys(
                    17f,
                    -17f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ShinFar),
                AlternatingKeys(
                    10f,
                    -8f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ShinNear),
                AlternatingKeys(
                    -8f,
                    10f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.UpperArmFar),
                AlternatingKeys(
                    14f,
                    -14f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.UpperArmNear),
                AlternatingKeys(
                    -14f,
                    14f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ForearmFar),
                AlternatingKeys(
                    5f,
                    -5f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ForearmNear),
                AlternatingKeys(
                    -5f,
                    5f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Chest),
                AlternatingKeys(
                    -2f,
                    2f));

            return clip;
        }


        private static void ConfigureController(
            AnimatorController controller,
            Motion idleMotion,
            Motion walkMotion)
        {
            controller.parameters =
                new[]
                {
                    new AnimatorControllerParameter
                    {
                        name = "Speed",
                        type =
                            AnimatorControllerParameterType.Float,
                        defaultFloat = 0f
                    }
                };

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;

            ChildAnimatorState[] existingStates =
                stateMachine.states;

            for (int index = 0;
                 index < existingStates.Length;
                 index++)
            {
                stateMachine.RemoveState(
                    existingStates[index].state);
            }

            AnimatorState idleState =
                stateMachine.AddState(
                    "Idle");

            idleState.motion = idleMotion;

            AnimatorState walkState =
                stateMachine.AddState(
                    "Walk");

            walkState.motion = walkMotion;

            stateMachine.defaultState = idleState;

            ConfigureSpeedTransition(
                idleState.AddTransition(
                    walkState),
                AnimatorConditionMode.Greater);

            ConfigureSpeedTransition(
                walkState.AddTransition(
                    idleState),
                AnimatorConditionMode.Less);
        }


        private static void ConfigureSpeedTransition(
            AnimatorStateTransition transition,
            AnimatorConditionMode conditionMode)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;

            transition.AddCondition(
                conditionMode,
                0.05f,
                "Speed");
        }


        private static AnimationClip CreateLoopingClip(
            string clipName)
        {
            AnimationClip clip =
                new AnimationClip
                {
                    name = clipName,
                    frameRate = 12f
                };

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(
                    clip);

            settings.loopTime = true;

            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);

            return clip;
        }


        private static AnimationClip SaveOrUpdateClip(
            AnimationClip generatedClip,
            string assetPath)
        {
            AnimationClip existingClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    assetPath);

            if (existingClip == null)
            {
                AssetDatabase.CreateAsset(
                    generatedClip,
                    assetPath);

                return generatedClip;
            }

            EditorUtility.CopySerialized(
                generatedClip,
                existingClip);

            UnityEngine.Object.DestroyImmediate(
                generatedClip);

            EditorUtility.SetDirty(
                existingClip);

            return existingClip;
        }


        private static void SetPositionYCurve(
            AnimationClip clip,
            string relativePath,
            params Keyframe[] keys)
        {
            clip.SetCurve(
                relativePath,
                typeof(Transform),
                "localPosition.y",
                new AnimationCurve(
                    keys));
        }


        private static void SetRotationCurve(
            AnimationClip clip,
            string relativePath,
            params Keyframe[] keys)
        {
            clip.SetCurve(
                relativePath,
                typeof(Transform),
                "localEulerAnglesRaw.z",
                new AnimationCurve(
                    keys));
        }


        private static Keyframe[] AlternatingKeys(
            float firstValue,
            float secondValue)
        {
            return new[]
            {
                new Keyframe(0f, firstValue),
                new Keyframe(0.20f, 0f),
                new Keyframe(0.40f, secondValue),
                new Keyframe(0.60f, 0f),
                new Keyframe(0.80f, firstValue)
            };
        }


        private static string BonePath(
            NpcRigBoneId boneId)
        {
            const string root =
                "Directional Visual/Root";

            switch (boneId)
            {
                case NpcRigBoneId.Pelvis:
                    return root + "/Pelvis";

                case NpcRigBoneId.Chest:
                    return root
                        + "/Pelvis/SpineLower/Chest";

                case NpcRigBoneId.Head:
                    return root
                        + "/Pelvis/SpineLower/Chest/Neck/Head";

                case NpcRigBoneId.UpperArmFar:
                    return root
                        + "/Pelvis/SpineLower/Chest"
                        + "/ShoulderFar/UpperArmFar";

                case NpcRigBoneId.ForearmFar:
                    return BonePath(
                            NpcRigBoneId.UpperArmFar)
                        + "/ForearmFar";

                case NpcRigBoneId.UpperArmNear:
                    return root
                        + "/Pelvis/SpineLower/Chest"
                        + "/ShoulderNear/UpperArmNear";

                case NpcRigBoneId.ForearmNear:
                    return BonePath(
                            NpcRigBoneId.UpperArmNear)
                        + "/ForearmNear";

                case NpcRigBoneId.ThighFar:
                    return root
                        + "/Pelvis/ThighFar";

                case NpcRigBoneId.ShinFar:
                    return BonePath(
                            NpcRigBoneId.ThighFar)
                        + "/ShinFar";

                case NpcRigBoneId.ThighNear:
                    return root
                        + "/Pelvis/ThighNear";

                case NpcRigBoneId.ShinNear:
                    return BonePath(
                            NpcRigBoneId.ThighNear)
                        + "/ShinNear";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(boneId),
                        boneId,
                        "No animation path is defined for this bone.");
            }
        }


        private static void EnsureAssetFolder(
            string folderPath)
        {
            string[] pathParts =
                folderPath.Split('/');

            string currentPath =
                pathParts[0];

            for (int index = 1;
                 index < pathParts.Length;
                 index++)
            {
                string nextPath =
                    $"{currentPath}/{pathParts[index]}";

                if (!AssetDatabase.IsValidFolder(
                        nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        pathParts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
