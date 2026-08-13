using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Focused animation-authoring surface for the shared Person cutout rig.
    /// All edits are made to an in-memory clip until the user explicitly saves.
    /// </summary>
    public sealed class NpcAnimationWorkbenchWindow : EditorWindow
    {
        private const string PersonPrefabPath = "Assets/Prefabs/Characters/Core/Person.prefab";
        private const string SouthWalkPath = "Assets/Animations/Characters/Core/Person_Walk_SouthFacing.anim";
        private const float ControlWidth = 430f;
        private const float MinimumPreviewWidth = 360f;

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

        private readonly Dictionary<NpcRigBoneId, float> poseAngles = new Dictionary<NpcRigBoneId, float>();
        private readonly List<TransformSnapshot> bindPose = new List<TransformSnapshot>();

        private PreviewRenderUtility previewUtility;
        private GameObject previewPerson;
        private NpcCutoutRig previewRig;
        private AnimationClip sourceClip;
        private AnimationClip workingClip;
        private Vector2 controlScroll;
        private NpcFacing facing = NpcFacing.SouthEast;
        private WorkbenchChain selectedChain = WorkbenchChain.ForegroundArm;
        private float sampleTime;
        private float playbackSpeed = 1f;
        private bool loop = true;
        private bool playing;
        private bool clipHasChanges;
        private bool poseHasChanges;
        private double previousEditorTime;

        [MenuItem("Big Retail/Population/Animation Workbench")]
        public static void Open()
        {
            NpcAnimationWorkbenchWindow window = GetWindow<NpcAnimationWorkbenchWindow>();
            window.titleContent = new GUIContent("Animation Workbench");
            window.minSize = new Vector2(900f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Animation Workbench");
            minSize = new Vector2(900f, 620f);
            EnsurePreviewPerson();

            AnimationClip defaultClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SouthWalkPath);
            if (defaultClip != null)
            {
                LoadSourceClip(defaultClip);
            }

            previousEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            DisposeWorkingClip();
            DisposePreview();
        }

        private void OnGUI()
        {
            EnsurePreviewPerson();

            EditorGUILayout.LabelField("Big Retail Animation Workbench", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Pose and key the shared Person rig without touching scenes, prefabs, or the source clip. " +
                "Changes remain in a temporary working copy until you deliberately save them.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawControlPanel();
            DrawPreviewPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawControlPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ControlWidth));
            controlScroll = EditorGUILayout.BeginScrollView(controlScroll);

            DrawClipSection();
            EditorGUILayout.Space(8f);
            DrawFacingSection();
            EditorGUILayout.Space(8f);
            DrawPlaybackSection();
            EditorGUILayout.Space(8f);
            DrawPoseSection();
            EditorGUILayout.Space(8f);
            DrawSaveSection();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawClipSection()
        {
            EditorGUILayout.LabelField("1. Working Clip", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            AnimationClip chosen = (AnimationClip)EditorGUILayout.ObjectField(
                "Source Clip",
                sourceClip,
                typeof(AnimationClip),
                false);
            if (EditorGUI.EndChangeCheck() && chosen != sourceClip)
            {
                if (CanDiscardWorkingChanges())
                {
                    LoadSourceClip(chosen);
                }
            }

            using (new EditorGUI.DisabledScope(sourceClip == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Show Source"))
                {
                    EditorGUIUtility.PingObject(sourceClip);
                    Selection.activeObject = sourceClip;
                }

                if (GUILayout.Button("Reload Source"))
                {
                    if (CanDiscardWorkingChanges())
                    {
                        LoadSourceClip(sourceClip);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (workingClip != null)
            {
                string status = clipHasChanges
                    ? "Working copy has unsaved animation changes. The source asset is still untouched."
                    : "Working copy matches the source clip.";
                EditorGUILayout.HelpBox(status, clipHasChanges ? MessageType.Warning : MessageType.None);
            }
        }

        private void DrawFacingSection()
        {
            EditorGUILayout.LabelField("2. Facing", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("West", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField("East", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            DrawFacingButton("North West", NpcFacing.NorthWest);
            DrawFacingButton("North East", NpcFacing.NorthEast);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawFacingButton("South West", NpcFacing.SouthWest);
            DrawFacingButton("South East", NpcFacing.SouthEast);
            EditorGUILayout.EndHorizontal();

            if (workingClip != null)
            {
                string expectedFamily = NpcFacingUtility.UsesNorthFacingAnimation(facing)
                    ? "North-facing clip family"
                    : "South-facing clip family";
                EditorGUILayout.LabelField(expectedFamily, EditorStyles.miniLabel);
            }
        }

        private void DrawFacingButton(string label, NpcFacing buttonFacing)
        {
            Color previous = GUI.backgroundColor;
            if (facing == buttonFacing)
            {
                GUI.backgroundColor = new Color(0.45f, 0.72f, 1f);
            }

            if (GUILayout.Button(label))
            {
                facing = buttonFacing;
                if (previewRig != null)
                {
                    previewRig.SetFacing(facing);
                }
                EvaluateWorkingClip(true);
            }

            GUI.backgroundColor = previous;
        }

        private void DrawPlaybackSection()
        {
            EditorGUILayout.LabelField("3. Playback", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(workingClip == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(playing ? "Pause" : "Play"))
                {
                    playing = !playing;
                    previousEditorTime = EditorApplication.timeSinceStartup;
                    poseHasChanges = false;
                }

                if (GUILayout.Button("Restart"))
                {
                    playing = false;
                    sampleTime = 0f;
                    EvaluateWorkingClip(true);
                }
                EditorGUILayout.EndHorizontal();

                loop = EditorGUILayout.Toggle("Loop", loop);
                playbackSpeed = EditorGUILayout.Slider("Speed", playbackSpeed, 0.1f, 2f);

                float duration = workingClip != null ? Mathf.Max(workingClip.length, 0.001f) : 1f;
                EditorGUI.BeginChangeCheck();
                float newTime = EditorGUILayout.Slider("Timeline", sampleTime, 0f, duration);
                if (EditorGUI.EndChangeCheck())
                {
                    playing = false;
                    sampleTime = newTime;
                    EvaluateWorkingClip(true);
                }

                if (workingClip != null)
                {
                    int frame = Mathf.RoundToInt(sampleTime * workingClip.frameRate);
                    int totalFrames = Mathf.RoundToInt(workingClip.length * workingClip.frameRate);
                    EditorGUILayout.LabelField(
                        $"{sampleTime:0.000}s / {workingClip.length:0.000}s     Frame {frame} / {totalFrames}     {workingClip.frameRate:0.##} fps",
                        EditorStyles.miniLabel);
                }
            }
        }

        private void DrawPoseSection()
        {
            EditorGUILayout.LabelField("4. Pose Current Frame", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Sliders only change the preview. Press Set / Replace Key Pose to write these rotations into the working copy.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            selectedChain = (WorkbenchChain)EditorGUILayout.EnumPopup("Body Chain", selectedChain);
            if (EditorGUI.EndChangeCheck())
            {
                ReadCurrentPoseFromPreview();
            }

            NpcRigBoneId[] chain = GetChainBones(selectedChain);
            for (int i = 0; i < chain.Length; i++)
            {
                NpcRigBoneId boneId = chain[i];
                if (!poseAngles.TryGetValue(boneId, out float angle))
                {
                    angle = 0f;
                }

                EditorGUI.BeginChangeCheck();
                float newAngle = EditorGUILayout.Slider(GetBoneLabel(boneId), angle, -180f, 180f);
                if (EditorGUI.EndChangeCheck())
                {
                    poseAngles[boneId] = newAngle;
                    poseHasChanges = true;
                    playing = false;
                    ApplyPoseOverrides();
                }
            }

            using (new EditorGUI.DisabledScope(workingClip == null || !poseHasChanges))
            {
                if (GUILayout.Button("Set / Replace Key Pose", GUILayout.Height(28f)))
                {
                    SetCurrentChainKeys();
                }
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(workingClip == null))
            {
                if (GUILayout.Button("Reset Preview Pose"))
                {
                    EvaluateWorkingClip(true);
                }
            }

            if (GUILayout.Button("Select All Core"))
            {
                selectedChain = WorkbenchChain.Core;
                ReadCurrentPoseFromPreview();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaveSection()
        {
            EditorGUILayout.LabelField("5. Save Deliberately", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(workingClip == null))
            {
                if (GUILayout.Button("Save Working Copy As New Clip", GUILayout.Height(26f)))
                {
                    SaveAsNewClip();
                }

                using (new EditorGUI.DisabledScope(sourceClip == null || !clipHasChanges))
                {
                    if (GUILayout.Button("Publish Working Copy To Source", GUILayout.Height(26f)))
                    {
                        PublishToSource();
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Save As New creates a separate .anim asset. Publish updates the selected source clip and supports Undo.",
                MessageType.Info);
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(MinimumPreviewWidth), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Isolated Person Preview", EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(
                MinimumPreviewWidth,
                400f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            DrawPreview(previewRect);
            EditorGUILayout.LabelField("Mouse wheel: zoom", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void EnsurePreviewPerson()
        {
            if (previewPerson != null && previewRig != null && previewUtility != null)
            {
                return;
            }

            DisposePreview();

            GameObject personPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPath);
            if (personPrefab == null)
            {
                return;
            }

            previewUtility = new PreviewRenderUtility();
            previewUtility.camera.orthographic = true;
            previewUtility.camera.orthographicSize = 1.35f;
            previewUtility.camera.transform.position = new Vector3(0f, 0.9f, -10f);
            previewUtility.camera.transform.rotation = Quaternion.identity;
            previewUtility.camera.backgroundColor = new Color(0.045f, 0.05f, 0.065f, 1f);
            previewUtility.ambientColor = Color.white;
            previewUtility.lights[0].intensity = 1.4f;
            previewUtility.lights[1].intensity = 1.1f;

            previewPerson = (GameObject)PrefabUtility.InstantiatePrefab(personPrefab);
            if (previewPerson == null)
            {
                previewPerson = Instantiate(personPrefab);
            }

            previewPerson.name = "Person Animation Workbench Preview";
            previewPerson.hideFlags = HideFlags.HideAndDontSave;
            previewPerson.transform.position = Vector3.zero;

            Animator animator = previewPerson.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            previewRig = previewPerson.GetComponent<NpcCutoutRig>();
            if (previewRig == null)
            {
                previewRig = previewPerson.GetComponentInChildren<NpcCutoutRig>(true);
            }

            if (previewRig != null)
            {
                previewRig.SetFacing(facing);
            }

            previewUtility.AddSingleGO(previewPerson);
            CaptureBindPose();
            EvaluateWorkingClip(true);
        }

        private void DrawPreview(Rect rect)
        {
            if (Event.current.type == EventType.ScrollWheel && rect.Contains(Event.current.mousePosition) && previewUtility != null)
            {
                previewUtility.camera.orthographicSize = Mathf.Clamp(
                    previewUtility.camera.orthographicSize + Event.current.delta.y * 0.04f,
                    0.45f,
                    3.5f);
                Event.current.Use();
                Repaint();
            }

            if (previewUtility == null || previewPerson == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.045f, 0.05f, 0.065f));
                GUI.Label(rect, "Person prefab could not be loaded.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.camera.Render();
            Texture texture = previewUtility.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        private void LoadSourceClip(AnimationClip clip)
        {
            playing = false;
            DisposeWorkingClip();
            sourceClip = clip;
            sampleTime = 0f;
            clipHasChanges = false;
            poseHasChanges = false;

            if (sourceClip != null)
            {
                workingClip = new AnimationClip
                {
                    name = sourceClip.name + " (Animation Workbench)",
                    hideFlags = HideFlags.HideAndDontSave
                };
                NpcAnimationWorkbenchClipUtility.CopyClipContents(sourceClip, workingClip);
            }

            EvaluateWorkingClip(true);
            Repaint();
        }

        private void EvaluateWorkingClip(bool refreshPoseControls)
        {
            if (previewPerson == null)
            {
                return;
            }

            RestoreBindPose();
            if (workingClip != null)
            {
                sampleTime = Mathf.Clamp(sampleTime, 0f, Mathf.Max(workingClip.length, 0f));
                workingClip.SampleAnimation(previewPerson, sampleTime);
            }

            if (previewRig != null)
            {
                previewRig.SetFacing(facing);
            }

            if (refreshPoseControls)
            {
                poseHasChanges = false;
                ReadCurrentPoseFromPreview();
            }

            Repaint();
        }

        private void ReadCurrentPoseFromPreview()
        {
            if (previewRig == null)
            {
                return;
            }

            NpcRigBoneId[] chain = GetChainBones(selectedChain);
            for (int i = 0; i < chain.Length; i++)
            {
                if (previewRig.TryGetBone(chain[i], out Transform bone) && bone != null)
                {
                    poseAngles[chain[i]] = NormalizeAngle(bone.localEulerAngles.z);
                }
            }
        }

        private void ApplyPoseOverrides()
        {
            if (previewRig == null)
            {
                return;
            }

            NpcRigBoneId[] chain = GetChainBones(selectedChain);
            for (int i = 0; i < chain.Length; i++)
            {
                NpcRigBoneId boneId = chain[i];
                if (!poseAngles.TryGetValue(boneId, out float angle) ||
                    !previewRig.TryGetBone(boneId, out Transform bone) ||
                    bone == null)
                {
                    continue;
                }

                Vector3 localEuler = bone.localEulerAngles;
                localEuler.z = angle;
                bone.localEulerAngles = localEuler;
            }

            Repaint();
        }

        private void SetCurrentChainKeys()
        {
            if (workingClip == null || previewRig == null)
            {
                return;
            }

            NpcRigBoneId[] chain = GetChainBones(selectedChain);
            for (int i = 0; i < chain.Length; i++)
            {
                NpcRigBoneId boneId = chain[i];
                if (!poseAngles.TryGetValue(boneId, out float angle) ||
                    !previewRig.TryGetBone(boneId, out Transform bone) ||
                    bone == null)
                {
                    continue;
                }

                string path = AnimationUtility.CalculateTransformPath(bone, previewPerson.transform);
                NpcAnimationWorkbenchClipUtility.SetOrReplaceRotationKey(workingClip, path, sampleTime, angle);
            }

            clipHasChanges = true;
            poseHasChanges = false;
            EvaluateWorkingClip(true);
        }

        private void SaveAsNewClip()
        {
            if (workingClip == null)
            {
                return;
            }

            string suggestedName = sourceClip != null ? sourceClip.name + "_Variant" : "Person_Animation";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Animation Working Copy",
                suggestedName,
                "anim",
                "Choose a project location for the new animation clip.",
                "Assets/Animations/Characters/Core");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            AnimationClip newClip = new AnimationClip();
            NpcAnimationWorkbenchClipUtility.CopyClipContents(workingClip, newClip);
            newClip.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(newClip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(newClip);
            Selection.activeObject = newClip;
        }

        private void PublishToSource()
        {
            if (sourceClip == null || workingClip == null)
            {
                return;
            }

            bool approved = EditorUtility.DisplayDialog(
                "Publish Animation Working Copy",
                $"Replace the animation curves in '{sourceClip.name}' with the current working copy?\n\nThis supports Undo.",
                "Publish",
                "Cancel");
            if (!approved)
            {
                return;
            }

            Undo.RecordObject(sourceClip, "Publish Animation Workbench Changes");
            NpcAnimationWorkbenchClipUtility.CopyClipContents(workingClip, sourceClip);
            EditorUtility.SetDirty(sourceClip);
            AssetDatabase.SaveAssets();
            clipHasChanges = false;
            Repaint();
        }

        private bool CanDiscardWorkingChanges()
        {
            if (!clipHasChanges)
            {
                return true;
            }

            return EditorUtility.DisplayDialog(
                "Discard Animation Working Copy?",
                "The temporary working copy has unsaved animation changes. The source clip has not been modified.",
                "Discard Working Copy",
                "Keep Editing");
        }

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            double delta = now - previousEditorTime;
            previousEditorTime = now;

            if (!playing || workingClip == null || workingClip.length <= 0f)
            {
                return;
            }

            sampleTime += (float)delta * playbackSpeed;
            if (sampleTime > workingClip.length)
            {
                if (loop)
                {
                    sampleTime %= workingClip.length;
                }
                else
                {
                    sampleTime = workingClip.length;
                    playing = false;
                }
            }

            EvaluateWorkingClip(true);
        }

        private void CaptureBindPose()
        {
            bindPose.Clear();
            if (previewPerson == null)
            {
                return;
            }

            Transform[] transforms = previewPerson.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                bindPose.Add(new TransformSnapshot(transforms[i]));
            }
        }

        private void RestoreBindPose()
        {
            for (int i = 0; i < bindPose.Count; i++)
            {
                bindPose[i].Restore();
            }
        }

        private void DisposeWorkingClip()
        {
            if (workingClip != null)
            {
                DestroyImmediate(workingClip);
                workingClip = null;
            }
        }

        private void DisposePreview()
        {
            bindPose.Clear();
            previewRig = null;

            if (previewPerson != null)
            {
                DestroyImmediate(previewPerson);
                previewPerson = null;
            }

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }
        }

        private static NpcRigBoneId[] GetChainBones(WorkbenchChain chain)
        {
            switch (chain)
            {
                case WorkbenchChain.Core:
                    return CoreBones;
                case WorkbenchChain.ForegroundArm:
                    return ForegroundArmBones;
                case WorkbenchChain.BackgroundArm:
                    return BackgroundArmBones;
                case WorkbenchChain.ForegroundLeg:
                    return ForegroundLegBones;
                case WorkbenchChain.BackgroundLeg:
                    return BackgroundLegBones;
                default:
                    return CoreBones;
            }
        }

        private static string GetBoneLabel(NpcRigBoneId boneId)
        {
            switch (boneId)
            {
                case NpcRigBoneId.Pelvis: return "Pelvis / Body Anchor";
                case NpcRigBoneId.SpineLower: return "Lower Spine";
                case NpcRigBoneId.Chest: return "Chest";
                case NpcRigBoneId.Neck: return "Neck";
                case NpcRigBoneId.Head: return "Head";
                case NpcRigBoneId.ShoulderForeground: return "Foreground Shoulder";
                case NpcRigBoneId.UpperArmForeground: return "Foreground Upper Arm";
                case NpcRigBoneId.ForearmForeground: return "Foreground Elbow / Forearm";
                case NpcRigBoneId.HandForeground: return "Foreground Wrist / Hand";
                case NpcRigBoneId.ShoulderBackground: return "Background Shoulder";
                case NpcRigBoneId.UpperArmBackground: return "Background Upper Arm";
                case NpcRigBoneId.ForearmBackground: return "Background Elbow / Forearm";
                case NpcRigBoneId.HandBackground: return "Background Wrist / Hand";
                case NpcRigBoneId.ThighForeground: return "Foreground Hip / Thigh";
                case NpcRigBoneId.ShinForeground: return "Foreground Knee / Shin";
                case NpcRigBoneId.FootForeground: return "Foreground Ankle / Foot";
                case NpcRigBoneId.ThighBackground: return "Background Hip / Thigh";
                case NpcRigBoneId.ShinBackground: return "Background Knee / Shin";
                case NpcRigBoneId.FootBackground: return "Background Ankle / Foot";
                default: return boneId.ToString();
            }
        }

        private static float NormalizeAngle(float angle)
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

        private enum WorkbenchChain
        {
            Core,
            ForegroundArm,
            BackgroundArm,
            ForegroundLeg,
            BackgroundLeg
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }
    }

    /// <summary>
    /// Shared, testable clip operations used by the Animation Workbench.
    /// </summary>
    public static class NpcAnimationWorkbenchClipUtility
    {
        private const string RotationProperty = "localEulerAnglesRaw.z";

        public static void CopyClipContents(AnimationClip source, AnimationClip destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            ClearClip(destination);
            destination.frameRate = source.frameRate;
            destination.wrapMode = source.wrapMode;
            destination.legacy = source.legacy;

            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(source);
            for (int i = 0; i < floatBindings.Length; i++)
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, floatBindings[i]);
                AnimationUtility.SetEditorCurve(destination, floatBindings[i], CloneCurve(sourceCurve));
            }

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            for (int i = 0; i < objectBindings.Length; i++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, objectBindings[i]);
                AnimationUtility.SetObjectReferenceCurve(destination, objectBindings[i], keys);
            }

            AnimationUtility.SetAnimationEvents(destination, AnimationUtility.GetAnimationEvents(source));
            AnimationUtility.SetAnimationClipSettings(destination, AnimationUtility.GetAnimationClipSettings(source));
        }

        public static void SetOrReplaceRotationKey(AnimationClip clip, string transformPath, float time, float angle)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                transformPath ?? string.Empty,
                typeof(Transform),
                RotationProperty);

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            float tolerance = Mathf.Max(0.0001f, 0.25f / Mathf.Max(clip.frameRate, 1f));
            int existingIndex = FindKeyAtTime(curve, time, tolerance);

            Keyframe key = new Keyframe(time, angle);
            if (existingIndex >= 0)
            {
                curve.RemoveKey(existingIndex);
            }

            int insertedIndex = curve.AddKey(key);
            if (insertedIndex >= 0)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, insertedIndex, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, insertedIndex, AnimationUtility.TangentMode.Auto);
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void ClearClip(AnimationClip clip)
        {
            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < floatBindings.Length; i++)
            {
                AnimationUtility.SetEditorCurve(clip, floatBindings[i], null);
            }

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int i = 0; i < objectBindings.Length; i++)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, objectBindings[i], null);
            }

            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return null;
            }

            AnimationCurve clone = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return clone;
        }

        private static int FindKeyAtTime(AnimationCurve curve, float time, float tolerance)
        {
            for (int i = 0; i < curve.length; i++)
            {
                if (Mathf.Abs(curve.keys[i].time - time) <= tolerance)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
