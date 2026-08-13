using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Generates a temporary group from the real population rules, previews
    /// the group together, and reports how much of each appearance pool was
    /// exercised. No person, scene, prefab, or generated selection is saved.
    /// </summary>
    public sealed class NpcPopulationAuditWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Population/Lineup & Audit";

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

        private NpcAppearanceCatalog catalog;
        private NpcPopulationDefinition selectedDefinition;
        private NpcPopulationAuditGenderFilter genderFilter =
            NpcPopulationAuditGenderFilter.PopulationMix;
        private NpcPopulationAuditMotion motion =
            NpcPopulationAuditMotion.WalkInPlace;
        private NpcFacing facing = NpcFacing.SouthEast;
        private int sampleCount = 12;
        private int baseSeed = 1000;
        private bool playing = true;
        private bool showLabels = true;
        private float playbackSpeed = 1f;
        private float elapsedTime;
        private double lastEditorUpdateTime;
        private Vector2 controlScroll;
        private Vector2 sampleScroll;
        private bool showSamples;
        private readonly bool[] categoryFoldouts =
            { true, true, true, true };

        private List<NpcPopulationAuditSample> samples =
            new List<NpcPopulationAuditSample>();
        private NpcPopulationAuditReport report;
        private NpcPopulationLineupCanvas lineupCanvas;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            NpcPopulationAuditWindow window =
                GetWindow<NpcPopulationAuditWindow>(
                    "Population Lineup & Audit");

            window.minSize = new Vector2(980f, 600f);
            window.Show();
        }


        private void OnEnable()
        {
            lineupCanvas = new NpcPopulationLineupCanvas();
            baseSeed = baseSeed == 0 ? 1000 : baseSeed;
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            FindCatalog();
            SelectFirstDefinition();
            RebuildLineup();
        }


        private void OnFocus()
        {
            if (catalog == null)
            {
                FindCatalog();
                SelectFirstDefinition();
                RebuildLineup();
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
                "Big Retail Population Lineup & Audit",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generate a temporary group with the same deterministic " +
                "population rules the simulation uses. Inspect variety, " +
                "walk animation, four-direction facing, and a small " +
                "preview-only test path. Nothing is saved or placed in a " +
                "scene.",
                MessageType.Info);

            if (catalog == null)
            {
                DrawMissingCatalog();
                return;
            }

            float panelWidth = Mathf.Clamp(
                position.width * 0.34f,
                360f,
                500f);

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
                    DrawMotionControls();
                    EditorGUILayout.Space(8f);
                    DrawAuditReport();
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope(
                           GUILayout.ExpandWidth(true),
                           GUILayout.ExpandHeight(true)))
                {
                    DrawLineupPreview();
                }
            }
        }


        private void DrawPopulationControls()
        {
            EditorGUILayout.LabelField(
                "1. Population Sample",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            NpcPopulationDefinition nextDefinition =
                DrawDefinitionPopup(selectedDefinition);
            int nextCount = EditorGUILayout.IntSlider(
                new GUIContent(
                    "People",
                    "Number of temporary people in this audit lineup."),
                sampleCount,
                4,
                24);
            NpcPopulationAuditGenderFilter nextFilter =
                (NpcPopulationAuditGenderFilter)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Gender Sample",
                        "Use the population's configured mix, or isolate one " +
                        "appearance pool for inspection."),
                    genderFilter);

            if (EditorGUI.EndChangeCheck())
            {
                selectedDefinition = nextDefinition;
                sampleCount = nextCount;
                genderFilter = nextFilter;
                RebuildLineup();
            }

            baseSeed = EditorGUILayout.IntField(
                new GUIContent(
                    "First Seed",
                    "The lineup uses this seed and each consecutive integer. " +
                    "The same block always produces the same people."),
                baseSeed);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Same Seeds"))
                {
                    RebuildLineup();
                }

                if (GUILayout.Button("New Seed Block"))
                {
                    baseSeed = Environment.TickCount;
                    RebuildLineup();
                }
            }

            EditorGUILayout.LabelField(
                $"Seeds {baseSeed} through " +
                $"{unchecked(baseSeed + sampleCount - 1)}",
                EditorStyles.centeredGreyMiniLabel);
        }


        private void DrawMotionControls()
        {
            EditorGUILayout.LabelField(
                "2. Movement Check",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            NpcPopulationAuditMotion nextMotion =
                (NpcPopulationAuditMotion)EditorGUILayout.EnumPopup(
                    "Preview",
                    motion);

            if (EditorGUI.EndChangeCheck())
            {
                motion = nextMotion;
                elapsedTime = 0f;
                EvaluateLineup();
            }

            if (motion == NpcPopulationAuditMotion.DiamondWalkTest)
            {
                EditorGUILayout.HelpBox(
                    "The diamond is a tiny preview-only route. It exercises " +
                    "South East, South West, North West, and North East " +
                    "without creating pathfinding rules or scene objects.",
                    MessageType.None);
            }
            else
            {
                int facingIndex = Array.IndexOf(Facings, facing);
                facingIndex = Mathf.Max(0, facingIndex);
                EditorGUI.BeginChangeCheck();
                facingIndex = GUILayout.SelectionGrid(
                    facingIndex,
                    FacingLabels,
                    2);

                if (EditorGUI.EndChangeCheck())
                {
                    facing = Facings[facingIndex];
                    RebuildCanvas();
                }
            }

            using (new EditorGUI.DisabledScope(
                       motion == NpcPopulationAuditMotion.BindPose))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(playing ? "Pause" : "Play"))
                    {
                        playing = !playing;
                    }

                    if (GUILayout.Button("Restart"))
                    {
                        elapsedTime = 0f;
                        EvaluateLineup();
                    }
                }

                playbackSpeed = EditorGUILayout.Slider(
                    "Playback Speed",
                    playbackSpeed,
                    0.25f,
                    2f);
            }

            showLabels = EditorGUILayout.Toggle(
                "Sample Labels",
                showLabels);

            if (GUILayout.Button("Reset Preview Zoom"))
            {
                lineupCanvas?.ResetZoom();
                Repaint();
            }
        }


        private void DrawAuditReport()
        {
            EditorGUILayout.LabelField(
                "3. Audit Results",
                EditorStyles.boldLabel);

            if (report == null)
            {
                EditorGUILayout.HelpBox(
                    "Build a lineup to see an audit.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Generated",
                    $"{report.ValidCount} / {report.RequestedCount}");
                EditorGUILayout.LabelField(
                    "Gender Mix",
                    $"{report.MenCount} men / " +
                    $"{report.WomenCount} women");
                EditorGUILayout.LabelField(
                    "Repeated Exact Recipes",
                    report.DuplicateRecipeCount.ToString());
            }

            for (int index = 0;
                 index < report.Warnings.Count;
                 index++)
            {
                EditorGUILayout.HelpBox(
                    report.Warnings[index],
                    MessageType.Warning);
            }

            for (int index = 0;
                 index < report.Categories.Count;
                 index++)
            {
                DrawCategory(index, report.Categories[index]);
            }

            showSamples = EditorGUILayout.Foldout(
                showSamples,
                "Generated Recipes",
                true);

            if (showSamples)
            {
                DrawSampleList();
            }
        }


        private void DrawCategory(
            int index,
            NpcPopulationAuditCategory category)
        {
            categoryFoldouts[index] = EditorGUILayout.Foldout(
                categoryFoldouts[index],
                $"{category.Label}: {category.ObservedCount} / " +
                $"{category.AllowedCount} seen",
                true);

            if (!categoryFoldouts[index])
            {
                return;
            }

            EditorGUI.indentLevel++;

            for (int frequencyIndex = 0;
                 frequencyIndex < category.Frequencies.Count;
                 frequencyIndex++)
            {
                NpcPopulationAuditFrequency frequency =
                    category.Frequencies[frequencyIndex];
                EditorGUILayout.LabelField(
                    frequency.DisplayName,
                    $"{frequency.Count} / {report.ValidCount}");
            }

            if (category.Frequencies.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "No valid assets observed.",
                    EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;
        }


        private void DrawSampleList()
        {
            sampleScroll = EditorGUILayout.BeginScrollView(
                sampleScroll,
                GUILayout.MinHeight(80f),
                GUILayout.MaxHeight(240f));

            for (int index = 0; index < samples.Count; index++)
            {
                NpcPopulationAuditSample sample = samples[index];

                if (sample == null || !sample.IsValid)
                {
                    string failure = sample?.FailureReason
                                     ?? "Unknown generation failure.";
                    EditorGUILayout.LabelField(
                        $"#{index + 1}  Seed {sample?.Seed ?? 0}",
                        failure,
                        EditorStyles.miniLabel);
                    continue;
                }

                NpcAppearanceSelection selection = sample.Selection;
                string gender = selection.Gender
                                == NpcPersonGender.Woman
                    ? "Woman"
                    : "Man";
                string recipe =
                    $"{selection.BodySilhouette.DisplayName} | " +
                    $"{selection.SkinPalette.DisplayName} | " +
                    $"{selection.OutfitSet.DisplayName} | " +
                    $"{selection.HairSet.DisplayName}";

                EditorGUILayout.LabelField(
                    $"#{index + 1}  Seed {sample.Seed}  {gender}",
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    recipe,
                    CreateWrappedMiniLabel());
            }

            EditorGUILayout.EndScrollView();
        }


        private void DrawLineupPreview()
        {
            string title = selectedDefinition != null
                ? $"{selectedDefinition.DisplayName} Lineup"
                : "Population Lineup";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(
                320f,
                10000f,
                360f,
                10000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            lineupCanvas?.Draw(previewRect, showLabels);
        }


        private void DrawMissingCatalog()
        {
            EditorGUILayout.HelpBox(
                "No NpcAppearanceCatalog asset could be found. Create or " +
                "repair the starter population content first.",
                MessageType.Error);

            if (GUILayout.Button("Find Catalog Again"))
            {
                FindCatalog();
                SelectFirstDefinition();
                RebuildLineup();
            }
        }


        private NpcPopulationDefinition DrawDefinitionPopup(
            NpcPopulationDefinition current)
        {
            List<NpcPopulationDefinition> definitions =
                GetDefinitions();

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The Appearance Catalog has no Population Definitions.",
                    MessageType.Error);
                return null;
            }

            string[] labels = new string[definitions.Count];
            int selectedIndex = 0;

            for (int index = 0; index < definitions.Count; index++)
            {
                labels[index] = definitions[index].DisplayName;

                if (definitions[index] == current)
                {
                    selectedIndex = index;
                }
            }

            selectedIndex = EditorGUILayout.Popup(
                "Population",
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

            for (int index = 0;
                 index < catalog.Definitions.Count;
                 index++)
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
                    AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(
                        path);

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


        private void RebuildLineup()
        {
            sampleCount = Mathf.Clamp(sampleCount, 4, 24);
            samples = NpcPopulationAuditSampler.Generate(
                selectedDefinition,
                baseSeed,
                sampleCount,
                genderFilter);
            report = NpcPopulationAuditReport.Create(
                selectedDefinition,
                genderFilter,
                samples);
            elapsedTime = 0f;
            RebuildCanvas();
        }


        private void RebuildCanvas()
        {
            lineupCanvas ??= new NpcPopulationLineupCanvas();
            lineupCanvas.SetSamples(samples, facing);
            EvaluateLineup();
            Repaint();
        }


        private void EvaluateLineup()
        {
            lineupCanvas?.Evaluate(elapsedTime, motion, facing);
        }


        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)Math.Min(
                0.1,
                Math.Max(0.0, now - lastEditorUpdateTime));
            lastEditorUpdateTime = now;

            if (playing
                && motion != NpcPopulationAuditMotion.BindPose)
            {
                elapsedTime += deltaTime * playbackSpeed;
                EvaluateLineup();
                Repaint();
            }
        }


        private void DisposeCanvas()
        {
            lineupCanvas?.Dispose();
            lineupCanvas = null;
        }


        private static GUIStyle CreateWrappedMiniLabel()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }
    }
}
