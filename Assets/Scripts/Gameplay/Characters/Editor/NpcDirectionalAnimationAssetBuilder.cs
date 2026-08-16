using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Maintains the independently authored north/south idle and walk views,
    /// plus the controller blends that select between them. East/west
    /// presentation is handled by mirroring the complete visual rig, never
    /// by rewriting animation curves.
    /// </summary>
    public static class NpcDirectionalAnimationAssetBuilder
    {
        public const string FacingNorthParameterName = "FacingNorth";

        public const string SouthFacingIdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle_SouthFacing.anim";

        public const string NorthFacingIdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle_NorthFacing.anim";

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

        private const string SouthFacingIdleClipName =
            "Person_Idle_SouthFacing";

        private const string NorthFacingIdleClipName =
            "Person_Idle_NorthFacing";

        private const string FacingIdleBlendTreeName =
            "Idle Facing Direction";

        private const string FacingWalkBlendTreeName =
            "Walk Facing Direction";

        private const string LegacyMotionMirrorParameterName =
            "MotionMirror";

        private const string LegacyBlendTreeName =
            "Walk Directional Motion";

        private const float NorthBackgroundRearwardArmScale = 0.55f;

        private const float NorthUpperArmForwardBias = 8f;

        private const string BootstrapSessionKey =
            "BigRetail.AuthoredWalkAssets.Bootstrapped.V8";

        private static readonly float[] UnbiasedNorthUpperArmTimes =
        {
            0f,
            0.16666667f,
            0.33333334f,
            0.6666667f,
            0.8333333f,
            1f,
            1.1666666f,
            1.3333334f,
            1.6666666f
        };

        private static readonly float[] UnbiasedNorthForegroundUpperArmValues =
        {
            -42f,
            -32f,
            -20f,
            -5f,
            5f,
            -5f,
            -20f,
            -32f,
            -42f
        };

        private static readonly float[] UnbiasedNorthBackgroundUpperArmValues =
        {
            22f,
            16f,
            0f,
            -10f,
            -13.75f,
            -9.900001f,
            0f,
            16f,
            22f
        };

        private static readonly float[] AuthoredBackgroundShoulderTimes =
        {
            0f,
            0.16666667f,
            0.33333334f,
            0.5f,
            0.6666667f,
            0.8333333f,
            1f,
            1.1666666f,
            1.3333334f,
            1.6666666f
        };

        private static readonly float[] AuthoredBackgroundShoulderValues =
        {
            42f,
            11f,
            25f,
            28f,
            20f,
            6f,
            -2f,
            0f,
            2f,
            42f
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
            "Big Retail/Animation/Ensure Directional Animation Assets")]
        public static void EnsureAuthoredWalkAssets()
        {
            AnimationClip southFacingIdle =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingIdleClipPath);

            if (southFacingIdle == null)
            {
                throw new InvalidOperationException(
                    "South-facing idle clip was not found at "
                    + SouthFacingIdleClipPath
                    + ".");
            }

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
                southFacingIdle,
                SouthFacingIdleClipName);
            EnsureClipName(
                southFacingWalk,
                SouthFacingWalkClipName);

            AnimationClip northFacingIdle =
                EnsureIndependentNorthFacingClip(
                    southFacingIdle,
                    NorthFacingIdleClipPath,
                    NorthFacingIdleClipName);
            AnimationClip northFacingWalk =
                EnsureIndependentNorthFacingClip(
                    southFacingWalk,
                    NorthFacingWalkClipPath,
                    NorthFacingWalkClipName);

            EnsureNorthWalkGaitCurves(
                southFacingWalk,
                northFacingWalk);

            ConfigureFacingAnimationController(
                southFacingIdle,
                northFacingIdle,
                southFacingWalk,
                northFacingWalk);

            AssetDatabase.SaveAssets();

            Debug.Log(
                "Verified the independent south-facing and north-facing idle "
                + "and walk assets, plus the Person controller view "
                + "selection.");
        }


        public static void EnsureAuthoredWalkAssetsBatch()
        {
            EnsureAuthoredWalkAssets();
        }


        [MenuItem("Big Retail/Animation/Seed North Walk Gait")]
        public static void SeedNorthWalkGait()
        {
            AnimationClip southFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);
            AnimationClip northFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);

            if (southFacingWalk == null || northFacingWalk == null)
            {
                throw new InvalidOperationException(
                    "Both directional walk clips must exist before the "
                    + "north-facing gait can be seeded.");
            }

            Undo.RecordObject(
                northFacingWalk,
                "Repair North Walk Gait");

            bool changed = EnsureNorthWalkGaitCurves(
                southFacingWalk,
                northFacingWalk);

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "Seeded the north-facing arm swing with the shoulder "
                    + "chains on their correct rear-view sides and corrected "
                    + "south-oriented foot curves without replacing existing "
                    + "north-facing body or leg animation.");
            }
            else
            {
                Debug.Log(
                    "The north-facing walk already has its arm swing and "
                    + "north-oriented foot curves.");
            }
        }


        private static bool HasCompleteAuthoredWalkAssets()
        {
            AnimationClip southFacingIdle =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingIdleClipPath);

            AnimationClip northFacingIdle =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingIdleClipPath);

            AnimationClip southFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    SouthFacingWalkClipPath);

            AnimationClip northFacingWalk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NorthFacingWalkClipPath);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);

            if (southFacingIdle == null
                || northFacingIdle == null
                || southFacingWalk == null
                || northFacingWalk == null
                || controller == null
                || southFacingIdle.name != SouthFacingIdleClipName
                || northFacingIdle.name != NorthFacingIdleClipName
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

            return HasCompleteNorthWalkGait(
                       southFacingWalk,
                       northFacingWalk)
                   && !HasState(controller, NorthFacingIdleClipName)
                   && !HasState(controller, NorthFacingWalkClipName)
                   && HasDirectionalBlend(
                       controller,
                       "Idle",
                       FacingIdleBlendTreeName,
                       southFacingIdle,
                       northFacingIdle)
                   && HasDirectionalBlend(
                       controller,
                       "Walk",
                       FacingWalkBlendTreeName,
                       southFacingWalk,
                       northFacingWalk);
        }


        public static bool EnsureNorthWalkGaitCurves(
            AnimationClip southFacingWalk,
            AnimationClip northFacingWalk)
        {
            if (southFacingWalk == null)
            {
                throw new ArgumentNullException(nameof(southFacingWalk));
            }

            if (northFacingWalk == null)
            {
                throw new ArgumentNullException(nameof(northFacingWalk));
            }

            bool changed = false;
            EditorCurveBinding[] southBindings =
                AnimationUtility.GetCurveBindings(southFacingWalk);

            for (int index = 0; index < southBindings.Length; index++)
            {
                EditorCurveBinding sourceBinding = southBindings[index];

                if (!IsArmBinding(sourceBinding))
                {
                    continue;
                }

                AnimationCurve sourceCurve =
                    AnimationUtility.GetEditorCurve(
                        southFacingWalk,
                        sourceBinding);

                if (sourceCurve == null)
                {
                    continue;
                }

                EditorCurveBinding targetBinding =
                    SwapArmBindingSide(sourceBinding);
                AnimationCurve existingCurve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalk,
                        targetBinding);

                if (CurvesMatch(existingCurve, sourceCurve))
                {
                    continue;
                }

                AnimationCurve mistakenlyCopiedSameSideCurve =
                    AnimationUtility.GetEditorCurve(
                        southFacingWalk,
                        targetBinding);

                if (existingCurve != null
                    && !CurvesMatch(
                        existingCurve,
                        mistakenlyCopiedSameSideCurve))
                {
                    // A curve that is neither missing nor our previous
                    // same-side copy is considered authored north data.
                    continue;
                }

                AnimationUtility.SetEditorCurve(
                    northFacingWalk,
                    targetBinding,
                    CloneCurve(sourceCurve));
                changed = true;
            }

            changed |= TightenGeneratedNorthBackgroundUpperArm(
                southFacingWalk,
                northFacingWalk);
            changed |= SmoothAuthoredNorthBackgroundShoulder(
                northFacingWalk);
            changed |= ShiftUnbiasedNorthUpperArmsForward(
                northFacingWalk);

            EditorCurveBinding[] northBindings =
                AnimationUtility.GetCurveBindings(northFacingWalk);

            for (int index = 0; index < northBindings.Length; index++)
            {
                EditorCurveBinding binding = northBindings[index];

                if (!IsFootEulerZBinding(binding))
                {
                    continue;
                }

                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalk,
                        binding);

                if (!HasSouthFacingHeading(curve))
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(
                    northFacingWalk,
                    binding,
                    ReverseSignedAngles(curve));
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(northFacingWalk);
            }

            return changed;
        }


        private static bool HasCompleteNorthWalkGait(
            AnimationClip southFacingWalk,
            AnimationClip northFacingWalk)
        {
            EditorCurveBinding[] southBindings =
                AnimationUtility.GetCurveBindings(southFacingWalk);
            EditorCurveBinding[] northBindings =
                AnimationUtility.GetCurveBindings(northFacingWalk);

            for (int index = 0; index < southBindings.Length; index++)
            {
                EditorCurveBinding sourceBinding = southBindings[index];

                if (!IsArmBinding(sourceBinding))
                {
                    continue;
                }

                EditorCurveBinding targetBinding =
                    SwapArmBindingSide(sourceBinding);
                AnimationCurve targetCurve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalk,
                        targetBinding);

                if (targetCurve == null)
                {
                    return false;
                }

                AnimationCurve correctOppositeSideCurve =
                    AnimationUtility.GetEditorCurve(
                        southFacingWalk,
                        sourceBinding);

                if (CurvesMatch(
                        targetCurve,
                        correctOppositeSideCurve))
                {
                    continue;
                }

                AnimationCurve mistakenlyCopiedSameSideCurve =
                    AnimationUtility.GetEditorCurve(
                        southFacingWalk,
                        targetBinding);

                if (CurvesMatch(
                        targetCurve,
                        mistakenlyCopiedSameSideCurve))
                {
                    return false;
                }
            }

            if (NeedsNorthBackgroundShoulderSmoothing(
                    northFacingWalk)
                || NeedsNorthBackgroundUpperArmTuning(
                    southFacingWalk,
                    northFacingWalk)
                || NeedsNorthUpperArmForwardBias(northFacingWalk))
            {
                return false;
            }

            bool hasForegroundFoot = false;
            bool hasBackgroundFoot = false;

            for (int index = 0; index < northBindings.Length; index++)
            {
                EditorCurveBinding binding = northBindings[index];

                if (!IsFootEulerZBinding(binding))
                {
                    continue;
                }

                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalk,
                        binding);

                if (!HasNorthFacingHeading(curve))
                {
                    return false;
                }

                hasForegroundFoot |=
                    binding.path.EndsWith(
                        "/FootForeground",
                        StringComparison.Ordinal);
                hasBackgroundFoot |=
                    binding.path.EndsWith(
                        "/FootBackground",
                        StringComparison.Ordinal);
            }

            return hasForegroundFoot && hasBackgroundFoot;
        }


        private static bool IsArmBinding(EditorCurveBinding binding)
        {
            return binding.path.Contains("/ShoulderForeground")
                   || binding.path.Contains("/ShoulderBackground");
        }


        private static bool TightenGeneratedNorthBackgroundUpperArm(
            AnimationClip southFacingWalk,
            AnimationClip northFacingWalk)
        {
            if (!TryGetGeneratedNorthBackgroundUpperArm(
                    southFacingWalk,
                    northFacingWalk,
                    out EditorCurveBinding targetBinding,
                    out AnimationCurve existingCurve))
            {
                return false;
            }

            AnimationUtility.SetEditorCurve(
                northFacingWalk,
                targetBinding,
                ScaleRearwardAngles(
                    existingCurve,
                    NorthBackgroundRearwardArmScale));
            return true;
        }


        private static bool SmoothAuthoredNorthBackgroundShoulder(
            AnimationClip northFacingWalk)
        {
            if (!TryGetAuthoredNorthBackgroundShoulder(
                    northFacingWalk,
                    out EditorCurveBinding binding,
                    out AnimationCurve curve))
            {
                return false;
            }

            AnimationUtility.SetEditorCurve(
                northFacingWalk,
                binding,
                SmoothPeriodicCurve(curve));
            return true;
        }


        private static bool NeedsNorthBackgroundShoulderSmoothing(
            AnimationClip northFacingWalk)
        {
            return TryGetAuthoredNorthBackgroundShoulder(
                northFacingWalk,
                out _,
                out _);
        }


        private static bool TryGetAuthoredNorthBackgroundShoulder(
            AnimationClip northFacingWalk,
            out EditorCurveBinding binding,
            out AnimationCurve curve)
        {
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(northFacingWalk);

            for (int index = 0; index < bindings.Length; index++)
            {
                EditorCurveBinding candidateBinding = bindings[index];

                if (!IsBackgroundShoulderEulerZBinding(candidateBinding))
                {
                    continue;
                }

                AnimationCurve candidateCurve =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalk,
                        candidateBinding);

                if (MatchesAuthoredBackgroundShoulder(candidateCurve))
                {
                    binding = candidateBinding;
                    curve = candidateCurve;
                    return true;
                }
            }

            binding = default;
            curve = null;
            return false;
        }


        private static bool IsBackgroundShoulderEulerZBinding(
            EditorCurveBinding binding)
        {
            return binding.path.EndsWith(
                       "/ShoulderBackground",
                       StringComparison.Ordinal)
                   && binding.propertyName.EndsWith(
                       ".z",
                       StringComparison.Ordinal)
                   && binding.propertyName.IndexOf(
                       "EulerAngles",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private static bool MatchesAuthoredBackgroundShoulder(
            AnimationCurve curve)
        {
            if (curve == null
                || curve.length != AuthoredBackgroundShoulderTimes.Length)
            {
                return false;
            }

            Keyframe[] keys = curve.keys;

            for (int index = 0; index < keys.Length; index++)
            {
                if (!Mathf.Approximately(
                        keys[index].time,
                        AuthoredBackgroundShoulderTimes[index])
                    || !Mathf.Approximately(
                        Mathf.DeltaAngle(0f, keys[index].value),
                        AuthoredBackgroundShoulderValues[index]))
                {
                    return false;
                }
            }

            return true;
        }


        private static bool NeedsNorthBackgroundUpperArmTuning(
            AnimationClip southFacingWalk,
            AnimationClip northFacingWalk)
        {
            return TryGetGeneratedNorthBackgroundUpperArm(
                southFacingWalk,
                northFacingWalk,
                out _,
                out _);
        }


        private static bool ShiftUnbiasedNorthUpperArmsForward(
            AnimationClip northFacingWalk)
        {
            bool changed = false;
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(northFacingWalk);

            for (int index = 0; index < bindings.Length; index++)
            {
                EditorCurveBinding binding = bindings[index];
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    northFacingWalk,
                    binding);

                if (!MatchesUnbiasedNorthUpperArm(binding, curve))
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(
                    northFacingWalk,
                    binding,
                    OffsetSignedAngles(curve, NorthUpperArmForwardBias));
                changed = true;
            }

            return changed;
        }


        private static bool NeedsNorthUpperArmForwardBias(
            AnimationClip northFacingWalk)
        {
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(northFacingWalk);

            for (int index = 0; index < bindings.Length; index++)
            {
                EditorCurveBinding binding = bindings[index];
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    northFacingWalk,
                    binding);

                if (MatchesUnbiasedNorthUpperArm(binding, curve))
                {
                    return true;
                }
            }

            return false;
        }


        private static bool MatchesUnbiasedNorthUpperArm(
            EditorCurveBinding binding,
            AnimationCurve curve)
        {
            if (!IsUpperArmEulerZBinding(binding))
            {
                return false;
            }

            float[] expectedValues;

            if (binding.path.EndsWith(
                    "/ShoulderForeground/UpperArmForeground",
                    StringComparison.Ordinal))
            {
                expectedValues = UnbiasedNorthForegroundUpperArmValues;
            }
            else if (binding.path.EndsWith(
                         "/ShoulderBackground/UpperArmBackground",
                         StringComparison.Ordinal))
            {
                expectedValues = UnbiasedNorthBackgroundUpperArmValues;
            }
            else
            {
                return false;
            }

            return MatchesCurveValues(
                curve,
                UnbiasedNorthUpperArmTimes,
                expectedValues);
        }


        private static bool IsUpperArmEulerZBinding(
            EditorCurveBinding binding)
        {
            return binding.propertyName.EndsWith(
                       ".z",
                       StringComparison.Ordinal)
                   && binding.propertyName.IndexOf(
                       "EulerAngles",
                       StringComparison.OrdinalIgnoreCase) >= 0
                   && (binding.path.EndsWith(
                           "/ShoulderForeground/UpperArmForeground",
                           StringComparison.Ordinal)
                       || binding.path.EndsWith(
                           "/ShoulderBackground/UpperArmBackground",
                           StringComparison.Ordinal));
        }


        private static bool MatchesCurveValues(
            AnimationCurve curve,
            float[] expectedTimes,
            float[] expectedValues)
        {
            if (curve == null
                || expectedTimes == null
                || expectedValues == null
                || curve.length != expectedTimes.Length
                || curve.length != expectedValues.Length)
            {
                return false;
            }

            Keyframe[] keys = curve.keys;

            for (int index = 0; index < keys.Length; index++)
            {
                if (!Mathf.Approximately(
                        keys[index].time,
                        expectedTimes[index])
                    || !Mathf.Approximately(
                        Mathf.DeltaAngle(0f, keys[index].value),
                        expectedValues[index])
                    || !Mathf.Approximately(keys[index].inTangent, 0f)
                    || !Mathf.Approximately(keys[index].outTangent, 0f))
                {
                    return false;
                }
            }

            return true;
        }


        private static bool TryGetGeneratedNorthBackgroundUpperArm(
            AnimationClip southFacingWalk,
            AnimationClip northFacingWalk,
            out EditorCurveBinding targetBinding,
            out AnimationCurve existingCurve)
        {
            EditorCurveBinding[] southBindings =
                AnimationUtility.GetCurveBindings(southFacingWalk);

            for (int index = 0; index < southBindings.Length; index++)
            {
                EditorCurveBinding sourceBinding = southBindings[index];

                if (!IsForegroundUpperArmEulerZBinding(sourceBinding))
                {
                    continue;
                }

                AnimationCurve sourceCurve =
                    AnimationUtility.GetEditorCurve(
                        southFacingWalk,
                        sourceBinding);
                EditorCurveBinding candidateTarget =
                    SwapArmBindingSide(sourceBinding);
                AnimationCurve candidateExisting =
                    AnimationUtility.GetEditorCurve(
                        northFacingWalk,
                        candidateTarget);

                if (CurvesMatch(candidateExisting, sourceCurve))
                {
                    targetBinding = candidateTarget;
                    existingCurve = candidateExisting;
                    return true;
                }
            }

            targetBinding = default;
            existingCurve = null;
            return false;
        }


        private static bool IsForegroundUpperArmEulerZBinding(
            EditorCurveBinding binding)
        {
            return binding.path.EndsWith(
                       "/ShoulderForeground/UpperArmForeground",
                       StringComparison.Ordinal)
                   && binding.propertyName.EndsWith(
                       ".z",
                       StringComparison.Ordinal)
                   && binding.propertyName.IndexOf(
                       "EulerAngles",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private static EditorCurveBinding SwapArmBindingSide(
            EditorCurveBinding source)
        {
            EditorCurveBinding target = source;

            if (source.path.Contains("Foreground"))
            {
                target.path = source.path.Replace(
                    "Foreground",
                    "Background");
            }
            else if (source.path.Contains("Background"))
            {
                target.path = source.path.Replace(
                    "Background",
                    "Foreground");
            }

            return target;
        }


        private static bool IsFootEulerZBinding(
            EditorCurveBinding binding)
        {
            bool isFoot =
                binding.path.EndsWith(
                    "/FootForeground",
                    StringComparison.Ordinal)
                || binding.path.EndsWith(
                    "/FootBackground",
                    StringComparison.Ordinal);

            return isFoot
                   && binding.propertyName.EndsWith(
                       ".z",
                       StringComparison.Ordinal)
                   && binding.propertyName.IndexOf(
                       "EulerAngles",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private static bool CurvesMatch(
            AnimationCurve first,
            AnimationCurve second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (first == null
                || second == null
                || first.length != second.length
                || first.preWrapMode != second.preWrapMode
                || first.postWrapMode != second.postWrapMode)
            {
                return false;
            }

            Keyframe[] firstKeys = first.keys;
            Keyframe[] secondKeys = second.keys;

            for (int index = 0; index < firstKeys.Length; index++)
            {
                Keyframe firstKey = firstKeys[index];
                Keyframe secondKey = secondKeys[index];

                if (!Mathf.Approximately(firstKey.time, secondKey.time)
                    || !Mathf.Approximately(
                        firstKey.value,
                        secondKey.value)
                    || !Mathf.Approximately(
                        firstKey.inTangent,
                        secondKey.inTangent)
                    || !Mathf.Approximately(
                        firstKey.outTangent,
                        secondKey.outTangent)
                    || !Mathf.Approximately(
                        firstKey.inWeight,
                        secondKey.inWeight)
                    || !Mathf.Approximately(
                        firstKey.outWeight,
                        secondKey.outWeight)
                    || firstKey.weightedMode != secondKey.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }


        private static AnimationCurve ScaleRearwardAngles(
            AnimationCurve source,
            float scale)
        {
            AnimationCurve adjusted = CloneCurve(source);
            Keyframe[] keys = adjusted.keys;
            float[] signedAngles = new float[keys.Length];

            for (int index = 0; index < keys.Length; index++)
            {
                signedAngles[index] =
                    Mathf.DeltaAngle(0f, keys[index].value);
            }

            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                bool keyIsRearward = signedAngles[index] < 0f;
                bool previousKeyIsRearward =
                    index > 0 && signedAngles[index - 1] < 0f;
                bool nextKeyIsRearward =
                    index + 1 < keys.Length
                    && signedAngles[index + 1] < 0f;

                key.value = keyIsRearward
                    ? signedAngles[index] * scale
                    : signedAngles[index];

                if (keyIsRearward || previousKeyIsRearward)
                {
                    key.inTangent *= scale;
                }

                if (keyIsRearward || nextKeyIsRearward)
                {
                    key.outTangent *= scale;
                }

                keys[index] = key;
            }

            adjusted.keys = keys;
            return adjusted;
        }


        private static AnimationCurve OffsetSignedAngles(
            AnimationCurve source,
            float offset)
        {
            AnimationCurve adjusted = CloneCurve(source);
            Keyframe[] keys = adjusted.keys;

            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                key.value = Mathf.DeltaAngle(0f, key.value) + offset;
                keys[index] = key;
            }

            adjusted.keys = keys;
            return adjusted;
        }


        private static AnimationCurve SmoothPeriodicCurve(
            AnimationCurve source)
        {
            AnimationCurve smoothedCurve = CloneCurve(source);
            Keyframe[] keys = smoothedCurve.keys;
            int uniqueKeyCount = keys.Length - 1;
            float[] smoothedValues = new float[uniqueKeyCount];

            for (int index = 0; index < uniqueKeyCount; index++)
            {
                int previousIndex =
                    (index - 1 + uniqueKeyCount) % uniqueKeyCount;
                int nextIndex = (index + 1) % uniqueKeyCount;
                float previousValue =
                    Mathf.DeltaAngle(0f, keys[previousIndex].value);
                float currentValue =
                    Mathf.DeltaAngle(0f, keys[index].value);
                float nextValue =
                    Mathf.DeltaAngle(0f, keys[nextIndex].value);

                smoothedValues[index] =
                    (previousValue + (2f * currentValue) + nextValue)
                    / 4f;
            }

            float cycleDuration =
                keys[keys.Length - 1].time - keys[0].time;

            for (int index = 0; index < uniqueKeyCount; index++)
            {
                int previousIndex =
                    (index - 1 + uniqueKeyCount) % uniqueKeyCount;
                int nextIndex = (index + 1) % uniqueKeyCount;
                float previousTime = keys[previousIndex].time;
                float nextTime = keys[nextIndex].time;

                if (previousIndex > index)
                {
                    previousTime -= cycleDuration;
                }

                if (nextIndex < index)
                {
                    nextTime += cycleDuration;
                }

                float tangent =
                    (smoothedValues[nextIndex]
                     - smoothedValues[previousIndex])
                    / (nextTime - previousTime);
                Keyframe key = keys[index];
                key.value = smoothedValues[index];
                key.inTangent = tangent;
                key.outTangent = tangent;
                keys[index] = key;
            }

            Keyframe loopKey = keys[keys.Length - 1];
            loopKey.value = smoothedValues[0];
            loopKey.inTangent = keys[0].inTangent;
            loopKey.outTangent = keys[0].outTangent;
            keys[keys.Length - 1] = loopKey;

            smoothedCurve.keys = keys;
            return smoothedCurve;
        }


        private static bool HasSouthFacingHeading(AnimationCurve curve)
        {
            return GetAverageSignedAngle(curve) < -0.01f;
        }


        private static bool HasNorthFacingHeading(AnimationCurve curve)
        {
            return GetAverageSignedAngle(curve) > 0.01f;
        }


        private static float GetAverageSignedAngle(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            Keyframe[] keys = curve.keys;
            float signedAngleTotal = 0f;

            for (int index = 0; index < keys.Length; index++)
            {
                signedAngleTotal +=
                    Mathf.DeltaAngle(0f, keys[index].value);
            }

            return signedAngleTotal / keys.Length;
        }


        private static AnimationCurve ReverseSignedAngles(
            AnimationCurve source)
        {
            AnimationCurve reversed = CloneCurve(source);
            Keyframe[] keys = reversed.keys;

            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                key.value = -Mathf.DeltaAngle(0f, key.value);
                key.inTangent = -key.inTangent;
                key.outTangent = -key.outTangent;
                keys[index] = key;
            }

            reversed.keys = keys;
            return reversed;
        }


        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }


        private static bool HasDirectionalBlend(
            AnimatorController controller,
            string stateName,
            string blendTreeName,
            AnimationClip southFacingClip,
            AnimationClip northFacingClip)
        {
            AnimatorState state;

            try
            {
                state = FindState(controller, stateName);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            if (!(state.motion is BlendTree blendTree)
                || blendTree.name != blendTreeName
                || blendTree.blendParameter != FacingNorthParameterName)
            {
                return false;
            }

            ChildMotion[] children = blendTree.children;

            return children.Length == 2
                   && children[0].motion == southFacingClip
                   && Mathf.Approximately(children[0].threshold, 0f)
                   && children[1].motion == northFacingClip
                   && Mathf.Approximately(children[1].threshold, 1f);
        }


        private static AnimationClip EnsureIndependentNorthFacingClip(
            AnimationClip southFacingClip,
            string northFacingClipPath,
            string northFacingClipName)
        {
            AnimationClip northFacingClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    northFacingClipPath);

            if (northFacingClip == null)
            {
                northFacingClip = new AnimationClip();
                AssetDatabase.CreateAsset(
                    northFacingClip,
                    northFacingClipPath);

                EditorUtility.CopySerialized(
                    southFacingClip,
                    northFacingClip);
            }

            EnsureClipName(
                northFacingClip,
                northFacingClipName);

            // Existing north-facing animation data is deliberately preserved.
            // This clip is an authored view, not a generated mirror.
            return northFacingClip;
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


        private static void ConfigureFacingAnimationController(
            AnimationClip southFacingIdle,
            AnimationClip northFacingIdle,
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

            ConfigureFacingState(
                controller,
                "Idle",
                FacingIdleBlendTreeName,
                null,
                southFacingIdle,
                northFacingIdle);
            ConfigureFacingState(
                controller,
                "Walk",
                FacingWalkBlendTreeName,
                LegacyBlendTreeName,
                southFacingWalk,
                northFacingWalk);

            RemoveUnconnectedClipState(
                controller,
                NorthFacingIdleClipName);
            RemoveUnconnectedClipState(
                controller,
                NorthFacingWalkClipName);

            EditorUtility.SetDirty(controller);
        }


        private static void ConfigureFacingState(
            AnimatorController controller,
            string stateName,
            string blendTreeName,
            string legacyBlendTreeName,
            AnimationClip southFacingClip,
            AnimationClip northFacingClip)
        {
            AnimatorState state = FindState(controller, stateName);
            BlendTree blendTree = FindOrCreateBlendTree(
                controller,
                state,
                blendTreeName,
                legacyBlendTreeName);

            blendTree.name = blendTreeName;
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = FacingNorthParameterName;
            blendTree.useAutomaticThresholds = false;
            blendTree.minThreshold = 0f;
            blendTree.maxThreshold = 1f;
            blendTree.children = new[]
            {
                new ChildMotion
                {
                    motion = southFacingClip,
                    threshold = 0f,
                    timeScale = 1f
                },
                new ChildMotion
                {
                    motion = northFacingClip,
                    threshold = 1f,
                    timeScale = 1f
                }
            };

            state.motion = blendTree;
            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(state);
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


        private static bool HasState(
            AnimatorController controller,
            string stateName)
        {
            ChildAnimatorState[] states =
                controller.layers[0].stateMachine.states;

            for (int index = 0; index < states.Length; index++)
            {
                if (states[index].state.name == stateName)
                {
                    return true;
                }
            }

            return false;
        }


        private static void RemoveUnconnectedClipState(
            AnimatorController controller,
            string stateName)
        {
            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            ChildAnimatorState[] states = stateMachine.states;

            for (int index = states.Length - 1; index >= 0; index--)
            {
                if (states[index].state.name == stateName)
                {
                    stateMachine.RemoveState(states[index].state);
                }
            }
        }


        private static BlendTree FindOrCreateBlendTree(
            AnimatorController controller,
            AnimatorState state,
            string blendTreeName,
            string legacyBlendTreeName)
        {
            if (state.motion is BlendTree stateBlendTree)
            {
                return stateBlendTree;
            }

            UnityEngine.Object[] controllerAssets =
                AssetDatabase.LoadAllAssetsAtPath(ControllerPath);

            for (int index = 0; index < controllerAssets.Length; index++)
            {
                if (controllerAssets[index] is BlendTree existing
                    && (existing.name == blendTreeName
                        || (!string.IsNullOrEmpty(legacyBlendTreeName)
                            && existing.name == legacyBlendTreeName)))
                {
                    return existing;
                }
            }

            BlendTree created = new BlendTree
            {
                name = blendTreeName
            };

            AssetDatabase.AddObjectToAsset(created, controller);
            return created;
        }
    }
}
