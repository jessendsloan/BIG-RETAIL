using System;
using System.Collections.Generic;
using System.Diagnostics;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Exercises the production Person prefab, identity generation, and path
    /// following in a disposable Editor-only world. This is the final bridge
    /// before a map-owned population spawner exists.
    /// </summary>
    public sealed class NpcRuntimePopulationLabWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Population/Runtime Lab";

        private static readonly int[] CountPresets = { 1, 12, 50, 100 };

        private NpcAppearanceCatalog catalog;
        private NpcPopulationDefinition selectedDefinition;
        private NpcRuntimePopulationLabCanvas canvas;
        private NpcRuntimePopulationComparison comparison;
        private int peopleCount = 12;
        private int baseSeed = 5000;
        private bool movementEnabled = true;
        private bool showLabels = true;
        private float playbackSpeed = 1f;
        private double lastEditorUpdateTime;
        private double averageTickMilliseconds;
        private int timingSampleCount;
        private Vector2 controlScroll;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            NpcRuntimePopulationLabWindow window =
                GetWindow<NpcRuntimePopulationLabWindow>(
                    "Runtime Population Lab");

            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }


        private void OnEnable()
        {
            canvas = new NpcRuntimePopulationLabCanvas();
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            FindCatalog();
            SelectFirstDefinition();
            CreatePopulation(false);
        }


        private void OnFocus()
        {
            if (catalog == null)
            {
                FindCatalog();
                SelectFirstDefinition();
                CreatePopulation(false);
            }
        }


        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            DisposeCanvas();
        }


        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            DisposeCanvas();
        }


        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Big Retail Runtime Population Lab",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Run the real shared Person prefab through population " +
                "identity generation and NpcPathFollower in a hidden test " +
                "world. Recreate employees to prove stable identity, or " +
                "advance customers to a fresh visit. Nothing is saved or " +
                "placed in the map.",
                MessageType.Info);

            if (catalog == null)
            {
                DrawMissingCatalog();
                return;
            }

            float panelWidth = Mathf.Clamp(
                position.width * 0.31f,
                340f,
                470f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(
                           EditorStyles.helpBox,
                           GUILayout.Width(panelWidth),
                           GUILayout.ExpandHeight(true)))
                {
                    controlScroll = EditorGUILayout.BeginScrollView(
                        controlScroll);
                    DrawPopulationControls();
                    EditorGUILayout.Space(8f);
                    DrawLifecycleControls();
                    EditorGUILayout.Space(8f);
                    DrawMovementControls();
                    EditorGUILayout.Space(8f);
                    DrawRuntimeEvidence();
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope(
                           GUILayout.ExpandWidth(true),
                           GUILayout.ExpandHeight(true)))
                {
                    DrawPreview();
                }
            }
        }


        private void DrawPopulationControls()
        {
            EditorGUILayout.LabelField(
                "1. Runtime Population",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            NpcPopulationDefinition nextDefinition =
                DrawDefinitionPopup(selectedDefinition);

            if (EditorGUI.EndChangeCheck())
            {
                selectedDefinition = nextDefinition;
                comparison = null;
                CreatePopulation(false);
            }

            EditorGUILayout.LabelField("People", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int index = 0; index < CountPresets.Length; index++)
                {
                    int preset = CountPresets[index];
                    Color previous = GUI.backgroundColor;

                    if (peopleCount == preset)
                    {
                        GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
                    }

                    if (GUILayout.Button(preset.ToString()))
                    {
                        peopleCount = preset;
                        comparison = null;
                        CreatePopulation(false);
                    }

                    GUI.backgroundColor = previous;
                }
            }

            peopleCount = EditorGUILayout.IntSlider(
                "Exact Count",
                peopleCount,
                1,
                NpcRuntimePopulationLabModel.MaximumPeople);
            baseSeed = EditorGUILayout.IntField("Seed Block", baseSeed);

            string identityMessage = selectedDefinition != null
                                     && selectedDefinition.Role
                                     == NpcCharacterRole.Employee
                ? "Employees receive stable lab IDs and repeatable seeds."
                : "Customers are transient; a new visit advances the seed block.";
            EditorGUILayout.HelpBox(identityMessage, MessageType.None);
        }


        private void DrawLifecycleControls()
        {
            EditorGUILayout.LabelField(
                "2. Lifecycle Check",
                EditorStyles.boldLabel);

            if (GUILayout.Button("Create / Recreate Same Population"))
            {
                CreatePopulation(true);
            }

            using (new EditorGUI.DisabledScope(
                       selectedDefinition == null
                       || selectedDefinition.Role
                       == NpcCharacterRole.Employee))
            {
                if (GUILayout.Button("Next Customer Visit"))
                {
                    baseSeed =
                        NpcRuntimePopulationLabModel.AdvanceCustomerSeedBlock(
                            baseSeed,
                            peopleCount);
                    comparison = null;
                    CreatePopulation(false);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear"))
                {
                    canvas?.Dispose();
                    comparison = null;
                    ResetTiming();
                }

                if (GUILayout.Button("Reset Preview Zoom"))
                {
                    canvas?.ResetZoom();
                }
            }
        }


        private void DrawMovementControls()
        {
            EditorGUILayout.LabelField(
                "3. Real Path Follower",
                EditorStyles.boldLabel);
            movementEnabled = EditorGUILayout.Toggle(
                "Walk Test Route",
                movementEnabled);
            playbackSpeed = EditorGUILayout.Slider(
                "Playback Speed",
                playbackSpeed,
                0.1f,
                2f);
            showLabels = EditorGUILayout.Toggle(
                "Labels (up to 24)",
                showLabels);
            EditorGUILayout.HelpBox(
                "Each hidden Person follows a small closed diamond using " +
                "NpcPathFollower. This is movement plumbing—not map " +
                "pathfinding or customer behavior.",
                MessageType.None);
        }


        private void DrawRuntimeEvidence()
        {
            EditorGUILayout.LabelField(
                "4. Runtime Evidence",
                EditorStyles.boldLabel);

            int liveCount = canvas?.LiveCount ?? 0;
            int initializedCount = canvas?.InitializedCount ?? 0;
            int uniqueRecipes = canvas?.UniqueRecipeCount ?? 0;
            int repeats = canvas?.RepeatedRecipeCount ?? 0;

            EditorGUILayout.LabelField("Live Person Prefabs", liveCount.ToString());
            EditorGUILayout.LabelField("Identity Initialized", initializedCount.ToString());
            EditorGUILayout.LabelField("Unique Recipes", uniqueRecipes.ToString());
            EditorGUILayout.LabelField("Repeated Recipes", repeats.ToString());
            EditorGUILayout.LabelField(
                "Editor Tick Average",
                timingSampleCount > 0
                    ? $"{averageTickMilliseconds:0.000} ms"
                    : "Not sampled");

            if (comparison != null && comparison.ComparedCount > 0)
            {
                MessageType type = comparison.IsComplete
                    ? MessageType.Info
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(
                    comparison.IsComplete
                        ? $"Stable recreation: {comparison.StableCount}/" +
                          $"{comparison.ComparedCount} identities kept the " +
                          "same seed, ID, and appearance recipe."
                        : $"{comparison.ChangedCount} of " +
                          $"{comparison.ComparedCount} recreated people " +
                          "changed identity or appearance.",
                    type);
            }

            if (!string.IsNullOrWhiteSpace(canvas?.ErrorMessage))
            {
                EditorGUILayout.HelpBox(
                    canvas.ErrorMessage,
                    MessageType.Error);
            }

            EditorGUILayout.HelpBox(
                "Tick timing is an Editor preview diagnostic only. It is " +
                "not a player-build performance benchmark.",
                MessageType.None);
        }


        private void DrawPreview()
        {
            Rect previewRect = GUILayoutUtility.GetRect(
                100f,
                10000f,
                100f,
                10000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            canvas?.Draw(previewRect, showLabels);
        }


        private void CreatePopulation(
            bool compareWithPrevious)
        {
            List<NpcRuntimePopulationSnapshot> previous = null;

            if (compareWithPrevious && canvas?.Snapshots != null)
            {
                previous = new List<NpcRuntimePopulationSnapshot>(
                    canvas.Snapshots);
            }

            canvas ??= new NpcRuntimePopulationLabCanvas();
            List<NpcRuntimePopulationPlanEntry> plan =
                NpcRuntimePopulationLabModel.BuildPlan(
                    selectedDefinition != null
                        ? selectedDefinition.Role
                        : NpcCharacterRole.Customer,
                    baseSeed,
                    peopleCount);
            canvas.CreatePopulation(selectedDefinition, plan);
            comparison = compareWithPrevious
                ? NpcRuntimePopulationLabModel.Compare(
                    previous,
                    canvas.Snapshots)
                : null;
            ResetTiming();
            Repaint();
        }


        private NpcPopulationDefinition DrawDefinitionPopup(
            NpcPopulationDefinition current)
        {
            List<NpcPopulationDefinition> definitions = GetDefinitions();

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The Appearance Catalog has no Population Definitions.",
                    MessageType.Warning);
                return null;
            }

            int selectedIndex = Mathf.Max(0, definitions.IndexOf(current));
            string[] labels = new string[definitions.Count];

            for (int index = 0; index < definitions.Count; index++)
            {
                labels[index] =
                    $"{definitions[index].DisplayName} " +
                    $"({definitions[index].Role})";
            }

            selectedIndex = EditorGUILayout.Popup(
                "Population Definition",
                selectedIndex,
                labels);
            return definitions[selectedIndex];
        }


        private List<NpcPopulationDefinition> GetDefinitions()
        {
            List<NpcPopulationDefinition> result =
                new List<NpcPopulationDefinition>();

            if (catalog?.Definitions == null)
            {
                return result;
            }

            for (int index = 0; index < catalog.Definitions.Count; index++)
            {
                NpcPopulationDefinition definition =
                    catalog.Definitions[index];

                if (definition != null)
                {
                    result.Add(definition);
                }
            }

            return result;
        }


        private void FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:NpcAppearanceCatalog");
            catalog = null;

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                NpcAppearanceCatalog candidate =
                    AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(path);

                if (candidate != null)
                {
                    catalog = candidate;
                    break;
                }
            }
        }


        private void SelectFirstDefinition()
        {
            List<NpcPopulationDefinition> definitions = GetDefinitions();

            if (selectedDefinition != null
                && definitions.Contains(selectedDefinition))
            {
                return;
            }

            selectedDefinition = definitions.Count > 0
                ? definitions[0]
                : null;
        }


        private void DrawMissingCatalog()
        {
            EditorGUILayout.HelpBox(
                "No NpcAppearanceCatalog asset is available. Repair or " +
                "create the starter population content first.",
                MessageType.Warning);
        }


        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)Math.Min(
                0.1,
                Math.Max(0.0, now - lastEditorUpdateTime));
            lastEditorUpdateTime = now;

            if (canvas == null || !canvas.IsReady)
            {
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            canvas.Tick(deltaTime, playbackSpeed, movementEnabled);
            stopwatch.Stop();
            AddTimingSample(stopwatch.Elapsed.TotalMilliseconds);
            Repaint();
        }


        private void AddTimingSample(
            double milliseconds)
        {
            timingSampleCount = Math.Min(240, timingSampleCount + 1);
            double weight = timingSampleCount <= 1 ? 1.0 : 0.08;
            averageTickMilliseconds = timingSampleCount <= 1
                ? milliseconds
                : averageTickMilliseconds * (1.0 - weight)
                  + milliseconds * weight;
        }


        private void ResetTiming()
        {
            averageTickMilliseconds = 0.0;
            timingSampleCount = 0;
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
        }


        private void DisposeCanvas()
        {
            canvas?.Dispose();
            canvas = null;
        }
    }
}
