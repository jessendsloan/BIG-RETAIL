using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BigRetail.Characters.Editor
{
    internal enum NpcPopulationPreviewSource
    {
        PopulationDefinition = 0,
        AppearanceLibrary = 1
    }


    internal enum NpcPopulationPreviewAnimation
    {
        BindPose = 0,
        Idle = 1,
        Walk = 2
    }


    /// <summary>
    /// Showroom for either spawn-authorized population options or every saved
    /// appearance-library asset. Preview people live in Unity's hidden preview
    /// scene. A deliberate workbench action may place a copy in the open scene,
    /// but this window never edits the shared Person prefab or saves a scene.
    /// </summary>
    public sealed class NpcPopulationPreviewerWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Population/Previewer";

        private const string PersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";

        private const string PersonSouthFacingIdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle_SouthFacing.anim";

        private const string PersonNorthFacingIdleClipPath =
            "Assets/Animations/Characters/Core/Person_Idle_NorthFacing.anim";

        private const string PersonSouthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_SouthFacing.anim";

        private const string PersonNorthFacingWalkClipPath =
            "Assets/Animations/Characters/Core/Person_Walk_NorthFacing.anim";

        private const string AppearanceRoot =
            "Assets/Art/Characters/Appearance";

        private const string BodiesFolder =
            AppearanceRoot + "/Bodies";

        private const string SkinsFolder =
            AppearanceRoot + "/Skin Palettes";

        private const string OutfitsFolder =
            AppearanceRoot + "/Outfits";

        private const string HairFolder =
            AppearanceRoot + "/Hair";

        private static readonly string[] FacingLabels =
        {
            "North West",
            "North East",
            "South West",
            "South East"
        };

        private static readonly NpcFacing[] Facings =
        {
            NpcFacing.NorthWest,
            NpcFacing.NorthEast,
            NpcFacing.SouthWest,
            NpcFacing.SouthEast
        };

        private static readonly string[] AnimationLabels =
        {
            "Bind Pose",
            "Idle",
            "Walk"
        };

        private readonly GUIContent populationLabel =
            new GUIContent(
                "Population Definition",
                "Only appearance assets allowed by this population are " +
                "available below.");

        private NpcAppearanceCatalog catalog;
        private NpcPopulationPreviewSource previewSource;
        private NpcPopulationDefinition selectedDefinition;
        private NpcPersonGender selectedGender = NpcPersonGender.Man;
        private NpcBodySilhouette selectedBody;
        private NpcSkinPalette selectedSkin;
        private NpcOutfitSet selectedOutfit;
        private NpcHairSet selectedHair;
        private NpcFacing facing = NpcFacing.SouthEast;
        private NpcPopulationPreviewAnimation selectedAnimation =
            NpcPopulationPreviewAnimation.Walk;

        private readonly List<NpcBodySilhouette> libraryBodies =
            new List<NpcBodySilhouette>();

        private readonly List<NpcSkinPalette> librarySkins =
            new List<NpcSkinPalette>();

        private readonly List<NpcOutfitSet> libraryOutfits =
            new List<NpcOutfitSet>();

        private readonly List<NpcHairSet> libraryHair =
            new List<NpcHairSet>();

        private PreviewRenderUtility previewUtility;
        private GameObject previewPerson;
        private NpcCutoutRig previewRig;
        private NpcAppearanceProfile previewProfile;
        private Texture previewTexture;
        private AnimationClip southFacingIdleClip;
        private AnimationClip northFacingIdleClip;
        private AnimationClip southFacingWalkClip;
        private AnimationClip northFacingWalkClip;
        private Hash128 personPrefabDependencyHash;
        private bool hasPersonPrefabDependencyHash;
        private bool personRigReloadQueued;

        private readonly List<PreviewTransformPose> bindPose =
            new List<PreviewTransformPose>();
        private Bounds bindPoseBounds;
        private bool hasBindPoseBounds;

        private Vector2 scrollPosition;
        private float zoom = 1f;
        private float animationTime;
        private float playbackSpeed = 1f;
        private bool animationPlaying = true;
        private bool loopAnimation = true;
        private bool showRigAnatomy;
        private double lastEditorUpdateTime;
        private int randomSeed;
        private string statusMessage;
        private MessageType statusType = MessageType.Info;


        private readonly struct PreviewTransformPose
        {
            public PreviewTransformPose(Transform transform)
            {
                Transform = transform;
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
                LocalScale = transform.localScale;
            }


            public Transform Transform { get; }


            public Vector3 LocalPosition { get; }


            public Quaternion LocalRotation { get; }


            public Vector3 LocalScale { get; }
        }


        [MenuItem(MenuPath)]
        public static void Open()
        {
            NpcPopulationPreviewerWindow window =
                GetWindow<NpcPopulationPreviewerWindow>(
                    "Population Previewer");

            window.minSize = new Vector2(820f, 520f);
            window.Show();
        }


        private void OnEnable()
        {
            LoadPreviewAnimations();
            randomSeed = Environment.TickCount;
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            FindCatalog();
            RefreshAppearanceLibrary();
            SelectFirstDefinition();
            EnsurePreviewScene();
            ApplyCurrentAppearance();
        }


        private void OnFocus()
        {
            QueuePersonRigReloadIfDependencyChanged();
            RefreshAppearanceLibrary();

            if (previewSource
                == NpcPopulationPreviewSource.AppearanceLibrary)
            {
                SelectFirstGender();
                PreserveOrSelectLibraryChoices();
                ApplyCurrentAppearance();
            }
        }


        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update -= ReloadPersonRigWhenEditorReady;
            EditorApplication.projectChanged -= OnProjectChanged;
            personRigReloadQueued = false;
            CleanupPreviewScene();
        }


        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update -= ReloadPersonRigWhenEditorReady;
            EditorApplication.projectChanged -= OnProjectChanged;
            personRigReloadQueued = false;
            CleanupPreviewScene();
        }

        private void OnProjectChanged()
        {
            QueuePersonRigReloadIfDependencyChanged();
        }

        private void QueuePersonRigReloadIfDependencyChanged()
        {
            Hash128 currentHash =
                AssetDatabase.GetAssetDependencyHash(PersonPrefabPath);

            if (!hasPersonPrefabDependencyHash)
            {
                personPrefabDependencyHash = currentHash;
                hasPersonPrefabDependencyHash = true;
                return;
            }

            if (currentHash == personPrefabDependencyHash)
            {
                return;
            }

            QueuePersonRigReload();
        }

        private void QueuePersonRigReload()
        {
            if (personRigReloadQueued)
            {
                return;
            }

            personRigReloadQueued = true;
            EditorApplication.update -= ReloadPersonRigWhenEditorReady;
            EditorApplication.update += ReloadPersonRigWhenEditorReady;
        }

        private void ReloadPersonRigWhenEditorReady()
        {
            if (EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                return;
            }

            EditorApplication.update -= ReloadPersonRigWhenEditorReady;
            personRigReloadQueued = false;
            ReloadPersonRig(
                "Person rig reloaded from the latest project assets.");
        }

        private void ReloadPersonRig(string successMessage)
        {
            CleanupPreviewScene();
            EnsurePreviewScene();

            if (previewRig == null || previewProfile == null)
            {
                Repaint();
                return;
            }

            ApplyCurrentAppearance();
            SetStatus(successMessage, MessageType.Info);
            Repaint();
        }


        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Big Retail Population Previewer",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Preview spawn-authorized Population options or every saved " +
                "asset in the Appearance Library. The person on the right " +
                "exists only inside this window unless you deliberately " +
                "place it in the open scene with the Unity AI workbench " +
                "button below. Prefabs are never changed.",
                MessageType.Info);

            if (catalog == null)
            {
                DrawMissingCatalog();
                return;
            }

            float controlPanelWidth = Mathf.Clamp(
                position.width * 0.34f,
                360f,
                520f);

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
                    EditorGUIUtility.labelWidth = 82f;

                    DrawPreviewSourceSelector();

                    if (previewSource
                        == NpcPopulationPreviewSource.PopulationDefinition)
                    {
                        DrawPopulationSelector();
                    }
                    else
                    {
                        DrawLibrarySource();
                    }

                    DrawGenderSelector();
                    EditorGUILayout.Space(10f);
                    DrawFacingSelector();
                    EditorGUILayout.Space(10f);
                    DrawAnimationControls();
                    EditorGUILayout.Space(10f);
                    DrawAppearanceSelectors();
                    EditorGUILayout.Space(10f);
                    DrawRandomControls();
                    EditorGUILayout.Space(10f);
                    DrawPersonRigReloadControls();
                    EditorGUILayout.Space(10f);
                    DrawSceneWorkbenchControls();

                    if (!string.IsNullOrWhiteSpace(statusMessage))
                    {
                        EditorGUILayout.Space(6f);
                        EditorGUILayout.HelpBox(
                            statusMessage,
                            statusType);
                    }

                    EditorGUIUtility.labelWidth = previousLabelWidth;
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope(
                           GUILayout.ExpandWidth(true),
                           GUILayout.ExpandHeight(true)))
                {
                    DrawPreviewViewport();
                }
            }
        }


        private void DrawPreviewSourceSelector()
        {
            EditorGUILayout.LabelField(
                "Preview Source",
                EditorStyles.boldLabel);

            int nextSource = GUILayout.Toolbar(
                (int)previewSource,
                new[]
                {
                    "Population Definition",
                    "Appearance Library"
                },
                GUILayout.Height(25f));

            if (nextSource == (int)previewSource)
            {
                return;
            }

            previewSource =
                (NpcPopulationPreviewSource)nextSource;

            if (previewSource
                == NpcPopulationPreviewSource.AppearanceLibrary)
            {
                RefreshAppearanceLibrary();
            }

            SelectFirstGender();
            SelectFirstCompatibleChoices();
            ApplyCurrentAppearance();
        }


        private void DrawLibrarySource()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "Asset Folder",
                    AppearanceRoot);

                if (GUILayout.Button(
                        "Refresh",
                        GUILayout.Width(75f)))
                {
                    RefreshAppearanceLibrary();
                    PreserveOrSelectLibraryChoices();
                    ApplyCurrentAppearance();
                }

                if (GUILayout.Button(
                        "Open Folder",
                        GUILayout.Width(90f)))
                {
                    DefaultAsset folder =
                        AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                            AppearanceRoot);

                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                }
            }

            EditorGUILayout.LabelField(
                "Every saved appearance asset is available here. An asset " +
                "still must be added to a Population Definition before the " +
                "simulation may generate it.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawGenderSelector()
        {
            if (previewSource
                    == NpcPopulationPreviewSource.PopulationDefinition
                && selectedDefinition == null)
            {
                return;
            }

            List<NpcPersonGender> available =
                new List<NpcPersonGender>();

            if (AllowsGender(NpcPersonGender.Man))
            {
                available.Add(NpcPersonGender.Man);
            }

            if (AllowsGender(NpcPersonGender.Woman))
            {
                available.Add(NpcPersonGender.Woman);
            }

            if (available.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This preview source has no complete appearance for " +
                    "men or women.",
                    MessageType.Warning);
                return;
            }

            int currentIndex = available.IndexOf(selectedGender);

            if (currentIndex < 0)
            {
                currentIndex = 0;
                selectedGender = available[0];
            }

            string[] names = new string[available.Count];

            for (int index = 0; index < available.Count; index++)
            {
                names[index] = available[index].ToString();
            }

            int nextIndex = EditorGUILayout.Popup(
                "Gender",
                currentIndex,
                names);

            if (nextIndex == currentIndex)
            {
                return;
            }

            selectedGender = available[nextIndex];
            SelectFirstCompatibleChoices();
            ApplyCurrentAppearance();
        }


        private void DrawMissingCatalog()
        {
            EditorGUILayout.HelpBox(
                "No population catalog was found. Open Population " +
                "Definitions to inspect the setup, or repair the starter " +
                "content if the catalog was removed.",
                MessageType.Warning);

            if (GUILayout.Button("Open Population Definitions"))
            {
                NpcPopulationDefinitionsWindow.Open();
            }

            if (GUILayout.Button("Repair Starter Content"))
            {
                NpcPopulationStarterFactory
                    .CreateOrUpdateStarterCatalog();
                FindCatalog();
                SelectFirstDefinition();
                EnsurePreviewScene();
                ApplyCurrentAppearance();
            }
        }


        private void DrawPopulationSelector()
        {
            List<NpcPopulationDefinition> definitions =
                GetAvailableDefinitions();

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The appearance catalog has no population definitions.",
                    MessageType.Warning);
                return;
            }

            string[] names = new string[definitions.Count];
            int currentIndex = 0;

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcPopulationDefinition definition = definitions[index];
                names[index] = GetDisplayName(
                    definition.DisplayName,
                    definition);

                if (definition == selectedDefinition)
                {
                    currentIndex = index;
                }
            }

            int selectedIndex = EditorGUILayout.Popup(
                populationLabel,
                currentIndex,
                names);

            if (definitions[selectedIndex] != selectedDefinition)
            {
                SelectDefinition(definitions[selectedIndex]);
            }
        }


        private void DrawPreviewViewport()
        {
            Rect previewRect = GUILayoutUtility.GetRect(
                240f,
                300f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            EditorGUI.DrawRect(
                previewRect,
                new Color(0.075f, 0.095f, 0.12f, 1f));

            HandlePreviewZoom(previewRect);

            if (previewRig == null)
            {
                EditorGUI.LabelField(
                    previewRect,
                    "The shared Person preview could not be loaded.",
                    CenteredLabelStyle());
                return;
            }

            RenderPreview(previewRect);

            if (previewTexture != null)
            {
                GUI.DrawTexture(
                    previewRect,
                    previewTexture,
                    ScaleMode.StretchToFill,
                    false);
            }

            if (showRigAnatomy)
            {
                DrawRigAnatomy(previewRect);
            }

            Rect hintRect = previewRect;
            hintRect.height = 20f;
            hintRect.y = previewRect.yMax - hintRect.height - 6f;
            hintRect.xMin += 8f;
            hintRect.xMax -= 8f;

            GUI.Label(
                hintRect,
                "Mouse wheel: zoom",
                EditorStyles.centeredGreyMiniLabel);
        }


        private void DrawFacingSelector()
        {
            EditorGUILayout.LabelField(
                "Facing",
                EditorStyles.boldLabel);

            int currentFacing = Array.IndexOf(Facings, facing);
            int nextFacing = GUILayout.SelectionGrid(
                Mathf.Max(0, currentFacing),
                FacingLabels,
                2,
                GUILayout.Height(44f));

            if (nextFacing == currentFacing)
            {
                return;
            }

            RestoreBindPose();
            facing = Facings[nextFacing];
            previewRig?.SetFacing(facing);
            CaptureBindPose();
            EvaluateSelectedAnimation();
            Repaint();
        }


        private void DrawAnimationControls()
        {
            EditorGUILayout.LabelField(
                "Animation Preview",
                EditorStyles.boldLabel);

            int nextAnimation = EditorGUILayout.Popup(
                "Animation",
                (int)selectedAnimation,
                AnimationLabels);

            if (nextAnimation != (int)selectedAnimation)
            {
                selectedAnimation =
                    (NpcPopulationPreviewAnimation)nextAnimation;
                animationTime = 0f;
                animationPlaying =
                    selectedAnimation
                    != NpcPopulationPreviewAnimation.BindPose;
                lastEditorUpdateTime =
                    EditorApplication.timeSinceStartup;
                EvaluateSelectedAnimation();
                Repaint();
            }

            AnimationClip clip = GetSelectedAnimationClip();

            using (new EditorGUI.DisabledScope(clip == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string playLabel = animationPlaying
                        ? "Pause"
                        : "Play";

                    if (GUILayout.Button(playLabel))
                    {
                        animationPlaying = !animationPlaying;
                        lastEditorUpdateTime =
                            EditorApplication.timeSinceStartup;
                    }

                    if (GUILayout.Button("Restart"))
                    {
                        animationTime = 0f;
                        animationPlaying = true;
                        lastEditorUpdateTime =
                            EditorApplication.timeSinceStartup;
                        EvaluateSelectedAnimation();
                        Repaint();
                    }
                }

                loopAnimation = EditorGUILayout.Toggle(
                    "Loop",
                    loopAnimation);

                playbackSpeed = EditorGUILayout.Slider(
                    "Speed",
                    playbackSpeed,
                    0.25f,
                    2f);

                float nextTime = EditorGUILayout.Slider(
                    "Timeline",
                    animationTime,
                    0f,
                    clip != null ? clip.length : 1f);

                if (!Mathf.Approximately(nextTime, animationTime))
                {
                    animationTime = nextTime;
                    animationPlaying = false;
                    lastEditorUpdateTime =
                        EditorApplication.timeSinceStartup;
                    EvaluateSelectedAnimation();
                    Repaint();
                }
            }

            if (clip == null)
            {
                EditorGUILayout.LabelField(
                    selectedAnimation
                    == NpcPopulationPreviewAnimation.BindPose
                        ? "The unanimated authored pose."
                        : "The selected Core animation clip is missing.",
                    EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(
                    FormatAnimationTime(animationTime) + " / " +
                    FormatAnimationTime(clip.length) + "    " +
                    Mathf.RoundToInt(clip.frameRate) + " fps",
                    EditorStyles.miniLabel);
            }

            showRigAnatomy = EditorGUILayout.Toggle(
                new GUIContent(
                    "Show Rig Anatomy",
                    "Draw the live animated bone chain over the preview."),
                showRigAnatomy);

            EditorGUILayout.LabelField(
                "Playback is temporary. It never edits the clip, Person " +
                "prefab, scene, or Population Definition.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawAppearanceSelectors()
        {
            EditorGUILayout.LabelField(
                previewSource
                    == NpcPopulationPreviewSource.PopulationDefinition
                    ? "Allowed Population Options"
                    : "Saved Appearance Assets",
                EditorStyles.boldLabel);

            if (previewSource
                    == NpcPopulationPreviewSource.PopulationDefinition
                && selectedDefinition == null)
            {
                EditorGUILayout.HelpBox(
                    "Choose a population definition first.",
                    MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            if (previewSource
                == NpcPopulationPreviewSource.PopulationDefinition)
            {
                NpcPopulationAppearancePool pool =
                    selectedDefinition.GetAppearancePool(selectedGender);

                selectedBody = DrawChoiceRow(
                    "Body",
                    selectedBody,
                    pool.Bodies,
                    choice => choice?.Asset,
                    asset => GetDisplayName(asset?.DisplayName, asset),
                    asset => asset.Supports(selectedGender));

                selectedSkin = DrawChoiceRow(
                    "Skin",
                    selectedSkin,
                    pool.Skins,
                    choice => choice?.Asset,
                    asset => GetDisplayName(asset?.DisplayName, asset),
                    asset => true);

                selectedOutfit = DrawChoiceRow(
                    "Outfit",
                    selectedOutfit,
                    pool.Outfits,
                    choice => choice?.Asset,
                    asset => GetDisplayName(asset?.DisplayName, asset),
                    asset => asset.Supports(selectedGender));

                selectedHair = DrawChoiceRow(
                    "Hair",
                    selectedHair,
                    pool.Hair,
                    choice => choice?.Asset,
                    asset => GetDisplayName(asset?.DisplayName, asset),
                    asset => asset.Supports(selectedGender));
            }
            else
            {
                selectedBody = DrawAssetRow(
                    "Body",
                    selectedBody,
                    libraryBodies,
                    asset => GetDisplayName(asset.DisplayName, asset),
                    asset => asset.Supports(selectedGender));

                selectedSkin = DrawAssetRow(
                    "Skin",
                    selectedSkin,
                    librarySkins,
                    asset => GetDisplayName(asset.DisplayName, asset),
                    asset => true);

                selectedOutfit = DrawAssetRow(
                    "Outfit",
                    selectedOutfit,
                    libraryOutfits,
                    asset => GetDisplayName(asset.DisplayName, asset),
                    asset => asset.Supports(selectedGender));

                selectedHair = DrawAssetRow(
                    "Hair",
                    selectedHair,
                    libraryHair,
                    asset => GetDisplayName(asset.DisplayName, asset),
                    asset => asset.Supports(selectedGender));
            }

            if (EditorGUI.EndChangeCheck())
            {
                ApplyCurrentAppearance();
            }
        }


        private void DrawRandomControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string buttonLabel;

                if (previewSource
                    == NpcPopulationPreviewSource.AppearanceLibrary)
                {
                    buttonLabel = "Randomize Library Preview";
                }
                else
                {
                    string populationName = selectedDefinition != null
                        ? selectedDefinition.DisplayName
                        : "Population";

                    buttonLabel = "Generate Random " + populationName;
                }

                if (GUILayout.Button(
                        buttonLabel,
                        GUILayout.Height(28f)))
                {
                    GenerateRandomAppearance();
                }

                if (GUILayout.Button(
                        "Reset",
                        GUILayout.Width(90f),
                        GUILayout.Height(28f)))
                {
                    SelectFirstChoices();
                    ApplyCurrentAppearance();
                }
            }

            EditorGUILayout.LabelField(
                previewSource
                    == NpcPopulationPreviewSource.PopulationDefinition
                    ? "Random generation uses this population's weights. " +
                      "Nothing is saved."
                    : "Library randomization uses every saved compatible " +
                      "asset. Nothing is saved or authorized for spawning.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawSceneWorkbenchControls()
        {
            EditorGUILayout.LabelField(
                "Unity AI Scene Workbench",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Place this exact preview as a real, selected Person in the " +
                "open scene. It keeps the shared rig and Animator so Unity " +
                "AI can inspect, pose, and animate it. The scene is not " +
                "saved automatically.",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button(
                    "Place Current Person in Open Scene",
                    GUILayout.Height(32f)))
            {
                PlaceCurrentPersonInOpenScene();
            }
        }

        private void DrawPersonRigReloadControls()
        {
            EditorGUILayout.LabelField(
                "Preview Rig",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "This hidden Person refreshes automatically when its " +
                "project assets change. Reload it manually if the preview " +
                "ever looks older than the current Person prefab.",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button(
                    "Reload Person Rig",
                    GUILayout.Height(24f)))
            {
                ReloadPersonRig(
                    "Person rig reloaded from the latest project assets.");
            }
        }


        private void PlaceCurrentPersonInOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SetStatus(
                    "Exit Play Mode before placing a workbench Person.",
                    MessageType.Warning);
                return;
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                SetStatus(
                    "Exit Prefab Mode first so the Person is placed in the " +
                    "open gameplay scene.",
                    MessageType.Warning);
                return;
            }

            NpcAppearanceSelection appearance =
                CreateCurrentSelection();

            if (!appearance.TryValidate(out string failureReason))
            {
                SetStatus(failureReason, MessageType.Warning);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                SetStatus(
                    "Open a gameplay scene before placing a workbench " +
                    "Person.",
                    MessageType.Warning);
                return;
            }

            GameObject personPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PersonPrefabPath);

            if (personPrefab == null)
            {
                SetStatus(
                    "The shared Person prefab was not found at " +
                    PersonPrefabPath + ".",
                    MessageType.Error);
                return;
            }

            GameObject scenePerson =
                PrefabUtility.InstantiatePrefab(
                    personPrefab,
                    activeScene) as GameObject;

            if (scenePerson == null)
            {
                SetStatus(
                    "Unity could not place the shared Person prefab in the " +
                    "open scene.",
                    MessageType.Error);
                return;
            }

            Undo.RegisterCreatedObjectUndo(
                scenePerson,
                "Place Unity AI Workbench Person");

            scenePerson.name = "Unity AI Workbench Person";

            Vector3 scenePosition = Vector3.zero;
            SceneView sceneView = SceneView.lastActiveSceneView;

            if (sceneView != null)
            {
                scenePosition = sceneView.pivot;
                scenePosition.z = 0f;
            }

            scenePerson.transform.SetPositionAndRotation(
                scenePosition,
                Quaternion.identity);
            scenePerson.transform.localScale = Vector3.one;

            NpcCutoutRig sceneRig =
                scenePerson.GetComponentInChildren<NpcCutoutRig>(true);

            if (sceneRig == null)
            {
                Undo.DestroyObjectImmediate(scenePerson);
                SetStatus(
                    "The shared Person prefab has no NPC cutout rig.",
                    MessageType.Error);
                return;
            }

            if (!sceneRig.TrySetAppearanceSelection(
                    appearance,
                    out failureReason))
            {
                Undo.DestroyObjectImmediate(scenePerson);
                SetStatus(failureReason, MessageType.Error);
                return;
            }

            sceneRig.SetFacing(facing);

            Animator animator =
                scenePerson.GetComponentInChildren<Animator>(true);

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            Selection.activeGameObject = scenePerson;
            EditorGUIUtility.PingObject(scenePerson);
            EditorSceneManager.MarkSceneDirty(activeScene);
            SceneView.RepaintAll();

            SetStatus(
                "Placed and selected Unity AI Workbench Person in " +
                activeScene.name + ". Save the scene only if you want to " +
                "keep it.",
                MessageType.Info);
        }


        private TAsset DrawChoiceRow<TChoice, TAsset>(
            string label,
            TAsset current,
            IReadOnlyList<TChoice> choices,
            Func<TChoice, TAsset> getAsset,
            Func<TAsset, string> getName,
            Func<TAsset, bool> isCompatible)
            where TAsset : Object
        {
            List<TAsset> assets = new List<TAsset>();

            if (choices != null)
            {
                for (int index = 0; index < choices.Count; index++)
                {
                    TAsset asset = getAsset(choices[index]);

                    if (asset != null && isCompatible(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }

            return DrawAssetRow(
                label,
                current,
                assets,
                getName,
                asset => true);
        }


        private TAsset DrawAssetRow<TAsset>(
            string label,
            TAsset current,
            IReadOnlyList<TAsset> source,
            Func<TAsset, string> getName,
            Func<TAsset, bool> isCompatible)
            where TAsset : Object
        {
            List<TAsset> assets = new List<TAsset>();

            if (source != null)
            {
                for (int index = 0; index < source.Count; index++)
                {
                    TAsset asset = source[index];

                    if (asset != null && isCompatible(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }

            if (assets.Count == 0)
            {
                EditorGUILayout.LabelField(
                    label,
                    "No compatible options");
                return null;
            }

            int currentIndex = assets.IndexOf(current);

            if (currentIndex < 0)
            {
                currentIndex = 0;
                current = assets[0];
            }

            string[] names = new string[assets.Count];

            for (int index = 0; index < assets.Count; index++)
            {
                names[index] = getName(assets[index]);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);

                if (GUILayout.Button("<", GUILayout.Width(28f)))
                {
                    currentIndex = WrapIndex(
                        currentIndex - 1,
                        assets.Count);
                }

                currentIndex = EditorGUILayout.Popup(
                    currentIndex,
                    names);

                if (GUILayout.Button(">", GUILayout.Width(28f)))
                {
                    currentIndex = WrapIndex(
                        currentIndex + 1,
                        assets.Count);
                }

                GUILayout.Label(
                    $"{currentIndex + 1}/{assets.Count}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(38f));
            }

            return assets[currentIndex];
        }


        private void GenerateRandomAppearance()
        {
            if (previewSource
                == NpcPopulationPreviewSource.AppearanceLibrary)
            {
                GenerateRandomLibraryAppearance();
                return;
            }

            if (selectedDefinition == null)
            {
                SetStatus(
                    "Choose a population definition first.",
                    MessageType.Warning);
                return;
            }

            unchecked
            {
                randomSeed++;
            }

            NpcAppearanceSelection current =
                CreateCurrentSelection();

            if (!NpcAppearanceGenerator.TryGenerate(
                    selectedDefinition,
                    randomSeed,
                    current,
                    new NpcAppearanceLocks(),
                    out NpcAppearanceSelection generated,
                    out string failureReason))
            {
                SetStatus(failureReason, MessageType.Warning);
                return;
            }

            selectedBody = generated.BodySilhouette;
            selectedGender = generated.Gender;
            selectedSkin = generated.SkinPalette;
            selectedOutfit = generated.OutfitSet;
            selectedHair = generated.HairSet;

            ApplyCurrentAppearance();

            SetStatus(
                $"Generated a random {selectedDefinition.DisplayName}. " +
                "This preview is temporary.",
                MessageType.Info);
        }


        private void GenerateRandomLibraryAppearance()
        {
            unchecked
            {
                randomSeed++;
            }

            List<NpcPersonGender> genders =
                GetAvailableGenders();

            if (genders.Count == 0)
            {
                SetStatus(
                    "The Appearance Library has no complete compatible " +
                    "person recipe.",
                    MessageType.Warning);
                return;
            }

            System.Random random = new System.Random(randomSeed);
            selectedGender = genders[random.Next(genders.Count)];
            selectedBody = PickRandomCompatible(
                libraryBodies,
                asset => asset.Supports(selectedGender),
                random);
            selectedSkin = PickRandomCompatible(
                librarySkins,
                asset => true,
                random);
            selectedOutfit = PickRandomCompatible(
                libraryOutfits,
                asset => asset.Supports(selectedGender),
                random);
            selectedHair = PickRandomCompatible(
                libraryHair,
                asset => asset.Supports(selectedGender),
                random);

            ApplyCurrentAppearance();
            SetStatus(
                "Randomized a temporary Appearance Library preview.",
                MessageType.Info);
        }


        private void SelectDefinition(
            NpcPopulationDefinition definition)
        {
            selectedDefinition = definition;
            SelectFirstGender();
            SelectFirstCompatibleChoices();
            ApplyCurrentAppearance();
        }


        private void SelectFirstDefinition()
        {
            List<NpcPopulationDefinition> definitions =
                GetAvailableDefinitions();

            if (definitions.Count == 0)
            {
                selectedDefinition = null;
                selectedBody = null;
                selectedSkin = null;
                selectedOutfit = null;
                selectedHair = null;
                return;
            }

            if (!definitions.Contains(selectedDefinition))
            {
                selectedDefinition = definitions[0];
            }

            SelectFirstGender();
            SelectFirstCompatibleChoices();
        }


        private void SelectFirstChoices()
        {
            SelectFirstGender();
            SelectFirstCompatibleChoices();
        }


        private void SelectFirstGender()
        {
            if (AllowsGender(selectedGender))
            {
                return;
            }

            if (AllowsGender(NpcPersonGender.Man))
            {
                selectedGender = NpcPersonGender.Man;
                return;
            }

            selectedGender = NpcPersonGender.Woman;
        }


        private void SelectFirstCompatibleChoices()
        {
            if (previewSource
                == NpcPopulationPreviewSource.AppearanceLibrary)
            {
                selectedBody = FirstAsset(
                    libraryBodies,
                    asset => asset.Supports(selectedGender));
                selectedSkin = FirstAsset(
                    librarySkins,
                    asset => true);
                selectedOutfit = FirstAsset(
                    libraryOutfits,
                    asset => asset.Supports(selectedGender));
                selectedHair = FirstAsset(
                    libraryHair,
                    asset => asset.Supports(selectedGender));
                return;
            }


            NpcPopulationAppearancePool pool =
                selectedDefinition?.GetAppearancePool(selectedGender);

            selectedBody = FirstAsset(
                pool?.Bodies,
                choice => choice?.Asset,
                asset => asset.Supports(selectedGender));

            selectedSkin = FirstAsset(
                pool?.Skins,
                choice => choice?.Asset,
                asset => true);

            selectedOutfit = FirstAsset(
                pool?.Outfits,
                choice => choice?.Asset,
                asset => asset.Supports(selectedGender));

            selectedHair = FirstAsset(
                pool?.Hair,
                choice => choice?.Asset,
                asset => asset.Supports(selectedGender));
        }


        private void PreserveOrSelectLibraryChoices()
        {
            selectedBody = PreserveOrFirst(
                selectedBody,
                libraryBodies,
                asset => asset.Supports(selectedGender));
            selectedSkin = PreserveOrFirst(
                selectedSkin,
                librarySkins,
                asset => true);
            selectedOutfit = PreserveOrFirst(
                selectedOutfit,
                libraryOutfits,
                asset => asset.Supports(selectedGender));
            selectedHair = PreserveOrFirst(
                selectedHair,
                libraryHair,
                asset => asset.Supports(selectedGender));
        }


        private void ApplyCurrentAppearance()
        {
            EnsurePreviewScene();

            if (previewRig == null || previewProfile == null)
            {
                return;
            }

            NpcAppearanceSelection selection =
                CreateCurrentSelection();

            if (!selection.TryValidate(out string failureReason))
            {
                SetStatus(failureReason, MessageType.Warning);
                return;
            }

            RestoreBindPose();

            previewProfile.Configure(
                "Population Preview",
                selection);

            previewRig.SetAppearancePreview(previewProfile);
            previewRig.SetFacing(facing);
            CaptureBindPose();
            EvaluateSelectedAnimation();
            SetStatus(string.Empty, MessageType.Info);
            Repaint();
        }


        private NpcAppearanceSelection CreateCurrentSelection()
        {
            return new NpcAppearanceSelection(
                selectedGender,
                selectedBody,
                selectedSkin,
                selectedOutfit,
                selectedHair);
        }


        private void EnsurePreviewScene()
        {
            if (previewUtility != null && previewRig != null)
            {
                return;
            }

            CleanupPreviewScene();

            GameObject personPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PersonPrefabPath);

            if (personPrefab == null)
            {
                SetStatus(
                    "The shared Person prefab was not found at " +
                    PersonPrefabPath + ".",
                    MessageType.Error);
                return;
            }

            previewUtility = new PreviewRenderUtility();
            previewUtility.camera.orthographic = true;
            previewUtility.camera.allowHDR = false;
            previewUtility.camera.allowMSAA = true;
            previewUtility.camera.clearFlags =
                CameraClearFlags.SolidColor;
            previewUtility.camera.backgroundColor =
                new Color(0.075f, 0.095f, 0.12f, 1f);
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = 50f;

            previewPerson =
                previewUtility.InstantiatePrefabInScene(personPrefab);

            if (previewPerson == null)
            {
                SetStatus(
                    "Unity could not instantiate the shared Person in " +
                    "the hidden preview scene.",
                    MessageType.Error);
                CleanupPreviewScene();
                return;
            }

            SetHideFlagsRecursively(
                previewPerson,
                HideFlags.HideAndDontSave);

            previewPerson.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            previewPerson.transform.localScale = Vector3.one;

            Animator[] animators =
                previewPerson.GetComponentsInChildren<Animator>(true);

            for (int index = 0; index < animators.Length; index++)
            {
                animators[index].enabled = false;
            }

            previewRig =
                previewPerson.GetComponentInChildren<NpcCutoutRig>(true);

            if (previewRig == null)
            {
                SetStatus(
                    "The shared Person prefab has no NpcCutoutRig.",
                    MessageType.Error);
                CleanupPreviewScene();
                return;
            }

            previewProfile =
                CreateInstance<NpcAppearanceProfile>();
            previewProfile.name = "Population Preview Appearance";
            previewProfile.hideFlags = HideFlags.HideAndDontSave;

            CaptureBindPose();

            personPrefabDependencyHash =
                AssetDatabase.GetAssetDependencyHash(PersonPrefabPath);
            hasPersonPrefabDependencyHash = true;
        }


        private void RenderPreview(
            Rect previewRect)
        {
            if (Event.current.type != EventType.Repaint
                || previewUtility == null
                || previewPerson == null)
            {
                return;
            }

            DestroyPreviewTexture();
            PositionPreviewCamera(previewRect);

            previewUtility.BeginPreview(
                previewRect,
                GUIStyle.none);
            previewUtility.Render(true);
            previewTexture = previewUtility.EndPreview();
        }


        private void PositionPreviewCamera(
            Rect previewRect)
        {
            Bounds bounds = hasBindPoseBounds
                ? bindPoseBounds
                : CalculateVisibleRendererBounds();

            float aspect = Mathf.Max(
                0.1f,
                previewRect.width / Mathf.Max(1f, previewRect.height));

            float verticalExtent = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / aspect);

            previewUtility.camera.orthographicSize =
                Mathf.Max(0.5f, verticalExtent * 1.3f * zoom);

            previewUtility.camera.transform.position =
                bounds.center + Vector3.back * 10f;
            previewUtility.camera.transform.rotation =
                Quaternion.identity;
        }


        private void HandlePreviewZoom(
            Rect previewRect)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.ScrollWheel
                || !previewRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            zoom = Mathf.Clamp(
                zoom + currentEvent.delta.y * 0.08f,
                0.55f,
                2.25f);

            currentEvent.Use();
            Repaint();
        }


        private void LoadPreviewAnimations()
        {
            southFacingIdleClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    PersonSouthFacingIdleClipPath);
            northFacingIdleClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    PersonNorthFacingIdleClipPath);
            southFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    PersonSouthFacingWalkClipPath);
            northFacingWalkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    PersonNorthFacingWalkClipPath);
        }


        private void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            double elapsed = currentTime - lastEditorUpdateTime;
            lastEditorUpdateTime = currentTime;

            AnimationClip clip = GetSelectedAnimationClip();

            if (!animationPlaying
                || clip == null
                || previewPerson == null
                || clip.length <= 0f)
            {
                return;
            }

            animationTime += (float)elapsed * playbackSpeed;

            if (loopAnimation)
            {
                animationTime = Mathf.Repeat(
                    animationTime,
                    clip.length);
            }
            else if (animationTime >= clip.length)
            {
                animationTime = clip.length;
                animationPlaying = false;
            }

            EvaluateSelectedAnimation();
            Repaint();
        }


        private AnimationClip GetSelectedAnimationClip()
        {
            switch (selectedAnimation)
            {
                case NpcPopulationPreviewAnimation.Idle:
                    return NpcFacingUtility.UsesNorthFacingAnimation(facing)
                        ? northFacingIdleClip
                        : southFacingIdleClip;

                case NpcPopulationPreviewAnimation.Walk:
                    return NpcFacingUtility.UsesNorthFacingAnimation(facing)
                        ? northFacingWalkClip
                        : southFacingWalkClip;

                default:
                    return null;
            }
        }


        private void EvaluateSelectedAnimation()
        {
            if (previewPerson == null)
            {
                return;
            }

            RestoreBindPose();

            AnimationClip clip = GetSelectedAnimationClip();

            if (clip == null || clip.length <= 0f)
            {
                return;
            }

            float sampleTime = loopAnimation
                ? Mathf.Repeat(animationTime, clip.length)
                : Mathf.Clamp(animationTime, 0f, clip.length);

            clip.SampleAnimation(previewPerson, sampleTime);
        }


        private void CaptureBindPose()
        {
            bindPose.Clear();

            if (previewPerson == null)
            {
                return;
            }

            Transform[] transforms =
                previewPerson.GetComponentsInChildren<Transform>(true);

            for (int index = 0; index < transforms.Length; index++)
            {
                bindPose.Add(
                    new PreviewTransformPose(transforms[index]));
            }

            bindPoseBounds = CalculateVisibleRendererBounds();
            hasBindPoseBounds = true;
        }


        private void RestoreBindPose()
        {
            for (int index = 0; index < bindPose.Count; index++)
            {
                PreviewTransformPose pose = bindPose[index];

                if (pose.Transform == null)
                {
                    continue;
                }

                pose.Transform.localPosition = pose.LocalPosition;
                pose.Transform.localRotation = pose.LocalRotation;
                pose.Transform.localScale = pose.LocalScale;
            }
        }


        private void DrawRigAnatomy(
            Rect previewRect)
        {
            if (Event.current.type != EventType.Repaint
                || previewRig == null
                || previewUtility == null)
            {
                return;
            }

            IReadOnlyList<NpcRigBoneDefinition> definitions =
                NpcRigDefinition.BoneDefinitions;

            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.95f);

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneDefinition definition = definitions[index];

                if (!definition.HasParent
                    || !previewRig.TryGetBone(
                        definition.Id,
                        out Transform bone)
                    || !previewRig.TryGetBone(
                        definition.ParentId,
                        out Transform parent))
                {
                    continue;
                }

                Handles.DrawAAPolyLine(
                    2.5f,
                    ToPreviewGuiPoint(previewRect, parent.position),
                    ToPreviewGuiPoint(previewRect, bone.position));
            }

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcRigBoneDefinition definition = definitions[index];

                if (!previewRig.TryGetBone(
                    definition.Id,
                    out Transform bone))
                {
                    continue;
                }

                Handles.DrawSolidDisc(
                    ToPreviewGuiPoint(previewRect, bone.position),
                    Vector3.forward,
                    definition.Id == NpcRigBoneId.Root ? 5f : 3.5f);
            }

            Handles.EndGUI();

            GUI.Label(
                new Rect(
                    previewRect.x + 10f,
                    previewRect.y + 8f,
                    250f,
                    20f),
                "Cyan: live animated rig",
                EditorStyles.whiteMiniLabel);
        }


        private Vector2 ToPreviewGuiPoint(
            Rect previewRect,
            Vector3 worldPosition)
        {
            Vector3 viewport = previewUtility.camera
                .WorldToViewportPoint(worldPosition);

            return new Vector2(
                previewRect.x + viewport.x * previewRect.width,
                previewRect.y +
                (1f - viewport.y) * previewRect.height);
        }


        private static string FormatAnimationTime(
            float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainingSeconds = seconds - minutes * 60f;

            return minutes + ":" +
                   remainingSeconds.ToString("00.00");
        }


        private Bounds CalculateVisibleRendererBounds()
        {
            Bounds bounds = new Bounds(
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 2f, 0.1f));

            if (previewPerson == null)
            {
                return bounds;
            }

            Renderer[] renderers =
                previewPerson.GetComponentsInChildren<Renderer>(true);
            bool foundRenderer = false;

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];

                if (!renderer.enabled)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = renderer.bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }


        private void CleanupPreviewScene()
        {
            bindPose.Clear();
            hasBindPoseBounds = false;
            previewRig = null;

            if (previewPerson != null)
            {
                DestroyImmediate(previewPerson);
                previewPerson = null;
            }

            if (previewProfile != null)
            {
                DestroyImmediate(previewProfile);
                previewProfile = null;
            }

            DestroyPreviewTexture();

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }
        }


        private void DestroyPreviewTexture()
        {
            if (previewTexture == null)
            {
                return;
            }

            DestroyImmediate(previewTexture);
            previewTexture = null;
        }


        private void FindCatalog()
        {
            string[] guids =
                AssetDatabase.FindAssets("t:NpcAppearanceCatalog");

            catalog = null;

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                NpcAppearanceCatalog candidate =
                    AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(
                        path);

                if (candidate != null)
                {
                    catalog = candidate;
                    return;
                }
            }
        }


        private void RefreshAppearanceLibrary()
        {
            libraryBodies.Clear();
            librarySkins.Clear();
            libraryOutfits.Clear();
            libraryHair.Clear();

            AddRegisteredAssets(libraryBodies, catalog?.Bodies);
            AddRegisteredAssets(librarySkins, catalog?.Skins);
            AddRegisteredAssets(libraryOutfits, catalog?.Outfits);
            AddRegisteredAssets(libraryHair, catalog?.Hair);

            AddFolderAssets(libraryBodies, BodiesFolder);
            AddFolderAssets(librarySkins, SkinsFolder);
            AddFolderAssets(libraryOutfits, OutfitsFolder);
            AddFolderAssets(libraryHair, HairFolder);

            SortAssets(libraryBodies, asset => asset.DisplayName);
            SortAssets(librarySkins, asset => asset.DisplayName);
            SortAssets(libraryOutfits, asset => asset.DisplayName);
            SortAssets(libraryHair, asset => asset.DisplayName);
        }


        private bool AllowsGender(
            NpcPersonGender gender)
        {
            if (previewSource
                == NpcPopulationPreviewSource.PopulationDefinition)
            {
                return selectedDefinition != null
                       && selectedDefinition.Allows(gender);
            }

            return FirstAsset(
                       libraryBodies,
                       asset => asset.Supports(gender)) != null
                   && librarySkins.Count > 0
                   && FirstAsset(
                       libraryOutfits,
                       asset => asset.Supports(gender)) != null
                   && FirstAsset(
                       libraryHair,
                       asset => asset.Supports(gender)) != null;
        }


        private List<NpcPersonGender> GetAvailableGenders()
        {
            List<NpcPersonGender> genders =
                new List<NpcPersonGender>();

            if (AllowsGender(NpcPersonGender.Man))
            {
                genders.Add(NpcPersonGender.Man);
            }

            if (AllowsGender(NpcPersonGender.Woman))
            {
                genders.Add(NpcPersonGender.Woman);
            }

            return genders;
        }


        private List<NpcPopulationDefinition>
            GetAvailableDefinitions()
        {
            List<NpcPopulationDefinition> definitions =
                new List<NpcPopulationDefinition>();

            if (catalog?.Definitions == null)
            {
                return definitions;
            }

            for (int index = 0;
                 index < catalog.Definitions.Count;
                 index++)
            {
                NpcPopulationDefinition definition =
                    catalog.Definitions[index];

                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            return definitions;
        }


        private void SetStatus(
            string message,
            MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }


        private static void AddRegisteredAssets<TAsset>(
            List<TAsset> destination,
            IReadOnlyList<TAsset> source)
            where TAsset : Object
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                TAsset asset = source[index];

                if (asset != null && !destination.Contains(asset))
                {
                    destination.Add(asset);
                }
            }
        }


        private static void AddFolderAssets<TAsset>(
            List<TAsset> destination,
            string folder)
            where TAsset : Object
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:" + typeof(TAsset).Name,
                new[] { folder });

            for (int index = 0; index < guids.Length; index++)
            {
                TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));

                if (asset != null && !destination.Contains(asset))
                {
                    destination.Add(asset);
                }
            }
        }


        private static void SortAssets<TAsset>(
            List<TAsset> assets,
            Func<TAsset, string> getDisplayName)
            where TAsset : Object
        {
            assets.Sort(
                (left, right) => string.Compare(
                    GetDisplayName(getDisplayName(left), left),
                    GetDisplayName(getDisplayName(right), right),
                    StringComparison.OrdinalIgnoreCase));
        }


        private static TAsset FirstAsset<TAsset>(
            IReadOnlyList<TAsset> assets,
            Func<TAsset, bool> isCompatible)
            where TAsset : Object
        {
            if (assets == null)
            {
                return null;
            }

            for (int index = 0; index < assets.Count; index++)
            {
                TAsset asset = assets[index];

                if (asset != null && isCompatible(asset))
                {
                    return asset;
                }
            }

            return null;
        }


        private static TAsset PreserveOrFirst<TAsset>(
            TAsset current,
            IReadOnlyList<TAsset> assets,
            Func<TAsset, bool> isCompatible)
            where TAsset : Object
        {
            if (current != null && isCompatible(current))
            {
                for (int index = 0; index < assets.Count; index++)
                {
                    if (assets[index] == current)
                    {
                        return current;
                    }
                }
            }

            return FirstAsset(assets, isCompatible);
        }


        private static TAsset PickRandomCompatible<TAsset>(
            IReadOnlyList<TAsset> assets,
            Func<TAsset, bool> isCompatible,
            System.Random random)
            where TAsset : Object
        {
            List<TAsset> compatible = new List<TAsset>();

            for (int index = 0; index < assets.Count; index++)
            {
                TAsset asset = assets[index];

                if (asset != null && isCompatible(asset))
                {
                    compatible.Add(asset);
                }
            }

            return compatible.Count > 0
                ? compatible[random.Next(compatible.Count)]
                : null;
        }


        private static TAsset FirstAsset<TChoice, TAsset>(
            IReadOnlyList<TChoice> choices,
            Func<TChoice, TAsset> getAsset,
            Func<TAsset, bool> isCompatible)
            where TAsset : Object
        {
            if (choices == null)
            {
                return null;
            }

            for (int index = 0; index < choices.Count; index++)
            {
                TAsset asset = getAsset(choices[index]);

                if (asset != null && isCompatible(asset))
                {
                    return asset;
                }
            }

            return null;
        }


        private static string GetDisplayName(
            string displayName,
            Object asset)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            return asset != null ? asset.name : "Missing";
        }


        private static int WrapIndex(
            int index,
            int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }


        private static void SetHideFlagsRecursively(
            GameObject root,
            HideFlags hideFlags)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            for (int index = 0; index < transforms.Length; index++)
            {
                transforms[index].gameObject.hideFlags = hideFlags;
            }
        }


        private static GUIStyle CenteredLabelStyle()
        {
            return new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
    }
}
