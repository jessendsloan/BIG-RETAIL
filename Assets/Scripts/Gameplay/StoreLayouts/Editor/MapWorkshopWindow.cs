using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BigRetail.Core.Session;
using BigRetail.StoreLayouts.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.StoreLayouts.Editor
{
    /// <summary>
    /// Focused developer workflow for building with the real store tools and
    /// explicitly capturing permanent model state into a StoreLayoutAsset.
    /// </summary>
    public sealed class MapWorkshopWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Map Workshop/Open Workshop";
        private const string FrankScenePath =
            "Assets/Scenes/FrankRoadside.unity";
        private const double RefreshIntervalSeconds = 0.5d;

        [SerializeField]
        private SceneAsset locationScene;

        [SerializeField]
        private StoreLayoutAsset selectedLayout;

        [SerializeField]
        private string draftLayoutId =
            "bigretail.layout.frank_store";

        [SerializeField]
        private string draftDisplayName =
            "Frank Store Layout";

        [SerializeField]
        private bool reloadSelectedLayoutOnEnter = true;

        private readonly List<string> validationIssues =
            new List<string>();
        private Vector2 scrollPosition;
        private bool runtimeAvailable;
        private bool runtimeHasUnsavedChanges;
        private string runtimeSummary = "Workshop is not running.";
        private string operationStatus = string.Empty;
        private MessageType operationStatusType = MessageType.None;
        private double nextRuntimeRefresh;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            MapWorkshopWindow window =
                GetWindow<MapWorkshopWindow>("Map Workshop");
            window.minSize = new Vector2(470f, 610f);
            window.Show();
        }


        private void OnEnable()
        {
            if (locationScene == null)
            {
                locationScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        FrankScenePath);
            }

            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
            RefreshRuntimeState();
        }


        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.delayCall -=
                ReloadSelectedLayoutAfterEnter;
        }


        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRuntimeRefresh)
            {
                return;
            }

            nextRuntimeRefresh =
                EditorApplication.timeSinceStartup
                + RefreshIntervalSeconds;
            RefreshRuntimeState();
            Repaint();
        }


        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawLocationAndLayout();
            DrawWorkshopState();
            DrawActions();
            DrawValidationIssues();

            EditorGUILayout.EndScrollView();
        }


        private void DrawHeader()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                "Big Retail Map Workshop",
                EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "Build with the real Play Mode construction tools, then "
                + "explicitly validate and capture permanent model state. "
                + "Stopping Play Mode never saves a layout automatically. "
                + "Use Validate and Save As New Layout in this window; "
                + "Scene Setup commands do not save a store layout. "
                + "Workshop sessions provide unlimited authoring cash.",
                MessageType.Info);
            EditorGUILayout.Space(6f);
        }


        private void DrawLocationAndLayout()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Authoring Target",
                    EditorStyles.boldLabel);

                locationScene =
                    (SceneAsset)EditorGUILayout.ObjectField(
                        "Location Scene",
                        locationScene,
                        typeof(SceneAsset),
                        false);

                EditorGUI.BeginChangeCheck();
                StoreLayoutAsset changedLayout =
                    (StoreLayoutAsset)EditorGUILayout.ObjectField(
                        "Layout Asset",
                        selectedLayout,
                        typeof(StoreLayoutAsset),
                        false);

                if (EditorGUI.EndChangeCheck())
                {
                    selectedLayout = changedLayout;
                    CopySelectedLayoutMetadata();
                    RefreshRuntimeState();
                }

                draftLayoutId =
                    EditorGUILayout.TextField(
                        "Layout ID",
                        draftLayoutId);
                draftDisplayName =
                    EditorGUILayout.TextField(
                        "Display Name",
                        draftDisplayName);

                reloadSelectedLayoutOnEnter =
                    EditorGUILayout.ToggleLeft(
                        "Reload the selected layout after entering Workshop",
                        reloadSelectedLayoutOnEnter);
            }
        }


        private void DrawWorkshopState()
        {
            EditorGUILayout.Space(6f);

            MessageType stateType;
            string stateTitle;

            if (!runtimeAvailable)
            {
                stateType = MessageType.None;
                stateTitle = "NOT RUNNING";
            }
            else if (selectedLayout == null)
            {
                stateType = MessageType.Warning;
                stateTitle = "UNSAVED DRAFT";
            }
            else if (runtimeHasUnsavedChanges)
            {
                stateType = MessageType.Warning;
                stateTitle = "UNSAVED CHANGES";
            }
            else
            {
                stateType = MessageType.Info;
                stateTitle = "SAVED — RUNTIME MATCHES ASSET";
            }

            EditorGUILayout.HelpBox(
                $"{stateTitle}\n{runtimeSummary}",
                stateType);

            if (!string.IsNullOrEmpty(operationStatus))
            {
                EditorGUILayout.HelpBox(
                    operationStatus,
                    operationStatusType);
            }
        }


        private void DrawActions()
        {
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Workshop Controls",
                    EditorStyles.boldLabel);

                bool launchBlocked =
                    locationScene == null
                    || EditorApplication.isPlayingOrWillChangePlaymode
                    || EditorApplication.isCompiling;

                using (new EditorGUI.DisabledScope(launchBlocked))
                {
                    if (GUILayout.Button(
                            "Build / Enter Workshop",
                            GUILayout.Height(38f)))
                    {
                        EnterWorkshop();
                    }
                }

                if (EditorApplication.isPlaying)
                {
                    using (new EditorGUI.DisabledScope(
                               !MapWorkshopSession.IsActive))
                    {
                        if (GUILayout.Button("Exit Workshop"))
                        {
                            EditorApplication.isPlaying = false;
                        }
                    }
                }

                EditorGUILayout.Space(6f);

                using (new EditorGUI.DisabledScope(!runtimeAvailable))
                {
                    if (GUILayout.Button("Validate"))
                    {
                        ValidateRuntime();
                    }

                    if (GUILayout.Button("Save As New Layout"))
                    {
                        SaveAsNewLayout();
                    }

                    using (new EditorGUI.DisabledScope(
                               selectedLayout == null))
                    {
                        if (GUILayout.Button("Update Selected Layout"))
                        {
                            UpdateSelectedLayout();
                        }

                        if (GUILayout.Button("Reload From Asset"))
                        {
                            ReloadSelectedLayout(true);
                        }
                    }
                }

                EditorGUILayout.Space(6f);

                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Button("Test Scenario (Phase D)");
                }

                EditorGUILayout.LabelField(
                    "Scenario selection and testing arrive with the scenario "
                    + "bootstrap in Phase D.",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }


        private void DrawValidationIssues()
        {
            if (validationIssues.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                $"Validation Issues ({validationIssues.Count})",
                EditorStyles.boldLabel);

            for (int index = 0;
                 index < validationIssues.Count;
                 index++)
            {
                EditorGUILayout.HelpBox(
                    validationIssues[index],
                    MessageType.Error);
            }
        }


        private void EnterWorkshop()
        {
            if (locationScene == null
                || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling)
            {
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(locationScene);

            if (string.IsNullOrEmpty(scenePath)
                || !string.Equals(
                    Path.GetExtension(scenePath),
                    ".unity",
                    StringComparison.OrdinalIgnoreCase))
            {
                SetStatus(
                    "Select a saved Unity scene for this location.",
                    MessageType.Error);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            DevelopmentSessionBootstrap.ClearRequest();

            try
            {
                EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
                DevelopmentSessionBootstrap.ArmMapWorkshop();
                SetStatus(
                    "Entering Map Workshop. The scene will remain unchanged "
                    + "unless you explicitly save a layout asset.",
                    MessageType.Info);
                EditorApplication.isPlaying = true;
            }
            catch
            {
                DevelopmentSessionBootstrap.ClearRequest();
                throw;
            }
        }


        private void ValidateRuntime()
        {
            if (!TryCaptureValidated(out StoreLayoutData candidate))
            {
                return;
            }

            SetStatus(
                "Validation passed. " + CreateSummary(candidate),
                MessageType.Info);
            RefreshRuntimeState();
        }


        private void SaveAsNewLayout()
        {
            if (!TryCaptureValidated(out StoreLayoutData candidate))
            {
                return;
            }

            try
            {
                StoreLayoutAssetWriter.EnsureDefaultFolder();
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                return;
            }

            string defaultName = CreateSafeFileName(
                string.IsNullOrWhiteSpace(draftDisplayName)
                    ? "StoreLayout"
                    : draftDisplayName);
            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Save New Store Layout",
                    defaultName,
                    "asset",
                    "Choose a new asset. Updating an existing layout uses the "
                    + "separate Update Selected Layout command.",
                    StoreLayoutAssetWriter.DefaultAssetFolder);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                selectedLayout =
                    StoreLayoutAssetWriter.CreateNew(path, candidate);
                Selection.activeObject = selectedLayout;
                EditorGUIUtility.PingObject(selectedLayout);
                validationIssues.Clear();
                SetStatus(
                    $"Saved new layout '{path}'. "
                    + CreateSummary(candidate),
                    MessageType.Info);
                RefreshRuntimeState();
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }


        private void UpdateSelectedLayout()
        {
            if (selectedLayout == null
                || !TryCaptureValidated(out StoreLayoutData candidate))
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(selectedLayout);

            if (!EditorUtility.DisplayDialog(
                    "Update Store Layout?",
                    $"Replace the saved contents of:\n\n{path}\n\nThis "
                    + "only happens because you chose Update Selected Layout.",
                    "Update Layout",
                    "Cancel"))
            {
                return;
            }

            try
            {
                StoreLayoutAssetWriter.UpdateExisting(
                    selectedLayout,
                    candidate);
                validationIssues.Clear();
                SetStatus(
                    $"Updated '{path}'. " + CreateSummary(candidate),
                    MessageType.Info);
                RefreshRuntimeState();
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
            }
        }


        private void ReloadSelectedLayout(
            bool askBeforeDiscarding)
        {
            if (selectedLayout == null)
            {
                return;
            }

            if (askBeforeDiscarding
                && runtimeHasUnsavedChanges
                && !EditorUtility.DisplayDialog(
                    "Discard Workshop Changes?",
                    "Reloading restores the selected asset and discards the "
                    + "current unsaved store draft.",
                    "Reload Asset",
                    "Cancel"))
            {
                return;
            }

            if (!StoreLayoutWorkshopRuntime.TryCreateLoader(
                    out StoreLayoutRuntimeLoader loader,
                    out string error))
            {
                SetStatus(error, MessageType.Error);
                return;
            }

            StoreLayoutLoadResult result = loader.Load(selectedLayout);
            SetValidationIssues(result.Validation);

            if (!result.Succeeded)
            {
                SetStatus(result.Message, MessageType.Error);
                return;
            }

            CopySelectedLayoutMetadata();
            SetStatus(result.Message, MessageType.Info);
            RefreshRuntimeState();
        }


        private bool TryCaptureValidated(
            out StoreLayoutData candidate)
        {
            candidate = null;
            validationIssues.Clear();

            if (string.IsNullOrWhiteSpace(draftLayoutId))
            {
                SetStatus(
                    "A stable Layout ID is required before validation or "
                    + "capture.",
                    MessageType.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(draftDisplayName))
            {
                SetStatus(
                    "A display name is required before validation or capture.",
                    MessageType.Error);
                return false;
            }

            if (!StoreLayoutWorkshopRuntime.TryCreateLoader(
                    out StoreLayoutRuntimeLoader loader,
                    out string error))
            {
                SetStatus(error, MessageType.Error);
                return false;
            }

            try
            {
                // The capture is synchronous on the Editor thread. No authoring
                // input can interleave between snapshot and validation.
                candidate =
                    loader.CaptureCurrent(
                        draftLayoutId.Trim(),
                        draftDisplayName.Trim());
                StoreDataValidationResult validation =
                    loader.Validate(candidate);
                SetValidationIssues(validation);

                if (!validation.IsValid)
                {
                    SetStatus(
                        $"Validation failed with {validation.IssueCount} "
                        + "issue(s). No asset was changed.",
                        MessageType.Error);
                    candidate = null;
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message, MessageType.Error);
                candidate = null;
                return false;
            }
        }


        private void RefreshRuntimeState()
        {
            runtimeAvailable = false;
            runtimeHasUnsavedChanges = false;

            if (!StoreLayoutWorkshopRuntime.TryCreateLoader(
                    out StoreLayoutRuntimeLoader loader,
                    out string error))
            {
                runtimeSummary = error;
                return;
            }

            try
            {
                StoreLayoutData current =
                    loader.CaptureCurrent(
                        GetComparisonLayoutId(),
                        GetComparisonDisplayName());
                runtimeAvailable = true;
                runtimeHasUnsavedChanges =
                    selectedLayout == null
                    || !StoreLayoutAssetWriter.Matches(
                        selectedLayout,
                        current);
                runtimeSummary = CreateSummary(current);
            }
            catch (Exception exception)
            {
                runtimeSummary = exception.Message;
            }
        }


        private void CopySelectedLayoutMetadata()
        {
            if (selectedLayout == null)
            {
                return;
            }

            StoreLayoutData data = selectedLayout.CreateRuntimeCopy();
            draftLayoutId = data.LayoutId;
            draftDisplayName = data.DisplayName;
        }


        private string GetComparisonLayoutId()
        {
            return draftLayoutId;
        }


        private string GetComparisonDisplayName()
        {
            return draftDisplayName;
        }


        private void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode
                && reloadSelectedLayoutOnEnter
                && selectedLayout != null)
            {
                EditorApplication.delayCall +=
                    ReloadSelectedLayoutAfterEnter;
            }

            RefreshRuntimeState();
            Repaint();
        }


        private void ReloadSelectedLayoutAfterEnter()
        {
            EditorApplication.delayCall -=
                ReloadSelectedLayoutAfterEnter;

            if (this == null
                || !EditorApplication.isPlaying
                || !MapWorkshopSession.IsActive)
            {
                return;
            }

            ReloadSelectedLayout(false);
        }


        private void SetValidationIssues(
            StoreDataValidationResult validation)
        {
            validationIssues.Clear();

            if (validation == null)
            {
                return;
            }

            for (int index = 0;
                 index < validation.IssueCount;
                 index++)
            {
                validationIssues.Add(
                    validation.Issues[index].ToString());
            }
        }


        private void SetStatus(
            string message,
            MessageType type)
        {
            operationStatus = message ?? string.Empty;
            operationStatusType = type;
            Repaint();
        }


        private static string CreateSummary(
            StoreLayoutData layout)
        {
            return $"{layout.Foundations.Count} foundations, "
                + $"{layout.Sidewalks.Count} sidewalks, "
                + $"{layout.Floors.Count} floors, "
                + $"{layout.Walls.Count} walls, "
                + $"{layout.Openings.Count} openings, "
                + $"{layout.Fixtures.Count} installed fixtures, "
                + $"{layout.FixturePlans.Count} fixture plans, "
                + $"{layout.Departments.Count} departments, and "
                + $"{layout.ReceivingCells.Count} Receiving cells.";
        }


        private static string CreateSafeFileName(
            string value)
        {
            HashSet<char> invalid =
                new HashSet<char>(Path.GetInvalidFileNameChars());
            StringBuilder builder = new StringBuilder(value.Length);

            for (int index = 0;
                 index < value.Length;
                 index++)
            {
                char character = value[index];

                if (!invalid.Contains(character)
                    && !char.IsWhiteSpace(character))
                {
                    builder.Append(character);
                }
            }

            return builder.Length > 0
                ? builder.ToString()
                : "StoreLayout";
        }
    }
}
