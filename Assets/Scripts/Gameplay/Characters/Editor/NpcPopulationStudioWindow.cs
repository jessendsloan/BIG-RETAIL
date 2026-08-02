using System;
using System.Collections.Generic;
using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    public sealed class NpcPopulationStudioWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Characters/Population Studio/Open Population Studio";

        private string personName = "Saved Appearance";
        private int seed = 1001;
        private bool lockBody;
        private bool lockSkin;
        private bool lockOutfit;
        private bool lockHair;
        private bool showSetupTools;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        private NpcAppearanceCatalog catalog;
        private NpcPopulationDefinition selectedDefinition;
        private NpcBodySilhouette bodySilhouette;
        private NpcSkinPalette skinPalette;
        private NpcOutfitSet outfitSet;
        private NpcHairSet hairSet;
        private NpcAppearanceProfile selectedProfile;
        private NpcAppearanceProfile previewProfile;
        private NpcCutoutRig previewRig;
        private Vector2 scrollPosition;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<NpcPopulationStudioWindow>(
                "Population Studio");
        }


        private void OnEnable()
        {
            previewProfile =
                CreateInstance<NpcAppearanceProfile>();
            previewProfile.hideFlags = HideFlags.HideAndDontSave;

            FindAppearanceCatalog();
            TryUseSelection();

            if (selectedDefinition != null
                && !TryValidateCurrentSelection(out _))
            {
                RandomizeSelection();
            }
        }


        private void OnDisable()
        {
            if (previewRig != null)
            {
                previewRig.ClearAppearancePreview();
            }

            if (previewProfile != null)
            {
                DestroyImmediate(previewProfile);
            }
        }


        private void OnSelectionChange()
        {
            TryUseSelection();
            Repaint();
        }


        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Big Retail Population Studio",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Define what each population may look like, then preview " +
                "repeatable samples. Population definitions control what " +
                "is allowed; the simulation generates the people.",
                MessageType.Info);

            DrawDefinitionSection();
            EditorGUILayout.Space(8f);
            DrawComposeSection();
            EditorGUILayout.Space(8f);
            DrawProfileSection();
            EditorGUILayout.Space(8f);
            DrawPreviewSection();
            EditorGUILayout.Space(8f);
            DrawSetupSection();

            EditorGUILayout.EndScrollView();
        }


        private void DrawDefinitionSection()
        {
            EditorGUILayout.LabelField(
                "1. Population Rules",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            catalog =
                (NpcAppearanceCatalog)EditorGUILayout.ObjectField(
                    "Appearance Catalog",
                    catalog,
                    typeof(NpcAppearanceCatalog),
                    false);

            if (EditorGUI.EndChangeCheck())
            {
                selectedDefinition = GetFirstDefinition(catalog);
                statusMessage = string.Empty;
            }

            if (catalog == null)
            {
                EditorGUILayout.HelpBox(
                    "No Appearance Catalog was found. Open Starter " +
                    "Content below and create it once.",
                    MessageType.Warning);
                return;
            }

            IReadOnlyList<NpcPopulationDefinition> definitions =
                catalog.Definitions;

            if (definitions == null || definitions.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This catalog has no population definitions.",
                    MessageType.Error);
                return;
            }

            string[] names = new string[definitions.Count];
            int selectedIndex = 0;

            for (int index = 0; index < definitions.Count; index++)
            {
                NpcPopulationDefinition definition = definitions[index];
                names[index] = definition != null
                    ? definition.DisplayName
                    : "Missing Definition";

                if (definition == selectedDefinition)
                {
                    selectedIndex = index;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(
                "Person Type",
                selectedIndex,
                names);

            if (EditorGUI.EndChangeCheck())
            {
                selectedDefinition = definitions[selectedIndex];
                ReleaseLocksNotAllowedByDefinition();
                RandomizeSelection();
            }

            if (selectedDefinition != null)
            {
                EditorGUILayout.LabelField(
                    selectedDefinition.Role == NpcCharacterRole.Employee
                        ? "Only approved employee uniforms can be chosen."
                        : "Only customer-appropriate outfits can be chosen.",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }


        private void DrawComposeSection()
        {
            EditorGUILayout.LabelField(
                "2. Generate Sample",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            seed = EditorGUILayout.IntField("Seed", seed);

            if (GUILayout.Button("Next", GUILayout.Width(58f)))
            {
                seed = unchecked(seed + 1);
                RandomizeSelection();
            }

            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(
                       selectedDefinition == null))
            {
                if (GUILayout.Button("Randomize Unlocked Choices"))
                {
                    RandomizeSelection();
                }
            }

            EditorGUILayout.Space(4f);

            if (selectedDefinition == null)
            {
                EditorGUILayout.HelpBox(
                    "Choose a person type before generating a sample.",
                    MessageType.Warning);
                return;
            }

            DrawBodyChoice();
            DrawSkinChoice();
            DrawOutfitChoice();
            DrawHairChoice();

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                EditorGUILayout.HelpBox(
                    statusMessage,
                    statusType);
            }
            else if (TryValidateCurrentSelection(
                         out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Valid recipe for " +
                    selectedDefinition.DisplayName + ".",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }


        private void DrawProfileSection()
        {
            EditorGUILayout.LabelField(
                "3. Optional Saved Appearance",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedProfile =
                (NpcAppearanceProfile)EditorGUILayout.ObjectField(
                    "Saved Appearance",
                    selectedProfile,
                    typeof(NpcAppearanceProfile),
                    false);

            if (EditorGUI.EndChangeCheck()
                && selectedProfile != null)
            {
                LoadRecipe(selectedProfile);
                PreviewCurrentRecipe();
            }

            personName = EditorGUILayout.TextField(
                "Label",
                personName);

            using (new EditorGUI.DisabledScope(
                       !TryValidateCurrentSelection(out _)))
            {
                if (GUILayout.Button("Save Exact Appearance"))
                {
                    SaveNewProfile();
                }

                using (new EditorGUI.DisabledScope(
                           selectedProfile == null))
                {
                    if (GUILayout.Button("Update Exact Appearance"))
                    {
                        UpdateSelectedProfile();
                    }
                }
            }

            EditorGUILayout.LabelField(
                "Optional: preserve an exact appearance for a default, " +
                "story character, or repeatable test. Normal customers " +
                "and employees are generated from population definitions.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField(
                "4. Preview on Shared Rig",
                EditorStyles.boldLabel);

            previewRig =
                (NpcCutoutRig)EditorGUILayout.ObjectField(
                    "Rig",
                    previewRig,
                    typeof(NpcCutoutRig),
                    true);

            using (new EditorGUI.DisabledScope(
                       previewRig == null
                       || !TryValidateCurrentSelection(out _)))
            {
                if (GUILayout.Button("Preview Current Recipe"))
                {
                    PreviewCurrentRecipe();
                }
            }

            using (new EditorGUI.DisabledScope(
                       previewRig == null
                       || selectedProfile == null))
            {
                if (GUILayout.Button("Assign Saved Person to Rig"))
                {
                    ApplySavedProfileToRig();
                }
            }

            using (new EditorGUI.DisabledScope(previewRig == null))
            {
                EditorGUILayout.LabelField("Facing");
                EditorGUILayout.BeginHorizontal();

                DrawFacingButton("SE", NpcFacing.SouthEast);
                DrawFacingButton("SW", NpcFacing.SouthWest);
                DrawFacingButton("NE", NpcFacing.NorthEast);
                DrawFacingButton("NW", NpcFacing.NorthWest);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox(
                "Preview is temporary. Assign Saved Appearance is the " +
                "intentional, persistent change. The Animator, pathing, " +
                "and other gameplay components are never replaced.",
                MessageType.None);
        }


        private void DrawSetupSection()
        {
            showSetupTools = EditorGUILayout.Foldout(
                showSetupTools,
                "Starter Content & Asset Authoring",
                true);

            if (!showSetupTools)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Use Repair Starter Content after adding or restoring " +
                "the base rig. For new art choices, duplicate a working " +
                "asset and edit the copy in its Inspector.",
                MessageType.None);

            if (GUILayout.Button("Repair / Refresh Starter Content"))
            {
                NpcPopulationStarterFactory
                    .CreateOrUpdateStarterCatalog();
                FindAppearanceCatalog();
                selectedDefinition ??= GetFirstDefinition(catalog);
                RandomizeSelection();
                statusMessage = "Starter content refreshed.";
                statusType = MessageType.Info;
            }

            EditorGUILayout.BeginHorizontal();
            DrawDuplicateButton(bodySilhouette, "Body");
            DrawDuplicateButton(skinPalette, "Skin");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawDuplicateButton(outfitSet, "Outfit");
            DrawDuplicateButton(hairSet, "Hair");
            EditorGUILayout.EndHorizontal();
        }


        private void DrawBodyChoice()
        {
            IReadOnlyList<NpcWeightedBodyChoice> choices =
                selectedDefinition.Bodies;
            string[] labels = new string[choices.Count];
            int current = 0;

            for (int index = 0; index < choices.Count; index++)
            {
                labels[index] = choices[index].Asset.DisplayName;

                if (choices[index].Asset == bodySilhouette)
                {
                    current = index;
                }
            }

            EditorGUILayout.BeginHorizontal();
            int next = EditorGUILayout.Popup("Body", current, labels);
            lockBody = GUILayout.Toggle(
                lockBody,
                "Lock",
                GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            if (bodySilhouette != choices[next].Asset)
            {
                bodySilhouette = choices[next].Asset;
                PreviewCurrentRecipe();
            }
        }


        private void DrawSkinChoice()
        {
            IReadOnlyList<NpcWeightedSkinChoice> choices =
                selectedDefinition.Skins;
            string[] labels = new string[choices.Count];
            int current = 0;

            for (int index = 0; index < choices.Count; index++)
            {
                labels[index] = choices[index].Asset.DisplayName;

                if (choices[index].Asset == skinPalette)
                {
                    current = index;
                }
            }

            EditorGUILayout.BeginHorizontal();
            int next = EditorGUILayout.Popup("Skin", current, labels);
            lockSkin = GUILayout.Toggle(
                lockSkin,
                "Lock",
                GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            if (skinPalette != choices[next].Asset)
            {
                skinPalette = choices[next].Asset;
                PreviewCurrentRecipe();
            }
        }


        private void DrawOutfitChoice()
        {
            IReadOnlyList<NpcWeightedOutfitChoice> choices =
                selectedDefinition.Outfits;
            string[] labels = new string[choices.Count];
            int current = 0;

            for (int index = 0; index < choices.Count; index++)
            {
                labels[index] = choices[index].Asset.DisplayName;

                if (choices[index].Asset == outfitSet)
                {
                    current = index;
                }
            }

            EditorGUILayout.BeginHorizontal();
            int next = EditorGUILayout.Popup("Outfit", current, labels);
            lockOutfit = GUILayout.Toggle(
                lockOutfit,
                "Lock",
                GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            if (outfitSet != choices[next].Asset)
            {
                outfitSet = choices[next].Asset;
                PreviewCurrentRecipe();
            }
        }


        private void DrawHairChoice()
        {
            IReadOnlyList<NpcWeightedHairChoice> choices =
                selectedDefinition.Hair;
            string[] labels = new string[choices.Count];
            int current = 0;

            for (int index = 0; index < choices.Count; index++)
            {
                labels[index] = choices[index].Asset.DisplayName;

                if (choices[index].Asset == hairSet)
                {
                    current = index;
                }
            }

            EditorGUILayout.BeginHorizontal();
            int next = EditorGUILayout.Popup("Hair", current, labels);
            lockHair = GUILayout.Toggle(
                lockHair,
                "Lock",
                GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            if (hairSet != choices[next].Asset)
            {
                hairSet = choices[next].Asset;
                PreviewCurrentRecipe();
            }
        }


        private void RandomizeSelection()
        {
            NpcAppearanceSelection current = CreateSelection();
            NpcAppearanceLocks locks = new NpcAppearanceLocks();
            locks.Configure(
                lockBody,
                lockSkin,
                lockOutfit,
                lockHair);

            if (!NpcAppearanceGenerator.TryGenerate(
                    selectedDefinition,
                    seed,
                    current,
                    locks,
                    out NpcAppearanceSelection generated,
                    out string reason))
            {
                statusMessage = reason;
                statusType = MessageType.Error;
                return;
            }

            bodySilhouette = generated.BodySilhouette;
            skinPalette = generated.SkinPalette;
            outfitSet = generated.OutfitSet;
            hairSet = generated.HairSet;
            statusMessage =
                $"Seed {seed} generated a valid " +
                $"{selectedDefinition.DisplayName}.";
            statusType = MessageType.Info;
            PreviewCurrentRecipe();
        }


        private void ReleaseLocksNotAllowedByDefinition()
        {
            if (selectedDefinition == null)
            {
                return;
            }

            lockBody &= selectedDefinition.Allows(bodySilhouette);
            lockSkin &= selectedDefinition.Allows(skinPalette);
            lockOutfit &= selectedDefinition.Allows(outfitSet);
            lockHair &= selectedDefinition.Allows(hairSet);
        }


        private void PreviewCurrentRecipe()
        {
            if (previewRig == null
                || previewProfile == null
                || !TryValidateCurrentSelection(out _))
            {
                return;
            }

            previewProfile.Configure(
                "Population Studio Preview",
                CreateSelection());
            previewRig.SetAppearancePreview(previewProfile);
            SceneView.RepaintAll();
        }


        private void DrawFacingButton(
            string label,
            NpcFacing facing)
        {
            if (!GUILayout.Button(label))
            {
                return;
            }

            previewRig.SetFacing(facing);
            SceneView.RepaintAll();
        }


        private void ApplySavedProfileToRig()
        {
            Undo.RecordObject(
                previewRig,
                "Assign Saved NPC Appearance");

            previewRig.SetAppearanceProfile(selectedProfile);
            MarkRigDirty(previewRig);
            SceneView.RepaintAll();
        }


        private void UpdateSelectedProfile()
        {
            Undo.RecordObject(
                selectedProfile,
                "Update NPC Appearance Profile");

            selectedProfile.Configure(
                personName,
                CreateSelection());

            EditorUtility.SetDirty(selectedProfile);
            AssetDatabase.SaveAssets();
            PreviewCurrentRecipe();
        }


        private void SaveNewProfile()
        {
            string defaultName =
                string.IsNullOrWhiteSpace(personName)
                    ? "PersonAppearance"
                    : personName.Replace(" ", string.Empty)
                        + "Appearance";

            string assetPath =
                EditorUtility.SaveFilePanelInProject(
                    "Save Person Appearance",
                    defaultName,
                    "asset",
                    "Choose where to save the exact appearance recipe.",
                    "Assets/Art/Characters/Appearance/Defaults");

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            NpcAppearanceProfile profile =
                CreateInstance<NpcAppearanceProfile>();

            profile.Configure(
                personName,
                CreateSelection());

            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();

            selectedProfile = profile;
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }


        private void LoadRecipe(
            NpcAppearanceProfile profile)
        {
            personName = profile.DisplayName;
            bodySilhouette = profile.BodySilhouette;
            skinPalette = profile.SkinPalette;
            outfitSet = profile.OutfitSet;
            hairSet = profile.HairSet;
            SelectCompatibleDefinition();
        }


        private void SelectCompatibleDefinition()
        {
            if (catalog?.Definitions == null)
            {
                return;
            }

            for (int index = 0;
                 index < catalog.Definitions.Count;
                 index++)
            {
                NpcPopulationDefinition candidate =
                    catalog.Definitions[index];

                if (candidate != null
                    && candidate.Allows(bodySilhouette)
                    && candidate.Allows(skinPalette)
                    && candidate.Allows(outfitSet)
                    && candidate.Allows(hairSet))
                {
                    selectedDefinition = candidate;
                    return;
                }
            }
        }


        private NpcAppearanceSelection CreateSelection()
        {
            return new NpcAppearanceSelection(
                bodySilhouette,
                skinPalette,
                outfitSet,
                hairSet);
        }


        private bool TryValidateCurrentSelection(
            out string reason)
        {
            NpcAppearanceSelection selection = CreateSelection();

            if (!selection.TryValidate(out reason))
            {
                return false;
            }

            if (selectedDefinition == null)
            {
                reason = "No population definition is selected.";
                return false;
            }

            if (!selectedDefinition.Allows(bodySilhouette)
                || !selectedDefinition.Allows(skinPalette)
                || !selectedDefinition.Allows(outfitSet)
                || !selectedDefinition.Allows(hairSet))
            {
                reason =
                    "One or more choices are not allowed by the " +
                    "selected person type.";
                return false;
            }

            reason = string.Empty;
            return true;
        }


        private void TryUseSelection()
        {
            if (Selection.activeObject
                is NpcAppearanceProfile profile)
            {
                selectedProfile = profile;
                LoadRecipe(profile);
                return;
            }

            if (Selection.activeObject
                is NpcAppearanceCatalog selectedCatalog)
            {
                catalog = selectedCatalog;
                selectedDefinition = GetFirstDefinition(catalog);
                return;
            }

            if (Selection.activeObject
                is NpcPopulationDefinition definition)
            {
                selectedDefinition = definition;
                return;
            }

            if (Selection.activeGameObject == null)
            {
                return;
            }

            NpcCutoutRig selectedRig =
                Selection.activeGameObject
                    .GetComponentInParent<NpcCutoutRig>();

            if (selectedRig == null)
            {
                return;
            }

            previewRig = selectedRig;

            if (selectedRig.AppearanceProfile != null)
            {
                selectedProfile = selectedRig.AppearanceProfile;
                LoadRecipe(selectedProfile);
            }
        }


        private void FindAppearanceCatalog()
        {
            if (catalog != null)
            {
                return;
            }

            string[] candidates =
                AssetDatabase.FindAssets("t:NpcAppearanceCatalog");

            if (candidates.Length == 0)
            {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(candidates[0]);
            catalog =
                AssetDatabase.LoadAssetAtPath<NpcAppearanceCatalog>(path);
            selectedDefinition = GetFirstDefinition(catalog);
        }


        private static NpcPopulationDefinition GetFirstDefinition(
            NpcAppearanceCatalog source)
        {
            return source != null
                   && source.Definitions != null
                   && source.Definitions.Count > 0
                ? source.Definitions[0]
                : null;
        }


        private static void MarkRigDirty(
            NpcCutoutRig rig)
        {
            EditorUtility.SetDirty(rig);

            if (PrefabUtility.IsPartOfPrefabInstance(rig))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    rig);
            }
        }


        private void DrawDuplicateButton<T>(
            T source,
            string kind)
            where T : ScriptableObject
        {
            using (new EditorGUI.DisabledScope(source == null))
            {
                if (!GUILayout.Button("Duplicate " + kind))
                {
                    return;
                }

                T copy = DuplicateAsset(source, kind);

                if (copy is NpcBodySilhouette body)
                {
                    bodySilhouette = body;
                }
                else if (copy is NpcSkinPalette skin)
                {
                    skinPalette = skin;
                }
                else if (copy is NpcOutfitSet outfit)
                {
                    outfitSet = outfit;
                }
                else if (copy is NpcHairSet hair)
                {
                    hairSet = hair;
                }
            }
        }


        private static T DuplicateAsset<T>(
            T source,
            string kind)
            where T : ScriptableObject
        {
            if (source == null)
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string directory = string.IsNullOrWhiteSpace(sourcePath)
                ? "Assets/Art/Characters/Appearance"
                : System.IO.Path.GetDirectoryName(sourcePath)
                    ?.Replace('\\', '/');

            string destination = EditorUtility.SaveFilePanelInProject(
                "Duplicate " + kind,
                source.name + " Copy",
                "asset",
                "Create a new " + kind.ToLowerInvariant() +
                " choice from this working definition.",
                directory);

            if (string.IsNullOrWhiteSpace(destination))
            {
                return source;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destination))
            {
                Debug.LogError(
                    $"Could not duplicate '{sourcePath}' to " +
                    $"'{destination}'.");
                return source;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            T copy = AssetDatabase.LoadAssetAtPath<T>(destination);
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
            return copy;
        }
    }
}
