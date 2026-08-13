using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Maintains the two authored walk views and the controller blend that
    /// selects between them. East/west presentation is handled by mirroring
    /// the complete visual rig, never by rewriting animation curves.
    /// </summary>
    public static class NpcDirectionalAnimationAssetBuilder
    {
        public const string FacingNorthParameterName = "FacingNorth";

        public const string SouthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_SouthFacing.anim";

        public const string NorthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_NorthFacing.anim";

        public const string ControllerPath =
            "Assets/Animations/Characters/Core/Person.controller";

        private const string SouthFacingWalkClipName =
            "Person_Walk_SouthFacing";

        private const string NorthFacingWalkClipName =
            "Person_Walk_NorthFacing";

        private const string FacingWalkBlendTreeName =
            "Walk Facing Direction";

        private const string LegacyMotionMirrorParameterName =
            "MotionMirror";

        private const string LegacyBlendTreeName =
            "Walk Directional Motion";

        private const string BootstrapSessionKey =
            "BigRetail.AuthoredWalkAssets.Bootstrapped.V2";


        [InitializeOnLoadMethod]
        private static void ScheduleMissingAssetBootstrap()
        {
            if (SessionState.GetBool(BootstrapSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(BootstrapSessionKey, true);
            EditorApplication.delayCall += BootstrapMissingAssets;
        }


        private static void BootstrapMissingAssets()
        {
            if (EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += BootstrapMissingAssets;
                return;
            }

            if (HasCompleteAuthoredWalkAssets())
            {
                return;
            }

            try
            {
                EnsureAuthoredWalkAssets();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }


        [MenuItem(
            "Big Retail/Population/Ensure Authored Walk Assets")]
        public static void EnsureAuthoredWalkAssets()
        {
            AnimationClip southFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);

            if (southFacingWalk == null)
            {
                throw new InvalidOperationException(
                    "South-facing walk clip was not found at "
                    + SouthFacingWalkClipPath
                    + ".");
            }

            EnsureClipName(
                southFacingWalk,
                SouthFacingWalkClipName);

            AnimationClip northFacingWalk =
                EnsureIndependentNorthFacingWalk(southFacingWalk);

            ConfigureFacingWalkController(
                southFacingWalk,
                northFacingWalk);

            AssetDatabase.SaveAssets();

            Debug.Log(
                "Verified the independent south-facing and north-facing "
                + "walk assets and the Person controller view selection.");
        }


        public static void EnsureAuthoredWalkAssetsBatch()
        {
            EnsureAuthoredWalkAssets();
        }


        private static bool HasCompleteAuthoredWalkAssets()
        {
            AnimationClip southFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);

            AnimationClip northFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            if (southFacingWalk == null
                || northFacingWalk == null
                || controller == null
                || southFacingWalk.name != SouthFacingWalkClipName
                || northFacingWalk.name != NorthFacingWalkClipName)
            {
                return false;
            }

            bool hasFacingNorthParameter = false;
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name == FacingNorthParameterName
                    && parameters[index].type
                    == AnimatorControllerParameterType.Float)
                {
                    hasFacingNorthParameter = true;
                    break;
                }
            }

            if (!hasFacingNorthParameter)
            {
                return false;
            }

            AnimatorState walkState;

            try
            {
                walkState = FindState(controller, "Walk");
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            if (!(walkState.motion is BlendTree blendTree)
                || blendTree.blendParameter != FacingNorthParameterName)
            {
                return false;
            }

            ChildMotion[] children = blendTree.children;

            return children.Length == 2
                   && children[0].motion == southFacingWalk
                   && Mathf.Approximately(children[0].threshold, 0f)
                   && children[1].motion == northFacingWalk
                   && Mathf.Approximately(children[1].threshold, 1f);
        }


        private static AnimationClip EnsureIndependentNorthFacingWalk(
            AnimationClip southFacingWalk)
        {
            AnimationClip northFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);

            if (northFacingWalk == null)
            {
                northFacingWalk = new AnimationClip();
                AssetDatabase.CreateAsset(
                    northFacingWalk,
                    NorthFacingWalkClipPath);

                EditorUtility.CopySerialized(
                    southFacingWalk,
                    northFacingWalk);
            }

            EnsureClipName(
                northFacingWalk,
                NorthFacingWalkClipName);

            // Existing north-facing animation data is deliberately preserved.
            // This clip is an authored view, not a generated mirror.
            return northFacingWalk;
        }


        private static void EnsureClipName(
            AnimationClip clip,
            string expectedName)
        {
            if (clip.name == expectedName)
            {
                return;
            }

            clip.name = expectedName;
            EditorUtility.SetDirty(clip);
        }


        private static void ConfigureFacingWalkController(
            AnimationClip southFacingWalk,
            AnimationClip northFacingWalk)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"Person controller was not found at {ControllerPath}.");
            }

            RemoveParameterIfPresent(
                controller,
                LegacyMotionMirrorParameterName);

            EnsureFloatParameter(
                controller,
                FacingNorthParameterName);

            AnimatorState walkState = FindState(controller, "Walk");
            BlendTree blendTree = FindOrCreateBlendTree(
                controller,
                walkState);

            blendTree.name = FacingWalkBlendTreeName;
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = FacingNorthParameterName;
            blendTree.useAutomaticThresholds = false;
            blendTree.minThreshold = 0f;
            blendTree.maxThreshold = 1f;
            blendTree.children = new[]
            {
                new ChildMotion
                {
                    motion = southFacingWalk,
                    threshold = 0f,
                    timeScale = 1f
                },
                new ChildMotion
                {
                    motion = northFacingWalk,
                    threshold = 1f,
                    timeScale = 1f
                }
            };

            walkState.motion = blendTree;
            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(walkState);
            EditorUtility.SetDirty(controller);
        }


        private static void EnsureFloatParameter(
            AnimatorController controller,
            string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name != parameterName)
                {
                    continue;
                }

                if (parameters[index].type
                    != AnimatorControllerParameterType.Float)
                {
                    throw new InvalidOperationException(
                        $"Animator parameter {parameterName} must be a float.");
                }

                return;
            }

            controller.AddParameter(
                parameterName,
                AnimatorControllerParameterType.Float);
        }


        private static void RemoveParameterIfPresent(
            AnimatorController controller,
            string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int index = parameters.Length - 1; index >= 0; index--)
            {
                if (parameters[index].name == parameterName)
                {
                    controller.RemoveParameter(index);
                }
            }
        }


        private static AnimatorState FindState(
            AnimatorController controller,
            string stateName)
        {
            ChildAnimatorState[] states =
                controller.layers[0].stateMachine.states;

            for (int index = 0; index < states.Length; index++)
            {
                if (states[index].state.name == stateName)
                {
                    return states[index].state;
                }
            }

            throw new InvalidOperationException(
                $"Animator state {stateName} was not found.");
        }


        private static BlendTree FindOrCreateBlendTree(
            AnimatorController controller,
            AnimatorState walkState)
        {
            if (walkState.motion is BlendTree stateBlendTree)
            {
                return stateBlendTree;
            }

            UnityEngine.Object[] controllerAssets =
                AssetDatabase.LoadAllAssetsAtPath(ControllerPath);

            for (int index = 0; index < controllerAssets.Length; index++)
            {
                if (controllerAssets[index] is BlendTree existing
                    && (existing.name == FacingWalkBlendTreeName
                        || existing.name == LegacyBlendTreeName))
                {
                    return existing;
                }
            }

            BlendTree created = new BlendTree
            {
                name = FacingWalkBlendTreeName
            };

            AssetDatabase.AddObjectToAsset(created, controller);
            return created;
        }
    }
}
