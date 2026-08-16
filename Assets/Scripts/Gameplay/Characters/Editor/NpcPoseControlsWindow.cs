using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// A compact companion to Unity's Animation window. Unity continues to
    /// own clips, frames, recording, playback, curves, and saving; this window
    /// only makes the Person rig's body rotations easier to reach.
    /// </summary>
    public sealed class NpcPoseControlsWindow : EditorWindow
    {
        private const float NormalPlaybackSpeed = 1f;
        private const float MinimumPlaybackSpeed = 0.1f;
        private const float MaximumPlaybackSpeed = 2f;

        private static readonly NpcRigBoneId[] CoreBones =
        {
            NpcRigBoneId.Pelvis,
            NpcRigBoneId.SpineLower,
            NpcRigBoneId.Chest,
            NpcRigBoneId.Neck,
            NpcRigBoneId.Head
        };

        private static readonly NpcRigBoneId[] ForegroundArmBones =
        {
            NpcRigBoneId.ShoulderForeground,
            NpcRigBoneId.UpperArmForeground,
            NpcRigBoneId.ForearmForeground,
            NpcRigBoneId.HandForeground
        };

        private static readonly NpcRigBoneId[] BackgroundArmBones =
        {
            NpcRigBoneId.ShoulderBackground,
            NpcRigBoneId.UpperArmBackground,
            NpcRigBoneId.ForearmBackground,
            NpcRigBoneId.HandBackground
        };

        private static readonly NpcRigBoneId[] ForegroundLegBones =
        {
            NpcRigBoneId.ThighForeground,
            NpcRigBoneId.ShinForeground,
            NpcRigBoneId.FootForeground
        };

        private static readonly NpcRigBoneId[] BackgroundLegBones =
        {
            NpcRigBoneId.ThighBackground,
            NpcRigBoneId.ShinBackground,
            NpcRigBoneId.FootBackground
        };

        private NpcCutoutRig targetRig;
        private Vector2 scrollPosition;
        [SerializeField]
        private float playbackSpeed = NormalPlaybackSpeed;
        [SerializeField]
        private bool editBasePose;
        private bool coreExpanded = true;
        private bool foregroundArmExpanded = true;
        private bool backgroundArmExpanded = true;
        private bool foregroundLegExpanded = true;
        private bool backgroundLegExpanded = true;
        private bool speedControlledPlayback;
        private AnimationWindow speedControlledAnimationWindow;
        private double previousPlaybackEditorTime;
        [SerializeField]
        private string lastAnimationReviewFolder;
        private string animationReviewStatus;
        private MessageType animationReviewStatusType = MessageType.None;


        [MenuItem("Big Retail/Animation/Pose Controls")]
        public static void Open()
        {
            NpcPoseControlsWindow window =
                GetWindow<NpcPoseControlsWindow>();
            window.titleContent = new GUIContent("NPC Pose Controls");
            window.minSize = new Vector2(390f, 480f);
            window.Show();
        }


        private void OnEnable()
        {
            titleContent = new GUIContent("NPC Pose Controls");
            minSize = new Vector2(390f, 480f);
            Undo.undoRedoPerformed += HandleUndoRedo;
            EditorApplication.update += UpdateSpeedControlledPlayback;
            AdoptSelectionWhenItContainsOneRig();
        }


        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.update -= UpdateSpeedControlledPlayback;
            StopSpeedControlledPlayback();
        }


        private void OnSelectionChange()
        {
            AdoptSelectionWhenItContainsOneRig();
            Repaint();
        }


        private void OnHierarchyChange()
        {
            if (targetRig == null)
            {
                AdoptSelectionWhenItContainsOneRig();
            }

            Repaint();
        }


        private void OnInspectorUpdate()
        {
            // Keep the native clip, frame, recording, and playback status
            // current while the user works in the Animation window.
            Repaint();
        }


        private void OnGUI()
        {
            AnimationWindow animationWindow =
                NpcPoseControlsUtility.FindOpenAnimationWindow();

            EditorGUILayout.LabelField(
                "NPC Pose Controls",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select a Person, choose a clip and frame in Unity's "
                + "Animation window, turn on Record, then pose with these "
                + "sliders. Unity still owns the animation clip and keys.",
                MessageType.Info);

            DrawTargetSection();
            EditorGUILayout.Space(8f);
            DrawFacingSection(animationWindow);
            EditorGUILayout.Space(8f);
            DrawAnimationWindowSection(animationWindow);
            EditorGUILayout.Space(8f);
            DrawAnimationReviewSection(animationWindow);
            EditorGUILayout.Space(8f);
            DrawPoseSection(animationWindow);
        }


        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField(
                "Person",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            NpcCutoutRig chosenRig =
                (NpcCutoutRig)EditorGUILayout.ObjectField(
                    "Target Rig",
                    targetRig,
                    typeof(NpcCutoutRig),
                    true);

            if (EditorGUI.EndChangeCheck())
            {
                SetTargetRig(chosenRig);
            }

            bool selectionHasRig =
                NpcPoseControlsUtility.TryResolveRig(
                    Selection.activeGameObject,
                    out NpcCutoutRig selectedRig);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!selectionHasRig))
                {
                    if (GUILayout.Button("Use Selection"))
                    {
                        SetTargetRig(selectedRig);
                    }
                }

                using (new EditorGUI.DisabledScope(targetRig == null))
                {
                    if (GUILayout.Button("Select Person"))
                    {
                        Selection.activeGameObject =
                            targetRig.gameObject;
                        EditorGUIUtility.PingObject(targetRig.gameObject);
                    }
                }
            }
        }


        private void DrawFacingSection(
            AnimationWindow animationWindow)
        {
            EditorGUILayout.LabelField(
                "Facing",
                EditorStyles.boldLabel);

            bool recording =
                animationWindow != null
                && animationWindow.recording;
            bool persistentTarget =
                targetRig != null
                && EditorUtility.IsPersistent(targetRig);
            bool canChangeFacing =
                targetRig != null
                && !recording
                && !persistentTarget
                && !EditorApplication.isPlaying;

            using (new EditorGUI.DisabledScope(!canChangeFacing))
            {
                EditorGUILayout.LabelField(
                    "North",
                    EditorStyles.centeredGreyMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawFacingButton(
                        "NW",
                        NpcFacing.NorthWest,
                        animationWindow);
                    DrawFacingButton(
                        "NE",
                        NpcFacing.NorthEast,
                        animationWindow);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawFacingButton(
                        "SW",
                        NpcFacing.SouthWest,
                        animationWindow);
                    DrawFacingButton(
                        "SE",
                        NpcFacing.SouthEast,
                        animationWindow);
                }

                EditorGUILayout.LabelField(
                    "South",
                    EditorStyles.centeredGreyMiniLabel);
            }

            if (recording)
            {
                EditorGUILayout.HelpBox(
                    "Stop recording before changing facing.",
                    MessageType.Warning);
            }
            else if (persistentTarget)
            {
                EditorGUILayout.HelpBox(
                    "Open the Person prefab in Prefab Mode, or select a "
                    + "Person in a scene, to change facing.",
                    MessageType.Warning);
            }
        }


        private void DrawFacingButton(
            string label,
            NpcFacing buttonFacing,
            AnimationWindow animationWindow)
        {
            Color previousColor = GUI.backgroundColor;

            if (targetRig != null
                && targetRig.Facing == buttonFacing)
            {
                GUI.backgroundColor =
                    new Color(0.45f, 0.72f, 1f);
            }

            if (GUILayout.Button(
                    label,
                    GUILayout.Height(28f)))
            {
                ApplyFacing(buttonFacing, animationWindow);
            }

            GUI.backgroundColor = previousColor;
        }


        private void ApplyFacing(
            NpcFacing facing,
            AnimationWindow animationWindow)
        {
            if (targetRig == null
                || targetRig.Facing == facing)
            {
                return;
            }

            PausePlayback(animationWindow);
            Undo.RegisterFullObjectHierarchyUndo(
                targetRig.gameObject,
                $"Face Person {facing}");
            targetRig.SetFacing(facing);
            EditorUtility.SetDirty(targetRig);

            if (PrefabUtility.IsPartOfPrefabInstance(targetRig))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    targetRig);
            }

            SceneView.RepaintAll();
            Repaint();
        }


        private void DrawAnimationWindowSection(
            AnimationWindow animationWindow)
        {
            EditorGUILayout.LabelField(
                "Unity Animation",
                EditorStyles.boldLabel);

            if (animationWindow == null)
            {
                EditorGUILayout.HelpBox(
                    "Open Unity's Animation window to choose the clip, "
                    + "frame, and recording state.",
                    MessageType.None);

                if (GUILayout.Button("Open Animation Window"))
                {
                    OpenAndFocusAnimationWindow();
                }

                return;
            }

            string clipName = animationWindow.animationClip != null
                ? animationWindow.animationClip.name
                : "No editable clip selected";

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Clip", clipName);
                EditorGUILayout.LabelField(
                    $"Frame {animationWindow.frame}",
                    GUILayout.Width(85f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Focus Animation"))
                {
                    animationWindow.Focus();
                }

                bool canToggleRecording =
                    animationWindow.recording
                    || animationWindow.canRecord;

                using (new EditorGUI.DisabledScope(
                           !canToggleRecording
                           || EditorApplication.isPlaying))
                {
                    Color previousColor = GUI.backgroundColor;
                    if (animationWindow.recording)
                    {
                        GUI.backgroundColor =
                            new Color(1f, 0.48f, 0.48f);
                    }

                    string label = animationWindow.recording
                        ? "Stop Recording"
                        : "Record";

                    if (GUILayout.Button(label))
                    {
                        PausePlayback(animationWindow);
                        editBasePose = false;
                        animationWindow.recording =
                            !animationWindow.recording;
                        animationWindow.Repaint();
                    }

                    GUI.backgroundColor = previousColor;
                }

                using (new EditorGUI.DisabledScope(
                           animationWindow.animationClip == null
                           || EditorApplication.isPlaying))
                {
                    string label = IsPlaybackActive(animationWindow)
                        ? "Pause"
                        : "Play";

                    if (GUILayout.Button(label))
                    {
                        TogglePlayback(animationWindow);
                    }
                }
            }

            DrawPlaybackSpeed(animationWindow);
        }


        private void DrawPlaybackSpeed(
            AnimationWindow animationWindow)
        {
            float chosenSpeed = playbackSpeed;
            bool resetSpeed = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                chosenSpeed = EditorGUILayout.Slider(
                    "Playback Speed",
                    playbackSpeed,
                    MinimumPlaybackSpeed,
                    MaximumPlaybackSpeed);
                bool speedChanged = EditorGUI.EndChangeCheck();

                using (new EditorGUI.DisabledScope(
                           Mathf.Approximately(
                               playbackSpeed,
                               NormalPlaybackSpeed)))
                {
                    resetSpeed = GUILayout.Button(
                        "1x",
                        GUILayout.Width(34f));
                }

                if (resetSpeed)
                {
                    chosenSpeed = NormalPlaybackSpeed;
                }

                if (speedChanged || resetSpeed)
                {
                    SetPlaybackSpeed(animationWindow, chosenSpeed);
                }
            }

            EditorGUILayout.LabelField(
                "Preview only; the clip's timing stays unchanged.",
                EditorStyles.miniLabel);
        }


        private void DrawAnimationReviewSection(
            AnimationWindow animationWindow)
        {
            EditorGUILayout.LabelField(
                "Animation Review",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Capture 11 labeled poses for a quick visual and data review.",
                EditorStyles.wordWrappedMiniLabel);

            AnimationClip clip = animationWindow != null
                ? animationWindow.animationClip
                : null;
            bool recording =
                animationWindow != null
                && animationWindow.recording;
            bool hasValidRig =
                targetRig != null
                && targetRig.TryValidate(out _);
            bool canCapture =
                hasValidRig
                && clip != null
                && !recording
                && !EditorApplication.isPlayingOrWillChangePlaymode;

            if (targetRig != null
                && clip != null
                && !NpcAnimationReviewCapture.IsFacingCompatible(
                    clip,
                    targetRig.Facing))
            {
                EditorGUILayout.HelpBox(
                    $"{clip.name} does not appear to match the selected "
                    + $"{targetRig.Facing} facing. Capture is still allowed.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!canCapture))
            {
                if (GUILayout.Button(
                        "Capture Animation Review",
                        GUILayout.Height(28f)))
                {
                    CaptureAnimationReview(animationWindow, clip);
                }
            }

            if (!canCapture)
            {
                string guidance = GetAnimationReviewGuidance(
                    animationWindow,
                    clip,
                    hasValidRig,
                    recording);

                if (!string.IsNullOrEmpty(guidance))
                {
                    EditorGUILayout.LabelField(
                        guidance,
                        EditorStyles.wordWrappedMiniLabel);
                }
            }

            if (!string.IsNullOrEmpty(animationReviewStatus))
            {
                EditorGUILayout.HelpBox(
                    animationReviewStatus,
                    animationReviewStatusType);
            }

            bool hasPreviousCapture =
                !string.IsNullOrEmpty(lastAnimationReviewFolder)
                && System.IO.Directory.Exists(
                    lastAnimationReviewFolder);

            using (new EditorGUI.DisabledScope(!hasPreviousCapture))
            {
                if (GUILayout.Button("Reveal Last Capture"))
                {
                    EditorUtility.RevealInFinder(
                        lastAnimationReviewFolder);
                }
            }
        }


        private void CaptureAnimationReview(
            AnimationWindow animationWindow,
            AnimationClip clip)
        {
            PausePlayback(animationWindow);

            try
            {
                NpcAnimationReviewCaptureResult result =
                    NpcAnimationReviewCapture.Capture(
                        targetRig,
                        clip);
                lastAnimationReviewFolder = result.ReviewFolder;
                animationReviewStatus =
                    $"Captured {NpcAnimationReviewCapture.SampleCount} "
                    + $"frames for {clip.name} facing {targetRig.Facing}.";
                animationReviewStatusType = MessageType.Info;
            }
            catch (System.Exception exception)
            {
                animationReviewStatus =
                    $"Capture failed: {exception.Message}";
                animationReviewStatusType = MessageType.Error;
                Debug.LogException(exception);
            }

            Repaint();
        }


        private static string GetAnimationReviewGuidance(
            AnimationWindow animationWindow,
            AnimationClip clip,
            bool hasValidRig,
            bool recording)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return "Exit Play Mode before capturing a review.";
            }

            if (!hasValidRig)
            {
                return "Select a complete Person rig to enable capture.";
            }

            if (animationWindow == null || clip == null)
            {
                return "Choose a clip in Unity's Animation window first.";
            }

            if (recording)
            {
                return "Stop recording before capturing a review.";
            }

            return string.Empty;
        }


        private void DrawPoseSection(
            AnimationWindow animationWindow)
        {
            EditorGUILayout.LabelField(
                "Body Rotation",
                EditorStyles.boldLabel);

            if (targetRig == null)
            {
                EditorGUILayout.HelpBox(
                    "Select the Person root or any of its body parts.",
                    MessageType.Warning);
                return;
            }

            if (EditorUtility.IsPersistent(targetRig))
            {
                EditorGUILayout.HelpBox(
                    "Open the Person prefab in Prefab Mode, or select a "
                    + "Person in a scene. Directly editing the prefab asset "
                    + "outside an animation context is intentionally blocked.",
                    MessageType.Warning);
                return;
            }

            if (!targetRig.TryValidate(out string failureReason))
            {
                EditorGUILayout.HelpBox(
                    $"This Person rig is incomplete: {failureReason}",
                    MessageType.Error);
                return;
            }

            bool recording =
                animationWindow != null
                && animationWindow.recording;
            bool playing =
                animationWindow != null
                && IsPlaybackActive(animationWindow);
            bool basePosesReady =
                DrawBasePoseControls(
                    animationWindow,
                    recording);
            bool animationPreviewing =
                animationWindow != null
                && animationWindow.previewing;
            bool editingBasePose =
                !recording
                && editBasePose
                && basePosesReady
                && !animationPreviewing;
            bool canPose =
                (recording || editingBasePose)
                && !playing
                && !EditorApplication.isPlaying;

            if (playing)
            {
                EditorGUILayout.HelpBox(
                    "Pause playback to edit the current frame or base pose.",
                    MessageType.None);
            }
            else if (recording)
            {
                EditorGUILayout.HelpBox(
                    "Animation Key mode: sliders write keys to the selected "
                    + "clip at the current frame.",
                    MessageType.Info);
            }
            else if (editBasePose && animationPreviewing)
            {
                EditorGUILayout.HelpBox(
                    "Animation Preview was turned back on. Exit Preview to "
                    + "continue editing the direction's base pose.",
                    MessageType.Warning);

                if (GUILayout.Button("Exit Preview and Resume Base Pose"))
                {
                    ExitAnimationPreview(animationWindow);
                }
            }
            else if (editingBasePose)
            {
                NpcAuthoredDirection direction =
                    NpcFacingUtility.GetAuthoredDirection(
                        targetRig.Facing);
                string directionLabel =
                    direction == NpcAuthoredDirection.NorthEast
                        ? "North (NE / NW)"
                        : "South (SE / SW)";

                EditorGUILayout.HelpBox(
                    $"Base Pose mode: sliders update only the {directionLabel} "
                    + "resting pose. Animation clips stay unchanged.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Turn on Record to key the animation, or enable Edit "
                    + "Direction Base Pose to adjust the current facing's "
                    + "independent resting pose.",
                    MessageType.Warning);
            }

            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            using (new EditorGUI.DisabledScope(!canPose))
            {
                DrawBoneGroup(
                    "Core",
                    CoreBones,
                    ref coreExpanded,
                    editingBasePose);
                DrawBoneGroup(
                    "Foreground Arm",
                    ForegroundArmBones,
                    ref foregroundArmExpanded,
                    editingBasePose);
                DrawBoneGroup(
                    "Background Arm",
                    BackgroundArmBones,
                    ref backgroundArmExpanded,
                    editingBasePose);
                DrawBoneGroup(
                    "Foreground Leg",
                    ForegroundLegBones,
                    ref foregroundLegExpanded,
                    editingBasePose);
                DrawBoneGroup(
                    "Background Leg",
                    BackgroundLegBones,
                    ref backgroundLegExpanded,
                    editingBasePose);
            }

            EditorGUILayout.EndScrollView();
        }


        private void DrawBoneGroup(
            string label,
            NpcRigBoneId[] boneIds,
            ref bool expanded,
            bool captureBasePose)
        {
            expanded = EditorGUILayout.Foldout(
                expanded,
                label,
                true);

            if (!expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            for (int index = 0; index < boneIds.Length; index++)
            {
                DrawBoneSlider(
                    boneIds[index],
                    captureBasePose);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(3f);
        }


        private void DrawBoneSlider(
            NpcRigBoneId boneId,
            bool captureBasePose)
        {
            if (!targetRig.TryGetBone(
                    boneId,
                    out Transform bone))
            {
                EditorGUILayout.LabelField(
                    NpcPoseControlsUtility.GetBoneLabel(boneId),
                    "Missing bone");
                return;
            }

            float currentAngle =
                NpcPoseControlsUtility.GetLocalZAngle(bone);
            float chosenAngle = currentAngle;
            bool setToZero = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                chosenAngle = EditorGUILayout.Slider(
                    NpcPoseControlsUtility.GetBoneLabel(boneId),
                    currentAngle,
                    -180f,
                    180f);
                bool sliderChanged = EditorGUI.EndChangeCheck();

                using (new EditorGUI.DisabledScope(
                           Mathf.Abs(currentAngle) < 0.001f))
                {
                    setToZero = GUILayout.Button(
                        "0",
                        GUILayout.Width(28f));
                }

                if (setToZero)
                {
                    chosenAngle = 0f;
                }

                if (sliderChanged || setToZero)
                {
                    ApplyBoneRotation(
                        boneId,
                        bone,
                        chosenAngle,
                        captureBasePose);
                }
            }
        }


        private void ApplyBoneRotation(
            NpcRigBoneId boneId,
            Transform bone,
            float angle,
            bool captureBasePose)
        {
            string boneLabel =
                NpcPoseControlsUtility.GetBoneLabel(boneId);

            if (captureBasePose)
            {
                Undo.RecordObjects(
                    new UnityEngine.Object[]
                    {
                        targetRig,
                        bone
                    },
                    $"Set {boneLabel} Base Pose");
            }
            else
            {
                Undo.RecordObject(
                    bone,
                    $"Pose {boneLabel}");
            }

            NpcPoseControlsUtility.SetLocalZAngle(bone, angle);

            if (captureBasePose)
            {
                NpcAuthoredDirection direction =
                    NpcFacingUtility.GetAuthoredDirection(
                        targetRig.Facing);
                targetRig.CaptureAuthoredBoneRotation(
                    direction,
                    boneId);
                MarkTargetRigDirty();
            }

            SceneView.RepaintAll();
        }


        private bool DrawBasePoseControls(
            AnimationWindow animationWindow,
            bool recording)
        {
            bool southPoseReady =
                targetRig.HasCompleteAuthoredBonePose(
                    NpcAuthoredDirection.SouthEast);
            bool northPoseReady =
                targetRig.HasCompleteAuthoredBonePose(
                    NpcAuthoredDirection.NorthEast);
            bool basePosesReady =
                southPoseReady && northPoseReady;

            if (!basePosesReady)
            {
                editBasePose = false;
                EditorGUILayout.HelpBox(
                    "This older Person has partial direction poses. Capture "
                    + "its current South and North setups once before editing "
                    + "their base poses independently.",
                    MessageType.Warning);

                using (new EditorGUI.DisabledScope(
                           recording
                           || EditorApplication.isPlaying))
                {
                    if (GUILayout.Button(
                            "Initialize South + North Base Poses"))
                    {
                        basePosesReady =
                            InitializeBasePoses(animationWindow);
                    }
                }
            }

            if (recording)
            {
                editBasePose = false;
                return basePosesReady;
            }

            using (new EditorGUI.DisabledScope(
                       !basePosesReady
                       || EditorApplication.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                bool chosenEditBasePose =
                    EditorGUILayout.ToggleLeft(
                        "Edit Direction Base Pose",
                        editBasePose);

                if (EditorGUI.EndChangeCheck())
                {
                    editBasePose = chosenEditBasePose;

                    if (editBasePose)
                    {
                        ExitAnimationPreview(animationWindow);
                    }
                }
            }

            return basePosesReady;
        }


        private bool InitializeBasePoses(
            AnimationWindow animationWindow)
        {
            Undo.RegisterFullObjectHierarchyUndo(
                targetRig.gameObject,
                "Initialize Direction Base Poses");
            ExitAnimationPreview(animationWindow);

            bool initialized =
                targetRig.InitializeCompleteAuthoredBonePoses();

            if (initialized)
            {
                editBasePose = true;
                MarkTargetRigDirty();
                SceneView.RepaintAll();
            }

            return initialized;
        }


        private void ExitAnimationPreview(
            AnimationWindow animationWindow)
        {
            PausePlayback(animationWindow);

            if (animationWindow != null
                && animationWindow.previewing
                && !animationWindow.recording)
            {
                animationWindow.previewing = false;
                animationWindow.Repaint();
            }

            if (targetRig != null)
            {
                targetRig.SetFacing(targetRig.Facing);
            }
        }


        private void MarkTargetRigDirty()
        {
            EditorUtility.SetDirty(targetRig);

            if (PrefabUtility.IsPartOfPrefabInstance(targetRig))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    targetRig);
            }
        }


        private void AdoptSelectionWhenItContainsOneRig()
        {
            if (NpcPoseControlsUtility.TryResolveRig(
                    Selection.activeGameObject,
                    out NpcCutoutRig selectedRig))
            {
                SetTargetRig(selectedRig);
            }
        }


        private void SetTargetRig(
            NpcCutoutRig chosenRig)
        {
            if (targetRig == chosenRig)
            {
                return;
            }

            targetRig = chosenRig;
            editBasePose = false;
        }


        private static void OpenAndFocusAnimationWindow()
        {
            AnimationWindow animationWindow =
                NpcPoseControlsUtility.OpenAnimationWindow();
            animationWindow.Focus();
        }


        private void TogglePlayback(
            AnimationWindow animationWindow)
        {
            if (IsPlaybackActive(animationWindow))
            {
                PausePlayback(animationWindow);
                return;
            }

            if (Mathf.Approximately(
                    playbackSpeed,
                    NormalPlaybackSpeed))
            {
                animationWindow.playing = true;
            }
            else
            {
                StartSpeedControlledPlayback(animationWindow);
            }

            animationWindow.Repaint();
        }


        private void PausePlayback(
            AnimationWindow animationWindow)
        {
            if (animationWindow != null)
            {
                animationWindow.playing = false;
                animationWindow.Repaint();
            }

            StopSpeedControlledPlayback();
        }


        private bool IsPlaybackActive(
            AnimationWindow animationWindow)
        {
            return animationWindow != null
                   && (animationWindow.playing
                       || (speedControlledPlayback
                           && speedControlledAnimationWindow
                           == animationWindow));
        }


        private void SetPlaybackSpeed(
            AnimationWindow animationWindow,
            float chosenSpeed)
        {
            playbackSpeed = Mathf.Clamp(
                chosenSpeed,
                MinimumPlaybackSpeed,
                MaximumPlaybackSpeed);

            if (!IsPlaybackActive(animationWindow))
            {
                return;
            }

            if (Mathf.Approximately(
                    playbackSpeed,
                    NormalPlaybackSpeed))
            {
                StopSpeedControlledPlayback();
                animationWindow.playing = true;
            }
            else if (animationWindow.playing)
            {
                animationWindow.playing = false;
                StartSpeedControlledPlayback(animationWindow);
            }
            else
            {
                previousPlaybackEditorTime =
                    EditorApplication.timeSinceStartup;
            }

            animationWindow.Repaint();
        }


        private void StartSpeedControlledPlayback(
            AnimationWindow animationWindow)
        {
            if (animationWindow == null
                || animationWindow.animationClip == null)
            {
                return;
            }

            if (!animationWindow.previewing
                && animationWindow.canPreview)
            {
                animationWindow.previewing = true;
            }

            if (!animationWindow.previewing)
            {
                return;
            }

            speedControlledAnimationWindow = animationWindow;
            speedControlledPlayback = true;
            previousPlaybackEditorTime =
                EditorApplication.timeSinceStartup;
        }


        private void StopSpeedControlledPlayback()
        {
            speedControlledPlayback = false;
            speedControlledAnimationWindow = null;
            previousPlaybackEditorTime = 0d;
        }


        private void UpdateSpeedControlledPlayback()
        {
            if (!speedControlledPlayback)
            {
                AnimationWindow openAnimationWindow =
                    NpcPoseControlsUtility.FindOpenAnimationWindow();

                if (openAnimationWindow != null
                    && openAnimationWindow.playing
                    && !Mathf.Approximately(
                        playbackSpeed,
                        NormalPlaybackSpeed))
                {
                    openAnimationWindow.playing = false;
                    StartSpeedControlledPlayback(openAnimationWindow);
                    openAnimationWindow.Repaint();
                    Repaint();
                }

                return;
            }

            AnimationWindow animationWindow =
                speedControlledAnimationWindow;

            if (animationWindow == null
                || animationWindow.animationClip == null
                || animationWindow.recording
                || EditorApplication.isPlaying)
            {
                StopSpeedControlledPlayback();
                Repaint();
                return;
            }

            if (animationWindow.playing)
            {
                animationWindow.playing = false;
                StopSpeedControlledPlayback();
                animationWindow.Repaint();
                Repaint();
                return;
            }

            double editorTime = EditorApplication.timeSinceStartup;
            float deltaTime =
                (float)(editorTime - previousPlaybackEditorTime);
            previousPlaybackEditorTime = editorTime;

            animationWindow.time =
                NpcPoseControlsUtility.AdvancePlaybackTime(
                    animationWindow.time,
                    deltaTime,
                    playbackSpeed,
                    animationWindow.animationClip.length);
            animationWindow.Repaint();
            Repaint();
        }


        private void HandleUndoRedo()
        {
            SceneView.RepaintAll();
            Repaint();
        }
    }


    /// <summary>
    /// Small, testable operations shared by the Pose Controls window.
    /// </summary>
    public static class NpcPoseControlsUtility
    {
        public static bool TryResolveRig(
            GameObject selectedObject,
            out NpcCutoutRig rig)
        {
            if (selectedObject == null)
            {
                rig = null;
                return false;
            }

            rig = selectedObject.GetComponent<NpcCutoutRig>();
            if (rig != null)
            {
                return true;
            }

            rig = selectedObject.GetComponentInParent<NpcCutoutRig>(true);
            if (rig != null)
            {
                return true;
            }

            NpcCutoutRig[] childRigs =
                selectedObject.GetComponentsInChildren<NpcCutoutRig>(true);

            if (childRigs.Length == 1)
            {
                rig = childRigs[0];
                return true;
            }

            rig = null;
            return false;
        }


        public static float GetLocalZAngle(
            Transform bone)
        {
            if (bone == null)
            {
                return 0f;
            }

            return NormalizeAngle(bone.localEulerAngles.z);
        }


        public static void SetLocalZAngle(
            Transform bone,
            float angle)
        {
            if (bone == null)
            {
                return;
            }

            Vector3 localEulerAngles = bone.localEulerAngles;
            localEulerAngles.z = NormalizeAngle(angle);
            bone.localEulerAngles = localEulerAngles;
        }


        public static float NormalizeAngle(
            float angle)
        {
            angle %= 360f;

            if (angle > 180f)
            {
                angle -= 360f;
            }
            else if (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }


        public static float AdvancePlaybackTime(
            float currentTime,
            float deltaTime,
            float playbackSpeed,
            float clipLength)
        {
            if (clipLength <= 0f)
            {
                return 0f;
            }

            float elapsedTime =
                Mathf.Max(0f, deltaTime)
                * Mathf.Max(0f, playbackSpeed);

            return Mathf.Repeat(
                currentTime + elapsedTime,
                clipLength);
        }


        public static AnimationWindow FindOpenAnimationWindow()
        {
            AnimationWindow[] windows =
                Resources.FindObjectsOfTypeAll<AnimationWindow>();

            return windows.Length > 0
                ? windows[0]
                : null;
        }


        public static AnimationWindow OpenAnimationWindow()
        {
            AnimationWindow window =
                EditorWindow.GetWindow<AnimationWindow>();
            window.Show();
            return window;
        }


        public static string GetBoneLabel(
            NpcRigBoneId boneId)
        {
            switch (boneId)
            {
                case NpcRigBoneId.Pelvis:
                    return "Pelvis";
                case NpcRigBoneId.SpineLower:
                    return "Lower Spine";
                case NpcRigBoneId.Chest:
                    return "Chest";
                case NpcRigBoneId.Neck:
                    return "Neck";
                case NpcRigBoneId.Head:
                    return "Head";
                case NpcRigBoneId.ShoulderForeground:
                    return "Shoulder";
                case NpcRigBoneId.UpperArmForeground:
                    return "Upper Arm";
                case NpcRigBoneId.ForearmForeground:
                    return "Forearm";
                case NpcRigBoneId.HandForeground:
                    return "Hand";
                case NpcRigBoneId.ShoulderBackground:
                    return "Shoulder";
                case NpcRigBoneId.UpperArmBackground:
                    return "Upper Arm";
                case NpcRigBoneId.ForearmBackground:
                    return "Forearm";
                case NpcRigBoneId.HandBackground:
                    return "Hand";
                case NpcRigBoneId.ThighForeground:
                    return "Thigh";
                case NpcRigBoneId.ShinForeground:
                    return "Shin";
                case NpcRigBoneId.FootForeground:
                    return "Foot";
                case NpcRigBoneId.ThighBackground:
                    return "Thigh";
                case NpcRigBoneId.ShinBackground:
                    return "Shin";
                case NpcRigBoneId.FootBackground:
                    return "Foot";
                default:
                    return boneId.ToString();
            }
        }
    }
}
