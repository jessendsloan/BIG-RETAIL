using System;
using System.Collections.Generic;
using System.IO;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BigRetail.Characters.Editor
{
    internal enum NpcAppearanceAssetCategory
    {
        Body = 0,
        Skin = 1,
        Outfit = 2,
        Hair = 3
    }


    internal enum NpcBodyAuthoringMode
    {
        Shape = 0,
        RigAlignment = 1,
        PoseTest = 2
    }


    /// <summary>
    /// Friendly authoring surface for the reusable assets consumed by
    /// Population Definitions. All edits occur on a hidden working copy
    /// until the user deliberately saves them.
    /// </summary>
    public sealed class NpcAppearanceCreatorWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Population/Appearance Creator";

        private const string DefaultAppearancePath =
            "Assets/Art/Characters/Appearance/Defaults/" +
            "DefaultAppearance.asset";

        private const string AppearanceRoot =
            "Assets/Art/Characters/Appearance";

        private const string StandardGarmentMaterialPath =
            AppearanceRoot +
            "/Materials/CharacterGarmentTextureLit.mat";

        private const string AuthoringPoseRoot =
            AppearanceRoot + "/Authoring Poses";

        private const string StarterTPosePath =
            AuthoringPoseRoot + "/TPose.asset";

        private static readonly string[] CategoryLabels =
        {
            "Body",
            "Skin",
            "Outfit",
            "Hair"
        };

        private static readonly string[] FacingLabels =
        {
            "South East",
            "South West",
            "North East",
            "North West"
        };

        private static readonly NpcFacing[] Facings =
        {
            NpcFacing.SouthEast,
            NpcFacing.SouthWest,
            NpcFacing.NorthEast,
            NpcFacing.NorthWest
        };

        private static readonly NpcRigPartId[] LimbPartIds =
        {
            NpcRigPartId.UpperArmNear,
            NpcRigPartId.ForearmNear,
            NpcRigPartId.HandNear,
            NpcRigPartId.UpperArmFar,
            NpcRigPartId.ForearmFar,
            NpcRigPartId.HandFar,
            NpcRigPartId.ThighNear,
            NpcRigPartId.ShinNear,
            NpcRigPartId.FootNear,
            NpcRigPartId.ThighFar,
            NpcRigPartId.ShinFar,
            NpcRigPartId.FootFar
        };

        private static readonly string[] BodyAuthoringModeLabels =
        {
            "1. Shape",
            "2. Rig Alignment",
            "3. Pose Test"
        };

        private static readonly string[] BodyAlignmentPartLabels =
        {
            "Head / Head Pivot",
            "Neck / Neck Pivot",
            "Torso / Chest",
            "Pelvis / Body Anchor",
            "Foreground Upper Arm / Shoulder",
            "Foreground Forearm / Elbow",
            "Foreground Hand / Wrist",
            "Background Upper Arm / Shoulder",
            "Background Forearm / Elbow",
            "Background Hand / Wrist",
            "Foreground Thigh / Hip",
            "Foreground Shin / Knee",
            "Foreground Foot / Ankle",
            "Background Thigh / Hip",
            "Background Shin / Knee",
            "Background Foot / Ankle"
        };

        private static readonly NpcRigPartId[] BodyAlignmentPartIds =
        {
            NpcRigPartId.Head,
            NpcRigPartId.Neck,
            NpcRigPartId.Torso,
            NpcRigPartId.Pelvis,
            NpcRigPartId.UpperArmNear,
            NpcRigPartId.ForearmNear,
            NpcRigPartId.HandNear,
            NpcRigPartId.UpperArmFar,
            NpcRigPartId.ForearmFar,
            NpcRigPartId.HandFar,
            NpcRigPartId.ThighNear,
            NpcRigPartId.ShinNear,
            NpcRigPartId.FootNear,
            NpcRigPartId.ThighFar,
            NpcRigPartId.ShinFar,
            NpcRigPartId.FootFar
        };

        private NpcAppearanceCatalog catalog;
        private NpcAppearanceProfile defaultAppearance;
        private NpcAppearanceAssetCategory category;
        private NpcBodyAuthoringMode bodyAuthoringMode;
        private int bodyAlignmentPartIndex;
        private bool showAdvancedRigAlignment;
        private ScriptableObject selectedAsset;
        private ScriptableObject workingAsset;
        private SerializedObject serializedWorkingAsset;
        private NpcPersonPreviewCanvas previewCanvas;
        private NpcPersonGender previewGender = NpcPersonGender.Man;
        private NpcFacing facing = NpcFacing.SouthEast;
        private bool showRigAnatomy;
        private NpcRigOverlayFocus rigOverlayFocus =
            NpcRigOverlayFocus.FullSkeleton;
        private NpcRigOverlayFocus poseTestChain =
            NpcRigOverlayFocus.NearArm;
        private readonly Dictionary<NpcRigBoneId, float> testPoseAngles =
            new Dictionary<NpcRigBoneId, float>();
        private readonly List<NpcAuthoringPose> authoringPoses =
            new List<NpcAuthoringPose>();
        private NpcAuthoringPose selectedAuthoringPose;
        private bool authoringPoseModified;
        private Vector2 scrollPosition;
        private string loadedJson;
        private string statusMessage;
        private MessageType statusType = MessageType.Info;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            NpcAppearanceCreatorWindow window =
                GetWindow<NpcAppearanceCreatorWindow>(
                    "Appearance Creator");

            window.minSize = new Vector2(900f, 560f);
            window.Show();
        }


        private void OnEnable()
        {
            previewCanvas = new NpcPersonPreviewCanvas();
            FindLibraries();
            RefreshAuthoringPoses();
            LoadFirstAsset();
        }


        private void OnDisable()
        {
            DestroyWorkingCopy();
            previewCanvas?.Dispose();
            previewCanvas = null;
        }


        private void OnDestroy()
        {
            DestroyWorkingCopy();
            previewCanvas?.Dispose();
            previewCanvas = null;
        }


        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Big Retail Appearance Creator",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Create the reusable Body, Skin, Outfit, and Hair assets " +
                "used by Population Definitions. Edits stay on a temporary " +
                "working copy until you deliberately save them.",
                MessageType.Info);

            if (catalog == null || defaultAppearance == null)
            {
                DrawMissingSetup();
                return;
            }

            float controlPanelWidth = Mathf.Clamp(
                position.width * 0.42f,
                500f,
                760f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox,
                           GUILayout.Width(controlPanelWidth),
                           GUILayout.ExpandHeight(true)))
                {
                    scrollPosition =
                        EditorGUILayout.BeginScrollView(scrollPosition);

                    float previousLabelWidth =
                        EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 115f;

                    DrawCategoryTabs();
                    DrawAssetSelector();

                    if (workingAsset == null)
                    {
                        EditorGUILayout.HelpBox(
                            "This category has no source asset to edit.",
                            MessageType.Warning);
                    }
                    else
                    {
                        DrawPreviewControls();
                        EditorGUILayout.Space(10f);
                        DrawCategoryEditor();
                        EditorGUILayout.Space(10f);
                        DrawSaveControls();
                        DrawValidation();

                        if (!string.IsNullOrWhiteSpace(statusMessage))
                        {
                            EditorGUILayout.Space(6f);
                            EditorGUILayout.HelpBox(
                                statusMessage,
                                statusType);
                        }

                        EditorGUILayout.Space(8f);
                        EditorGUILayout.LabelField(
                            "Saving adds the asset to the central " +
                            "appearance library. It will not begin " +
                            "spawning until you add it to a Population " +
                            "Definition.",
                            EditorStyles.wordWrappedMiniLabel);
                    }

                    EditorGUIUtility.labelWidth = previousLabelWidth;
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope(
                           GUILayout.ExpandWidth(true),
                           GUILayout.ExpandHeight(true)))
                {
                    EditorGUILayout.LabelField(
                        GetCategoryLabel() + " Preview",
                        EditorStyles.boldLabel);

                    if (workingAsset == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Choose or create an asset to preview.",
                            MessageType.Info);
                    }
                    else
                    {
                        DrawPreview();
                    }
                }
            }
        }


        private void DrawMissingSetup()
        {
            EditorGUILayout.HelpBox(
                "The appearance catalog or neutral default appearance is " +
                "missing. Repair the starter content before authoring new " +
                "appearance assets.",
                MessageType.Warning);

            if (GUILayout.Button("Repair Starter Content"))
            {
                NpcPopulationStarterFactory
                    .CreateOrUpdateStarterCatalog();
                FindLibraries();
                LoadFirstAsset();
            }
        }


        private void DrawCategoryTabs()
        {
            int nextCategory = GUILayout.Toolbar(
                (int)category,
                CategoryLabels,
                GUILayout.Height(26f));

            if (nextCategory == (int)category)
            {
                return;
            }

            if (!ConfirmDiscardWorkingChanges())
            {
                return;
            }

            category = (NpcAppearanceAssetCategory)nextCategory;
            previewCanvas?.ResetZoom();
            LoadFirstAsset();
        }


        private void DrawAssetSelector()
        {
            List<ScriptableObject> assets = GetCategoryAssets();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Working Asset",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Asset Folder");
                EditorGUILayout.SelectableLabel(
                    GetCategoryFolder(),
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (GUILayout.Button(
                        "Open Folder",
                        GUILayout.Width(90f)))
                {
                    ShowCategoryFolder();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (assets.Count == 0)
                {
                    EditorGUILayout.LabelField("No assets registered");
                }
                else
                {
                    int currentIndex = assets.IndexOf(selectedAsset);

                    if (currentIndex < 0)
                    {
                        currentIndex = 0;
                    }

                    string[] names = new string[assets.Count];

                    for (int index = 0; index < assets.Count; index++)
                    {
                        names[index] = GetDisplayName(assets[index]);
                    }

                    int nextIndex = EditorGUILayout.Popup(
                        "Source",
                        currentIndex,
                        names);

                    if (assets[nextIndex] != selectedAsset
                        && ConfirmDiscardWorkingChanges())
                    {
                        LoadAsset(assets[nextIndex]);
                    }
                }

                using (new EditorGUI.DisabledScope(
                           selectedAsset == null))
                {
                    if (GUILayout.Button(
                            "Show Asset",
                            GUILayout.Width(90f)))
                    {
                        Selection.activeObject = selectedAsset;
                        EditorGUIUtility.PingObject(selectedAsset);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           workingAsset == null
                           || selectedAsset == null))
                {
                    if (GUILayout.Button("New From Current"))
                    {
                        BeginNewAsset();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           selectedAsset == null || !HasUnsavedChanges()))
                {
                    if (GUILayout.Button("Revert"))
                    {
                        LoadAsset(selectedAsset);
                    }
                }
            }

            if (selectedAsset == null && workingAsset != null)
            {
                EditorGUILayout.HelpBox(
                    "New unsaved asset. Give it a clear name, adjust it, " +
                    "then use Save as New Asset.",
                    MessageType.Info);
            }
            else if (HasUnsavedChanges())
            {
                EditorGUILayout.HelpBox(
                    "Unsaved working-copy changes.",
                    MessageType.Warning);
            }
        }


        private void DrawPreviewControls()
        {
            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "Preview Gender",
                    GUILayout.Width(EditorGUIUtility.labelWidth));

                using (new EditorGUI.DisabledScope(
                           category == NpcAppearanceAssetCategory.Body))
                {
                    int nextGender = GUILayout.Toolbar(
                        previewGender == NpcPersonGender.Man ? 0 : 1,
                        new[] { "Man", "Woman" });

                    NpcPersonGender selected = nextGender == 0
                        ? NpcPersonGender.Man
                        : NpcPersonGender.Woman;

                    if (selected != previewGender)
                    {
                        previewGender = selected;
                        ApplyPreview();
                    }
                }
            }

            int currentFacing = Array.IndexOf(Facings, facing);
            int nextFacing = GUILayout.Toolbar(
                Mathf.Max(0, currentFacing),
                FacingLabels);

            if (nextFacing != currentFacing)
            {
                facing = Facings[nextFacing];
                previewCanvas?.SetFacing(facing);
                Repaint();
            }
        }


        private void DrawPreview()
        {
            if (category == NpcAppearanceAssetCategory.Body
                && testPoseAngles.Count > 0)
            {
                previewCanvas?.SetTestPose(testPoseAngles);
            }
            else
            {
                previewCanvas?.ResetTestPose();
            }

            Rect rect = GUILayoutUtility.GetRect(
                260f,
                300f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            previewCanvas?.Draw(
                rect,
                GetPreviewFocus(),
                category == NpcAppearanceAssetCategory.Body
                && showRigAnatomy,
                rigOverlayFocus);
        }


        private void DrawCategoryEditor()
        {
            serializedWorkingAsset.Update();
            EditorGUI.BeginChangeCheck();

            switch (category)
            {
                case NpcAppearanceAssetCategory.Body:
                    DrawBodyEditor();
                    break;

                case NpcAppearanceAssetCategory.Skin:
                    DrawSkinEditor();
                    break;

                case NpcAppearanceAssetCategory.Outfit:
                    DrawOutfitEditor();
                    break;

                case NpcAppearanceAssetCategory.Hair:
                    DrawHairEditor();
                    break;
            }

            bool changed = EditorGUI.EndChangeCheck();

            if (serializedWorkingAsset.ApplyModifiedProperties()
                || changed)
            {
                SynchronizeGenderFromBody();
                ApplyPreview();
                Repaint();
            }
        }


        private void DrawBodyEditor()
        {
            EditorGUILayout.LabelField(
                "Body Silhouette",
                EditorStyles.boldLabel);

            DrawDisplayName();

            SerializedProperty kind =
                serializedWorkingAsset.FindProperty("kind");

            int nextGender = EditorGUILayout.Popup(
                "Gender",
                kind.enumValueIndex,
                new[] { "Man", "Woman" });
            kind.enumValueIndex = nextGender;

            DrawBodyWorkflow();

            switch (bodyAuthoringMode)
            {
                case NpcBodyAuthoringMode.RigAlignment:
                    DrawBodyRigAlignmentEditor();
                    break;

                case NpcBodyAuthoringMode.PoseTest:
                    DrawBodyPoseTestEditor();
                    break;

                default:
                    DrawBodyShapeEditor();
                    break;
            }
        }


        private void DrawBodyWorkflow()
        {
            EditorGUILayout.Space(6f);
            int nextMode = GUILayout.Toolbar(
                (int)bodyAuthoringMode,
                BodyAuthoringModeLabels,
                GUILayout.Height(26f));

            if (nextMode != (int)bodyAuthoringMode)
            {
                bodyAuthoringMode = (NpcBodyAuthoringMode)nextMode;
                showRigAnatomy =
                    bodyAuthoringMode != NpcBodyAuthoringMode.Shape;

                if (bodyAuthoringMode
                    == NpcBodyAuthoringMode.RigAlignment)
                {
                    FocusRigOverlayOnSelectedPart();
                }
                else if (bodyAuthoringMode
                         == NpcBodyAuthoringMode.PoseTest)
                {
                    rigOverlayFocus = poseTestChain;
                }

                Repaint();
            }

            EditorGUILayout.LabelField(
                "Shape  >  Fit Skeleton  >  Align Artwork  >  Test Pose  >  Save",
                EditorStyles.centeredGreyMiniLabel);

            DrawAuthoringPoseControls();
        }


        private void DrawAuthoringPoseControls()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Authoring Pose",
                EditorStyles.miniBoldLabel);

            string[] poseNames = new string[authoringPoses.Count + 1];
            poseNames[0] = "Neutral / Bind Pose";
            int currentIndex = 0;

            for (int index = 0; index < authoringPoses.Count; index++)
            {
                NpcAuthoringPose pose = authoringPoses[index];
                poseNames[index + 1] = pose != null
                    ? pose.DisplayName
                    : "Missing Pose";

                if (pose == selectedAuthoringPose)
                {
                    currentIndex = index + 1;
                }
            }

            int nextIndex = EditorGUILayout.Popup(
                "Active Pose",
                currentIndex,
                poseNames);

            if (nextIndex != currentIndex)
            {
                if (nextIndex == 0)
                {
                    ActivateNeutralPose();
                }
                else
                {
                    ActivateAuthoringPose(
                        authoringPoses[nextIndex - 1]);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("T-Pose"))
                {
                    ActivateStarterTPose();
                }

                if (GUILayout.Button("Neutral"))
                {
                    ActivateNeutralPose();
                }

                using (new EditorGUI.DisabledScope(
                           selectedAuthoringPose == null))
                {
                    if (GUILayout.Button("Show Pose Asset"))
                    {
                        Selection.activeObject = selectedAuthoringPose;
                        EditorGUIUtility.PingObject(
                            selectedAuthoringPose);
                    }
                }
            }

            if (testPoseAngles.Count > 0)
            {
                string poseName = selectedAuthoringPose != null
                    ? selectedAuthoringPose.DisplayName
                    : "Custom / Unsaved";

                if (authoringPoseModified
                    && selectedAuthoringPose != null)
                {
                    poseName += " (Modified)";
                }

                EditorGUILayout.HelpBox(
                    poseName + " is active in every Body tab. It is " +
                    "preview-only and does not alter the Body asset, " +
                    "Person prefab, or animation clips.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "Neutral bind pose is active.",
                    EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }


        private void DrawBodyShapeEditor()
        {
            showRigAnatomy = false;

            EditorGUILayout.HelpBox(
                "Define the person's proportions here. Paired limbs change " +
                "together and keep their joint connections. Use Rig " +
                "Alignment afterward for exact cyan-joint and artwork " +
                "placement.",
                MessageType.Info);

            EditorGUILayout.LabelField(
                "Head and Body",
                EditorStyles.boldLabel);

            DrawPartSize(
                "Head",
                0.18f,
                0.75f,
                0.18f,
                0.70f,
                NpcRigPartId.Head);
            DrawPartSize(
                "Neck",
                0.04f,
                0.28f,
                0.04f,
                0.32f,
                NpcRigPartId.Neck);
            DrawPartSize(
                "Torso",
                0.16f,
                0.85f,
                0.22f,
                0.95f,
                NpcRigPartId.Torso);
            DrawPartSize(
                "Pelvis / Hips",
                0.12f,
                0.65f,
                0.08f,
                0.45f,
                NpcRigPartId.Pelvis);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Arms and Hands",
                EditorStyles.boldLabel);

            DrawPartSize(
                "Upper Arms",
                0.05f,
                0.35f,
                0.10f,
                0.55f,
                NpcRigPartId.UpperArmNear,
                NpcRigPartId.UpperArmFar);
            DrawPartSize(
                "Forearms",
                0.045f,
                0.32f,
                0.09f,
                0.55f,
                NpcRigPartId.ForearmNear,
                NpcRigPartId.ForearmFar);
            DrawPartSize(
                "Hands",
                0.04f,
                0.28f,
                0.045f,
                0.32f,
                NpcRigPartId.HandNear,
                NpcRigPartId.HandFar);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Legs and Feet",
                EditorStyles.boldLabel);

            DrawPartSize(
                "Thighs / Upper Legs",
                0.05f,
                0.42f,
                0.14f,
                0.70f,
                NpcRigPartId.ThighNear,
                NpcRigPartId.ThighFar);
            DrawPartSize(
                "Shins / Lower Legs",
                0.045f,
                0.38f,
                0.14f,
                0.70f,
                NpcRigPartId.ShinNear,
                NpcRigPartId.ShinFar);
            DrawPartSize(
                "Feet",
                0.06f,
                0.48f,
                0.04f,
                0.34f,
                NpcRigPartId.FootNear,
                NpcRigPartId.FootFar);
        }


        private void DrawBodyRigAlignmentEditor()
        {
            showRigAnatomy = true;
            FocusRigOverlayOnSelectedPart();

            EditorGUILayout.HelpBox(
                "Fit the cyan skeleton to the current proportions, then " +
                "align one visible body piece at a time. These alignment " +
                "values are saved in the Body asset.",
                MessageType.Info);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Overall Stance",
                EditorStyles.miniBoldLabel);
            DrawSkeletonStanceControls();
            EditorGUILayout.LabelField(
                "Set the skeleton's overall foreground/background spacing " +
                "and depth " +
                "before fitting its joints and artwork.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            if (GUILayout.Button(
                    "START HERE - Fit Entire Skeleton",
                    GUILayout.Height(28f)))
            {
                FitCoreConnections();
                RealignLimbConnections();
                rigOverlayFocus = NpcRigOverlayFocus.FullSkeleton;
                SetStatus(
                    "The complete skeleton was fitted to the current " +
                    "shape. Select a body part below for visual alignment.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Selected Artwork",
                EditorStyles.boldLabel);

            int nextPart = EditorGUILayout.Popup(
                "Body Part",
                Mathf.Clamp(
                    bodyAlignmentPartIndex,
                    0,
                    BodyAlignmentPartIds.Length - 1),
                BodyAlignmentPartLabels);

            if (nextPart != bodyAlignmentPartIndex)
            {
                bodyAlignmentPartIndex = nextPart;
                FocusRigOverlayOnSelectedPart();
            }

            NpcRigPartId selectedPart = GetSelectedAlignmentPart();
            DrawPartRotation("Sprite Tilt", selectedPart);
            DrawPartArtworkAlignment("Selected Artwork", selectedPart);

            if (GUILayout.Button("Fit Selected Artwork to Cyan Joint"))
            {
                FitSelectedAlignmentPart(selectedPart);
            }

            EditorGUILayout.LabelField(
                "Sideways and Along-Bone offsets move only the selected " +
                "visible piece. They never move the cyan skeleton.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);
            showAdvancedRigAlignment = EditorGUILayout.Foldout(
                showAdvancedRigAlignment,
                "Skeleton Placement & Individual Fit Tools",
                true,
                EditorStyles.foldoutHeader);

            if (!showAdvancedRigAlignment)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Ground and Core Joint Placement",
                EditorStyles.miniBoldLabel);

            DrawBonePlacement2D(
                "Body over Ground Root",
                "Horizontal",
                "Height",
                NpcRigBoneId.Pelvis,
                -0.5f,
                0.5f,
                0.2f,
                1.8f);

            DrawBonePlacement2D(
                "Lower Spine from Pelvis",
                "Horizontal",
                "Height",
                NpcRigBoneId.SpineLower,
                -0.35f,
                0.35f,
                -0.1f,
                0.8f);

            DrawBonePlacement2D(
                "Chest from Lower Spine",
                "Horizontal",
                "Height",
                NpcRigBoneId.Chest,
                -0.35f,
                0.35f,
                0f,
                1f);

            DrawBonePlacement2D(
                "Neck from Chest",
                "Horizontal",
                "Height",
                NpcRigBoneId.Neck,
                -0.3f,
                0.3f,
                0f,
                0.8f);

            DrawBonePlacement2D(
                "Head from Neck",
                "Horizontal",
                "Height",
                NpcRigBoneId.Head,
                -0.3f,
                0.3f,
                0f,
                0.8f);

            EditorGUILayout.LabelField(
                "Root stays at the world/ground position. Body over " +
                "Ground Root moves the complete skeleton relative to " +
                "that point so you can place it between the feet.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                "Individual Chain Fit",
                EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit Core Body Joints"))
                {
                    FitCoreConnections();
                    showRigAnatomy = true;
                    rigOverlayFocus = NpcRigOverlayFocus.BodyAndHead;
                    SetStatus(
                        "The torso, neck, and head were reconnected on " +
                        "this unsaved working copy.",
                        MessageType.Info);
                }

                if (GUILayout.Button("Fit All Limb Joints"))
                {
                    RealignLimbConnections();
                    showRigAnatomy = true;
                    rigOverlayFocus = NpcRigOverlayFocus.FullSkeleton;
                    SetStatus(
                        "All arm and leg joints were fitted on this " +
                        "unsaved working copy.",
                        MessageType.Info);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit Foreground Arm"))
                {
                    FitLimbChain(
                        NpcRigOverlayFocus.NearArm);
                }

                if (GUILayout.Button("Fit Background Arm"))
                {
                    FitLimbChain(
                        NpcRigOverlayFocus.FarArm);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fit Foreground Leg"))
                {
                    FitLimbChain(
                        NpcRigOverlayFocus.NearLeg);
                }

                if (GUILayout.Button("Fit Background Leg"))
                {
                    FitLimbChain(
                        NpcRigOverlayFocus.FarLeg);
                }
            }

            EditorGUILayout.LabelField(
                "Fit is deterministic: it rebuilds a chain from the " +
                "current segment sizes instead of stacking corrections. " +
                "It affects only this unsaved working copy.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }


        private void DrawBodyPoseTestEditor()
        {
            showRigAnatomy = true;
            rigOverlayFocus = poseTestChain;

            EditorGUILayout.HelpBox(
                "Bend the real preview skeleton to expose weak joints. " +
                "The pose remains active when you return to Shape or Rig " +
                "Alignment. It never becomes part of the Body asset or " +
                "animation clips.",
                MessageType.Info);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawPoseTestControls();
            EditorGUILayout.EndVertical();

            DrawAuthoringPoseSaveControls();
        }


        private NpcRigPartId GetSelectedAlignmentPart()
        {
            bodyAlignmentPartIndex = Mathf.Clamp(
                bodyAlignmentPartIndex,
                0,
                BodyAlignmentPartIds.Length - 1);
            return BodyAlignmentPartIds[bodyAlignmentPartIndex];
        }


        private void FocusRigOverlayOnSelectedPart()
        {
            NpcRigPartId partId = GetSelectedAlignmentPart();

            switch (partId)
            {
                case NpcRigPartId.UpperArmNear:
                case NpcRigPartId.ForearmNear:
                case NpcRigPartId.HandNear:
                    rigOverlayFocus =
                        NpcRigOverlayFocus.NearArm;
                    break;

                case NpcRigPartId.UpperArmFar:
                case NpcRigPartId.ForearmFar:
                case NpcRigPartId.HandFar:
                    rigOverlayFocus =
                        NpcRigOverlayFocus.FarArm;
                    break;

                case NpcRigPartId.ThighNear:
                case NpcRigPartId.ShinNear:
                case NpcRigPartId.FootNear:
                    rigOverlayFocus =
                        NpcRigOverlayFocus.NearLeg;
                    break;

                case NpcRigPartId.ThighFar:
                case NpcRigPartId.ShinFar:
                case NpcRigPartId.FootFar:
                    rigOverlayFocus =
                        NpcRigOverlayFocus.FarLeg;
                    break;

                default:
                    rigOverlayFocus = NpcRigOverlayFocus.BodyAndHead;
                    break;
            }
        }


        private void FitSelectedAlignmentPart(
            NpcRigPartId partId)
        {
            if (Array.IndexOf(LimbPartIds, partId) >= 0)
            {
                FitLimbPart(partId);
            }
            else
            {
                FitPartToJointAnchor(partId);
            }

            FocusRigOverlayOnSelectedPart();
            SetStatus(
                BodyAlignmentPartLabels[bodyAlignmentPartIndex]
                + " was reset to its cyan joint on this unsaved working " +
                "copy.",
                MessageType.Info);
        }


        private void DrawPoseTestControls()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Pose Test (Preview Only)",
                EditorStyles.miniBoldLabel);

            NpcRigOverlayFocus[] chains =
            {
                NpcRigOverlayFocus.BodyAndHead,
                NpcRigOverlayFocus.NearArm,
                NpcRigOverlayFocus.FarArm,
                NpcRigOverlayFocus.NearLeg,
                NpcRigOverlayFocus.FarLeg
            };
            string[] labels =
            {
                "Body / Head",
                "Foreground Arm",
                "Background Arm",
                "Foreground Leg",
                "Background Leg"
            };
            int current = Array.IndexOf(chains, poseTestChain);
            int next = EditorGUILayout.Popup(
                "Chain",
                Mathf.Max(0, current),
                labels);

            if (next != current)
            {
                poseTestChain = chains[next];
                showRigAnatomy = true;
                rigOverlayFocus = poseTestChain;
            }

            NpcRigBoneId[] selectedBones;

            if (poseTestChain == NpcRigOverlayFocus.BodyAndHead)
            {
                selectedBones = new[]
                {
                    NpcRigBoneId.Pelvis,
                    NpcRigBoneId.SpineLower,
                    NpcRigBoneId.Chest,
                    NpcRigBoneId.Neck,
                    NpcRigBoneId.Head
                };

                DrawTestPoseAngle(
                    "Pelvis",
                    NpcRigBoneId.Pelvis,
                    -45f,
                    45f);
                DrawTestPoseAngle(
                    "Lower Spine",
                    NpcRigBoneId.SpineLower,
                    -45f,
                    45f);
                DrawTestPoseAngle(
                    "Chest",
                    NpcRigBoneId.Chest,
                    -45f,
                    45f);
                DrawTestPoseAngle(
                    "Neck",
                    NpcRigBoneId.Neck,
                    -45f,
                    45f);
                DrawTestPoseAngle(
                    "Head",
                    NpcRigBoneId.Head,
                    -60f,
                    60f);
            }
            else
            {
                GetPoseTestBones(
                    poseTestChain,
                    out NpcRigBoneId proximal,
                    out NpcRigBoneId middle,
                    out NpcRigBoneId distal,
                    out string proximalLabel,
                    out string middleLabel,
                    out string distalLabel);

                selectedBones = new[] { proximal, middle, distal };
                DrawTestPoseAngle(
                    proximalLabel,
                    proximal,
                    -140f,
                    140f);
                DrawTestPoseAngle(
                    middleLabel,
                    middle,
                    -140f,
                    140f);
                DrawTestPoseAngle(
                    distalLabel,
                    distal,
                    -90f,
                    90f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Selected Chain"))
                {
                    for (int index = 0;
                         index < selectedBones.Length;
                         index++)
                    {
                        testPoseAngles.Remove(selectedBones[index]);
                    }

                    authoringPoseModified = true;
                    Repaint();
                }

                if (GUILayout.Button("Reset Entire Pose"))
                {
                    ActivateNeutralPose();
                }
            }

            EditorGUILayout.LabelField(
                "These sliders rotate the actual hidden preview bones " +
                "around their joints. They are for checking connections " +
                "and never become part of the Body asset.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawTestPoseAngle(
            string label,
            NpcRigBoneId boneId,
            float minimum,
            float maximum)
        {
            float value = testPoseAngles.TryGetValue(
                boneId,
                out float stored)
                ? stored
                : 0f;
            float next = EditorGUILayout.Slider(
                label,
                value,
                minimum,
                maximum);

            if (Mathf.Approximately(next, value))
            {
                return;
            }

            if (Mathf.Approximately(next, 0f))
            {
                testPoseAngles.Remove(boneId);
            }
            else
            {
                testPoseAngles[boneId] = next;
            }

            authoringPoseModified = true;
            showRigAnatomy = true;
            rigOverlayFocus = poseTestChain;
        }


        private void DrawAuthoringPoseSaveControls()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "Reusable Authoring Pose",
                EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           testPoseAngles.Count == 0))
                {
                    if (GUILayout.Button("Save Current Pose as New..."))
                    {
                        SaveCurrentAuthoringPoseAsNew();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           selectedAuthoringPose == null
                           || !authoringPoseModified))
                {
                    if (GUILayout.Button("Update Selected Pose"))
                    {
                        UpdateSelectedAuthoringPose();
                    }
                }
            }

            EditorGUILayout.LabelField(
                "Saved authoring poses are reusable inspection stances. " +
                "They are separate from gameplay animation clips.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }


        private void ActivateNeutralPose()
        {
            selectedAuthoringPose = null;
            authoringPoseModified = false;
            testPoseAngles.Clear();
            previewCanvas?.ResetTestPose();
            Repaint();
        }


        private void ActivateAuthoringPose(
            NpcAuthoringPose pose)
        {
            if (pose == null)
            {
                ActivateNeutralPose();
                return;
            }

            selectedAuthoringPose = pose;
            authoringPoseModified = false;
            pose.CopyAnglesTo(testPoseAngles);
            previewCanvas?.SetTestPose(testPoseAngles);
            Repaint();
        }


        private void ActivateStarterTPose()
        {
            NpcAuthoringPose pose =
                AssetDatabase.LoadAssetAtPath<NpcAuthoringPose>(
                    StarterTPosePath);

            if (pose == null)
            {
                RefreshAuthoringPoses();
                pose = AssetDatabase.LoadAssetAtPath<NpcAuthoringPose>(
                    StarterTPosePath);
            }

            ActivateAuthoringPose(pose);
        }


        private void SaveCurrentAuthoringPoseAsNew()
        {
            EnsureAuthoringPoseFolder();

            string suggestedName = selectedAuthoringPose != null
                ? selectedAuthoringPose.DisplayName + " Copy"
                : "Custom Authoring Pose";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Authoring Pose",
                CreateSafeFileName(suggestedName),
                "asset",
                "Choose where to save this reusable authoring pose.",
                AuthoringPoseRoot);

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            NpcAuthoringPose pose =
                CreateInstance<NpcAuthoringPose>();
            string displayName = ObjectNames.NicifyVariableName(
                Path.GetFileNameWithoutExtension(path));
            pose.Configure(displayName, testPoseAngles);
            pose.name = CreateSafeFileName(displayName);
            AssetDatabase.CreateAsset(pose, path);
            Undo.RegisterCreatedObjectUndo(
                pose,
                "Create Authoring Pose");
            AssetDatabase.SaveAssets();

            RefreshAuthoringPoseList();
            selectedAuthoringPose = pose;
            authoringPoseModified = false;
            Selection.activeObject = pose;
            EditorGUIUtility.PingObject(pose);
            SetStatus(
                pose.DisplayName + " authoring pose saved.",
                MessageType.Info);
        }


        private void UpdateSelectedAuthoringPose()
        {
            if (selectedAuthoringPose == null)
            {
                return;
            }

            Undo.RecordObject(
                selectedAuthoringPose,
                "Update Authoring Pose");
            selectedAuthoringPose.Configure(
                selectedAuthoringPose.DisplayName,
                testPoseAngles);
            EditorUtility.SetDirty(selectedAuthoringPose);
            AssetDatabase.SaveAssets();
            authoringPoseModified = false;
            SetStatus(
                selectedAuthoringPose.DisplayName +
                " authoring pose updated.",
                MessageType.Info);
        }


        private void RefreshAuthoringPoses()
        {
            EnsureAuthoringPoseFolder();
            EnsureStarterTPose();
            RefreshAuthoringPoseList();
        }


        private void RefreshAuthoringPoseList()
        {
            authoringPoses.Clear();

            if (!AssetDatabase.IsValidFolder(AuthoringPoseRoot))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:NpcAuthoringPose",
                new[] { AuthoringPoseRoot });

            for (int index = 0; index < guids.Length; index++)
            {
                NpcAuthoringPose pose =
                    AssetDatabase.LoadAssetAtPath<NpcAuthoringPose>(
                        AssetDatabase.GUIDToAssetPath(guids[index]));

                if (pose != null && !authoringPoses.Contains(pose))
                {
                    authoringPoses.Add(pose);
                }
            }

            authoringPoses.Sort(
                (left, right) => string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison.OrdinalIgnoreCase));
        }


        private static void EnsureAuthoringPoseFolder()
        {
            if (!AssetDatabase.IsValidFolder(AuthoringPoseRoot))
            {
                AssetDatabase.CreateFolder(
                    AppearanceRoot,
                    "Authoring Poses");
            }
        }


        private static void EnsureStarterTPose()
        {
            if (AssetDatabase.LoadAssetAtPath<NpcAuthoringPose>(
                    StarterTPosePath) != null)
            {
                return;
            }

            NpcAuthoringPose pose =
                CreateInstance<NpcAuthoringPose>();
            Dictionary<NpcRigBoneId, float> angles =
                new Dictionary<NpcRigBoneId, float>
                {
                    {
                        NpcRigBoneId.UpperArmNear,
                        -90f
                    },
                    {
                        NpcRigBoneId.UpperArmFar,
                        90f
                    }
                };

            pose.Configure("T-Pose", angles);
            pose.name = "TPose";
            AssetDatabase.CreateAsset(pose, StarterTPosePath);
            AssetDatabase.SaveAssets();
        }


        private static void GetPoseTestBones(
            NpcRigOverlayFocus chain,
            out NpcRigBoneId proximal,
            out NpcRigBoneId middle,
            out NpcRigBoneId distal,
            out string proximalLabel,
            out string middleLabel,
            out string distalLabel)
        {
            switch (chain)
            {
                case NpcRigOverlayFocus.FarArm:
                    proximal = NpcRigBoneId.UpperArmFar;
                    middle = NpcRigBoneId.ForearmFar;
                    distal = NpcRigBoneId.HandFar;
                    proximalLabel = "Shoulder";
                    middleLabel = "Elbow";
                    distalLabel = "Wrist";
                    return;

                case NpcRigOverlayFocus.NearLeg:
                    proximal = NpcRigBoneId.ThighNear;
                    middle = NpcRigBoneId.ShinNear;
                    distal = NpcRigBoneId.FootNear;
                    proximalLabel = "Hip";
                    middleLabel = "Knee";
                    distalLabel = "Ankle";
                    return;

                case NpcRigOverlayFocus.FarLeg:
                    proximal = NpcRigBoneId.ThighFar;
                    middle = NpcRigBoneId.ShinFar;
                    distal = NpcRigBoneId.FootFar;
                    proximalLabel = "Hip";
                    middleLabel = "Knee";
                    distalLabel = "Ankle";
                    return;

                default:
                    proximal = NpcRigBoneId.UpperArmNear;
                    middle = NpcRigBoneId.ForearmNear;
                    distal = NpcRigBoneId.HandNear;
                    proximalLabel = "Shoulder";
                    middleLabel = "Elbow";
                    distalLabel = "Wrist";
                    return;
            }
        }


        private void DrawSkinEditor()
        {
            EditorGUILayout.LabelField(
                "Skin Palette",
                EditorStyles.boldLabel);

            DrawDisplayName();
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("skinColor"),
                new GUIContent("Skin Color"));
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty(
                    "farSideShade"),
                new GUIContent(
                    "Far-Side Shading",
                    "Multiplies skin color on the visually farther side " +
                    "for isometric depth."));
        }


        private void DrawOutfitEditor()
        {
            EditorGUILayout.LabelField(
                "Outfit Set",
                EditorStyles.boldLabel);

            DrawDisplayName();
            DrawGenderCompatibility();
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("primaryFabric"),
                new GUIContent("Shirt / Primary"));
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("secondaryFabric"),
                new GUIContent("Trousers / Secondary"));
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("footwear"),
                new GUIContent("Footwear"));
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("accent"),
                new GUIContent("Badge / Accent"));
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("showBadge"),
                new GUIContent("Show Employee Badge"));

            SerializedProperty showBadge =
                serializedWorkingAsset.FindProperty("showBadge");

            if (showBadge.boolValue)
            {
                SerializedProperty badgeAnchor =
                    serializedWorkingAsset.FindProperty(
                        "badgeTorsoAnchor");
                Vector2 nextAnchor = badgeAnchor.vector2Value;

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField(
                    "Badge Placement",
                    EditorStyles.miniBoldLabel);
                nextAnchor.x = EditorGUILayout.Slider(
                    new GUIContent(
                        "Horizontal Chest Position",
                        "Normalized across the torso: -0.5 is the " +
                        "camera-left edge and 0.5 is camera-right."),
                    nextAnchor.x,
                    -0.5f,
                    0.5f);
                nextAnchor.y = EditorGUILayout.Slider(
                    new GUIContent(
                        "Vertical Chest Position",
                        "Normalized across the torso: -0.5 is the " +
                        "bottom edge and 0.5 is the top."),
                    nextAnchor.y,
                    -0.5f,
                    0.5f);
                badgeAnchor.vector2Value = nextAnchor;

                EditorGUILayout.LabelField(
                    "The badge follows this torso-relative anchor when " +
                    "a Body asset changes chest width or height.",
                    EditorStyles.wordWrappedMiniLabel);
            }

            DrawSleeveStyle();

            EditorGUILayout.Space(8f);
            DrawTorsoGarmentArtwork();

            EditorGUILayout.HelpBox(
                "SouthWest mirrors SouthEast; NorthWest mirrors NorthEast. " +
                "The underlying outfit still retains its optional authored " +
                "sprite overrides.",
                MessageType.Info);
        }


        private void DrawTorsoGarmentArtwork()
        {
            SerializedProperty torsoStyle =
                FindOutfitPartStyle(NpcRigPartId.Torso);

            if (torsoStyle == null)
            {
                EditorGUILayout.HelpBox(
                    "This outfit is missing its Torso part rule.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(
                "Torso Garment Artwork",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                torsoStyle.FindPropertyRelative("southEastSprite"),
                new GUIContent(
                    "South / Front PNG",
                    "Used for SouthEast and mirrored for SouthWest."));
            EditorGUILayout.PropertyField(
                torsoStyle.FindPropertyRelative("northEastSprite"),
                new GUIContent(
                    "North / Back PNG",
                    "Used for NorthEast and mirrored for NorthWest."));
            SerializedProperty materialOverride =
                torsoStyle.FindPropertyRelative("materialOverride");

            EditorGUILayout.PropertyField(
                materialOverride,
                new GUIContent(
                    "Garment Material",
                    "Assign CharacterGarmentTextureLit for painted PNG " +
                    "clothing. Leave empty to use the plain body material."));

            using (new EditorGUI.DisabledScope(
                       materialOverride.objectReferenceValue != null))
            {
                if (GUILayout.Button("Use Standard PNG Garment Material"))
                {
                    materialOverride.objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Material>(
                            StandardGarmentMaterialPath);
                }
            }

            EditorGUILayout.HelpBox(
                "The PNG supplies the clothing art. The material supplies " +
                "lighting, tint behavior, and subtle cleanup. One material " +
                "works with both authored directions.",
                MessageType.Info);
        }


        private void DrawHairEditor()
        {
            EditorGUILayout.LabelField(
                "Hair Set",
                EditorStyles.boldLabel);

            DrawDisplayName();
            DrawGenderCompatibility();
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("hairColor"),
                new GUIContent("Hair Color"));

            EditorGUILayout.Space(6f);
            DrawHairPiece(
                "Front Hair",
                "hairFront",
                "hairFrontShape");

            EditorGUILayout.Space(6f);
            DrawHairPiece(
                "Rear Hair",
                "hairRear",
                "hairRearShape");

            EditorGUILayout.Space(8f);
            DrawHairDetailLayers();
        }


        private void DrawDisplayName()
        {
            EditorGUILayout.PropertyField(
                serializedWorkingAsset.FindProperty("displayName"),
                new GUIContent("Name"));
        }


        private void DrawGenderCompatibility()
        {
            SerializedProperty supportedGenders =
                serializedWorkingAsset.FindProperty("supportedGenders");

            NpcGenderCompatibility current =
                (NpcGenderCompatibility)supportedGenders.intValue;

            int currentIndex;

            switch (current)
            {
                case NpcGenderCompatibility.Men:
                    currentIndex = 0;
                    break;

                case NpcGenderCompatibility.Women:
                    currentIndex = 1;
                    break;

                default:
                    currentIndex = 2;
                    break;
            }

            int nextIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Available For",
                    "Choose Men, Women, or Everyone. Population " +
                    "generation filters incompatible assets."),
                currentIndex,
                new[] { "Men", "Women", "Everyone" });

            switch (nextIndex)
            {
                case 0:
                    supportedGenders.intValue =
                        (int)NpcGenderCompatibility.Men;
                    break;

                case 1:
                    supportedGenders.intValue =
                        (int)NpcGenderCompatibility.Women;
                    break;

                default:
                    supportedGenders.intValue =
                        (int)NpcGenderCompatibility.Everyone;
                    break;
            }
        }


        private void DrawHairPiece(
            string label,
            string stylePropertyName,
            string shapePropertyName)
        {
            SerializedProperty style =
                serializedWorkingAsset.FindProperty(stylePropertyName);
            SerializedProperty shape =
                serializedWorkingAsset.FindProperty(shapePropertyName);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            SerializedProperty position =
                shape.FindPropertyRelative("localPosition");
            SerializedProperty euler =
                shape.FindPropertyRelative("localEulerAngles");
            SerializedProperty size =
                shape.FindPropertyRelative("size");

            Vector3 nextPosition = position.vector3Value;
            nextPosition.x = EditorGUILayout.Slider(
                "Horizontal Position",
                nextPosition.x,
                -0.45f,
                0.45f);
            nextPosition.y = EditorGUILayout.Slider(
                "Vertical Position",
                nextPosition.y,
                -0.45f,
                0.45f);
            position.vector3Value = nextPosition;

            Vector2 nextSize = size.vector2Value;
            nextSize.x = EditorGUILayout.Slider(
                "Width",
                nextSize.x,
                0.03f,
                0.85f);
            nextSize.y = EditorGUILayout.Slider(
                "Height",
                nextSize.y,
                0.03f,
                0.85f);
            size.vector2Value = nextSize;

            Vector3 nextEuler = euler.vector3Value;
            nextEuler.z = EditorGUILayout.Slider(
                "Angle",
                nextEuler.z,
                -90f,
                90f);
            euler.vector3Value = nextEuler;

            EditorGUILayout.PropertyField(
                shape.FindPropertyRelative("visible"),
                new GUIContent("Visible"));

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(
                "Optional Directional Art",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(
                style.FindPropertyRelative("southEastSprite"),
                new GUIContent("South East Sprite"));
            EditorGUILayout.PropertyField(
                style.FindPropertyRelative("northEastSprite"),
                new GUIContent("North East Sprite"));

            EditorGUILayout.EndVertical();
        }


        private void DrawHairDetailLayers()
        {
            SerializedProperty layers =
                serializedWorkingAsset.FindProperty("detailLayers");

            EditorGUILayout.LabelField(
                "Optional Silhouette Layers",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Add small shapes such as a side sweep, fringe, crown " +
                "tuft, temple, or bun. These follow the existing Head " +
                "bone, so they add hairstyle variety without changing " +
                "the shared rig or any animations.",
                MessageType.Info);

            int removeIndex = -1;

            for (int index = 0; index < layers.arraySize; index++)
            {
                SerializedProperty layer =
                    layers.GetArrayElementAtIndex(index);
                SerializedProperty layerName =
                    layer.FindPropertyRelative("displayName");

                string label = string.IsNullOrWhiteSpace(
                    layerName.stringValue)
                        ? $"Hair Layer {index + 1}"
                        : layerName.stringValue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                layer.isExpanded = EditorGUILayout.Foldout(
                    layer.isExpanded,
                    label,
                    true);

                if (layer.isExpanded)
                {
                    EditorGUILayout.PropertyField(
                        layerName,
                        new GUIContent("Layer Name"));
                    EditorGUILayout.PropertyField(
                        layer.FindPropertyRelative("depth"),
                        new GUIContent(
                            "Depth",
                            "Behind Head sits below the face; Crown and " +
                            "Fringe sit above it."));
                    EditorGUILayout.Slider(
                        layer.FindPropertyRelative("shadeMultiplier"),
                        0.35f,
                        1.35f,
                        new GUIContent("Color Shade"));

                    EditorGUILayout.Space(3f);
                    EditorGUILayout.LabelField(
                        "Optional Directional Art",
                        EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(
                        layer.FindPropertyRelative("southEastSprite"),
                        new GUIContent("South East Sprite"));
                    EditorGUILayout.PropertyField(
                        layer.FindPropertyRelative("northEastSprite"),
                        new GUIContent("North East Sprite"));

                    EditorGUILayout.Space(4f);
                    DrawHairLayerPose(
                        "South East Shape",
                        layer.FindPropertyRelative("southEastPose"));
                    DrawHairLayerPose(
                        "North East Shape",
                        layer.FindPropertyRelative("northEastPose"));

                    if (GUILayout.Button("Remove Layer"))
                    {
                        removeIndex = index;
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                layers.DeleteArrayElementAtIndex(removeIndex);
            }

            if (GUILayout.Button(
                    "+ Add Hair Shape",
                    GUILayout.Height(25f)))
            {
                AddHairDetailLayer(layers);
            }
        }


        private static void DrawHairLayerPose(
            string label,
            SerializedProperty pose)
        {
            if (pose == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            SerializedProperty position =
                pose.FindPropertyRelative("localPosition");
            SerializedProperty euler =
                pose.FindPropertyRelative("localEulerAngles");
            SerializedProperty size =
                pose.FindPropertyRelative("size");

            Vector3 nextPosition = position.vector3Value;
            nextPosition.x = EditorGUILayout.Slider(
                "Horizontal Position",
                nextPosition.x,
                -0.55f,
                0.55f);
            nextPosition.y = EditorGUILayout.Slider(
                "Vertical Position",
                nextPosition.y,
                -0.55f,
                0.55f);
            position.vector3Value = nextPosition;

            Vector2 nextSize = size.vector2Value;
            nextSize.x = EditorGUILayout.Slider(
                "Width",
                nextSize.x,
                0.02f,
                0.85f);
            nextSize.y = EditorGUILayout.Slider(
                "Height",
                nextSize.y,
                0.02f,
                0.85f);
            size.vector2Value = nextSize;

            Vector3 nextEuler = euler.vector3Value;
            nextEuler.z = EditorGUILayout.Slider(
                "Angle",
                nextEuler.z,
                -180f,
                180f);
            euler.vector3Value = nextEuler;

            EditorGUILayout.PropertyField(
                pose.FindPropertyRelative("visible"),
                new GUIContent("Visible"));
            EditorGUILayout.EndVertical();
        }


        private void AddHairDetailLayer(
            SerializedProperty layers)
        {
            int index = layers.arraySize;
            layers.InsertArrayElementAtIndex(index);

            SerializedProperty layer =
                layers.GetArrayElementAtIndex(index);
            layer.isExpanded = true;
            layer.FindPropertyRelative("displayName").stringValue =
                $"Hair Shape {index + 1}";
            layer.FindPropertyRelative("depth").enumValueIndex =
                (int)NpcHairLayerDepth.Crown;
            layer.FindPropertyRelative("shadeMultiplier").floatValue = 1f;

            SerializedProperty frontStyle =
                serializedWorkingAsset.FindProperty("hairFront");
            Sprite southEast = frontStyle
                .FindPropertyRelative("southEastSprite")
                .objectReferenceValue as Sprite;
            Sprite northEast = frontStyle
                .FindPropertyRelative("northEastSprite")
                .objectReferenceValue as Sprite;

            layer.FindPropertyRelative("southEastSprite")
                .objectReferenceValue = southEast;
            layer.FindPropertyRelative("northEastSprite")
                .objectReferenceValue = northEast;

            ResetHairLayerPose(
                layer.FindPropertyRelative("southEastPose"));
            ResetHairLayerPose(
                layer.FindPropertyRelative("northEastPose"));
        }


        private static void ResetHairLayerPose(
            SerializedProperty pose)
        {
            pose.FindPropertyRelative("localPosition").vector3Value =
                Vector3.zero;
            pose.FindPropertyRelative("localEulerAngles").vector3Value =
                Vector3.zero;
            pose.FindPropertyRelative("size").vector2Value =
                new Vector2(0.12f, 0.12f);
            pose.FindPropertyRelative("visible").boolValue = true;
        }


        private void DrawPartSize(
            string label,
            float minimumWidth,
            float maximumWidth,
            float minimumLength,
            float maximumLength,
            params NpcRigPartId[] partIds)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            DrawPartDimension(
                "Width / Thickness",
                minimumWidth,
                maximumWidth,
                false,
                partIds);

            DrawPartDimension(
                "Height / Length",
                minimumLength,
                maximumLength,
                true,
                partIds);

            EditorGUILayout.EndVertical();
        }


        private void DrawPartRotation(
            string label,
            NpcRigPartId partId)
        {
            SerializedProperty shape = FindPartShapeEntry(partId);

            if (shape == null)
            {
                return;
            }

            SerializedProperty eulerProperty =
                shape.FindPropertyRelative("localEulerAngles");
            Vector3 oldEuler = eulerProperty.vector3Value;
            float oldAngle = Mathf.DeltaAngle(0f, oldEuler.z);
            float nextAngle = EditorGUILayout.Slider(
                new GUIContent(
                    label,
                    "A small saved correction for the artwork's resting " +
                    "angle. This is not a pose control; use Pose Test to " +
                    "rotate the real bones."),
                oldAngle,
                -45f,
                45f);

            if (Mathf.Approximately(nextAngle, oldAngle))
            {
                return;
            }

            RotatePartAroundJoint(
                shape,
                partId,
                oldEuler,
                nextAngle);
        }


        private void DrawPartArtworkAlignment(
            string label,
            NpcRigPartId partId)
        {
            SerializedProperty shape = FindPartShapeEntry(partId);

            if (shape == null
                || !TryGetJointAnchoredPartPosition(
                    shape,
                    partId,
                    out Vector3 fittedPosition))
            {
                return;
            }

            SerializedProperty positionProperty =
                shape.FindPropertyRelative("localPosition");
            Vector3 currentPosition = positionProperty.vector3Value;
            fittedPosition.z = currentPosition.z;
            Vector3 currentOffset = currentPosition - fittedPosition;

            EditorGUILayout.LabelField(
                label + " Alignment",
                EditorStyles.miniLabel);

            float nextSideways = EditorGUILayout.Slider(
                new GUIContent(
                    "  Sideways Offset",
                    "Moves only this visible sprite sideways relative to " +
                    "its cyan joint. The skeleton and animation are not " +
                    "changed."),
                currentOffset.x,
                -0.25f,
                0.25f);

            float nextAlongBone = EditorGUILayout.Slider(
                new GUIContent(
                    "  Along Bone (+ toward parent)",
                    "Moves only this visible sprite along the cyan bone. " +
                    "Positive values pull it toward its parent joint: shin " +
                    "toward knee, forearm toward elbow, or foot toward " +
                    "ankle."),
                currentOffset.y,
                -0.25f,
                0.25f);

            if (!Mathf.Approximately(nextSideways, currentOffset.x)
                || !Mathf.Approximately(nextAlongBone, currentOffset.y))
            {
                positionProperty.vector3Value = fittedPosition
                    + new Vector3(
                        nextSideways,
                        nextAlongBone,
                        0f);
            }

            using (new EditorGUI.DisabledScope(
                       Mathf.Approximately(currentOffset.x, 0f)
                       && Mathf.Approximately(currentOffset.y, 0f)))
            {
                if (GUILayout.Button(
                        "Reset " + label + " to Cyan Joint"))
                {
                    positionProperty.vector3Value = fittedPosition;
                }
            }
        }


        private void RotatePartAroundJoint(
            SerializedProperty shape,
            NpcRigPartId partId,
            Vector3 oldEuler,
            float nextAngle)
        {
            SerializedProperty eulerProperty =
                shape.FindPropertyRelative("localEulerAngles");
            SerializedProperty positionProperty =
                shape.FindPropertyRelative("localPosition");
            Vector2 size = shape
                .FindPropertyRelative("size")
                .vector2Value;
            Vector3 nextEuler = oldEuler;
            nextEuler.z = nextAngle;

            if (TryGetNormalizedJointAnchor(
                    partId,
                    out Vector2 normalizedAnchor))
            {
                Vector3 localAnchor = new Vector3(
                    normalizedAnchor.x * size.x,
                    normalizedAnchor.y * size.y,
                    0f);
                Vector3 oldAnchorOffset =
                    Quaternion.Euler(oldEuler) * localAnchor;
                Vector3 nextAnchorOffset =
                    Quaternion.Euler(nextEuler) * localAnchor;

                positionProperty.vector3Value +=
                    oldAnchorOffset - nextAnchorOffset;
            }

            eulerProperty.vector3Value = nextEuler;

        }


        private void DrawPartDimension(
            string label,
            float minimum,
            float maximum,
            bool vertical,
            params NpcRigPartId[] partIds)
        {
            List<SerializedProperty> shapes =
                FindPartShapeEntries(partIds);

            if (shapes.Count == 0)
            {
                return;
            }

            float value = 0f;

            for (int index = 0; index < shapes.Count; index++)
            {
                Vector2 size = shapes[index]
                    .FindPropertyRelative("size")
                    .vector2Value;
                value += vertical ? size.y : size.x;
            }

            value /= shapes.Count;
            float next = EditorGUILayout.Slider(
                label,
                value,
                minimum,
                maximum);

            if (Mathf.Approximately(next, value))
            {
                return;
            }

            float scale = value > 0.0001f
                ? next / value
                : 1f;
            bool preserveCoreConnections = vertical
                && ContainsCoreBodyPart(shapes);
            float previousChestTarget = 0f;
            float previousNeckTarget = 0f;
            float previousHeadTarget = 0f;
            bool hasPreviousCoreTargets = preserveCoreConnections
                && TryCalculateCoreConnectionTargets(
                    out previousChestTarget,
                    out previousNeckTarget,
                    out previousHeadTarget);

            for (int index = 0; index < shapes.Count; index++)
            {
                SerializedProperty shape = shapes[index];
                SerializedProperty sizeProperty =
                    shape.FindPropertyRelative("size");
                Vector2 oldSize = sizeProperty.vector2Value;
                Vector2 newSize = oldSize;

                if (vertical)
                {
                    newSize.y = value > 0.0001f
                        ? oldSize.y * scale
                        : next;
                }
                else
                {
                    newSize.x = value > 0.0001f
                        ? oldSize.x * scale
                        : next;
                }

                NpcRigPartId partId = (NpcRigPartId)shape
                    .FindPropertyRelative("id")
                    .enumValueIndex;

                KeepVisualAnchorFixed(
                    shape,
                    partId,
                    oldSize,
                    newSize);

                sizeProperty.vector2Value = newSize;

                if (vertical
                    && TryGetDistalBone(
                        partId,
                        out NpcRigBoneId distalBone)
                    && NpcRigDefinition.TryGetPartDefinition(
                        partId,
                        out NpcRigPartDefinition definition)
                    && definition.PlaceholderSize.y > 0.0001f)
                {
                    float angle = Mathf.DeltaAngle(
                        0f,
                        shape.FindPropertyRelative("localEulerAngles")
                            .vector3Value.z);

                    AdjustBonePlacementLength(
                        distalBone,
                        oldSize.y / definition.PlaceholderSize.y,
                        newSize.y / definition.PlaceholderSize.y,
                        angle);
                }
            }

            if (hasPreviousCoreTargets
                && TryCalculateCoreConnectionTargets(
                    out float nextChestTarget,
                    out float nextNeckTarget,
                    out float nextHeadTarget))
            {
                ShiftCoreConnectionTargets(
                    nextChestTarget - previousChestTarget,
                    nextNeckTarget - previousNeckTarget,
                    nextHeadTarget - previousHeadTarget);
            }
        }


        private static bool ContainsCoreBodyPart(
            List<SerializedProperty> shapes)
        {
            for (int index = 0; index < shapes.Count; index++)
            {
                NpcRigPartId partId = (NpcRigPartId)shapes[index]
                    .FindPropertyRelative("id")
                    .enumValueIndex;

                if (IsCoreBodyPart(partId))
                {
                    return true;
                }
            }

            return false;
        }


        private static bool IsCoreBodyPart(
            NpcRigPartId partId)
        {
            switch (partId)
            {
                case NpcRigPartId.Pelvis:
                case NpcRigPartId.Torso:
                case NpcRigPartId.Neck:
                case NpcRigPartId.Head:
                    return true;

                default:
                    return false;
            }
        }


        private void KeepVisualAnchorFixed(
            SerializedProperty shape,
            NpcRigPartId partId,
            Vector2 oldSize,
            Vector2 newSize)
        {
            if (!TryGetNormalizedJointAnchor(
                    partId,
                    out Vector2 normalizedAnchor))
            {
                return;
            }

            SerializedProperty positionProperty =
                shape.FindPropertyRelative("localPosition");
            Vector3 position = positionProperty.vector3Value;
            Quaternion rotation = Quaternion.Euler(
                shape.FindPropertyRelative("localEulerAngles")
                    .vector3Value);

            Vector3 oldAnchorOffset = rotation * new Vector3(
                normalizedAnchor.x * oldSize.x,
                normalizedAnchor.y * oldSize.y,
                0f);
            Vector3 newAnchorOffset = rotation * new Vector3(
                normalizedAnchor.x * newSize.x,
                normalizedAnchor.y * newSize.y,
                0f);

            positionProperty.vector3Value =
                position + oldAnchorOffset - newAnchorOffset;
        }


        private void RealignLimbConnections()
        {
            for (int index = 0; index < LimbPartIds.Length; index++)
            {
                FitLimbPart(LimbPartIds[index]);
            }
        }


        private void FitCoreConnections()
        {
            const float seamOverlap = 0.015f;

            // The neck and head are articulated segments just like the
            // limbs: their owning bones must remain on the canonical hinge
            // inside the artwork.  Merely moving the bones until the visible
            // seams touch can leave a previously offset sprite hanging from
            // the wrong end of its bone, which only becomes obvious when the
            // neck is rotated in Pose Test.
            FitPartToJointAnchor(NpcRigPartId.Neck);
            FitPartToJointAnchor(NpcRigPartId.Head);

            if (!TryGetShapeGeometry(
                    NpcRigPartId.Pelvis,
                    out Vector2 pelvisCenter,
                    out Vector2 pelvisSize)
                || !TryGetShapeGeometry(
                    NpcRigPartId.Torso,
                    out Vector2 torsoCenter,
                    out Vector2 torsoSize)
                || !TryGetShapeGeometry(
                    NpcRigPartId.Neck,
                    out Vector2 neckCenter,
                    out Vector2 neckSize)
                || !TryGetShapeGeometry(
                    NpcRigPartId.Head,
                    out Vector2 headCenter,
                    out Vector2 headSize))
            {
                return;
            }

            // Creating an array entry can invalidate previously captured
            // SerializedProperty handles, so ensure all entries exist first
            // and then reacquire their position properties together.
            FindOrCreateBonePlacementPosition(NpcRigBoneId.SpineLower);
            FindOrCreateBonePlacementPosition(NpcRigBoneId.Chest);
            FindOrCreateBonePlacementPosition(NpcRigBoneId.Neck);
            FindOrCreateBonePlacementPosition(NpcRigBoneId.Head);

            SerializedProperty spine = FindBonePlacementPosition(
                NpcRigBoneId.SpineLower);
            SerializedProperty chest = FindBonePlacementPosition(
                NpcRigBoneId.Chest);
            SerializedProperty neck = FindBonePlacementPosition(
                NpcRigBoneId.Neck);
            SerializedProperty head = FindBonePlacementPosition(
                NpcRigBoneId.Head);

            if (spine == null || chest == null
                || neck == null || head == null)
            {
                return;
            }

            Vector3 chestPosition = chest.vector3Value;
            float pelvisTop = pelvisCenter.y + pelvisSize.y * 0.5f;
            float torsoBottom = torsoCenter.y - torsoSize.y * 0.5f;
            chestPosition.y = pelvisTop
                              - seamOverlap
                              - spine.vector3Value.y
                              - torsoBottom;
            chest.vector3Value = chestPosition;

            Vector3 neckPosition = neck.vector3Value;
            float torsoTop = torsoCenter.y + torsoSize.y * 0.5f;
            float neckBottom = neckCenter.y - neckSize.y * 0.5f;
            neckPosition.y = torsoTop - seamOverlap - neckBottom;
            neck.vector3Value = neckPosition;

            Vector3 headPosition = head.vector3Value;
            float neckTop = neckCenter.y + neckSize.y * 0.5f;
            float headBottom = headCenter.y - headSize.y * 0.5f;
            headPosition.y = neckTop - seamOverlap - headBottom;
            head.vector3Value = headPosition;
        }


        private bool TryCalculateCoreConnectionTargets(
            out float chestTarget,
            out float neckTarget,
            out float headTarget)
        {
            const float seamOverlap = 0.015f;

            if (!TryGetShapeGeometry(
                    NpcRigPartId.Pelvis,
                    out Vector2 pelvisCenter,
                    out Vector2 pelvisSize)
                || !TryGetShapeGeometry(
                    NpcRigPartId.Torso,
                    out Vector2 torsoCenter,
                    out Vector2 torsoSize)
                || !TryGetShapeGeometry(
                    NpcRigPartId.Neck,
                    out Vector2 neckCenter,
                    out Vector2 neckSize)
                || !TryGetShapeGeometry(
                    NpcRigPartId.Head,
                    out Vector2 headCenter,
                    out Vector2 headSize))
            {
                chestTarget = default;
                neckTarget = default;
                headTarget = default;
                return false;
            }

            SerializedProperty spine = FindBonePlacementPosition(
                NpcRigBoneId.SpineLower);
            float spineHeight = spine != null
                ? spine.vector3Value.y
                : 0f;

            float pelvisTop = pelvisCenter.y + pelvisSize.y * 0.5f;
            float torsoBottom = torsoCenter.y - torsoSize.y * 0.5f;
            chestTarget = pelvisTop
                          - seamOverlap
                          - spineHeight
                          - torsoBottom;

            float torsoTop = torsoCenter.y + torsoSize.y * 0.5f;
            float neckBottom = neckCenter.y - neckSize.y * 0.5f;
            neckTarget = torsoTop - seamOverlap - neckBottom;

            float neckTop = neckCenter.y + neckSize.y * 0.5f;
            float headBottom = headCenter.y - headSize.y * 0.5f;
            headTarget = neckTop - seamOverlap - headBottom;
            return true;
        }


        private void ShiftCoreConnectionTargets(
            float chestDelta,
            float neckDelta,
            float headDelta)
        {
            ShiftBonePlacementHeight(
                NpcRigBoneId.Chest,
                chestDelta);
            ShiftBonePlacementHeight(
                NpcRigBoneId.Neck,
                neckDelta);
            ShiftBonePlacementHeight(
                NpcRigBoneId.Head,
                headDelta);
        }


        private void ShiftBonePlacementHeight(
            NpcRigBoneId boneId,
            float delta)
        {
            SerializedProperty position =
                FindBonePlacementPosition(boneId);

            if (position == null || Mathf.Approximately(delta, 0f))
            {
                return;
            }

            Vector3 value = position.vector3Value;
            value.y += delta;
            position.vector3Value = value;
        }


        private bool TryGetShapeGeometry(
            NpcRigPartId partId,
            out Vector2 center,
            out Vector2 size)
        {
            SerializedProperty shape = FindPartShapeEntry(partId);

            if (shape == null)
            {
                center = default;
                size = default;
                return false;
            }

            Vector3 localPosition = shape
                .FindPropertyRelative("localPosition")
                .vector3Value;
            center = new Vector2(localPosition.x, localPosition.y);
            size = shape
                .FindPropertyRelative("size")
                .vector2Value;
            return true;
        }


        private void FitLimbChain(
            NpcRigOverlayFocus chain)
        {
            NpcRigPartId[] parts;

            switch (chain)
            {
                case NpcRigOverlayFocus.FarArm:
                    parts = new[]
                    {
                        NpcRigPartId.UpperArmFar,
                        NpcRigPartId.ForearmFar,
                        NpcRigPartId.HandFar
                    };
                    break;

                case NpcRigOverlayFocus.NearLeg:
                    parts = new[]
                    {
                        NpcRigPartId.ThighNear,
                        NpcRigPartId.ShinNear,
                        NpcRigPartId.FootNear
                    };
                    break;

                case NpcRigOverlayFocus.FarLeg:
                    parts = new[]
                    {
                        NpcRigPartId.ThighFar,
                        NpcRigPartId.ShinFar,
                        NpcRigPartId.FootFar
                    };
                    break;

                default:
                    parts = new[]
                    {
                        NpcRigPartId.UpperArmNear,
                        NpcRigPartId.ForearmNear,
                        NpcRigPartId.HandNear
                    };
                    chain = NpcRigOverlayFocus.NearArm;
                    break;
            }

            for (int index = 0; index < parts.Length; index++)
            {
                FitLimbPart(parts[index]);
            }

            showRigAnatomy = true;
            rigOverlayFocus = chain;
            SetStatus(
                "The selected limb was rebuilt from its current sizes " +
                "and fitted to each joint on this unsaved working copy.",
                MessageType.Info);
        }


        private void FitLimbPart(
            NpcRigPartId partId)
        {
            if (!FitPartToJointAnchor(partId))
            {
                return;
            }

            SerializedProperty shape = FindPartShapeEntry(partId);

            Vector2 size = shape
                .FindPropertyRelative("size")
                .vector2Value;
            Vector3 euler = shape
                .FindPropertyRelative("localEulerAngles")
                .vector3Value;
            if (!TryGetDistalBone(
                    partId,
                    out NpcRigBoneId distalBone)
                || !NpcRigDefinition.TryGetPartDefinition(
                    partId,
                    out NpcRigPartDefinition partDefinition)
                || partDefinition.PlaceholderSize.y < 0.0001f)
            {
                return;
            }

            SetBonePlacementScale(
                distalBone,
                size.y / partDefinition.PlaceholderSize.y,
                Mathf.DeltaAngle(0f, euler.z));
        }


        private bool FitPartToJointAnchor(
            NpcRigPartId partId)
        {
            SerializedProperty shape = FindPartShapeEntry(partId);

            if (shape == null
                || !TryGetJointAnchoredPartPosition(
                    shape,
                    partId,
                    out Vector3 fittedPosition))
            {
                return false;
            }

            shape.FindPropertyRelative("localPosition")
                .vector3Value = fittedPosition;
            return true;
        }


        private static bool TryGetJointAnchoredPartPosition(
            SerializedProperty shape,
            NpcRigPartId partId,
            out Vector3 fittedPosition)
        {
            if (shape == null
                || !TryGetNormalizedJointAnchor(
                    partId,
                    out Vector2 normalizedAnchor))
            {
                fittedPosition = default;
                return false;
            }

            Vector2 size = shape
                .FindPropertyRelative("size")
                .vector2Value;
            Vector3 euler = shape
                .FindPropertyRelative("localEulerAngles")
                .vector3Value;
            Quaternion rotation = Quaternion.Euler(euler);
            Vector3 anchorOffset = rotation * new Vector3(
                normalizedAnchor.x * size.x,
                normalizedAnchor.y * size.y,
                0f);

            fittedPosition = -anchorOffset;
            fittedPosition.z = shape
                .FindPropertyRelative("localPosition")
                .vector3Value.z;
            return true;
        }


        private static bool TryGetNormalizedJointAnchor(
            NpcRigPartId partId,
            out Vector2 normalizedAnchor)
        {
            if (!NpcRigDefinition.TryGetPartDefinition(
                    partId,
                    out NpcRigPartDefinition definition))
            {
                normalizedAnchor = default;
                return false;
            }

            normalizedAnchor = new Vector2(
                -definition.LocalPosition.x
                / Mathf.Max(definition.PlaceholderSize.x, 0.0001f),
                -definition.LocalPosition.y
                / Mathf.Max(definition.PlaceholderSize.y, 0.0001f));
            return true;
        }


        private static bool TryGetDistalBone(
            NpcRigPartId partId,
            out NpcRigBoneId distalBone)
        {
            switch (partId)
            {
                case NpcRigPartId.UpperArmNear:
                    distalBone = NpcRigBoneId.ForearmNear;
                    return true;

                case NpcRigPartId.ForearmNear:
                    distalBone = NpcRigBoneId.HandNear;
                    return true;

                case NpcRigPartId.UpperArmFar:
                    distalBone = NpcRigBoneId.ForearmFar;
                    return true;

                case NpcRigPartId.ForearmFar:
                    distalBone = NpcRigBoneId.HandFar;
                    return true;

                case NpcRigPartId.ThighNear:
                    distalBone = NpcRigBoneId.ShinNear;
                    return true;

                case NpcRigPartId.ShinNear:
                    distalBone = NpcRigBoneId.FootNear;
                    return true;

                case NpcRigPartId.ThighFar:
                    distalBone = NpcRigBoneId.ShinFar;
                    return true;

                case NpcRigPartId.ShinFar:
                    distalBone = NpcRigBoneId.FootFar;
                    return true;

                default:
                    distalBone = default;
                    return false;
            }
        }


        private void SetBonePlacementScale(
            NpcRigBoneId boneId,
            float scale,
            float angle)
        {
            SerializedProperty position =
                FindOrCreateBonePlacementPosition(boneId);

            if (position == null
                || !NpcRigDefinition.TryGetBoneDefinition(
                    boneId,
                    out NpcRigBoneDefinition definition))
            {
                return;
            }

            position.vector3Value =
                Quaternion.Euler(0f, 0f, angle)
                * (definition.LocalPosition * scale);
        }


        private void AdjustBonePlacementLength(
            NpcRigBoneId boneId,
            float previousScale,
            float nextScale,
            float fallbackAngle)
        {
            SerializedProperty position =
                FindBonePlacementPosition(boneId);

            if (position != null && previousScale > 0.0001f)
            {
                position.vector3Value *= nextScale / previousScale;
                return;
            }

            // An unconfigured body has no authored direction to preserve.
            // In that case only, initialize it from the canonical rig.
            SetBonePlacementScale(
                boneId,
                nextScale,
                fallbackAngle);
        }


        private void DrawBonePlacement2D(
            string label,
            string horizontalLabel,
            string verticalLabel,
            NpcRigBoneId boneId,
            float minimumX,
            float maximumX,
            float minimumY,
            float maximumY)
        {
            SerializedProperty position =
                FindOrCreateBonePlacementPosition(boneId);

            if (position == null)
            {
                return;
            }

            Vector3 value = position.vector3Value;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            float nextX = EditorGUILayout.Slider(
                horizontalLabel,
                value.x,
                minimumX,
                maximumX);
            float nextY = EditorGUILayout.Slider(
                verticalLabel,
                value.y,
                minimumY,
                maximumY);

            if (!Mathf.Approximately(nextX, value.x)
                || !Mathf.Approximately(nextY, value.y))
            {
                value.x = nextX;
                value.y = nextY;
                position.vector3Value = value;
                showRigAnatomy = true;
                rigOverlayFocus = NpcRigOverlayFocus.BodyAndHead;
            }

            EditorGUILayout.EndVertical();
        }


        private void DrawBoneSpacing(
            string label,
            float minimum,
            float maximum,
            NpcRigBoneId nearId,
            NpcRigBoneId farId)
        {
            SerializedProperty near =
                FindBonePlacementPosition(nearId);
            SerializedProperty far =
                FindBonePlacementPosition(farId);

            if (near == null || far == null)
            {
                return;
            }

            float value =
                (Mathf.Abs(near.vector3Value.x)
                 + Mathf.Abs(far.vector3Value.x)) * 0.5f;

            float next = EditorGUILayout.Slider(
                label,
                value,
                minimum,
                maximum);

            if (Mathf.Approximately(next, value))
            {
                return;
            }

            Vector3 nearPosition = near.vector3Value;
            Vector3 farPosition = far.vector3Value;
            nearPosition.x = -next;
            farPosition.x = next;
            near.vector3Value = nearPosition;
            far.vector3Value = farPosition;
        }


        private void DrawSkeletonStanceControls()
        {
            DrawBoneSpacing(
                "Shoulder Spacing",
                0.04f,
                0.35f,
                NpcRigBoneId.ShoulderNear,
                NpcRigBoneId.ShoulderFar);

            DrawBonePairVerticalOffset(
                "Shoulder Height",
                "Raises or lowers both shoulder pivots relative to the " +
                "chest. The complete arm chains follow them.",
                -0.3f,
                0.4f,
                NpcRigBoneId.ShoulderNear,
                NpcRigBoneId.ShoulderFar);

            DrawBonePairVerticalSpread(
                "Shoulder Depth Offset",
                "Separates the shoulder pivots vertically for isometric " +
                "depth. Positive raises the background shoulder and " +
                "lowers the foreground shoulder; negative reverses them. " +
                "Their shared height stays unchanged.",
                -0.2f,
                0.2f,
                NpcRigBoneId.ShoulderNear,
                NpcRigBoneId.ShoulderFar);

            EditorGUILayout.Space(3f);

            DrawBoneSpacing(
                "Leg Spacing",
                0.02f,
                0.25f,
                NpcRigBoneId.ThighNear,
                NpcRigBoneId.ThighFar);

            DrawBonePairVerticalOffset(
                "Hip Pivot Height",
                "Raises or lowers both hip/thigh pivots relative to the " +
                "pelvis. The complete leg chains follow them.",
                -0.35f,
                0.3f,
                NpcRigBoneId.ThighNear,
                NpcRigBoneId.ThighFar);

            DrawBonePairVerticalSpread(
                "Hip Depth Offset",
                "Separates the hip/thigh pivots vertically for isometric " +
                "depth. Positive raises the background hip and lowers " +
                "the foreground hip; negative reverses them. Their shared " +
                "height stays unchanged.",
                -0.2f,
                0.2f,
                NpcRigBoneId.ThighNear,
                NpcRigBoneId.ThighFar);
        }


        private void DrawBonePairVerticalOffset(
            string label,
            string tooltip,
            float minimum,
            float maximum,
            NpcRigBoneId nearId,
            NpcRigBoneId farId)
        {
            SerializedProperty near =
                FindBonePlacementPosition(nearId);
            SerializedProperty far =
                FindBonePlacementPosition(farId);

            if (near == null || far == null)
            {
                return;
            }

            Vector3 nearPosition = near.vector3Value;
            Vector3 farPosition = far.vector3Value;
            float value = (nearPosition.y + farPosition.y) * 0.5f;
            float next = EditorGUILayout.Slider(
                new GUIContent(label, tooltip),
                value,
                minimum,
                maximum);

            if (Mathf.Approximately(next, value))
            {
                return;
            }

            // Apply a shared delta instead of flattening both Y values. This
            // preserves any authored near/far height difference between the
            // the two stable depth anchors.
            float delta = next - value;
            nearPosition.y += delta;
            farPosition.y += delta;
            near.vector3Value = nearPosition;
            far.vector3Value = farPosition;
        }


        private void DrawBonePairVerticalSpread(
            string label,
            string tooltip,
            float minimum,
            float maximum,
            NpcRigBoneId nearId,
            NpcRigBoneId farId)
        {
            SerializedProperty near =
                FindBonePlacementPosition(nearId);
            SerializedProperty far =
                FindBonePlacementPosition(farId);

            if (near == null || far == null)
            {
                return;
            }

            Vector3 nearPosition = near.vector3Value;
            Vector3 farPosition = far.vector3Value;
            float center = (nearPosition.y + farPosition.y) * 0.5f;
            float value = (farPosition.y - nearPosition.y) * 0.5f;
            float next = EditorGUILayout.Slider(
                new GUIContent(label, tooltip),
                value,
                minimum,
                maximum);

            if (Mathf.Approximately(next, value))
            {
                return;
            }

            // Change the signed near/far separation without moving the
            // pair's shared height. Horizontal shoulder spacing is untouched.
            nearPosition.y = center - next;
            farPosition.y = center + next;
            near.vector3Value = nearPosition;
            far.vector3Value = farPosition;
        }


        private void DrawSleeveStyle()
        {
            SerializedProperty nearRole = FindOutfitColorRole(
                NpcRigPartId.ForearmNear);
            SerializedProperty farRole = FindOutfitColorRole(
                NpcRigPartId.ForearmFar);

            if (nearRole == null || farRole == null)
            {
                return;
            }

            bool shortSleeves =
                nearRole.enumValueIndex
                == (int)NpcAppearanceColorRole.Skin;

            int next = EditorGUILayout.Popup(
                "Sleeves",
                shortSleeves ? 1 : 0,
                new[] { "Long Sleeves", "Short Sleeves" });

            int nextRole = next == 1
                ? (int)NpcAppearanceColorRole.Skin
                : (int)NpcAppearanceColorRole.PrimaryFabric;

            nearRole.enumValueIndex = nextRole;
            farRole.enumValueIndex = nextRole;
        }


        private void DrawSaveControls()
        {
            bool valid = TryValidateWorkingAsset(out _);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           selectedAsset == null
                           || !HasUnsavedChanges()
                           || !valid))
                {
                    if (GUILayout.Button(
                        "Save Changes to Selected",
                            GUILayout.Height(28f)))
                    {
                        SaveCurrentAsset();
                    }
                }

                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (GUILayout.Button(
                            "Save as New Asset",
                            GUILayout.Height(28f)))
                    {
                        SaveAsNewAsset();
                    }
                }
            }

            EditorGUILayout.LabelField(
                "Save Changes updates the selected asset and every " +
                "Population Definition that references it. Save as New " +
                "Asset creates an independent library entry and leaves " +
                "the selected asset untouched.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawValidation()
        {
            if (TryValidateWorkingAsset(out string reason))
            {
                EditorGUILayout.HelpBox(
                    GetCategoryLabel() + " is valid.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }


        private void ApplyPreview()
        {
            if (workingAsset == null
                || previewCanvas == null
                || defaultAppearance == null)
            {
                return;
            }

            SynchronizeGenderFromBody();

            NpcBodySilhouette body =
                category == NpcAppearanceAssetCategory.Body
                    ? workingAsset as NpcBodySilhouette
                    : FindBody(previewGender);

            NpcSkinPalette skin =
                category == NpcAppearanceAssetCategory.Skin
                    ? workingAsset as NpcSkinPalette
                    : defaultAppearance.SkinPalette;

            NpcOutfitSet outfit =
                category == NpcAppearanceAssetCategory.Outfit
                    ? workingAsset as NpcOutfitSet
                    : FindOutfit(previewGender);

            NpcHairSet hair =
                category == NpcAppearanceAssetCategory.Hair
                    ? workingAsset as NpcHairSet
                    : FindHair(previewGender);

            if (body == null || skin == null || outfit == null || hair == null)
            {
                SetStatus(
                    "The catalog lacks a complete preview context for " +
                    previewGender + ".",
                    MessageType.Warning);
                return;
            }

            previewCanvas.Apply(
                new NpcAppearanceSelection(
                    previewGender,
                    body,
                    skin,
                    outfit,
                    hair),
                facing);
        }


        private void LoadFirstAsset()
        {
            List<ScriptableObject> assets = GetCategoryAssets();
            LoadAsset(assets.Count > 0 ? assets[0] : null);
        }


        private void LoadAsset(
            ScriptableObject asset)
        {
            DestroyWorkingCopy();
            selectedAsset = asset;

            if (asset == null)
            {
                return;
            }

            workingAsset = Instantiate(asset);
            workingAsset.name = asset.name + " Working Copy";
            // HideAndDontSave includes NotEditable. DontSave keeps this
            // temporary working copy out of assets while allowing the
            // serialized controls in this window to edit it.
            workingAsset.hideFlags = HideFlags.DontSave;
            serializedWorkingAsset = new SerializedObject(workingAsset);
            loadedJson = EditorJsonUtility.ToJson(workingAsset);
            SynchronizeGenderFromBody();
            ApplyPreview();
            SetStatus(string.Empty, MessageType.Info);
        }


        private void BeginNewAsset()
        {
            selectedAsset = null;
            loadedJson = string.Empty;

            SerializedProperty displayName =
                serializedWorkingAsset.FindProperty("displayName");
            displayName.stringValue =
                "New " + GetCategoryLabel();
            serializedWorkingAsset.ApplyModifiedProperties();
            workingAsset.name = "New " + GetCategoryLabel();
            ApplyPreview();
        }


        private void SaveCurrentAsset()
        {
            if (selectedAsset == null || workingAsset == null)
            {
                return;
            }

            Undo.RecordObject(
                selectedAsset,
                "Edit " + GetCategoryLabel());
            EditorUtility.CopySerialized(workingAsset, selectedAsset);
            EditorUtility.SetDirty(selectedAsset);
            AssetDatabase.SaveAssets();

            string savedName = GetDisplayName(selectedAsset);
            LoadAsset(selectedAsset);
            SetStatus(savedName + " saved.", MessageType.Info);
        }


        private void SaveAsNewAsset()
        {
            if (workingAsset == null)
            {
                return;
            }

            EnsureCategoryFolder();

            ScriptableObject asset = Instantiate(workingAsset);
            asset.hideFlags = HideFlags.None;

            string displayName = GetDisplayName(asset);
            string fileName = CreateSafeFileName(displayName);
            string path = AssetDatabase.GenerateUniqueAssetPath(
                GetCategoryFolder() + "/" + fileName + ".asset");

            asset.name = fileName;
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(
                asset,
                "Create " + GetCategoryLabel());

            Undo.RecordObject(
                catalog,
                "Register " + GetCategoryLabel());
            RegisterAsset(asset);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            selectedAsset = asset;
            LoadAsset(asset);
            SetStatus(
                displayName + " was added to the appearance library.",
                MessageType.Info);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }


        private bool TryValidateWorkingAsset(
            out string reason)
        {
            if (workingAsset == null)
            {
                reason = "No working asset is loaded.";
                return false;
            }

            switch (workingAsset)
            {
                case NpcBodySilhouette body:
                    return body.TryValidate(out reason);

                case NpcSkinPalette _:
                    reason = string.Empty;
                    return true;

                case NpcOutfitSet outfit:
                    return outfit.TryValidate(out reason);

                case NpcHairSet hair:
                    return hair.TryValidate(out reason);

                default:
                    reason = "Unsupported appearance asset type.";
                    return false;
            }
        }


        private void FindLibraries()
        {
            string[] catalogGuids =
                AssetDatabase.FindAssets("t:NpcAppearanceCatalog");

            catalog = catalogGuids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(
                    AssetDatabase.GUIDToAssetPath(catalogGuids[0]))
                : null;

            defaultAppearance =
                AssetDatabase.LoadAssetAtPath<NpcAppearanceProfile>(
                    DefaultAppearancePath);
        }


        private List<ScriptableObject> GetCategoryAssets()
        {
            List<ScriptableObject> assets =
                new List<ScriptableObject>();

            if (catalog == null)
            {
                return assets;
            }

            switch (category)
            {
                case NpcAppearanceAssetCategory.Body:
                    AddAssets(assets, catalog.Bodies);
                    break;

                case NpcAppearanceAssetCategory.Skin:
                    AddAssets(assets, catalog.Skins);
                    break;

                case NpcAppearanceAssetCategory.Outfit:
                    AddAssets(assets, catalog.Outfits);
                    break;

                case NpcAppearanceAssetCategory.Hair:
                    AddAssets(assets, catalog.Hair);
                    break;
            }

            AddAssetsFromCategoryFolder(assets);
            assets.Sort(
                (left, right) => string.Compare(
                    GetDisplayName(left),
                    GetDisplayName(right),
                    StringComparison.OrdinalIgnoreCase));

            return assets;
        }


        private void AddAssetsFromCategoryFolder(
            List<ScriptableObject> assets)
        {
            if (!AssetDatabase.IsValidFolder(GetCategoryFolder()))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:" + GetCategoryTypeName(),
                new[] { GetCategoryFolder() });

            for (int index = 0; index < guids.Length; index++)
            {
                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                        AssetDatabase.GUIDToAssetPath(guids[index]));

                if (asset != null && !assets.Contains(asset))
                {
                    assets.Add(asset);
                }
            }
        }


        private NpcBodySilhouette FindBody(
            NpcPersonGender gender)
        {
            for (int index = 0; index < catalog.Bodies.Count; index++)
            {
                NpcBodySilhouette asset = catalog.Bodies[index];

                if (asset != null && asset.Supports(gender))
                {
                    return asset;
                }
            }

            return null;
        }


        private NpcOutfitSet FindOutfit(
            NpcPersonGender gender)
        {
            NpcOutfitSet preferred = defaultAppearance.OutfitSet;

            if (preferred != null && preferred.Supports(gender))
            {
                return preferred;
            }

            for (int index = 0; index < catalog.Outfits.Count; index++)
            {
                NpcOutfitSet asset = catalog.Outfits[index];

                if (asset != null && asset.Supports(gender))
                {
                    return asset;
                }
            }

            return null;
        }


        private NpcHairSet FindHair(
            NpcPersonGender gender)
        {
            NpcHairSet preferred = defaultAppearance.HairSet;

            if (preferred != null && preferred.Supports(gender))
            {
                return preferred;
            }

            for (int index = 0; index < catalog.Hair.Count; index++)
            {
                NpcHairSet asset = catalog.Hair[index];

                if (asset != null && asset.Supports(gender))
                {
                    return asset;
                }
            }

            return null;
        }


        private void SynchronizeGenderFromBody()
        {
            if (category == NpcAppearanceAssetCategory.Body
                && workingAsset is NpcBodySilhouette body)
            {
                previewGender = body.Gender;
            }
        }


        private List<SerializedProperty> FindPartShapeEntries(
            NpcRigPartId[] ids)
        {
            List<SerializedProperty> shapes =
                new List<SerializedProperty>();

            for (int index = 0; index < ids.Length; index++)
            {
                SerializedProperty shape = FindPartShapeEntry(ids[index]);

                if (shape != null)
                {
                    shapes.Add(shape);
                }
            }

            return shapes;
        }


        private SerializedProperty FindPartShapeEntry(
            NpcRigPartId requestedId)
        {
            SerializedProperty shapes =
                serializedWorkingAsset.FindProperty("partShapes");

            for (int index = 0; index < shapes.arraySize; index++)
            {
                SerializedProperty shape =
                    shapes.GetArrayElementAtIndex(index);

                if (shape.FindPropertyRelative("id").enumValueIndex
                    == (int)requestedId)
                {
                    return shape;
                }
            }

            return null;
        }


        private SerializedProperty FindBonePlacementPosition(
            NpcRigBoneId requestedId)
        {
            SerializedProperty placements =
                serializedWorkingAsset.FindProperty("bonePlacements");

            for (int index = 0;
                 index < placements.arraySize;
                 index++)
            {
                SerializedProperty placement =
                    placements.GetArrayElementAtIndex(index);

                if (placement.FindPropertyRelative("id").enumValueIndex
                    == (int)requestedId)
                {
                    return placement.FindPropertyRelative(
                        "localPosition");
                }
            }

            return null;
        }


        private SerializedProperty FindOrCreateBonePlacementPosition(
            NpcRigBoneId requestedId)
        {
            SerializedProperty existing =
                FindBonePlacementPosition(requestedId);

            if (existing != null)
            {
                return existing;
            }

            if (!NpcRigDefinition.TryGetBoneDefinition(
                    requestedId,
                    out NpcRigBoneDefinition definition))
            {
                return null;
            }

            SerializedProperty placements =
                serializedWorkingAsset.FindProperty("bonePlacements");
            int newIndex = placements.arraySize;
            placements.arraySize++;

            SerializedProperty placement =
                placements.GetArrayElementAtIndex(newIndex);
            placement.FindPropertyRelative("id").enumValueIndex =
                (int)requestedId;
            SerializedProperty position =
                placement.FindPropertyRelative("localPosition");
            position.vector3Value = definition.LocalPosition;
            return position;
        }


        private SerializedProperty FindOutfitColorRole(
            NpcRigPartId requestedId)
        {
            SerializedProperty style =
                FindOutfitPartStyle(requestedId);

            return style?.FindPropertyRelative("colorRole");
        }


        private SerializedProperty FindOutfitPartStyle(
            NpcRigPartId requestedId)
        {
            SerializedProperty styles =
                serializedWorkingAsset.FindProperty("partStyles");

            for (int index = 0; index < styles.arraySize; index++)
            {
                SerializedProperty style =
                    styles.GetArrayElementAtIndex(index);

                if (style.FindPropertyRelative("id").enumValueIndex
                    == (int)requestedId)
                {
                    return style;
                }
            }

            return null;
        }


        private void RegisterAsset(
            ScriptableObject asset)
        {
            switch (asset)
            {
                case NpcBodySilhouette body:
                    catalog.RegisterAsset(body);
                    break;

                case NpcSkinPalette skin:
                    catalog.RegisterAsset(skin);
                    break;

                case NpcOutfitSet outfit:
                    catalog.RegisterAsset(outfit);
                    break;

                case NpcHairSet hair:
                    catalog.RegisterAsset(hair);
                    break;
            }
        }


        private bool ConfirmDiscardWorkingChanges()
        {
            return !HasUnsavedChanges()
                   || EditorUtility.DisplayDialog(
                       "Discard working-copy changes?",
                       "This appearance has unsaved changes. Discard them " +
                       "and switch assets?",
                       "Discard",
                       "Cancel");
        }


        private bool HasUnsavedChanges()
        {
            if (workingAsset == null)
            {
                return false;
            }

            if (selectedAsset == null)
            {
                return true;
            }

            return EditorJsonUtility.ToJson(workingAsset) != loadedJson;
        }


        private void DestroyWorkingCopy()
        {
            serializedWorkingAsset = null;

            if (workingAsset != null)
            {
                DestroyImmediate(workingAsset);
                workingAsset = null;
            }

            loadedJson = string.Empty;
        }


        private NpcPreviewFocus GetPreviewFocus()
        {
            switch (category)
            {
                case NpcAppearanceAssetCategory.Hair:
                    return NpcPreviewFocus.Head;

                case NpcAppearanceAssetCategory.Skin:
                    return NpcPreviewFocus.UpperBody;

                default:
                    return NpcPreviewFocus.FullBody;
            }
        }


        private string GetCategoryFolder()
        {
            switch (category)
            {
                case NpcAppearanceAssetCategory.Body:
                    return AppearanceRoot + "/Bodies";

                case NpcAppearanceAssetCategory.Skin:
                    return AppearanceRoot + "/Skin Palettes";

                case NpcAppearanceAssetCategory.Outfit:
                    return AppearanceRoot + "/Outfits";

                default:
                    return AppearanceRoot + "/Hair";
            }
        }


        private string GetCategoryTypeName()
        {
            switch (category)
            {
                case NpcAppearanceAssetCategory.Body:
                    return nameof(NpcBodySilhouette);

                case NpcAppearanceAssetCategory.Skin:
                    return nameof(NpcSkinPalette);

                case NpcAppearanceAssetCategory.Outfit:
                    return nameof(NpcOutfitSet);

                default:
                    return nameof(NpcHairSet);
            }
        }


        private void ShowCategoryFolder()
        {
            EnsureCategoryFolder();

            DefaultAsset folder =
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    GetCategoryFolder());

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }


        private string GetCategoryLabel()
        {
            switch (category)
            {
                case NpcAppearanceAssetCategory.Body:
                    return "Body Silhouette";

                case NpcAppearanceAssetCategory.Skin:
                    return "Skin Palette";

                case NpcAppearanceAssetCategory.Outfit:
                    return "Outfit Set";

                default:
                    return "Hair Set";
            }
        }


        private void EnsureCategoryFolder()
        {
            string path = GetCategoryFolder();

            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.CreateFolder(
                AppearanceRoot,
                path.Substring(path.LastIndexOf('/') + 1));
        }


        private void SetStatus(
            string message,
            MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }


        private static void AddAssets<T>(
            List<ScriptableObject> destination,
            IReadOnlyList<T> source)
            where T : ScriptableObject
        {
            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] != null)
                {
                    destination.Add(source[index]);
                }
            }
        }


        private static string GetDisplayName(
            ScriptableObject asset)
        {
            switch (asset)
            {
                case NpcBodySilhouette body:
                    return body.DisplayName;

                case NpcSkinPalette skin:
                    return skin.DisplayName;

                case NpcOutfitSet outfit:
                    return outfit.DisplayName;

                case NpcHairSet hair:
                    return hair.DisplayName;

                default:
                    return asset != null ? asset.name : "Missing";
            }
        }


        private static string CreateSafeFileName(
            string displayName)
        {
            char[] characters = new char[displayName.Length];
            int length = 0;

            for (int index = 0; index < displayName.Length; index++)
            {
                char character = displayName[index];

                if (char.IsLetterOrDigit(character))
                {
                    characters[length++] = character;
                }
            }

            return length > 0
                ? new string(characters, 0, length)
                : "NewAppearanceAsset";
        }
    }
}
