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
        private const string PolishedWalkMenuPath =
            "Big Retail/Characters/Animation/Apply Polished Rowan Walk";

        private const float RowanPelvisX = -0.065f;
        private const float RowanPelvisY = 0.808f;

        public const string AnimationFolder =
            "Assets/Animations/Characters/Prototype";

        public const string IdleClipPath =
            AnimationFolder + "/Rowan_Idle.anim";

        public const string WalkClipPath =
            AnimationFolder + "/Rowan_Walk.anim";

        public const string HandTunedWalkClipPath =
            AnimationFolder + "/Rowan_Walk_HandTuned.anim";

        public const string ControllerPath =
            AnimationFolder + "/Rowan.controller";


        public static RuntimeAnimatorController
            CreateOrUpdateRowanController()
        {
            EnsureAssetFolder(
                AnimationFolder);

            PreserveHandTunedWalk();

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


        [MenuItem(PolishedWalkMenuPath)]
        public static void ApplyPolishedRowanWalk()
        {
            EnsureAssetFolder(
                AnimationFolder);

            PreserveHandTunedWalk();

            AnimationClip walkClip =
                SaveOrUpdateClip(
                    CreateWalkClip(),
                    WalkClipPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = walkClip;
            EditorGUIUtility.PingObject(
                walkClip);

            Debug.Log(
                "Applied Rowan's polished walk cycle and preserved "
                + "the hand-tuned baseline.");
        }


        private static AnimationClip CreateIdleClip()
        {
            AnimationClip clip =
                CreateLoopingClip(
                    "Rowan_Idle");

            SetPositionYCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Pelvis),
                new Keyframe(0f, RowanPelvisY),
                new Keyframe(0.80f, RowanPelvisY + 0.010f),
                new Keyframe(1.60f, RowanPelvisY));

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
                    "Rowan_Walk");

            // Eight distinct poses create contact, compression, passing,
            // lift, and the mirrored second step. The ninth key closes the
            // loop exactly at Rowan's 12 fps prototype cadence.
            SetPositionXCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Pelvis),
                SmoothKeys(
                    RowanPelvisX + 0.007f,
                    RowanPelvisX + 0.010f,
                    RowanPelvisX,
                    RowanPelvisX - 0.010f,
                    RowanPelvisX - 0.007f,
                    RowanPelvisX - 0.010f,
                    RowanPelvisX,
                    RowanPelvisX + 0.010f,
                    RowanPelvisX + 0.007f));

            SetPositionYCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Pelvis),
                SmoothKeys(
                    RowanPelvisY,
                    RowanPelvisY - 0.018f,
                    RowanPelvisY + 0.008f,
                    RowanPelvisY + 0.020f,
                    RowanPelvisY,
                    RowanPelvisY - 0.018f,
                    RowanPelvisY + 0.008f,
                    RowanPelvisY + 0.020f,
                    RowanPelvisY));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ThighSourceCameraLeft),
                SmoothKeys(
                    -20f,
                    -16f,
                    0f,
                    14f,
                    20f,
                    16f,
                    0f,
                    -14f,
                    -20f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ThighSourceCameraRight),
                SmoothKeys(
                    20f,
                    16f,
                    0f,
                    -14f,
                    -20f,
                    -16f,
                    0f,
                    14f,
                    20f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ShinSourceCameraLeft),
                SmoothKeys(
                    -2f,
                    -5f,
                    -3f,
                    -7f,
                    -12f,
                    -22f,
                    -32f,
                    -15f,
                    -2f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ShinSourceCameraRight),
                SmoothKeys(
                    12f,
                    22f,
                    32f,
                    15f,
                    2f,
                    5f,
                    3f,
                    7f,
                    12f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.UpperArmSourceCameraLeft),
                SmoothKeys(
                    13f,
                    10f,
                    0f,
                    -10f,
                    -13f,
                    -10f,
                    0f,
                    10f,
                    13f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.UpperArmSourceCameraRight),
                SmoothKeys(
                    -13f,
                    -10f,
                    0f,
                    10f,
                    13f,
                    10f,
                    0f,
                    -10f,
                    -13f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ForearmSourceCameraLeft),
                SmoothKeys(
                    4f,
                    6f,
                    8f,
                    10f,
                    7f,
                    6f,
                    5f,
                    4f,
                    4f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.ForearmSourceCameraRight),
                SmoothKeys(
                    -7f,
                    -6f,
                    -5f,
                    -4f,
                    -4f,
                    -6f,
                    -8f,
                    -10f,
                    -7f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.SpineLower),
                SmoothKeys(
                    1f,
                    0.5f,
                    0f,
                    -0.5f,
                    -1f,
                    -0.5f,
                    0f,
                    0.5f,
                    1f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Chest),
                SmoothKeys(
                    -2.4f,
                    -1.6f,
                    0f,
                    1.6f,
                    2.4f,
                    1.6f,
                    0f,
                    -1.6f,
                    -2.4f));

            SetRotationCurve(
                clip,
                BonePath(
                    NpcRigBoneId.Head),
                SmoothKeys(
                    0.8f,
                    0.4f,
                    0f,
                    -0.4f,
                    -0.8f,
                    -0.4f,
                    0f,
                    0.4f,
                    0.8f));

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
                CreateSmoothCurve(
                    keys));
        }


        private static void SetPositionXCurve(
            AnimationClip clip,
            string relativePath,
            params Keyframe[] keys)
        {
            clip.SetCurve(
                relativePath,
                typeof(Transform),
                "localPosition.x",
                CreateSmoothCurve(
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
                CreateSmoothCurve(
                    keys));
        }


        private static Keyframe[] SmoothKeys(
            params float[] values)
        {
            Keyframe[] keys =
                new Keyframe[values.Length];

            for (int index = 0;
                 index < values.Length;
                 index++)
            {
                keys[index] =
                    new Keyframe(
                        index * 0.10f,
                        values[index]);
            }

            return keys;
        }


        private static AnimationCurve CreateSmoothCurve(
            params Keyframe[] keys)
        {
            AnimationCurve curve =
                new AnimationCurve(
                    keys);

            for (int index = 0;
                 index < curve.length;
                 index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.ClampedAuto);

                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
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

                case NpcRigBoneId.SpineLower:
                    return root
                        + "/Pelvis/SpineLower";

                case NpcRigBoneId.Chest:
                    return root
                        + "/Pelvis/SpineLower/Chest";

                case NpcRigBoneId.Head:
                    return root
                        + "/Pelvis/SpineLower/Chest/Neck/Head";

                case NpcRigBoneId.UpperArmSourceCameraLeft:
                    return root
                        + "/Pelvis/SpineLower/Chest"
                        + "/ShoulderSourceCameraLeft/UpperArmSourceCameraLeft";

                case NpcRigBoneId.ForearmSourceCameraLeft:
                    return BonePath(
                            NpcRigBoneId.UpperArmSourceCameraLeft)
                        + "/ForearmSourceCameraLeft";

                case NpcRigBoneId.UpperArmSourceCameraRight:
                    return root
                        + "/Pelvis/SpineLower/Chest"
                        + "/ShoulderSourceCameraRight/UpperArmSourceCameraRight";

                case NpcRigBoneId.ForearmSourceCameraRight:
                    return BonePath(
                            NpcRigBoneId.UpperArmSourceCameraRight)
                        + "/ForearmSourceCameraRight";

                case NpcRigBoneId.ThighSourceCameraLeft:
                    return root
                        + "/Pelvis/ThighSourceCameraLeft";

                case NpcRigBoneId.ShinSourceCameraLeft:
                    return BonePath(
                            NpcRigBoneId.ThighSourceCameraLeft)
                        + "/ShinSourceCameraLeft";

                case NpcRigBoneId.ThighSourceCameraRight:
                    return root
                        + "/Pelvis/ThighSourceCameraRight";

                case NpcRigBoneId.ShinSourceCameraRight:
                    return BonePath(
                            NpcRigBoneId.ThighSourceCameraRight)
                        + "/ShinSourceCameraRight";

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


        private static void PreserveHandTunedWalk()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HandTunedWalkClipPath) != null)
            {
                return;
            }

            AnimationClip currentWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    WalkClipPath);

            if (currentWalk == null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(
                    WalkClipPath,
                    HandTunedWalkClipPath))
            {
                throw new InvalidOperationException(
                    "Could not preserve Rowan's hand-tuned walk clip.");
            }

            AnimationClip preservedWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    HandTunedWalkClipPath);

            preservedWalk.name =
                "Rowan_Walk_HandTuned";

            EditorUtility.SetDirty(
                preservedWalk);
        }
    }
}
