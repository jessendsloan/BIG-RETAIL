using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Creates the two independently authored shelf-grab starter clips. The
    /// clips are registered as transition-free controller states so Unity's
    /// Animation window can select them. Gameplay still owns if and when an
    /// interaction transitions into either state.
    /// </summary>
    public static class NpcShelfGrabAnimationAssetBuilder
    {
        public const string SouthFacingClipPath =
            "Assets/Animations/Characters/Core/"
            + "Person_ShelfGrab_SouthFacing.anim";

        public const string NorthFacingClipPath =
            "Assets/Animations/Characters/Core/"
            + "Person_ShelfGrab_NorthFacing.anim";

        public const float FrameRate = 6f;

        public const int SampleFrameCount = 11;

        public const float DurationSeconds =
            (SampleFrameCount - 1) / FrameRate;

        private const string ControllerPath =
            "Assets/Animations/Characters/Core/Person.controller";

        private const string SouthFacingClipName =
            "Person_ShelfGrab_SouthFacing";

        private const string NorthFacingClipName =
            "Person_ShelfGrab_NorthFacing";

        private const string PelvisPath =
            "Directional Visual/Root/Pelvis";

        private const string ChestPath =
            PelvisPath + "/SpineLower/Chest";

        private const string HeadPath =
            ChestPath + "/Neck/Head";

        private const string ForegroundShoulderPath =
            ChestPath + "/ShoulderForeground";

        private const string ForegroundUpperArmPath =
            ForegroundShoulderPath + "/UpperArmForeground";

        private const string ForegroundForearmPath =
            ForegroundUpperArmPath + "/ForearmForeground";

        private const string ForegroundHandPath =
            ForegroundForearmPath + "/HandForeground";

        private const string BackgroundShoulderPath =
            ChestPath + "/ShoulderBackground";

        private const string BackgroundUpperArmPath =
            BackgroundShoulderPath + "/UpperArmBackground";

        private const string BackgroundForearmPath =
            BackgroundUpperArmPath + "/ForearmBackground";

        private const string BackgroundHandPath =
            BackgroundForearmPath + "/HandBackground";

        private const string BootstrapSessionKey =
            "BigRetail.ShelfGrabAnimationAssets.Bootstrapped.V4";

        private static readonly float[] SouthFirstPassReach =
        {
            0f, 0f, -10f, 18f, 48f, 72f,
            72f, 52f, 28f, 8f, 0f
        };

        private static readonly float[] NorthFirstPassReach =
        {
            -16f, -16f, -28f, 10f, 82f, 112f,
            112f, 96f, 55f, 10f, -16f
        };

        private static readonly float[] SouthBentReach =
        {
            0f, 0f, -10f, 12f, 42f, 65f,
            68f, 58f, 34f, 10f, 0f
        };

        private static readonly float[] NorthBentReach =
        {
            -16f, -16f, -28f, 8f, 76f, 100f,
            102f, 90f, 50f, 8f, -16f
        };


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

            if (HasCompleteShelfGrabLibrary())
            {
                return;
            }

            try
            {
                EnsureShelfGrabAssets();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }


        [MenuItem("Big Retail/Animation/Ensure Shelf Grab Animation Assets")]
        public static void EnsureShelfGrabAssets()
        {
            bool createdSouth = EnsureShelfGrabClip(
                SouthFacingClipPath,
                SouthFacingClipName,
                ConfigureSouthFacingMotion);
            bool createdNorth = EnsureShelfGrabClip(
                NorthFacingClipPath,
                NorthFacingClipName,
                ConfigureNorthFacingMotion);
            AnimationClip southClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingClipPath);
            AnimationClip northClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingClipPath);
            bool refinedSouth = RefineFirstPassMotion(
                southClip,
                ForegroundUpperArmPath,
                SouthFirstPassReach,
                ConfigureSouthFacingMotion);
            bool refinedNorth = RefineFirstPassMotion(
                northClip,
                ForegroundUpperArmPath,
                NorthFirstPassReach,
                ConfigureNorthFacingMotion);
            bool correctedSouth = RefineFirstPassMotion(
                southClip,
                BackgroundUpperArmPath,
                SouthBentReach,
                ConfigureSouthFacingMotion);
            bool correctedNorth = RefineFirstPassMotion(
                northClip,
                ForegroundUpperArmPath,
                NorthBentReach,
                ConfigureNorthFacingMotion);
            bool registeredStates = EnsureControllerStates(
                southClip,
                northClip);

            if (!createdSouth
                && !createdNorth
                && !refinedSouth
                && !refinedNorth
                && !correctedSouth
                && !correctedNorth
                && !registeredStates)
            {
                Debug.Log(
                    "The independent north-facing and south-facing shelf "
                    + "grab clips are already available in the Person "
                    + "Animation window; authored animation data was "
                    + "preserved.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Prepared the independent north-facing and south-facing "
                + "shelf grab clips and registered them as transition-free "
                + "Person controller states. Gameplay transitions were "
                + "left unchanged.");
        }


        public static void EnsureShelfGrabAssetsBatch()
        {
            EnsureShelfGrabAssets();
        }


        private static bool HasBothShelfGrabAssets()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(
                       SouthFacingClipPath) != null
                   && AssetDatabase.LoadAssetAtPath<AnimationClip>(
                       NorthFacingClipPath) != null;
        }


        private static bool HasCompleteShelfGrabLibrary()
        {
            if (!HasBothShelfGrabAssets())
            {
                return false;
            }

            AnimationClip southClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingClipPath);
            AnimationClip northClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingClipPath);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            return controller != null
                   && HasControllerState(
                       controller,
                       SouthFacingClipName,
                       southClip)
                   && HasControllerState(
                       controller,
                       NorthFacingClipName,
                       northClip)
                   && !CurveMatchesValues(
                       southClip,
                       ForegroundUpperArmPath,
                       SouthFirstPassReach)
                   && !CurveMatchesValues(
                       northClip,
                       ForegroundUpperArmPath,
                       NorthFirstPassReach)
                   && !CurveMatchesValues(
                       southClip,
                       BackgroundUpperArmPath,
                       SouthBentReach)
                   && !CurveMatchesValues(
                       northClip,
                       ForegroundUpperArmPath,
                       NorthBentReach);
        }


        private static bool RefineFirstPassMotion(
            AnimationClip clip,
            string signaturePath,
            float[] firstPassValues,
            Action<AnimationClip> configureMotion)
        {
            if (!CurveMatchesValues(
                    clip,
                    signaturePath,
                    firstPassValues))
            {
                return false;
            }

            configureMotion(clip);
            EditorUtility.SetDirty(clip);
            return true;
        }


        private static bool CurveMatchesValues(
            AnimationClip clip,
            string path,
            float[] values)
        {
            if (clip == null || values == null)
            {
                return false;
            }

            EditorCurveBinding binding =
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "localEulerAnglesRaw.z");
            AnimationCurve curve =
                AnimationUtility.GetEditorCurve(
                    clip,
                    binding);

            if (curve == null || curve.length != values.Length)
            {
                return false;
            }

            Keyframe[] keys = curve.keys;

            for (int index = 0; index < keys.Length; index++)
            {
                if (!Mathf.Approximately(keys[index].value, values[index]))
                {
                    return false;
                }
            }

            return true;
        }


        private static bool EnsureControllerStates(
            AnimationClip southClip,
            AnimationClip northClip)
        {
            if (southClip == null || northClip == null)
            {
                throw new InvalidOperationException(
                    "Both shelf grab clips must exist before they can be "
                    + "registered with the Person controller.");
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            if (controller == null || controller.layers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Person controller was not found at {ControllerPath}.");
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            bool changed = EnsureControllerState(
                stateMachine,
                SouthFacingClipName,
                southClip,
                new Vector3(500f, 190f, 0f));
            changed |= EnsureControllerState(
                stateMachine,
                NorthFacingClipName,
                northClip,
                new Vector3(500f, 250f, 0f));

            if (changed)
            {
                EditorUtility.SetDirty(stateMachine);
                EditorUtility.SetDirty(controller);
            }

            return changed;
        }


        private static bool EnsureControllerState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            Vector3 position)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int index = 0; index < states.Length; index++)
            {
                AnimatorState state = states[index].state;

                if (state.name != stateName)
                {
                    continue;
                }

                if (state.motion == clip)
                {
                    return false;
                }

                state.motion = clip;
                EditorUtility.SetDirty(state);
                return true;
            }

            AnimatorState newState = stateMachine.AddState(
                stateName,
                position);
            newState.motion = clip;
            EditorUtility.SetDirty(newState);
            return true;
        }


        private static bool HasControllerState(
            AnimatorController controller,
            string stateName,
            AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            ChildAnimatorState[] states =
                controller.layers[0].stateMachine.states;

            for (int index = 0; index < states.Length; index++)
            {
                AnimatorState state = states[index].state;

                if (state.name == stateName && state.motion == clip)
                {
                    return true;
                }
            }

            return false;
        }


        private static bool EnsureShelfGrabClip(
            string assetPath,
            string clipName,
            Action<AnimationClip> configureMotion)
        {
            AnimationClip existingClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            if (existingClip != null)
            {
                return false;
            }

            AnimationClip clip = new AnimationClip
            {
                name = clipName,
                frameRate = FrameRate
            };

            configureMotion(clip);

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, assetPath);
            EditorUtility.SetDirty(clip);
            return true;
        }


        private static void ConfigureSouthFacingMotion(
            AnimationClip clip)
        {
            SetLocalXCurve(
                clip,
                PelvisPath,
                0f, 0f, -0.004f, 0.008f, 0.022f, 0.036f,
                0.04f, 0.032f, 0.018f, 0.006f, 0f);
            SetLocalYCurve(
                clip,
                PelvisPath,
                0.808f, 0.808f, 0.805f, 0.799f, 0.794f, 0.792f,
                0.792f, 0.796f, 0.802f, 0.806f, 0.808f);
            SetEulerZCurve(
                clip,
                ChestPath,
                -0.8f, -0.4f, 1.2f, -1.2f, -3.4f, -4.8f,
                -4.8f, -3f, -1.4f, -0.8f, -0.8f);
            SetEulerZCurve(
                clip,
                HeadPath,
                0.5f, 0.5f, -0.5f, 1f, 2f, 2.8f,
                2.8f, 2f, 1f, 0.5f, 0.5f);

            SetEulerZCurve(
                clip,
                ForegroundShoulderPath,
                0f, 0f, 2f, 1f, -2f, -3f,
                -3f, -2f, -1f, 0f, 0f);
            SetEulerZCurve(
                clip,
                ForegroundUpperArmPath,
                0f, 0f, 6f, 2f, -4f, -8f,
                -8f, -5f, -2f, 0f, 0f);
            SetEulerZCurve(
                clip,
                ForegroundForearmPath,
                0f, 0f, -4f, 0f, 6f, 10f,
                10f, 7f, 3f, 0f, 0f);
            SetEulerZCurve(
                clip,
                ForegroundHandPath,
                0f, 0f, 0f, 0f, -2f, -3f,
                -3f, -2f, 0f, 0f, 0f);

            // From the south-facing view, the background arm occupies the
            // shelf side of the silhouette. Reaching with it avoids dragging
            // the opposite arm across the entire torso.
            SetEulerZCurve(
                clip,
                BackgroundShoulderPath,
                0f, 0f, -3f, 0f, 2f, 4f,
                4f, 3f, 2f, 1f, 0f);
            SetEulerZCurve(
                clip,
                BackgroundUpperArmPath,
                0f, 0f, -10f, 12f, 40f, 58f,
                62f, 54f, 32f, 10f, 0f);
            SetEulerZCurve(
                clip,
                BackgroundForearmPath,
                0f, 0f, 14f, 12f, 14f, 14f,
                14f, 14f, 10f, 4f, 0f);
            SetEulerZCurve(
                clip,
                BackgroundHandPath,
                0f, 0f, 0f, -2f, -4f, -7f,
                -12f, -7f, -3f, -1f, 0f);
        }


        private static void ConfigureNorthFacingMotion(
            AnimationClip clip)
        {
            SetLocalXCurve(
                clip,
                PelvisPath,
                0f, 0f, -0.004f, 0.008f, 0.022f, 0.036f,
                0.04f, 0.032f, 0.018f, 0.006f, 0f);
            SetLocalYCurve(
                clip,
                PelvisPath,
                0.808f, 0.808f, 0.806f, 0.802f, 0.798f, 0.797f,
                0.797f, 0.799f, 0.803f, 0.806f, 0.808f);
            SetEulerZCurve(
                clip,
                ChestPath,
                -0.8f, -0.4f, 1f, -1f, -3f, -4.5f,
                -4.8f, -3.5f, -1.5f, -0.8f, -0.8f);
            SetEulerZCurve(
                clip,
                HeadPath,
                0.5f, 0.5f, -0.4f, 0.8f, 1.5f, 2.2f,
                2.2f, 1.6f, 0.9f, 0.5f, 0.5f);

            // The rear-view reach travels higher in screen space so the
            // foreground forearm reads as depth toward a north-side shelf.
            SetEulerZCurve(
                clip,
                ForegroundShoulderPath,
                0f, 0f, -4f, -6f, -6f, -6f,
                -6f, -5f, -3f, -2f, 0f);
            SetEulerZCurve(
                clip,
                ForegroundUpperArmPath,
                -16f, -16f, -28f, 8f, 76f, 82f,
                84f, 78f, 44f, 8f, -16f);
            SetEulerZCurve(
                clip,
                ForegroundForearmPath,
                14f, 14f, 24f, 10f, 3f, 0f,
                0f, 2f, 6f, 12f, 14f);
            SetEulerZCurve(
                clip,
                ForegroundHandPath,
                0f, 0f, -2f, 0f, 5f, 10f,
                14f, 9f, 4f, 1f, 0f);

            SetEulerZCurve(
                clip,
                BackgroundShoulderPath,
                0f, 0f, 2f, 3f, 4f, 4f,
                4f, 3f, 2f, 1f, 0f);
            SetEulerZCurve(
                clip,
                BackgroundUpperArmPath,
                36f, 36f, 40f, 36f, 30f, 26f,
                26f, 28f, 32f, 35f, 36f);
            SetEulerZCurve(
                clip,
                BackgroundForearmPath,
                2f, 2f, -2f, 2f, 8f, 12f,
                12f, 10f, 7f, 4f, 2f);
            SetEulerZCurve(
                clip,
                BackgroundHandPath,
                0f, 0f, 0f, -1f, -2f, -3f,
                -3f, -2f, -1f, 0f, 0f);
        }


        private static void SetLocalYCurve(
            AnimationClip clip,
            string path,
            params float[] values)
        {
            SetFloatCurve(
                clip,
                path,
                "m_LocalPosition.y",
                values);
        }


        private static void SetLocalXCurve(
            AnimationClip clip,
            string path,
            params float[] values)
        {
            SetFloatCurve(
                clip,
                path,
                "m_LocalPosition.x",
                values);
        }


        private static void SetEulerZCurve(
            AnimationClip clip,
            string path,
            params float[] values)
        {
            SetFloatCurve(
                clip,
                path,
                "localEulerAnglesRaw.z",
                values);
        }


        private static void SetFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            float[] values)
        {
            if (values == null
                || values.Length != SampleFrameCount)
            {
                throw new ArgumentException(
                    $"Shelf grab curves require exactly "
                    + $"{SampleFrameCount} authored values.",
                    nameof(values));
            }

            Keyframe[] keys = new Keyframe[values.Length];

            for (int index = 0; index < values.Length; index++)
            {
                keys[index] = new Keyframe(
                    index / FrameRate,
                    values[index],
                    0f,
                    0f);
            }

            AnimationCurve curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            EditorCurveBinding binding =
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    propertyName);

            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                curve);
        }
    }
}
